<#
.SYNOPSIS
    Unity WebGL 빌드 산출물을 GitHub Pages 배포 브랜치로 올린다.

.DESCRIPTION
    Unity 로 WebGL 을 빌드한 폴더(기본 `WebGLBuild/`)를 통째로 배포 브랜치
    (기본 `gh-pages`)의 **부모 없는 커밋 하나**로 만들어 강제 푸시한다.

    왜 스크립트인가 — 손으로 하면 세 군데에서 조용히 깨진다.

      1. 저장소 `.gitignore` 에 `**/[Bb]uild/` 가 있다. Unity WebGL 산출물의
         핵심 폴더 이름이 정확히 `Build/` 다 (wasm · data · framework 가 전부
         그 안에 있다). 그냥 `git add` 하면 index.html 만 올라가고 알맹이가
         빠진 채 배포된다 — 브라우저에는 404 만 뜬다.
         → 이 스크립트는 배포 트리를 `--work-tree` 로 따로 잡고 `add -f` 한다.

      2. GitHub Pages 는 Jekyll 을 거치면서 `_` 로 시작하는 경로를 버린다.
         → `.nojekyll` 을 항상 같이 올린다.

      3. GitHub Pages 는 커스텀 응답 헤더를 못 준다. Brotli/Gzip 으로 빌드했는데
         Decompression Fallback 이 꺼져 있으면 로더가
         "Unable to parse Build/*.br!" 로 죽는다.
         → 푸시 전에 ProjectSettings 를 읽어 막는다.

    히스토리는 매번 버린다(부모 없는 커밋 + 강제 푸시). wasm 은 커밋마다 통째로
    쌓이는데 diff 가 안 되는 바이너리라, 히스토리를 남기면 저장소가 배포 횟수에
    비례해 커진다. 배포본은 언제나 "지금 올라가 있는 것" 하나면 된다.

.PARAMETER BuildDir
    Unity 가 WebGL 을 뱉은 폴더. `index.html` 과 `Build/` 가 그 안에 있어야 한다.

.PARAMETER Branch
    배포 브랜치. GitHub 저장소 Settings > Pages 에서 이 브랜치 / (root) 를 고른다.

.PARAMETER Remote
    푸시할 리모트.

.PARAMETER NoPush
    로컬 브랜치만 갱신하고 푸시하지 않는다. `git show --stat <branch>` 로 내용을
    먼저 확인하고 싶을 때.

.PARAMETER Force
    압축 설정 검사에 걸려도 그대로 진행한다.

.EXAMPLE
    .\Tools\deploy-webgl.ps1
    WebGLBuild/ 를 gh-pages 로 올린다.

.EXAMPLE
    .\Tools\deploy-webgl.ps1 -BuildDir D:\builds\nhnai-web -NoPush
    다른 폴더를 쓰고, 푸시는 직접 한다.
#>
[CmdletBinding()]
param(
    [string]$BuildDir = 'WebGLBuild',
    [string]$Branch   = 'gh-pages',
    [string]$Remote   = 'origin',
    [switch]$NoPush,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

function Fail($message) {
    Write-Host "[실패] $message" -ForegroundColor Red
    exit 1
}

function Note($message) {
    Write-Host "  $message" -ForegroundColor DarkGray
}

# --- 저장소와 빌드 폴더 확인 ------------------------------------------------

$repoRoot = git rev-parse --show-toplevel
if (-not $?) { Fail 'git 저장소 안에서 실행해야 한다.' }
$repoRoot = $repoRoot.Trim()

$gitDir = (git rev-parse --absolute-git-dir).Trim()

if ([System.IO.Path]::IsPathRooted($BuildDir)) {
    $buildAbs = $BuildDir
} else {
    $buildAbs = Join-Path $repoRoot $BuildDir
}

if (-not (Test-Path -LiteralPath $buildAbs -PathType Container)) {
    Fail @"
빌드 폴더가 없다: $buildAbs

Unity 에서 File > Build Settings > WebGL > Build 로 먼저 빌드하고,
출력 폴더를 저장소 루트의 WebGLBuild 로 잡는다.
"@
}

$indexHtml = Join-Path $buildAbs 'index.html'
$buildSub  = Join-Path $buildAbs 'Build'

if (-not (Test-Path -LiteralPath $indexHtml -PathType Leaf)) {
    Fail "index.html 이 없다: $indexHtml — WebGL 빌드 출력 폴더가 맞는지 확인한다."
}
if (-not (Test-Path -LiteralPath $buildSub -PathType Container)) {
    Fail "Build/ 폴더가 없다: $buildSub — WebGL 빌드 출력 폴더가 맞는지 확인한다."
}

# --- 압축 설정 검사 ---------------------------------------------------------
#
# GitHub Pages 는 Content-Encoding 헤더를 붙여 주지 못한다. 압축을 켰다면
# Decompression Fallback 도 켜져 있어야 로더가 스스로 풀어 낸다.

$settingsPath = Join-Path $repoRoot 'ProjectSettings\ProjectSettings.asset'
if (Test-Path -LiteralPath $settingsPath -PathType Leaf) {
    $settings = Get-Content -LiteralPath $settingsPath -Raw

    $compression = $null
    if ($settings -match '(?m)^\s*webGLCompressionFormat:\s*(\d+)\s*$') {
        $compression = [int]$Matches[1]
    }
    $fallback = $null
    if ($settings -match '(?m)^\s*webGLDecompressionFallback:\s*(\d+)\s*$') {
        $fallback = [int]$Matches[1]
    }
    $threads = $null
    if ($settings -match '(?m)^\s*webGLThreadsSupport:\s*(\d+)\s*$') {
        $threads = [int]$Matches[1]
    }

    # 0 = Brotli, 1 = Gzip, 2 = Disabled
    if ($null -ne $compression -and $compression -ne 2 -and $fallback -ne 1) {
        $name = 'Gzip'
        if ($compression -eq 0) { $name = 'Brotli' }
        $message = @"
압축이 $name 인데 Decompression Fallback 이 꺼져 있다.

GitHub Pages 는 Content-Encoding 응답 헤더를 설정할 수 없어서, 이대로 올리면
브라우저에서 이렇게 죽는다:

  Unable to parse Build/<이름>.framework.js.$(if ($compression -eq 0) { 'br' } else { 'gz' })!
  This can happen if build compression was enabled but web server hosting the
  content was not configured to serve the file with a '$(if ($compression -eq 0) { 'Content-Encoding: br' } else { 'Content-Encoding: gzip' })' HTTP response header.

고치는 곳:
  Player Settings > WebGL > Publishing Settings > Decompression Fallback 체크
  → 다시 빌드 → 다시 이 스크립트 실행

(설정을 이미 고쳤는데 빌드가 그 전 것이라면 다시 빌드해야 한다.
 검사만 건너뛰려면 -Force)
"@
        if ($Force) {
            Write-Host "[경고] $message" -ForegroundColor Yellow
        } else {
            Fail $message
        }
    }

    if ($threads -eq 1) {
        Write-Host @"
[경고] webGLThreadsSupport 가 켜져 있다. 스레드를 쓰려면 서버가
       Cross-Origin-Opener-Policy / Cross-Origin-Embedder-Policy 헤더를 줘야 하는데
       GitHub Pages 는 못 준다. 페이지가 안 뜨면 여기부터 끈다.
"@ -ForegroundColor Yellow
    }
}

# --- .nojekyll --------------------------------------------------------------

$noJekyll = Join-Path $buildAbs '.nojekyll'
if (-not (Test-Path -LiteralPath $noJekyll -PathType Leaf)) {
    New-Item -ItemType File -Path $noJekyll | Out-Null
}

# --- 크기 안내 --------------------------------------------------------------

$bytes = (Get-ChildItem -LiteralPath $buildAbs -Recurse -File | Measure-Object -Property Length -Sum).Sum
$mb = [math]::Round($bytes / 1MB, 1)
Write-Host "배포 대상: $buildAbs ($mb MB)" -ForegroundColor Cyan
if ($mb -gt 900) {
    Write-Host "[경고] GitHub Pages 사이트 권장 한도는 1 GB 다." -ForegroundColor Yellow
}

# --- 부모 없는 커밋 만들기 --------------------------------------------------
#
# 워킹트리를 건드리지 않으려고 임시 인덱스에 직접 쌓아 commit-tree 로 커밋을
# 만든다. 체크아웃이 없으니 지금 보고 있는 브랜치는 그대로다.
#
#   --work-tree=$buildAbs : 빌드 폴더를 트리의 뿌리로 본다. 저장소 루트의
#                           .gitignore 는 이 위에 있어서 읽히지 않는다
#   add -f                : 그래도 전역 ignore 가 남아 있을 수 있어 강제한다
#   commit-tree (부모 없음): 매번 새 뿌리 — 히스토리를 쌓지 않는다

$indexFile = Join-Path ([System.IO.Path]::GetTempPath()) ("nhnai-deploy-" + [guid]::NewGuid().ToString('N') + ".index")
$stamp = Get-Date -Format 'yyyy-MM-dd HH:mm'

Push-Location -LiteralPath $buildAbs
$env:GIT_INDEX_FILE = $indexFile
try {
    git --git-dir=$gitDir --work-tree=$buildAbs add -A -f -- .
    if (-not $?) { Fail '빌드 파일을 인덱스에 넣지 못했다.' }

    $tree = (git --git-dir=$gitDir write-tree).Trim()
    if (-not $tree) { Fail 'write-tree 가 실패했다.' }

    $commit = (git --git-dir=$gitDir commit-tree $tree -m "build(webgl): $stamp 배포").Trim()
    if (-not $commit) { Fail 'commit-tree 가 실패했다.' }
}
finally {
    Remove-Item Env:\GIT_INDEX_FILE -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $indexFile -Force -ErrorAction SilentlyContinue
    Pop-Location
}

git --git-dir=$gitDir update-ref "refs/heads/$Branch" $commit
if (-not $?) { Fail "브랜치 $Branch 를 갱신하지 못했다." }

Write-Host "브랜치 $Branch → $($commit.Substring(0,8)) (부모 없는 새 커밋)" -ForegroundColor Green

# --- 푸시 ------------------------------------------------------------------

if ($NoPush) {
    Write-Host ''
    Note "푸시는 안 했다. 내용 확인: git show --stat $Branch"
    Note "올리려면:        git push --force $Remote ${Branch}:${Branch}"
    exit 0
}

git push --force $Remote "refs/heads/${Branch}:refs/heads/${Branch}"
if (-not $?) { Fail "푸시에 실패했다. 리모트 이름($Remote)과 권한을 확인한다." }

$url = git remote get-url $Remote
Write-Host ''
Write-Host "푸시 완료." -ForegroundColor Green
Note "GitHub 저장소 Settings > Pages 에서 Source = 'Deploy from a branch',"
Note "Branch = $Branch / (root) 로 한 번만 설정하면 된다."
Note "리모트: $url"
