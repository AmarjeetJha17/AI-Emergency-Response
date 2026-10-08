[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Server,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Database,
    [Parameter(Mandatory)][ValidateScript({ Test-Path -LiteralPath $_ })][string]$BackupPath
)
$ErrorActionPreference = 'Stop'
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) { throw 'sqlcmd is required for the local SQL backup verification drill.' }
if ($Database -match '[;\[\]]') { throw 'Database contains unsupported SQL identifier characters.' }
if (-not (Test-Path -LiteralPath $BackupPath -PathType Leaf)) { throw 'Backup file does not exist.' }
& $sqlcmd.Source -S $Server -d master -b -Q "RESTORE VERIFYONLY FROM DISK = N'$($BackupPath.Replace("'", "''"))'"
if ($LASTEXITCODE -ne 0) { throw "SQL backup verification failed with exit code $LASTEXITCODE." }
Write-Host "Backup verification succeeded for database '$Database'."
