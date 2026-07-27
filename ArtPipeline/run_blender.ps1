# Blender 헤드리스 실행 래퍼.
# 사용법: .\run_blender.ps1 <script.py> [스크립트 인자...]
#
# Blender 위치 해석은 blender_common.ps1이 담당한다(레포 내 tools → 사용자 공유 캐시 순).
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Script,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$ScriptArgs
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'blender_common.ps1')

$blender = Resolve-BlenderPath $root
if (-not $blender) {
    Write-Host "blender.exe not found. Checked:"
    Show-BlenderStatus $root
    throw "No Blender available. Run .\setup.ps1  (or .\setup.ps1 -InstallDir <path> to choose where)."
}
$scriptPath = Resolve-Path $Script

# --python-exit-code 1: bpy 스크립트에서 예외 발생 시 0이 아닌 종료 코드를 보장
$argList = @('--background', '--factory-startup', '--python-exit-code', '1', '--python', $scriptPath.Path)
if ($ScriptArgs) { $argList += @('--') + $ScriptArgs }

& $blender @argList
exit $LASTEXITCODE
