# Blender 위치 해석 + 사용자 설정 공용 로직 (kit).
# run_blender.ps1 / setup.ps1이 dot-source 한다.
#
# 해석 순서가 한 곳에만 있어야 "setup이 깐 곳과 run이 찾는 곳이 다른" 사고가 안 난다.
#
# 설치 위치는 개발자마다 다르다(드라이브 용량·기존 설치 여부). 그래서 위치는 코드가 아니라
# **레포 밖 사용자 설정 파일**에 저장한다 — 환경변수는 셸 세션이 끝나면 사라져 매번 다시
# 세팅해야 하고, 레포 안에 두면 커밋돼서 남의 경로를 덮어쓴다.
#
#   설정 파일: %APPDATA%\BlenderArtKit\config.json   ($env:BLENDER_KIT_CONFIG로 이동 가능)
#   { "blenderExe": "...", "installDir": "..." }
#
# 콘솔 출력 문자열은 ASCII로 둔다: 이 파일은 BOM 없는 UTF-8이라
# Windows PowerShell 5.1이 실행할 때 한글 문자열은 깨진다(주석은 무해).

# --- 버전/다운로드 상수 (버전 올릴 때 이 두 줄만 수정) ---
$BlenderVersion = '4.5.11'
$BlenderDirName = "blender-$BlenderVersion-windows-x64"
$BlenderUrl     = "https://download.blender.org/release/Blender4.5/$BlenderDirName.zip"


# ============================ 사용자 설정 ============================

function Get-KitConfigPath {
    if ($env:BLENDER_KIT_CONFIG) { return $env:BLENDER_KIT_CONFIG }
    return (Join-Path $env:APPDATA 'BlenderArtKit\config.json')
}

function Read-KitConfig {
    $p = Get-KitConfigPath
    if (-not (Test-Path $p)) { return $null }
    try {
        return (Get-Content -Raw -Path $p | ConvertFrom-Json)
    } catch {
        Write-Warning "Ignoring unreadable config: $p"
        return $null
    }
}

# blenderExe와 installDir은 "어느 Blender를 쓸 것인가"에 대한 서로 경쟁하는 답이다.
# 그래서 하나를 정하면 다른 하나를 지운다 — 둘 다 남겨두면 우선순위가 높은 옛 설정이 계속
# 이겨서 "바꿨는데 안 바뀐다"가 된다(설정 변경이 항상 한 번에 먹혀야 한다).
function Write-KitConfig {
    param([string]$BlenderExe, [string]$InstallDir)
    $p = Get-KitConfigPath
    New-Item -ItemType Directory -Force -Path (Split-Path $p -Parent) | Out-Null
    $o = [ordered]@{}
    $cur = Read-KitConfig
    if ($cur) {
        foreach ($prop in $cur.PSObject.Properties) { $o[$prop.Name] = $prop.Value }
    }
    if ($BlenderExe) { $o['blenderExe'] = $BlenderExe; $o.Remove('installDir') }
    if ($InstallDir) { $o['installDir'] = $InstallDir; $o.Remove('blenderExe') }
    # BOM 없는 UTF-8: 다른 도구가 읽을 때 BOM 하나에 파서가 죽는 일을 막는다.
    [IO.File]::WriteAllText($p, ($o | ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding($false)))
    return $p
}

# 설정을 지워 "설정 안 한 상태"로 되돌린다. 잘못 등록했을 때의 탈출구.
function Clear-KitConfig {
    $p = Get-KitConfigPath
    if (Test-Path $p) {
        Remove-Item -Force -Path $p
        return $p
    }
    return $null
}

# 설정 파일이 없을 때의 기본 캐시 위치 (설정은 별도 후보로 이미 잡히므로 여기서 보지 않는다).
function Get-DefaultKitHome {
    if ($env:BLENDER_KIT_HOME) { return $env:BLENDER_KIT_HOME }
    return (Join-Path $env:LOCALAPPDATA 'BlenderArtKit')
}

# 포터블을 새로 설치할 위치. 설정 파일 > 환경변수 > OS 기본 순.
function Get-BlenderKitHome {
    $cfg = Read-KitConfig
    if ($cfg -and $cfg.installDir) { return $cfg.installDir }
    return (Get-DefaultKitHome)
}


# ============================ 위치 해석 ============================

# 주어진 디렉터리 바로 아래의 blender-*/blender.exe 를 찾는다.
function Find-PortableBlender {
    param([string]$Dir)
    if (-not $Dir -or -not (Test-Path $Dir)) { return $null }
    return Get-ChildItem -Path $Dir -Directory -Filter 'blender-*' -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName 'blender.exe' } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
}

# 후보 전체를 우선순위 순으로 돌려준다 (-Status 출력과 실제 해석이 같은 목록을 쓰도록).
#
#   1) $env:BLENDER_EXE        - 이 셸 세션에서만 쓰는 임시 오버라이드
#   2) 설정 blenderExe         - 개발자가 직접 등록한 기존 설치
#   3) 설정 installDir         - 개발자가 고른 포터블 설치 위치
#   4) <ArtPipeline>/tools/    - 레포에 박아둔 포터블 (설정이 없으면 이게 이긴다 = 기존 사용자 무손상)
#   5) 기본 공유 캐시          - 설정을 아직 안 한 사람의 기본값
#   6) Program Files           - 시스템 설치
function Get-BlenderCandidates {
    param([string]$PipelineRoot)

    $cfg = Read-KitConfig
    $list = @()

    $detail = '(not set)'
    $path = $null
    if ($env:BLENDER_EXE) {
        $detail = $env:BLENDER_EXE
        if (Test-Path $env:BLENDER_EXE) { $path = $env:BLENDER_EXE }
    }
    $list += [pscustomobject]@{ Source = '$env:BLENDER_EXE'; Detail = $detail; Path = $path }

    $detail = '(not set)'
    $path = $null
    if ($cfg -and $cfg.blenderExe) {
        $detail = $cfg.blenderExe
        if (Test-Path $cfg.blenderExe) { $path = $cfg.blenderExe }
    }
    $list += [pscustomobject]@{ Source = 'config blenderExe'; Detail = $detail; Path = $path }

    $detail = '(not set)'
    $path = $null
    if ($cfg -and $cfg.installDir) {
        $detail = $cfg.installDir
        $path = Find-PortableBlender $cfg.installDir
    }
    $list += [pscustomobject]@{ Source = 'config installDir'; Detail = $detail; Path = $path }

    $toolsDir = Join-Path $PipelineRoot 'tools'
    $list += [pscustomobject]@{ Source = 'repo tools/'; Detail = $toolsDir; Path = (Find-PortableBlender $toolsDir) }

    # 설정값은 위에서 이미 별도 후보로 잡혔다. 여기서는 순수 기본값만 보여줘야
    # "설정을 지우면 어디를 보게 되는지"와 "그 자리에 뭐가 남아 있는지"가 드러난다.
    # ($home은 PowerShell 자동 변수라 덮어쓰면 안 된다.)
    $kitHome = Get-DefaultKitHome
    $list += [pscustomobject]@{ Source = 'default cache'; Detail = $kitHome; Path = (Find-PortableBlender $kitHome) }

    $pf = 'C:\Program Files\Blender Foundation\Blender 4.5\blender.exe'
    $pfPath = $null
    if (Test-Path $pf) { $pfPath = $pf }
    $list += [pscustomobject]@{ Source = 'Program Files'; Detail = $pf; Path = $pfPath }

    return $list
}

# 실제로 쓰일 blender.exe 경로. 없으면 $null.
function Resolve-BlenderPath {
    param([string]$PipelineRoot)
    $hit = Get-BlenderCandidates $PipelineRoot | Where-Object { $_.Path } | Select-Object -First 1
    if ($hit) { return $hit.Path }
    return $null
}

# 어디서 왔는지까지 알려준다 (-Status / 경고 메시지용).
function Resolve-BlenderInfo {
    param([string]$PipelineRoot)
    return (Get-BlenderCandidates $PipelineRoot | Where-Object { $_.Path } | Select-Object -First 1)
}

function Show-BlenderStatus {
    param([string]$PipelineRoot)
    $cands = Get-BlenderCandidates $PipelineRoot
    $hit = $cands | Where-Object { $_.Path } | Select-Object -First 1

    Write-Host "Required version : Blender $BlenderVersion (4.5 LTS)"
    Write-Host "Config file      : $(Get-KitConfigPath)"
    if (Test-Path (Get-KitConfigPath)) { Write-Host "                   (exists)" } else { Write-Host "                   (not created yet)" }
    Write-Host ""
    Write-Host "Lookup order (first hit wins):"
    foreach ($c in $cands) {
        $mark = ' '
        if ($c.Path) { $mark = 'x' }
        Write-Host ("  [$mark] {0,-18} {1}" -f $c.Source, $c.Detail)
        if ($c.Path -and $c.Detail -ne $c.Path) { Write-Host ("       -> {0}" -f $c.Path) }
    }
    Write-Host ""
    if ($hit) {
        Write-Host "RESOLVED: $($hit.Path)"
        Write-Host "  via   : $($hit.Source)"
    } else {
        Write-Host "RESOLVED: (none) - run setup.ps1 to install"
    }
}


# ============================ 설치 / 이전 ============================

# 포터블 Blender를 지정 디렉터리 아래에 내려받아 푼다. 반환값 = blender.exe 절대 경로.
function Install-PortableBlender {
    param([string]$DestDir)
    New-Item -ItemType Directory -Force -Path $DestDir | Out-Null
    $zipPath   = Join-Path $DestDir "$BlenderDirName.zip"
    $targetExe = Join-Path $DestDir (Join-Path $BlenderDirName 'blender.exe')

    Write-Host "Downloading Blender $BlenderVersion ... (~380 MB, may take a few minutes)"
    Write-Host "  URL:  $BlenderUrl"
    Write-Host "  Dest: $DestDir"
    # PS 5.1에서 대용량 다운로드 속도 확보: 진행바 렌더 비활성 + TLS 1.2 강제
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $prevProgress = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'
    try {
        Invoke-WebRequest -Uri $BlenderUrl -OutFile $zipPath -UseBasicParsing
    } finally {
        $ProgressPreference = $prevProgress
    }

    Write-Host "Extracting -> $DestDir"
    # zip 안에 최상위 폴더 blender-<버전>-windows-x64/ 가 들어있어 그대로 DestDir 밑에 풀림.
    Expand-Archive -Path $zipPath -DestinationPath $DestDir -Force

    if (-not (Test-Path $targetExe)) {
        throw "Install verification failed: '$targetExe' not found. The zip layout may have changed."
    }
    Remove-Item $zipPath -Force
    return $targetExe
}

# 이미 받아둔 포터블을 새 위치로 옮긴다(또는 복사). 같은 버전을 또 받지 않으려는 것.
function Move-PortableBlender {
    param([string]$SourceExe, [string]$DestDir, [switch]$CopyInstead)
    $srcDir = Split-Path $SourceExe -Parent            # ...\blender-<버전>-windows-x64
    $leaf   = Split-Path $srcDir -Leaf
    New-Item -ItemType Directory -Force -Path $DestDir | Out-Null
    $target = Join-Path $DestDir $leaf
    if (Test-Path $target) {
        throw "Target already exists: $target (remove it first, or pick another -InstallDir)"
    }
    if ($CopyInstead) {
        Write-Host "Copying  $srcDir"
        Write-Host "     ->  $target"
        Copy-Item -Recurse -Path $srcDir -Destination $target
    } else {
        Write-Host "Moving   $srcDir"
        Write-Host "     ->  $target"
        Move-Item -Path $srcDir -Destination $target
    }
    $exe = Join-Path $target 'blender.exe'
    if (-not (Test-Path $exe)) {
        throw "Relocation failed: '$exe' not found."
    }
    return $exe
}
