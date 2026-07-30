---
name: nhnai-troubleshooting
description: >
  NHNAI 에서 무언가 잘못 나올 때 원인을 찾는 스킬. 이 프로젝트에서 **실제로 겪은**
  증상 → 원인 목록과 디버깅 도구를 담는다. 화면이 새까맣다, 머티리얼이 분홍색·마젠타·회색,
  UI 가 안 보이거나 스타일이 안 먹는다, 모바일 버튼이 안 눌린다, 빌드에서만 글자가 빈칸,
  메뉴를 골랐는데 조작이 안 먹는다, WebGL 페이지가 비었다·404·오류 알림창·화면이 스크롤된다,
  세로 안내가 안 뜬다 같은 증상이면 트리거한다.
  "왜 안 돼", "안 보여", "안 눌려", "빌드 실패", "에디터에서는 되는데" 같은 말이 나오면 여기부터 본다.
---

# nhnai-troubleshooting — 증상 → 원인

CLAUDE.md 「디버깅 도구」에서 옮겨 왔다. 일반적인 팁 모음이 아니라 **이 프로젝트에서
실제로 겪은 증상** 목록이다. 겪을 때마다 한 줄씩 늘린다 — 추측으로 파기 전에 여기부터 본다.

| 문제 | 도구 |
|---|---|
| 화면이 새까맣거나 너무 밝음 | Window > Rendering > Lighting, 그리고 Volume 프로파일 |
| 머티리얼이 분홍색 | 셰이더 컴파일 실패. Console 에서 `NHNAI/LightCone` 오류부터 본다 |
| FBX 색이 마젠타 | 팔레트 UV 가 미할당 셀을 가리킨다 — 생성 스크립트의 `color=` 이름 오타 |
| FBX 가 회색 단색 | 머티리얼 리맵 실패. `NHNAI > Setup > 1` 을 다시 돌린다 |
| 멀리서 색이 번짐 | 팔레트 텍스처의 밉맵·압축이 켜졌다. `ArtMaterialLibrary` 가 끄는데 수동으로 되돌린 것 |
| 드로우 콜이 많음 | Frame Debugger |
| 프레임 저하 | Unity Profiler |
| UI 요소가 안 보임 / 스타일이 안 먹음 | Window > UI Toolkit > Debugger |
| 모바일 조작 UI 가 보이는데 안 눌림 | `InputSystem_Actions.inputactions` 의 `UI` 액션 맵. UI Toolkit 런타임이 포인터를 여기서 가져간다 |
| **빌드에서만** 버튼 글자가 안 보임 (테두리는 있는데 빈칸) | 문구에 한글이 들어갔다. 기본 폰트에 글리프가 없고 빌드에는 OS 폰트 폴백이 없다 — `unity-ui-authoring` 스킬 「폰트 준비」. 에디터에서는 재현되지 않는다 |
| 메뉴를 골랐는데 조작이 안 먹음 | `MainMenu.uss` 의 페이드 길이와 `MainMenuScreen.FadeOutMs` 가 어긋났다 |
| **WebGL** — 빌드가 `Preprocessor error "TypeError: Cannot read property 'toString' of undefined"` 로 실패 | 템플릿 `index.html` 에 값 없는 매크로가 있다. **HTML 주석도 검사 대상이다** — 전처리기는 주석을 가리지 않고 파일 전체를 정규식으로 훑는다(`BuildTools/Preprocess.js:63`). 매크로 문법을 설명하는 주석을 쓸 때 중괄호 세 겹을 그대로 적으면 그게 평가된다 |
| **WebGL** — 페이지가 비었고 콘솔에 `Unable to parse Build/*.br!` | Decompression Fallback 이 꺼진 채 빌드됐다. 켜고 **다시 빌드**한다 — 설정만 고치면 이전 빌드가 그대로 올라간다. 산출물 확장자가 `.br` 이 아니라 **`.unityweb`** 이면 fallback 이 켜진 것이다 |
| **WebGL** — 페이지에 `index.html` 만 뜨고 404 뿐 | `Build/` 가 `.gitignore` 에 걸려 빠졌다. 손으로 add 하지 말고 `Tools/deploy-webgl.ps1` 을 쓴다 |
| **WebGL** — PC 를 골랐는데 시야가 안 돌아감 | 포인터 잠금이 거부됐다. 화면을 한 번 클릭하면 되잡힌다(그 클릭은 상호작용으로 안 센다). 반복되면 `ClaimCursor` 가 클릭 핸들러 안에서 불리는지 본다 |
| **WebGL** — 플레이 도중 `An error occurred running the Unity content ... NotAllowedError: A user gesture is required to request Pointer Lock.` 알림창 | 게임이 죽은 게 아니다. 잠금 거부가 처리되지 않은 rejection 으로 새어 Unity 로더의 오류 alert 를 탄 것이다. 템플릿 `index.html` 의 `requestPointerLock` 래퍼가 빠졌는지 본다 — **알림창은 한 번만 뜨고**(`didShowErrorMessage`) 그 뒤로는 조용하니, 없어졌다고 고쳐진 게 아니다 |
| **WebGL** — 손가락을 끄니 게임 대신 페이지가 스크롤됨 | 템플릿의 `touch-action: none` / `overscroll-behavior: none` 이 빠졌다 |
| **WebGL** — 폰에서만 프레임이 안 나옴 | 템플릿의 `MaxPixelRatio` 를 낮춘다. 그 다음이 `Mobile_RPAsset` 과 SMAA 품질이다 |
| 세로로 들었는데 안내가 안 뜸 / 가로인데 안 걷힘 | `RotateGateScreen` 이 콜백을 **문서 루트**가 아니라 `gate-root` 에 걸었다. 접힌 요소에는 `GeometryChangedEvent` 가 오지 않아 한 번 숨으면 못 돌아온다 |
| 게임 UI 가 안 나타남 | `HudScreen.Begin` / `MobileControlsScreen.Begin` 이 안 불렸다 — 메뉴가 셋을 다 Bind 받았는지 본다 |

**UI 쪽 "스타일이 안 먹는다"의 1순위 원인은 컴포넌트 USS를 `GameTheme.tss`에 등록하지 않은 것이다.**

---

원인을 찾은 뒤 고칠 때 읽는 스킬:

| 어디를 고치나 | 스킬 |
|---|---|
| UXML · USS · 컴포넌트 · 폰트 | `unity-ui-authoring` |
| 씬 · 조명 · 조작 · 시작 흐름 | `unity-scene-bootstrap` |
| FBX · 팔레트 · 머티리얼 | `blender-art-pipeline` |
| WebGL 빌드 · 배포 · 템플릿 | `webgl-deploy` |
