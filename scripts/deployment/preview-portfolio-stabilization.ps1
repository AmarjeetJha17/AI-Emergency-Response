#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('api', 'web')][string]$Component,
    [Parameter(Mandatory)][ValidatePattern('^acerportfoliobxwiyolb\.azurecr\.io/emergencyresponse-(api|web)@sha256:[a-f0-9]{64}$')][string]$Image,
    [Parameter(Mandatory)][ValidatePattern('^[a-z][a-z0-9-]{0,39}$')][string]$RevisionSuffix
)

# Preview only: no PUT/PATCH, deployment helper, secret-list operation or file output.
# A full desired-state projection is needed by What-If. Actual rollout must update
# revision-scoped settings only; NEVER submit this projection as a deployment.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$DebugPreference = 'SilentlyContinue'
$VerbosePreference = 'SilentlyContinue'
$subscription = '0a05d5ff-7fef-4886-8e72-22a6cafd5567'
$group = 'rg-er-portfolio-demo-centralus'
$origin = 'https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io'
$appName = "ca-er-$Component-portfolio"
$id = "/subscriptions/$subscription/resourceGroups/$group/providers/Microsoft.App/containerApps/$appName"
$root = "https://management.azure.com/subscriptions/$subscription/"
$stage = 'initialization'
$client = $null
$token = $null

function Invoke-PreviewRequest([string]$Method, [string]$Uri, [object]$Body = $null) {
    $address = [uri]$Uri
    if ($address.Scheme -cne 'https' -or $address.Host -cne 'management.azure.com' -or
        -not $Uri.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -or
        $address.UserInfo -or $address.Fragment -or $address.Query -notmatch 'api-version=') {
        throw 'Unsafe ARM address; preview stopped.'
    }
    if ($Method -notin @('GET', 'POST') -or ($Method -eq 'POST' -and $address.AbsolutePath -notmatch '/whatIf$')) {
        throw 'Only GET and What-If POST are supported.'
    }
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($Method), $address)
    $response = $null
    try {
        $request.Headers.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $token)
        if ($null -ne $Body) {
            $request.Content = [Net.Http.StringContent]::new(($Body | ConvertTo-Json -Depth 100 -Compress), [Text.Encoding]::UTF8, 'application/json')
        }
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $status = [int]$response.StatusCode
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($status -notin @(200, 202)) { Write-Host "ARM HTTP $status; details withheld."; throw 'ARM request failed.' }
        $parsed = if ([string]::IsNullOrWhiteSpace($content)) { $null } else { $content | ConvertFrom-Json -AsHashtable }
        return @{ Status = $status; Body = $parsed; Location = [string]$response.Headers.Location }
    } finally {
        if ($response) { $response.Dispose() }
        $request.Dispose()
        $content = $null
    }
}

function Get-DeltaPaths($Deltas, [string]$Parent = '') {
    foreach ($delta in $Deltas) {
        $child = [string]$delta.path
        $path = if ($child.StartsWith('properties.')) { $child } elseif ($child -match '^\d+$') { "${Parent}[$child]" } elseif ($Parent) { "$Parent.$child" } else { $child }
        if ($path -notmatch "^[A-Za-z0-9_./\[\]'-]{1,500}$" -or $delta.propertyChangeType -notin @('Create','Delete','Modify','Array','NoEffect')) {
            throw 'Unrecognized delta metadata; preview stopped.'
        }
        [pscustomobject]@{ Path = $path; Kind = [string]$delta.propertyChangeType }
        if ($delta.ContainsKey('children') -and $delta.children) { Get-DeltaPaths $delta.children $path }
    }
}

try {
    if ($Image -notmatch "/emergencyresponse-$Component@") { throw 'Component/image mismatch.' }
    $stage = 'Azure context verification'
    $contextText = & az account show --only-show-errors --output json 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'Azure context unavailable.' }
    $context = ($contextText -join "`n") | ConvertFrom-Json
    if ($context.id -ne $subscription -or $context.name -ne 'Azure subscription 1' -or $context.state -ne 'Enabled') { throw 'Azure context mismatch.' }
    $tokenText = & az account get-access-token --subscription $subscription --resource https://management.azure.com/ --query accessToken --output tsv --only-show-errors 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $tokenText) { throw 'ARM authentication unavailable.' }
    $token = ($tokenText -join '').Trim()
    $tokenText = $null
    $handler = [Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(90)
    $stage = 'live configuration verification'
    $live = (Invoke-PreviewRequest GET "https://management.azure.com${id}?api-version=2024-03-01").Body
    $p = $live.properties
    if (($live.location -replace '\s','') -ine 'centralus' -or $p.configuration.activeRevisionsMode -ne 'Single' -or
        $p.latestRevisionName -ne $p.latestReadyRevisionName -or -not $p.latestReadyRevisionName) { throw 'Live region/revision baseline mismatch.' }
    if ($p.template.containers.Count -ne 1) { throw 'Unexpected container count.' }
    $stage = 'native-secret metadata verification'
    foreach ($secret in @($p.configuration['secrets'])) {
        if ($null -eq $secret) { continue }
        if ($secret.ContainsKey('value') -and $secret.value) { throw 'Unexpected secret material in control-plane response.' }
        [void]$secret.Remove('value')
    }
    # Names without values are rejected by ACA as ContainerAppSecretInvalid.
    # Omit this app-scoped property from the preview projection as from the
    # actual update. If What-If predicts a secret deletion, the gate MUST close.
    [void]$p.configuration.Remove('secrets')
    $stage = 'revision template preparation'
    $template = ($p.template | ConvertTo-Json -Depth 100 -Compress) | ConvertFrom-Json -AsHashtable
    $container = $template.containers[0]
    $beforeEnv = $container.env
    $approvedApiEnvIndexes = [System.Collections.Generic.HashSet[int]]::new()
    if ($Component -eq 'api') {
        if (@($beforeEnv | Where-Object { $_.name -eq 'AI__Enabled' -and $_.value -eq 'false' }).Count -ne 1 -or
            @($beforeEnv | Where-Object { $_.name -match '^(AzureOpenAI|Foundry|AI__)' -and $_.name -ne 'AI__Enabled' }).Count) { throw 'AI-disabled baseline mismatch.' }
        if (@($beforeEnv | Where-Object { $_.name -eq 'Cors__AllowedOrigins__0' }).Count -gt 0) { throw 'CORS baseline has changed; review it before preview.' }
        $approvedApiSettings = @(
            @{ name='Cors__AllowedOrigins__0'; value=$origin },
            @{ name='Forecasting__BackgroundWorkerEnabled'; value='false' },
            @{ name='DataRetention__BackgroundWorkerEnabled'; value='false' },
            @{ name='AzureMaps__Enabled'; value='false' }
        )
        foreach ($setting in $approvedApiSettings) {
            $matches = @($container.env | Where-Object { $_.name -eq $setting.name })
            if ($matches.Count -gt 1) { throw 'Duplicate portfolio API environment setting.' }
            if ($matches.Count -eq 1) {
                $index = [Array]::IndexOf($container.env, $matches[0])
                $container.env[$index].value = $setting.value
            } else {
                $index = $container.env.Count
                $container.env += $setting
            }
            [void]$approvedApiEnvIndexes.Add($index)
        }
    }
    $container.image = $Image
    $template.revisionSuffix = $RevisionSuffix
    # Copy deployable top-level properties, retaining configuration metadata in
    # memory only. Runtime response properties are deliberately not deployable.
    $stage = 'desired-state projection preparation'
    $properties = @{}
    foreach ($key in @('environmentId','managedEnvironmentId','configuration','workloadProfileName')) {
        if ($p.ContainsKey($key) -and $null -ne $p[$key]) { $properties[$key] = $p[$key] }
    }
    $properties.template = $template
    $resource = @{ type='Microsoft.App/containerApps'; apiVersion='2024-03-01'; name=$appName; location=$live.location; properties=$properties; identity=$live['identity'] }
    if ($live.ContainsKey('tags')) { $resource.tags = $live['tags'] }
    $arm = @{ '$schema'='https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#'; contentVersion='1.0.0.0'; resources=@($resource) }
    $body = @{ properties=@{ mode='Incremental'; template=$arm; parameters=@{}; whatIfSettings=@{resultFormat='FullResourcePayloads'} } }
    $stage = 'ARM What-If'
    $name = "stabilization-preview-$Component-$([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))"
    $uri = "https://management.azure.com/subscriptions/$subscription/resourcegroups/$group/providers/Microsoft.Resources/deployments/${name}/whatIf?api-version=2025-04-01"
    $stage = 'submitting ARM What-If'
    $reply = Invoke-PreviewRequest POST $uri $body
    $deadline = [DateTime]::UtcNow.AddMinutes(8)
    while ($reply.Status -eq 202 -or ($reply.Body -and $reply.Body.status -in @('Running','Accepted','InProgress'))) {
        $stage = 'polling ARM What-If'
        if ([DateTime]::UtcNow -ge $deadline -or -not $reply.Location) { throw 'Missing polling address or preview timed out.' }
        $poll = $reply.Location
        Start-Sleep -Seconds 5
        $reply = Invoke-PreviewRequest GET $poll
        if (-not $reply.Location) { $reply.Location = $poll }
    }
    $stage = 'validating completed ARM What-If'
    $result = $reply.Body
    if (-not $result -or $result.status -ne 'Succeeded' -or -not $result.properties.ContainsKey('changes') -or $result.properties.changes -isnot [array]) {
        $safeStatus = if ($result -and $result.status -in @('Succeeded','Failed','Canceled','Running','Accepted','InProgress')) { $result.status } else { 'Unrecognized' }
        Write-Host "What-If completion status: $safeStatus."
        $queue = [Collections.Generic.Queue[object]]::new()
        $queue.Enqueue($result)
        $visited = 0
        while ($queue.Count -and $visited++ -lt 1000) {
            $node = $queue.Dequeue()
            if ($node -is [Collections.IDictionary]) {
                if ($node['code'] -in @('InvalidTemplate','InvalidTemplateDeployment','AuthorizationFailed','ResourceNotFound','InvalidParameter','InvalidRequestContent','BadRequest','InvalidResourceLocation','InvalidRequest','ContainerAppSecretInvalid','InvalidIdentityValues','MissingRequiredParameter','InvalidApiVersionParameter','InvalidResourceType','DeploymentFailed')) { Write-Host ("Recognized Azure code: {0}" -f $node['code']) }
                foreach ($item in $node.Values) { if ($null -ne $item) { $queue.Enqueue($item) } }
            } elseif ($node -is [array]) { foreach ($item in $node) { $queue.Enqueue($item) } }
        }
        throw 'Incomplete What-If response.'
    }
    $actionable = 0
    $unexpected = 0
    foreach ($change in $result.properties.changes) {
        if ([string]$change.resourceId -notmatch '^/subscriptions/[a-f0-9-]+/resourceGroups/[A-Za-z0-9_.-]+/providers/[A-Za-z0-9_./-]+$') { throw 'Invalid resource metadata.' }
        if ($change.changeType -notin @('Ignore','NoChange','Modify','Create','Delete','Deploy','Unsupported')) { throw 'Unknown resource change type.' }
        Write-Host ("{0} {1}" -f $change.changeType, $change.resourceId)
        if ($change.changeType -in @('Ignore','NoChange')) { continue }
        $actionable++
        if ($change.resourceId -ine $id -or $change.changeType -ne 'Modify' -or -not $change.delta) { $unexpected++; continue }
        foreach ($delta in @(Get-DeltaPaths $change.delta)) {
            Write-Host ("  {0} [{1}]" -f $delta.Path, $delta.Kind)
            $allowed = $delta.Path -in @('properties.template.revisionSuffix','properties.template.containers','properties.template.containers[0]','properties.template.containers[0].image')
            if ($Component -eq 'api') {
                if ($delta.Path -eq 'properties.template.containers[0].env' -and $delta.Kind -eq 'Array') { $allowed = $true }
                if ($delta.Path -match '^properties\.template\.containers\[0\]\.env\[(\d+)\](?:\.(name|value))?$') {
                    $envIndex = [int]$Matches[1]
                    $propertyName = $Matches[2]
                    if ($approvedApiEnvIndexes.Contains($envIndex) -and
                        $delta.Kind -in @('Create','Modify') -and
                        ($null -eq $propertyName -or $propertyName -in @('name','value'))) {
                        $allowed = $true
                    }
                }
            }
            # Exact response-only field, absent from 2024-03-01 PATCH/PUT request contract.
            if ($delta.Path -eq 'properties.runningStatus' -and $delta.Kind -eq 'Delete') { $allowed = $true }
            if (-not $allowed) { $unexpected++ }
        }
    }
    if ($actionable -ne 1 -or $unexpected) { throw 'Unexpected actionable resources/property paths; gate closed.' }
    Write-Host 'PREVIEW PASSED: one intended app modification. No deployment submitted.'
    Write-Host 'Projection preserves live configuration; actual rollout must omit app-scoped configuration and secrets entirely.'
} catch {
    # Fixed stage and exception type only; never emit raw HTTP/exception content.
    Write-Host "Preview stopped at stage: $stage. ExceptionType=$($_.Exception.GetType().Name). Details withheld."
    throw 'Stabilization preview failed closed. No deployment submitted.'
} finally {
    $token = $null; $body = $null; $live = $null; $result = $null; $reply = $null
    if ($client) { $client.Dispose() }
}
