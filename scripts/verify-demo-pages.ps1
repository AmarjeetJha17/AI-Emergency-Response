param(
    [string] $WebUrl = 'https://localhost:7201',
    [string] $ReporterEmail = 'DEMO18-citizen@test.local',
    [Parameter(Mandatory)][securestring] $ReporterPassword,
    [Parameter(Mandatory)][string] $ReviewerEmail,
    [Parameter(Mandatory)][securestring] $ReviewerPassword,
    [Parameter(Mandatory)][string] $SeedResultPath,
    [string] $OutputPath = 'artifacts/portfolio/demo18-pages.json'
)

$ErrorActionPreference = 'Stop'
$origin = [uri] $WebUrl
if ($origin.Scheme -ne 'https' -or $origin.Host -notin @('localhost', '127.0.0.1') -or
    $origin.AbsolutePath -ne '/' -or $origin.Query -or $origin.UserInfo) {
    throw 'Page verification is restricted to a local HTTPS Web origin.'
}
$seed = Get-Content -LiteralPath $SeedResultPath -Raw | ConvertFrom-Json
if ($seed.IncidentIds.Count -ne 6 -or $seed.SituationIds.Count -ne 1) {
    throw 'Use the JSON result of the DEMO18 seed command.'
}
$results = [System.Collections.Generic.List[object]]::new()

function New-DemoSession([string] $email, [securestring] $password) {
    $login = Invoke-WebRequest "$WebUrl/Account/Login" -SessionVariable webSession
    $match = [regex]::Match($login.Content, '<input[^>]*name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) { throw 'Login page did not provide an antiforgery token.' }
    $plainPassword = [System.Net.NetworkCredential]::new('', $password).Password
    try {
        $null = Invoke-WebRequest "$WebUrl/Account/Login" -Method Post -WebSession $webSession -Body @{
            Email = $email; Password = $plainPassword
            __RequestVerificationToken = [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
        }
    } finally { $plainPassword = $null }
    if (-not ($webSession.Cookies.GetCookies($origin) | Where-Object Name -Like '.AspNetCore.Cookies*')) {
        throw 'Web login failed; no authentication cookie was issued.'
    }
    return $webSession
}

function Assert-DemoPage($session, [string] $role, [string] $route, [string[]] $markers) {
    $response = Invoke-WebRequest "$WebUrl$route" -WebSession $session
    if ($response.StatusCode -ne 200) { throw "Unexpected response at $route." }
    foreach ($marker in $markers) {
        if (-not $response.Content.Contains($marker)) { throw "Expected demo content missing at $route ($role)." }
    }
    if ($route -like '/Incidents/Details/*') {
        foreach ($key in @('FIRE-01', 'COLLAPSE-01')) {
            if ($response.Content.Contains("DEMO18-$key") -and
                -not $response.Content.Contains("DEMO18 manual exercise escalation for $key")) {
                throw "Manual escalation missing from incident details at $route."
            }
        }
    }
    $results.Add([pscustomobject]@{ Role = $role; Route = $route; Status = 200; Markers = $markers; Passed = $true })
}

$reporter = New-DemoSession $ReporterEmail $ReporterPassword
$reviewer = New-DemoSession $ReviewerEmail $ReviewerPassword
Assert-DemoPage $reporter 'Citizen' '/Incidents?search=DEMO18-&pageSize=50' @('DEMO18-FLOOD-01','DEMO18-FIRE-01','DEMO18-ROAD-01','DEMO18-COLLAPSE-01','DEMO18-FLOOD-02','DEMO18-DUPLICATE-01')
Assert-DemoPage $reporter 'Citizen' '/Incidents/MyReports?pageSize=50' @('DEMO18-FLOOD-01')
foreach ($id in $seed.IncidentIds) {
    $incidentId = [guid] $id
    # Citizen details avoid the operator view's on-demand prediction side effect.
    Assert-DemoPage $reporter 'Citizen' "/Incidents/Details/$incidentId" @('DEMO18-', 'DEMO18 synthetic exercise only.')
}
Assert-DemoPage $reviewer 'SystemAdmin' '/Incidents?search=DEMO18-&pageSize=50' @('DEMO18-FIRE-01','DEMO18-COLLAPSE-01')
Assert-DemoPage $reviewer 'SystemAdmin' '/Resources' @('DEMO18-BOAT','DEMO18-FIRE-TRUCK','DEMO18-AMBULANCE')
Assert-DemoPage $reviewer 'SystemAdmin' '/Situations' @('DEMO18-SITUATION-FLOOD')
$situationId = [guid] $seed.SituationIds[0]
Assert-DemoPage $reviewer 'SystemAdmin' "/Situations/Details/$situationId" @('DEMO18-SITUATION-FLOOD','may double-count')
Assert-DemoPage $reviewer 'SystemAdmin' '/Notifications' @('DEMO18-FIRE-01','DEMO18-COLLAPSE-01','DEMO18-flood-group')
Assert-DemoPage $reviewer 'SystemAdmin' '/Dashboard' @('DEMO18-FLOOD-01','Open Escalation Alerts')
Assert-DemoPage $reviewer 'SystemAdmin' '/Map' @('DEMO18-FLOOD-01','DEMO18-BOAT')
Assert-DemoPage $reviewer 'SystemAdmin' '/Map/Snapshot' @('DEMO18-FLOOD-01','DEMO18-BOAT')

$inbox = Invoke-WebRequest "$WebUrl/Notifications" -WebSession $reporter
if ($inbox.Content.Contains('DEMO18-FIRE-01') -or $inbox.Content.Contains('DEMO18-flood-group')) {
    throw 'Reviewer notifications appeared in the Citizen inbox.'
}
$results.Add([pscustomobject]@{ Role = 'Citizen'; Route = '/Notifications'; Status = $inbox.StatusCode; Passed = $true; Check = 'Reviewer reminders absent' })
$denied = Invoke-WebRequest "$WebUrl/Resources" -WebSession $reporter
if (-not $denied.BaseResponse.RequestMessage.RequestUri.AbsolutePath.Equals('/Account/AccessDenied')) {
    throw 'Citizen resource page did not follow the expected access-denied route.'
}
$results.Add([pscustomobject]@{ Role = 'Citizen'; Route = '/Resources'; Status = $denied.StatusCode; Passed = $true; Check = 'Redirected to AccessDenied' })
$anonymous = Invoke-WebRequest "$WebUrl/Incidents?search=DEMO18-"
if (-not $anonymous.BaseResponse.RequestMessage.RequestUri.AbsolutePath.Equals('/Account/Login')) {
    throw 'Anonymous incident list did not require login.'
}
$results.Add([pscustomobject]@{ Role = 'Anonymous'; Route = '/Incidents'; Status = $anonymous.StatusCode; Passed = $true; Check = 'Redirected to Login' })

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory) { $null = New-Item -ItemType Directory -Force -Path $outputDirectory }
[pscustomobject]@{
    VerifiedUtc = [DateTime]::UtcNow.ToString('O')
    EvidenceType = 'Local HTTPS requests through real Web cookies and Web-to-API JWT bridge'
    Checks = $results
    Unverified = @('Browser rendering','JavaScript/charts/map tiles','SignalR','Live AI and duplicate detection','Operational state transitions','Azure staging')
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath
Write-Host "$($results.Count) authorized MVC/snapshot and access checks passed. Browser evidence remains pending."
