param(
    [Parameter(Mandatory = $true)]
    [string] $ResourceGroupName,
    [Parameter(Mandatory = $true)]
    [string] $VaultName,
    [string] $ExpectedLocation = 'centralus',
    [string] $WebIdentityName = 'id-er-portfolio-web',
    [string] $KeyName = 'web-data-protection'
)

$ErrorActionPreference = 'Stop'
$temporaryRuleMayExist = $false
$temporaryRuleAddress = $null
$keyUri = $null

function Invoke-AzJson {
    param([Parameter(Mandatory = $true)][string[]] $Arguments)

    $json = & az @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI command failed: az $($Arguments -join ' ')"
    }
    return ($json | ConvertFrom-Json)
}

try {
    $account = Invoke-AzJson -Arguments @('account', 'show', '--query', '{name:name,state:state}', '--output', 'json')
    if ($account.name -ne 'Azure subscription 1' -or $account.state -ne 'Enabled') {
        throw "Active subscription must be enabled 'Azure subscription 1'; found '$($account.name)' with state '$($account.state)' no temporary firewall rule was added."
    }

    $vault = Invoke-AzJson -Arguments @('keyvault', 'show', '--resource-group', $ResourceGroupName, '--name', $VaultName, '--query', '{id:id,location:location,networkAcls:properties.networkAcls}', '--output', 'json')
    if ($vault.id -notmatch "/resourceGroups/$([regex]::Escape($ResourceGroupName))/providers/Microsoft\.KeyVault/vaults/$([regex]::Escape($VaultName))$" -or
        $vault.location -ine $ExpectedLocation) {
        throw "Refusing to operate on a vault outside resource group '$ResourceGroupName' and region '$ExpectedLocation'."
    }
    if ($vault.networkAcls.defaultAction -ne 'Deny' -or $vault.networkAcls.bypass -ne 'None') {
        throw 'Key Vault must retain networkAcls.defaultAction= Deny and bypass=None; no temporary firewall rule was added.'
    }

    $identity = Invoke-AzJson -Arguments @('identity', 'show', '--resource-group', $ResourceGroupName, '--name', $WebIdentityName, '--query', '{id:id,principalId:principalId}', '--output', 'json')
    if ([string]::IsNullOrWhiteSpace($identity.id) -or [string]::IsNullOrWhiteSpace($identity.principalId)) {
        throw "Managed identity '$WebIdentityName' was not fully provisioned; no temporary firewall rule was added."
    }

    $publicIpText = (Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 15).ToString().Trim()
    $publicIp = [System.Net.IPAddress]::Parse($publicIpText)
    if ($publicIp.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork) {
        throw 'Public IP discovery did not return IPv4; refusing to add a firewall rule.'
    }
    $temporaryRuleAddress = "$($publicIp.ToString())/32"

    $existingRule = @($vault.networkAcls.ipRules | Where-Object {
        $_.value.Split('/')[0] -eq $publicIp.ToString()
    })
    if ($existingRule.Count -eq 0) {
        # Mark it first so finally attempts removal even if the CLI loses its response.
        $temporaryRuleMayExist = $true
        & az keyvault network-rule add --resource-group $ResourceGroupName --name $VaultName --ip-address $temporaryRuleAddress --output none
        if ($LASTEXITCODE -ne 0) {
            throw 'Could not add the temporary /32 Key Vault firewall rule.'
        }
        Write-Host "Added temporary Key Vault firewall rule for $temporaryRuleAddress."
    }
    else {
        Write-Host 'The verified workstation IP is already allowed; no temporary rule will be removed.'
    }

    # Allow the Key Vault firewall update to propagate, then require a successful data-plane list.
    $keyList = $null
    for ($attempt = 1; $attempt -le 18; $attempt++) {
        $keyListJson = & az keyvault key list --vault-name $VaultName --output json 2>$null
        if ($LASTEXITCODE -eq 0) {
            $keyList = @($keyListJson | ConvertFrom-Json)
            break
        }
        Start-Sleep -Seconds 10
    }
    if ($null -eq $keyList) {
        throw 'Key Vault data-plane access did not become available within 3 minutes.'
    }

    $existingKey = @($keyList | Where-Object { $_.name -eq $KeyName })
    if ($existingKey.Count -eq 0) {
        & az keyvault key create --vault-name $VaultName --name $KeyName --kty RSA --size 2048 --ops encrypt decrypt wrapKey unwrapKey sign verify --output none
        if ($LASTEXITCODE -ne 0) {
            throw 'Key Vault data-plane key creation failed.'
        }
    }

    $key = Invoke-AzJson -Arguments @('keyvault', 'key', 'show', '--vault-name', $VaultName, '--name', $KeyName, '--output', 'json')
    $expectedKeyOperations = @('encrypt', 'decrypt', 'wrapKey', 'unwrapKey', 'sign', 'verify')
    $actualKeyOperations = @($key.key.key_ops | Sort-Object)
    $sortedExpectedOperations = @($expectedKeyOperations | Sort-Object)
    if (-not $key.key.kid -or
        $key.key.kty -ne 'RSA' -or
        $key.key.n.Length -lt 340 -or
        $key.key.n.Length -gt 344 -or
        $key.attributes.enabled -ne $true -or
        (Compare-Object -ReferenceObject $sortedExpectedOperations -DifferenceObject $actualKeyOperations).Count -ne 0) {
        throw "Key '$KeyName' exists but did not pass the RSA-2048 enabled-key verification."
    }
    $keyUri = $key.key.kid

    $keyArmScope = "$($vault.id)/keys/$KeyName"
    $assignments = Invoke-AzJson -Arguments @('role', 'assignment', 'list', '--scope', $keyArmScope, '--assignee-object-id', $identity.principalId, '--query', "[?roleDefinitionName=='Key Vault Crypto User']", '--output', 'json')
    if (@($assignments).Count -eq 0) {
        & az role assignment create --assignee-object-id $identity.principalId --assignee-principal-type ServicePrincipal --role 'Key Vault Crypto User' --scope $keyArmScope --output none
        if ($LASTEXITCODE -ne 0) {
            throw 'The key was created and verified, but assigning the least-privilege Key Vault Crypto User role at the key scope failed.'
        }
    }

    $verifiedAssignments = Invoke-AzJson -Arguments @('role', 'assignment', 'list', '--scope', $keyArmScope, '--assignee-object-id', $identity.principalId, '--query', "[?roleDefinitionName=='Key Vault Crypto User']", '--output', 'json')
    if (@($verifiedAssignments).Count -eq 0) {
        throw 'The key exists, but the Web identity key-scoped Crypto User permission could not be verified.'
    }
}
finally {
    if ($temporaryRuleMayExist -and $temporaryRuleAddress) {
        $cleanupVerified = $false
        $cleanupError = $null
        for ($attempt = 1; $attempt -le 3; $attempt++) {
            try {
                & az keyvault network-rule remove --resource-group $ResourceGroupName --name $VaultName --ip-address $temporaryRuleAddress --output none 2>$null
                $removeExitCode = $LASTEXITCODE
                $finalVault = Invoke-AzJson -Arguments @('keyvault', 'show', '--resource-group', $ResourceGroupName, '--name', $VaultName, '--query', '{networkAcls:properties.networkAcls}', '--output', 'json')
                $remainingRule = @($finalVault.networkAcls.ipRules | Where-Object { $_.value.Split('/')[0] -eq $temporaryRuleAddress.Split('/')[0] })
                if ($remainingRule.Count -eq 0 -and $finalVault.networkAcls.defaultAction -eq 'Deny' -and $finalVault.networkAcls.bypass -eq 'None') {
                    $cleanupVerified = $true
                    break
                }
                $cleanupError = "Rule removal exit code: $removeExitCode; rule remains or ACL verification failed."
            }
            catch {
                $cleanupError = $_.Exception.Message
            }
            Start-Sleep -Seconds 5
        }
        if (-not $cleanupVerified) {
            throw "URGENT: Could not verify removal of temporary Key Vault firewall rule $temporaryRuleAddress after 3 attempts. Last cleanup error: $cleanupError. Remove it manually with az keyvault network-rule remove --resource-group '$ResourceGroupName' --name '$VaultName' --ip-address '$temporaryRuleAddress', then verify defaultAction Deny and bypass None."
        }
        Write-Host 'Temporary Key Vault firewall rule removed and restricted ACL verified.'
    }
}

if ($keyUri) {
    Write-Host "Verified key URI: $keyUri"
    Write-Host "Verified Key Vault Crypto User role for '$WebIdentityName' at the key resource scope."
}
