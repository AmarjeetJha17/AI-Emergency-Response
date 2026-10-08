[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Server,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$SourceDatabase,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$RestoreDatabase,
    [Parameter(Mandatory)][ValidateScript({ Test-Path -LiteralPath $_ })][string]$BackupPath,
    [Parameter(Mandatory)][ValidateScript({ Test-Path -LiteralPath $_ -PathType Container })][string]$DataDirectory
)
$ErrorActionPreference = 'Stop'
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) { throw 'sqlcmd is required for the local SQL restore drill.' }
foreach ($name in @($SourceDatabase, $RestoreDatabase)) {
    if ($name -match '[;\[\]]') { throw "Database identifier '$name' contains unsupported characters." }
}
if ($SourceDatabase -eq $RestoreDatabase) { throw 'Restore target must differ from the source database.' }
$exists = & $sqlcmd.Source -S $Server -d master -h -1 -W -b -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'$($RestoreDatabase.Replace("'", "''"))';"
if ($LASTEXITCODE -ne 0) { throw 'Could not inspect SQL Server databases.' }
if ([int]($exists | Select-Object -Last 1) -gt 0) { throw "Restore target '$RestoreDatabase' already exists; refusing to overwrite it." }
$logical = & $sqlcmd.Source -S $Server -d master -h -1 -s '|' -W -b -Q "RESTORE FILELISTONLY FROM DISK = N'$($BackupPath.Replace("'", "''"))';"
if ($LASTEXITCODE -ne 0) { throw 'Could not inspect backup file list.' }
$rows = @($logical | ForEach-Object { $parts = $_.Split('|'); if ($parts.Count -gt 2 -and $parts[2].Trim() -in @('D', 'L')) { [pscustomobject]@{ LogicalName = $parts[0].Trim(); Type = $parts[2].Trim() } } })
if ($rows.Count -lt 1 -or -not $rows[0].LogicalName) { throw 'Could not identify logical data file in backup.' }
$dataLogical = ($rows | Where-Object { $_.Type -eq 'D' } | Select-Object -First 1).LogicalName
$logLogical = ($rows | Where-Object { $_.Type -eq 'L' } | Select-Object -First 1).LogicalName
if (-not $logLogical) { throw 'Could not identify logical log file in backup.' }
$dataPath = Join-Path $DataDirectory "$RestoreDatabase.mdf"
$logPath = Join-Path $DataDirectory "$RestoreDatabase`_log.ldf"
$restoreSql = "RESTORE DATABASE [$RestoreDatabase] FROM DISK = N'$($BackupPath.Replace("'", "''"))' WITH RECOVERY, MOVE N'$($dataLogical.Replace("'", "''"))' TO N'$($dataPath.Replace("'", "''"))', MOVE N'$($logLogical.Replace("'", "''"))' TO N'$($logPath.Replace("'", "''"))';"
if ($PSCmdlet.ShouldProcess("$Server/$RestoreDatabase", 'Restore isolated SQL backup')) {
    & $sqlcmd.Source -S $Server -d master -b -Q $restoreSql
    if ($LASTEXITCODE -ne 0) { throw "Restore failed with exit code $LASTEXITCODE." }
    & $sqlcmd.Source -S $Server -d $RestoreDatabase -b -Q "DBCC CHECKDB ([$RestoreDatabase]) WITH NO_INFOMSGS; IF OBJECT_ID(N'__EFMigrationsHistory') IS NULL THROW 51000, 'EF migrations history missing', 1; SELECT COUNT(*) AS TableCount FROM sys.tables;"
    if ($LASTEXITCODE -ne 0) { throw 'Restored database integrity or migration-state check failed.' }
    Write-Host "Restore verification succeeded: $Server/$RestoreDatabase"
}
