# ArtPipeline 부트스트랩: Blender CLI 개발 환경 준비 (Windows 전용).
#
# 새 협업자가 저장소를 클론한 뒤 이 스크립트를 한 번 실행하면 Blender 4.5 LTS가 준비된다.
# 외부 준비물은 없다.
#
# **설치 위치는 개발자가 고른다.** 드라이브 용량도, 이미 깔린 Blender가 있는지도 사람마다
# 다르기 때문. 고른 위치는 레포 밖 사용자 설정 파일(%APPDATA%\BlenderArtKit\config.json)에
# 저장돼 다음부터 자동으로 쓰인다 — 환경변수처럼 셸을 닫으면 사라지지 않고, 레포에 커밋돼
# 남의 경로를 덮어쓰지도 않는다. 같은 kit을 쓰는 다른 프로젝트도 이 설정을 공유하므로
# Blender를 프로젝트마다 다시 받지 않는다.
#
# 콘솔 출력 문자열은 ASCII로 둔다: 이 파일은 BOM 없는 UTF-8이라
# Windows PowerShell 5.1이 실행할 때 한글 문자열은 깨진다(주석은 무해).
#
# 사용법:
#   .\setup.ps1 -Status                      # 지금 어디를 보고 있는지만 출력 (아무것도 안 바꿈)
#   .\setup.ps1                              # 없으면 기본 위치에 받아서 설치 + 스모크 검증
#   .\setup.ps1 -InstallDir <원하는 경로>    # 원하는 위치에 설치하고 그 선택을 저장
#                                            #   (이미 받아둔 같은 버전이 있으면 다운로드 없이 옮긴다)
#   .\setup.ps1 -InstallDir <경로> -Copy     # 옮기지 말고 복사
#   .\setup.ps1 -BlenderExe <기존 blender.exe 경로>  # 이미 설치된 Blender를 등록만 (다운로드 없음)
#   .\setup.ps1 -Local                       # 레포 안(ArtPipeline\tools\)에 설치
#   .\setup.ps1 -Reset                       # 저장된 위치 설정을 지우고 "설정 안 한 상태"로 되돌림
#   .\setup.ps1 -Force                       # 이미 있어도 새로 받아 재설치
#   .\setup.ps1 -SkipSmoke                   # 설치만 하고 스모크 렌더 검증은 건너뜀
#
# 위치를 **바꿀 때도** 같은 명령을 그냥 다시 쓰면 된다 — -InstallDir / -BlenderExe는 서로
# 경쟁하는 설정이라 하나를 정하면 다른 하나를 지운다(옛 설정이 남아 계속 이기는 일이 없다).
param(
    [string]$InstallDir,
    [string]$BlenderExe,
    [switch]$Status,
    [switch]$Reset,
    [switch]$Copy,
    [switch]$Local,
    [switch]$Force,
    [switch]$SkipSmoke
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'blender_common.ps1')

$toolsDir = Join-Path $root 'tools'

# --- -Status: 조회만 하고 종료 (LLM/사람이 현재 상태를 먼저 확인할 때) ---
if ($Status) {
    Write-Host "== ArtPipeline Blender status =="
    Show-BlenderStatus $root
    exit 0
}

# 인자 검증은 아무것도 건드리기 전에 (반쯤 적용된 상태를 남기지 않는다).
if ($InstallDir -and $BlenderExe) {
    throw "Use either -InstallDir (install a portable build) or -BlenderExe (register an existing one), not both."
}
if ($InstallDir -and $Local) {
    throw "-Local already means 'install into ArtPipeline\tools'. Drop one of -Local / -InstallDir."
}

Write-Host "== ArtPipeline setup =="

# --- -Reset: 저장된 위치 설정을 지운다 ---
if ($Reset) {
    $removed = Clear-KitConfig
    if ($removed) { Write-Host "Cleared user config: $removed" } else { Write-Host "No user config to clear." }
    # 단독 -Reset은 여기서 끝낸다. 이어서 설치까지 하면 의도치 않은 380 MB 다운로드가 돌 수 있다.
    if (-not $InstallDir -and -not $BlenderExe -and -not $Local -and -not $Force) {
        Write-Host ""
        Show-BlenderStatus $root
        exit 0
    }
}

# --- -BlenderExe: 이미 설치된 Blender를 등록만 한다 ---
if ($BlenderExe) {
    if (-not (Test-Path $BlenderExe)) {
        throw "blender.exe not found at: $BlenderExe"
    }
    $BlenderExe = (Resolve-Path $BlenderExe).Path
    $ver = (& $BlenderExe --version 2>$null | Select-Object -First 1)
    Write-Host "Registering existing install: $BlenderExe"
    if ($ver) { Write-Host "  reports: $ver" }
    if ($ver -and $ver -notmatch '4\.5') {
        Write-Warning "This does not look like Blender 4.5 LTS. The pipeline pins 4.5; other versions may break exports."
    }
    $cfgPath = Write-KitConfig -BlenderExe $BlenderExe
    Write-Host "Saved to: $cfgPath"
} else {
    # --- 설치 위치 결정 ---
    if ($Local) {
        $destDir = $toolsDir
    } elseif ($InstallDir) {
        $destDir = $InstallDir
        # 아직 없는 경로도 받아들인다 (New-Item이 만든다). 절대 경로로 정규화만 해 둔다.
        if (-not [IO.Path]::IsPathRooted($destDir)) { $destDir = Join-Path (Get-Location).Path $destDir }
    } else {
        $destDir = Get-BlenderKitHome
    }

    $existing = Resolve-BlenderInfo $root

    if ($existing -and -not $Force) {
        # 이미 있는데 사용자가 다른 위치를 지정했다면, 다시 받지 말고 옮긴다.
        $alreadyThere = $existing.Path.StartsWith((Join-Path $destDir ''), [StringComparison]::OrdinalIgnoreCase)
        if ($InstallDir -and -not $alreadyThere -and $existing.Path -like '*\blender-*\blender.exe') {
            Write-Host "Found an existing portable build; relocating instead of downloading."
            $installed = Move-PortableBlender -SourceExe $existing.Path -DestDir $destDir -CopyInstead:$Copy
            Write-Host "Installed: $installed"
        } else {
            Write-Host "Blender already available: $($existing.Path)"
            Write-Host "  via: $($existing.Source)"
            Write-Host "  (use -Force to re-download, or -InstallDir <path> to move it elsewhere)"
        }
    } else {
        if ($existing -and $Force) {
            Write-Host "-Force: re-downloading portable Blender even though '$($existing.Path)' exists."
        }
        $installed = Install-PortableBlender $destDir
        Write-Host "Installed: $installed"
    }

    # 위치 선택을 영속화한다 (-Local은 레포 한정이라 저장하지 않는다).
    if ($InstallDir) {
        $cfgPath = Write-KitConfig -InstallDir $destDir
        Write-Host "Saved install location to: $cfgPath"
    }
}

# --- 실제로 쓰일 것이 방금 설치한 것인지 재확인 ---
# 우선순위가 더 높은 후보가 남아 있으면 방금 한 일이 무시된다. 조용히 넘어가면 안 된다.
$final = Resolve-BlenderInfo $root
if (-not $final) {
    throw "No Blender resolved after setup. Run .\setup.ps1 -Status to see what was checked."
}
Write-Host ""
Write-Host "Active blender.exe: $($final.Path)"
Write-Host "  via: $($final.Source)"
if ($BlenderExe -and $final.Path -ne $BlenderExe) {
    Write-Warning "A higher-priority entry wins over the one you just registered. Run .\setup.ps1 -Status."
}
if ($installed -and $final.Path -ne $installed) {
    Write-Warning "A higher-priority entry wins over the build just installed. Run .\setup.ps1 -Status."
}

# --- 스모크 검증: run_blender.ps1로 헤드리스 큐브 렌더 ---
if (-not $SkipSmoke) {
    $runner = Join-Path $root 'run_blender.ps1'
    $smoke  = Join-Path $root 'assets\_smoke\smoke_test.py'
    Write-Host ""
    Write-Host "Smoke check: run_blender.ps1 assets\_smoke\smoke_test.py"
    & $runner $smoke
    if ($LASTEXITCODE -ne 0) {
        throw "Smoke check failed (exit $LASTEXITCODE). Verify the Blender install."
    }
    Write-Host "Smoke passed -> previews/_smoke/smoke_cube.png written"
}

Write-Host ""
Write-Host "== Blender CLI ready =="
Write-Host "Try generating an asset:"
Write-Host "  .\run_blender.ps1 assets\pickaxe\generate_pickaxe.py"
Write-Host "Conventions:  ArtPipeline/CONVENTIONS.md"
Write-Host "Reuse in another project:  ArtPipeline/KIT.md  (.\install_kit.ps1 -Target <path>)"
Write-Host ""
Write-Host "[note] This script provisions Blender only. To open the game or run"
Write-Host "       batch checks you also need Unity 6000.3.10f1 (see root README.md)."
