# ArtPipeline kit을 다른 프로젝트로 이식한다 (Windows 전용).
#
# 복사되는 것은 프로젝트 중립인 kit뿐이다:
#   blender_common.ps1 / run_blender.ps1 / setup.ps1 / lib/lowpoly_lib/*.py / KIT.md
# 게임 콘텐츠(assets/의 생성 스크립트, project/palette_registry.py의 색 목록)는 가져가지 않는다.
#
# 대상 프로젝트에는 pipeline.json과 빈 팔레트 레지스트리가 새로 만들어지고,
# Blender 자체는 사용자 공유 캐시(%LOCALAPPDATA%\BlenderArtKit\)를 그대로 재사용하므로
# 두 번째 프로젝트부터는 380 MB 다운로드가 없다.
#
# 이 레포는 이식 후에도 자족성을 유지한다 — 여기서 나가는 것은 사본이고, 원본은 그대로 남는다.
#
# 콘솔 출력 문자열은 ASCII로 둔다: 이 파일은 BOM 없는 UTF-8이라
# Windows PowerShell 5.1이 실행할 때 한글 문자열은 깨진다(주석은 무해).
#
# 사용법:
#   .\install_kit.ps1 -Target <새 프로젝트 경로>
#   .\install_kit.ps1 -Target <새 프로젝트 경로> -ProjectName "Other Game" -WithUnityEditorScripts
#   .\install_kit.ps1 -Target <새 프로젝트 경로> -Force   # 기존 kit 파일 덮어쓰기(업데이트)
param(
    [Parameter(Mandatory = $true)][string]$Target,
    [string]$ProjectName,
    [string]$AssetsRoot = 'Assets/Art',
    [ValidateSet('unity', 'none')][string]$Engine = 'unity',
    [switch]$WithUnityEditorScripts,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$src = $PSScriptRoot

# 이 스크립트는 '복사기'일 뿐 내용물이 아니다 — 같은 폴더의 kit 파일들을 대상으로 옮긴다.
# install_kit.ps1만 따로 떼어 놓고 실행하는 오해가 흔해서, 알아보기 힘든
# Get-ChildItem 오류로 죽기 전에 무엇이 없는지 먼저 알려준다.
$required = @(
    'blender_common.ps1',
    'run_blender.ps1',
    'setup.ps1',
    'KIT.md',
    'NEW-PROJECT.md',
    'lib\lowpoly_lib',
    'kit_templates\kit_check.py',
    'kit_templates\pipeline.json.template',
    'kit_templates\palette_registry.py.template',
    'assets\_smoke\smoke_test.py'
)
$missing = $required | Where-Object { -not (Test-Path (Join-Path $src $_)) }
if ($missing) {
    Write-Host "This script only copies the kit that sits next to it. Missing here ($src):"
    foreach ($m in $missing) { Write-Host "  - $m" }
    Write-Host ""
    Write-Host "Run it from a full ArtPipeline folder (clone the source repo), not from a copy"
    Write-Host "of install_kit.ps1 on its own."
    throw "Incomplete kit source: $($missing.Count) item(s) missing."
}

if (-not (Test-Path $Target)) {
    throw "Target project folder not found: $Target"
}
$Target = (Resolve-Path $Target).Path
if ($Target -eq $src -or $Target -eq (Split-Path $src -Parent)) {
    throw "Target is this repository. Choose a different project folder."
}
if (-not $ProjectName) { $ProjectName = Split-Path $Target -Leaf }
$AssetsRoot = $AssetsRoot.Replace('\', '/').Trim('/')

$dst = Join-Path $Target 'ArtPipeline'
Write-Host "== Install ArtPipeline kit =="
Write-Host "  from: $src"
Write-Host "  to  : $dst"
Write-Host ""

foreach ($d in @('', 'lib\lowpoly_lib', 'project', 'assets\_smoke', 'kit_templates')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $dst $d) | Out-Null
}

# --- kit 본체 (프로젝트 중립 — 업데이트 시 그대로 덮어써도 되는 파일들) ---
#
# install_kit.ps1과 kit_templates/도 함께 보낸다: 이식된 프로젝트가 **자기도 씨앗이 되도록**.
# 안 보내면 새 프로젝트를 만들 때마다 원본 레포가 반드시 살아 있어야 하는 단일 실패 지점이
# 생긴다. 사본이 늘면 버전 드리프트가 생길 수 있는데, 그건 KIT-ORIGIN.txt로 추적하고
# `-Force` 재설치로 정리하는 편이 낫다.
$kitFiles = @(
    'blender_common.ps1',
    'run_blender.ps1',
    'setup.ps1',
    'install_kit.ps1',
    'KIT.md',
    'NEW-PROJECT.md',
    'kit_templates\kit_check.py',
    'kit_templates\pipeline.json.template',
    'kit_templates\palette_registry.py.template'
)
Get-ChildItem -Path (Join-Path $src 'lib\lowpoly_lib') -Filter '*.py' -File | ForEach-Object {
    $kitFiles += ('lib\lowpoly_lib\' + $_.Name)
}

$copied = 0
$skipped = 0
foreach ($rel in $kitFiles) {
    $s = Join-Path $src $rel
    $t = Join-Path $dst $rel
    if (-not (Test-Path $s)) { continue }
    if ((Test-Path $t) -and -not $Force) {
        Write-Host "  skip (exists): $rel"
        $skipped++
        continue
    }
    Copy-Item -Path $s -Destination $t -Force
    Write-Host "  copy: $rel"
    $copied++
}

# --- 출처 기록 ---
# 사본이 또 사본을 낳을 수 있으므로(이식된 프로젝트도 씨앗이 된다), 이 kit이 어디서
# 언제 왔는지 남긴다. 버전 드리프트가 의심될 때 이 파일부터 비교하면 된다.
$originCommit = '(not a git repo)'
try {
    $c = & git -C $src rev-parse --short HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and $c) { $originCommit = "$c".Trim() }
} catch { }
# git이 실패했을 때(사본에서 사본을 뜨는 경우가 정상적으로 그렇다) 그 종료 코드가
# 스크립트 종료 코드로 새어나가면 호출자가 설치 실패로 오판한다. 여기서 끊는다.
$global:LASTEXITCODE = 0
# 내용은 ASCII로 둔다: 이 파일은 BOM 없는 UTF-8이라 PS 5.1이 CP949로 읽어
# 한글 '문자열 리터럴'은 깨진 채로 기록된다(주석은 무해). run_blender.ps1과 같은 관례.
$originLines = @(
    "# Where this ArtPipeline kit came from (written by install_kit.ps1).",
    "# If a copy looks out of date, compare 'commit' and reinstall with -Force from a newer source.",
    "source : $src",
    "commit : $originCommit",
    "date   : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
)
[IO.File]::WriteAllText(
    (Join-Path $dst 'KIT-ORIGIN.txt'),
    ($originLines -join "`r`n") + "`r`n",
    (New-Object Text.UTF8Encoding($false))
)
Write-Host "  write: KIT-ORIGIN.txt  (source=$originCommit)"

# --- 검증 스크립트 (스모크 + kit 통합 확인) ---
foreach ($pair in @(
    @{ From = 'assets\_smoke\smoke_test.py'; To = 'assets\_smoke\smoke_test.py' },
    @{ From = 'kit_templates\kit_check.py'; To = 'assets\_smoke\kit_check.py' }
)) {
    $t = Join-Path $dst $pair.To
    if ((Test-Path $t) -and -not $Force) {
        Write-Host ("  skip (exists): " + $pair.To)
        $skipped++
    } else {
        Copy-Item -Path (Join-Path $src $pair.From) -Destination $t -Force
        Write-Host ("  copy: " + $pair.To)
        $copied++
    }
}

# --- 프로젝트 레이어 스캐폴딩 ---
# 여기부터는 대상 프로젝트가 소유한다. -Force는 kit 파일에만 걸린다 — kit 업데이트가
# 프로젝트 설정(출력 경로)이나 팔레트(= 익스포트된 전 에셋의 UV 계약)를 날리면 안 되기 때문.
$jsonPath = Join-Path $dst 'pipeline.json'
if (Test-Path $jsonPath) {
    Write-Host "  keep (project-owned): pipeline.json"
} else {
    $tpl = Get-Content -Raw (Join-Path $src 'kit_templates\pipeline.json.template')
    $tpl = $tpl.Replace('__PROJECT__', $ProjectName).Replace('__ENGINE__', $Engine).Replace('__ASSETS_ROOT__', $AssetsRoot)
    # BOM 없는 UTF-8로 써야 Python의 json.load가 읽는다.
    [IO.File]::WriteAllText($jsonPath, $tpl, (New-Object Text.UTF8Encoding($false)))
    Write-Host "  write: pipeline.json  (project='$ProjectName', assetsRoot='$AssetsRoot')"
}

$regPath = Join-Path $dst 'project\palette_registry.py'
if (Test-Path $regPath) {
    Write-Host "  keep (project-owned): project\palette_registry.py"
} else {
    Copy-Item -Path (Join-Path $src 'kit_templates\palette_registry.py.template') -Destination $regPath -Force
    Write-Host "  write: project\palette_registry.py  (starter palette - replace with your colors)"
}

# --- Unity 임포트 후처리 (선택) ---
if ($WithUnityEditorScripts) {
    $editorDst = Join-Path $Target ($AssetsRoot.Replace('/', '\') + '\Editor')
    New-Item -ItemType Directory -Force -Path $editorDst | Out-Null
    foreach ($cs in @('ArtAssetPostprocessor.cs', 'ArtPipelineSetup.cs')) {
        $s = Join-Path (Split-Path $src -Parent) ('Assets\Art\Editor\' + $cs)
        $t = Join-Path $editorDst $cs
        if (-not (Test-Path $s)) { continue }
        if ((Test-Path $t) -and -not $Force) {
            Write-Host "  skip (exists): $cs"
            continue
        }
        Copy-Item -Path $s -Destination $t -Force
        Write-Host "  copy (reference impl, edit for your project): $cs"
    }
    Write-Host "  [note] These two C# files are Dungeon Miner's importer rules."
    Write-Host "         Treat them as a starting point: material slot names, clip"
    Write-Host "         loop lists and shader references all need review."
}

Write-Host ""
Write-Host "Copied $copied file(s), skipped $skipped (use -Force to overwrite)."
Write-Host ""
Write-Host "== Next steps in $Target =="
Write-Host "  1. cd ArtPipeline"
Write-Host "  2. .\setup.ps1                                  # reuses the shared Blender if present"
Write-Host "  3. .\run_blender.ps1 assets\_smoke\kit_check.py  # verifies the whole chain"
Write-Host "  4. Edit project\palette_registry.py with your colors"
Write-Host ""
Write-Host "  Full walkthrough: ArtPipeline\NEW-PROJECT.md   (reference: KIT.md)"
Write-Host ""
Write-Host "  This copy is self-contained: it can seed further projects with"
Write-Host "    .\install_kit.ps1 -Target <another project>"
Write-Host ""
Write-Host "  Add to that project's .gitignore:"
Write-Host "    /ArtPipeline/tools/"
Write-Host "    /ArtPipeline/build/"
Write-Host "    /ArtPipeline/previews/"
Write-Host "    /ArtPipeline/reports/"
Write-Host "    __pycache__/"

exit 0
