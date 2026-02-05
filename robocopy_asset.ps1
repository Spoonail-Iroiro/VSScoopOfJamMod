param (
    [string]$TargetName,
    [string]$ProjectDir,
    [string]$BinDir
)

if ([string]::IsNullOrWhiteSpace($TargetName) -or
    -not (Test-Path $ProjectDir) -or
    -not (Test-Path $BinDir)) {
    Write-Host 'Error: Invalid project context. TargetName is invalid, or paths does not exist. Abort.'; exit 1
}

if ($ProjectDir -cnotlike (-join ("*", $TargetName, "*"))) {
    Write-Host 'Error: Invalid project context! ProjectDir does not contain TargetName. Abort.'; exit 1
}

if ($BinDir -cnotlike (-join ("*", $TargetName, "*"))) {
    Write-Host 'Error: Invalid project context! BinDir does not contain TargetName. Abort.'; exit 1
}

$src = Join-Path $ProjectDir 'assets'
$dst   = Join-Path $BinDir 'assets'

# robocopy '$(ProjectDir)assets' '$(BinDir)\assets' /E /IS /IT /IM

robocopy $src $dst /E /IS /IT /IM