---
name: webgl-deploy
description: >
  NHNAI 를 WebGL 로 빌드하고 GitHub Pages(gh-pages)에 올릴 때 읽는 스킬. 빌드 메뉴와
  Tools/deploy-webgl.ps1 절차, 압축·Decompression Fallback·스레드 같은 필수 빌드 설정,
  WebGL 템플릿 index.html 이 하는 일(캔버스 채우기 · touch-action · MaxPixelRatio ·
  requestPointerLock 래퍼 · 매크로), 브라우저 정책 때문에 WebGL 에서만 달라지는 전제를 다룬다.
  "웹으로 배포해줘", "WebGL 빌드", "GitHub Pages", "gh-pages", "페이지가 비어 있어",
  "브라우저에서만 안 돼", "index.html 템플릿", "폰에서 느려" 같은 요청이면 트리거한다.
---

# webgl-deploy — WebGL 빌드 · GitHub Pages 배포

CLAUDE.md 「WebGL 배포 — GitHub Pages」에서 옮겨 왔다.

```
1. Unity 메뉴  NHNAI > Build > WebGL → WebGLBuild
2. PowerShell  .\Tools\deploy-webgl.ps1
```

**빌드 출력 폴더를 손으로 고르지 않는다.** Build Settings 창으로 빌드하면 매번 폴더를
고르게 되고, 한 번 다른 곳으로 뱉으면 배포 스크립트는 그걸 모른 채 **예전 빌드를
그대로 올린다.** 메뉴(`Assets/Editor/WebGlBuild.cs`)가 출력 경로를 배포 스크립트의
기본값(`WebGLBuild/`)에 고정해 둔 이유다. 압축 설정도 빌드를 시작하기 **전에** 검사한다 —
배포 단계에서 걸리면 빌드 시간을 이미 버린 뒤다.

CLI 입구도 같은 함수를 부른다. **한 프로젝트를 두 인스턴스가 열 수 없어서 에디터가
열려 있으면 잠금에 걸린다** — 에디터를 닫고 쓴다.

```powershell
<Unity 설치 경로>\Unity.exe -quit -batchmode -logFile - `
  -projectPath (Get-Location) `
  -executeMethod NHNAI.EditorTools.WebGlBuild.BuildFromCommandLine
```

`WebGLBuild/` 는 `.gitignore` 에 있다. main 에 담지 않고 스크립트가 배포 브랜치
(`gh-pages`)로 따로 올린다 — **부모 없는 커밋 하나로 매번 덮어쓴다.** wasm 은 diff 가
안 되는 바이너리라 히스토리를 남기면 저장소가 배포 횟수에 비례해 커진다.
저장소 Settings > Pages 에서 Source = `gh-pages` / (root) 를 **한 번만** 설정한다.

**손으로 `git add` 해서 올리지 않는다.** `.gitignore` 의 `**/[Bb]uild/` 가 Unity WebGL
산출물의 핵심 폴더 이름(`Build/` — wasm·data·framework 가 전부 그 안에 있다)과 같아서
`index.html` 만 올라가고 알맹이가 빠진다. 브라우저에는 404 만 뜬다. 스크립트는
`--work-tree` 를 빌드 폴더로 잡고 `add -f` 로 이 함정을 피한다.

---

## WebGL 에서만 달라지는 설정 — **모두 서버 헤더를 못 주는 환경 때문이다**

| 항목 | 값 | 이유 |
|---|---|---|
| `webGLCompressionFormat` | `0` (Brotli) | 전송량이 가장 작다 |
| `webGLDecompressionFallback` | **`1` (필수)** | Pages 는 `Content-Encoding: br` 을 못 준다. 끄면 `Unable to parse Build/*.br!` 로 죽는다 |
| `webGLThreadsSupport` | `0` | 스레드는 COOP/COEP 헤더가 필요한데 Pages 는 못 준다 |
| `webGLTemplate` | `PROJECT:NHNAI` | 기본 템플릿은 고정 크기 캔버스 + 로고 푸터라 페이지 가운데 작은 박스로 뜬다 |
| 품질 레벨 | `0` = **Mobile** | `QualitySettings.asset` 의 `m_PerPlatformDefaultQuality: WebGL: 0`. **WebGL 이 무거우면 `PC_RPAsset` 이 아니라 `Mobile_RPAsset` 을 만진다** |

배포 스크립트가 압축·스레드 설정을 푸시 전에 검사한다. 어긋나면 올라가기 전에 막힌다.

---

## 웹 페이지 껍데기 — `Assets/WebGLTemplates/NHNAI/index.html`

이 파일이 하는 일 넷 — 캔버스를 뷰포트에 꽉 채우고, 브라우저의 터치 제스처를 막고,
렌더 해상도를 죄고, **포인터 잠금 거부를 삼킨다.** 빌드에는 안 들어간다 —
페이지를 감싸는 껍데기다.

| 만지는 곳 | 무엇이 달라지나 |
|---|---|
| `touch-action: none` · `overscroll-behavior: none` | **지우면 모바일 조작이 죽는다.** 시점 패드를 끄는 순간 페이지가 스크롤되거나 당겨서-새로고침이 걸린다 |
| `MaxPixelRatio` (기본 2) | 폰의 DPR 은 3~4 다. 올리면 선명하고 느려진다. 포스트 프로세싱을 다 켠 상태라 이 값이 프레임을 가장 크게 좌우한다 |
| `Element.prototype.requestPointerLock` 래퍼 | **지우면 플레이 도중 오류 알림창이 뜬다.** Unity 가 쓰는 emscripten 은 이 함수를 `.catch()` 없이 부르고, Chromium 은 Promise 를 돌려준다 — 잠금 거부가 처리되지 않은 rejection 으로 남고 Unity 로더가 그것을 `alert()` 로 띄운다. 게임은 멀쩡한데 오류창만 뜬다 |
| `#if` 블록과 `{{{ }}}` 매크로 | **이름을 지어내지 않는다.** Unity 6.3 의 Minimal 템플릿에서 그대로 가져온 것이고, 틀리면 빌드가 조용히 빈 URL 을 넣는다. 원본은 `<Unity>/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/WebGLTemplates/Base/Minimal` |

---

## WebGL 에서 재현되지 않는 전제 넷 — 코드 문제가 아니라 브라우저 정책이다

- **배경음이 첫 클릭 전까지 무음이다.** 「시작 흐름」(`unity-scene-bootstrap` 스킬)의
  "씬이 열리는 순간부터 틀고 있어 메뉴가 떠 있는 동안에도 울린다" 가 WebGL 에서는 안 맞는다.
  브라우저 자동재생 정책상 AudioContext 가 멈춘 채로 시작해서, PC/MOBILE 을 누른 뒤부터 들린다.
- **landscape 고정이 안 걸린다.** `defaultScreenOrientation: 3` 은 네이티브 모바일
  빌드용이고 브라우저는 보지 않는다 (Screen Orientation API 는 전체화면에서만 잠글 수
  있다). 강제할 수단이 없어서 **막고 안내한다** — `RotateGate` 층이 세로일 때 화면을
  덮는다. 세로 레이아웃을 만들지 않는다는 규칙은 그대로다.
- **커서 잠금 타이밍이 빡빡하다.** `ClaimCursor` 를 클릭 핸들러 안에서 부르는 이유는
  `unity-scene-bootstrap` 스킬의 「시작 흐름」 ⚠️ 를 본다.
- **잠금 거부가 게임 쪽에만 조용하다.** 브라우저에는 거부된 Promise 로 남아서, 템플릿의
  래퍼가 받지 않으면 Unity 로더가 alert 를 띄운다. 요청은 Unity 프레임 루프에서 나가
  emscripten 이 **다음 이벤트 핸들러까지 미뤄 두는데**, 그 핸들러가 키 이벤트일 수도
  있다 — Esc·Alt 같은 키는 브라우저가 사용자 조작으로 세지 않아 그 요청은 거부로
  끝난다. **클릭에 맞춰 요청해도 이 경로는 남는다**, 그래서 템플릿에서 삼킨다.
  포커스를 잃을 때 `PlayerInputSource.OnApplicationFocus` 가 잠금을 같이 풀어 두는
  것도 같은 이유다 — 희망 상태를 Locked 로 남기면 포커스가 돌아올 때 조작 없이
  다시 요청되고, 그건 무조건 거부된다.

---

WebGL 증상(빈 페이지 · 404 · alert · 스크롤 · 프레임 저하 · 전처리기 오류)은
`nhnai-troubleshooting` 스킬의 표에 원인과 함께 정리돼 있다.
