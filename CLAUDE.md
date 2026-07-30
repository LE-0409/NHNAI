# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

이 파일은 **매 세션 통째로 읽힌다.** 그래서 여기에는 항상 필요한 것만 둔다 — 작업별 상세
규칙은 `.claude/skills/` 로 나눠 두었고, 그 작업을 할 때 읽는다.
**규칙이 늘어나면 여기가 아니라 해당 스킬에 쓴다.** 이 파일은 200줄을 넘기지 않는다.

---

## 프로젝트 개요

NHNAI는 Unity로 만든 1인칭 3D 게임이다. 독방 하나에 슬롯머신 하나가 있다.
**게임 내용과 만든 방법은 `README.md`가 정본이다** — 여기는 작업 규칙만 다룬다.

이 저장소의 코드는 **LLM이 작성한다는 전제**로 구조가 잡혀 있다. 그래서 다음을 지킨다.

- 값은 한 곳에만 둔다 (씬 구성은 부트스트랩, 색은 팔레트 레지스트리, 배치 수식은 C#).
- 파일 하나만 읽어도 그 파일이 뭘 하는지 알 수 있게 쓴다.
- 결과를 눈으로 확인할 수 있는 경로(HTML 프로토타입 · Blender 프리뷰 렌더)를 항상 확보한다.

**정해지지 않은 것을 추측해서 코드로 만들지 않는다.** 먼저 물어본다 —
게임 규칙(확률·배당·연출 타이밍)이 특히 그렇다. 그럴듯한 값이 한 번 코드에 박히면
왜 그 값인지 아무도 모르게 된다.

### 확정된 것

| 항목 | 값 |
|---|---|
| Unity | 6000.3.10f1 (Unity 6.3) |
| 렌더 파이프라인 | URP 3D (PC · Mobile 렌더러 분리 — 렌더 설정은 **둘 다** 본다) |
| UI | UI Toolkit 기본, uGUI 예외 허용 (판단표는 `unity-ui-authoring`) |
| 입력 | Input System (`activeInputHandler: 1`) |
| 빌드 타깃 | PC (Windows/Mac) + 모바일 (Android/iOS) + WebGL (GitHub Pages) |
| 화면 방향 | **landscape 고정.** 세로 모드 미지원 |
| 기준 해상도 | 1920 x 1080 |
| 저장소 공개 | **public.** 담는 것이 곧 공개다 — 「금지 사항」의 에셋 출처·로컬 경로 항목을 본다 |

화면 방향과 기준 해상도는 문서에만 있는 규칙이 아니라 `ProjectSettings/ProjectSettings.asset`에
실제로 박혀 있다 (`allowedAutorotateToPortrait` = `0`, `defaultScreenOrientation` = `3`).
세로를 지원하게 되면 이 표와 설정을 **같이** 고친다.

---

## 작업별 스킬 — 손대기 전에 읽는다

| 하려는 일 | 스킬 |
|---|---|
| 새 화면의 HTML/CSS 프로토타입 (파이프라인 1단계) | `unity-ui-prototype` |
| UXML·USS·TSS·컴포넌트 작성, 화면 색·간격 변경, uGUI 판단, 화면 문구·폰트 | `unity-ui-authoring` |
| 씬 생성, 조명·포스트 프로세싱, 조작·입력 추가, 시작 흐름, 룩 조정 | `unity-scene-bootstrap` |
| 3D 에셋 생성 (Blender), 팔레트, 머티리얼 리맵 | `blender-art-pipeline` |
| WebGL 빌드·배포·템플릿, 브라우저 정책 | `webgl-deploy` |
| 증상이 있다 — 안 보임·안 눌림·빌드 실패·에디터에서만 됨 | `nhnai-troubleshooting` |

본문은 `.claude/skills/{이름}/SKILL.md` 다. 저장소에 포함돼 있어 클론하면 바로 쓴다.

**코드 주석·문서가 `CLAUDE.md 「…」` 로 가리키는 절은 위 스킬로 옮겼다.**
「UI Toolkit 컨벤션」·「인라인 스타일 vs USS」·「디자인 값은 화면마다 로컬 변수로 모은다」·
「컴포넌트 작성 규칙」·「USS 제약」·「폰트 준비」·「새 화면을 만들 때」·「참조 문서」 →
`unity-ui-authoring`. 「시작 흐름」·「조작」 → `unity-scene-bootstrap`.
「디버깅 도구」 → `nhnai-troubleshooting`.

---

## 저장소 지도

```
Assets/Scripts/     NHNAI.Game — 게임 로직. UI 를 참조하지 않는다
   App/ControlMode.cs           PC / 모바일 선택
   Player/PlayerInputSource.cs  **조작이 들어오는 유일한 문**
   Interaction/ · Coins/ · Slot/   상호작용 · 동전 · 슬롯머신 규칙과 연출
Assets/UI/          NHNAI.UI — 화면 4층 (Hud 0 · MobileControls 10 · MainMenu 20 · RotateGate 30)
   Theme/GameTheme.tss          컴포넌트 USS 를 @import 하는 곳. 빠뜨리면 스타일이 안 먹는다
   Components/                  VirtualJoystick · TouchLookPad
Assets/Editor/      에디터 툴 (asmdef 없음 = Assembly-CSharp-Editor)
   CellRoomBootstrap.cs         **독방 씬의 정본.** UI 세 층도 여기서 붙인다
   UiBootstrap.cs               PanelSettings 의 정본
   ArtMaterialLibrary.cs        .mat 생성 + FBX 머티리얼 리맵
   SceneBuildList.cs · WebGlBuild.cs
Assets/Scenes/CellRoom.unity    **게임의 유일한 씬.** 부트스트랩이 생성한다. 손으로 쓰지 않는다
Assets/Art/         ArtPipeline 산출물 (.fbx · .mat · palette.png). 손으로 만들지 않는다
Assets/Audio/       효과음 · 앰비언스. **소리를 추가하면 README.md 에 출처를 같은 커밋에 쓴다**
Assets/Settings/    PC_ / Mobile_ 렌더러 쌍, URP 글로벌, GamePanelSettings, CellRoomVolume
Assets/Shaders/LightCone.shader          전등 아래 빛 기둥
Assets/WebGLTemplates/NHNAI/index.html   WebGL 페이지 껍데기 (빌드에는 안 들어간다)
Assets/InputSystem_Actions.inputactions  **지우지 않는다** — UI Toolkit 이 포인터를 여기서 가져간다

ArtPipeline/        Blender 헤드리스 파이프라인 (Unity 밖, 벤더링 kit)
prototype/          HTML/CSS 프로토타입. README.md 가 USS 호환 규칙의 정본
docs/reference/unity-ui-toolkit/   UI Toolkit 참조 문서. **웹 검색 전에 여기부터 본다**
Tools/deploy-webgl.ps1             WebGL 빌드를 gh-pages 로 올린다
.githooks/commit-msg · .gitmessage  커밋 메시지 검사와 템플릿
```

### 어셈블리 의존 방향

```
NHNAI.Game  ←  NHNAI.UI  ←  Assembly-CSharp-Editor (Assets/Editor)
```

- `NHNAI.Game`은 `UnityEngine.UIElements`를 **참조하지 않는다.** 게임 로직에 UI 타입이 들어오면 안 된다.
- `NHNAI.UI`는 `NHNAI.Game`을 참조한다. 역방향은 금지다.
- `Assets/Editor/`에는 asmdef를 두지 않는다. 사전 정의 어셈블리라 모든 패키지를 참조 설정 없이 쓸 수 있다.

---

## 금지 사항

- **편의를 이유로 uGUI를 쓰지 않는다.** `unity-ui-authoring` 의 판단표에 해당할 때만 쓰고,
  쓸 때는 파일 맨 위에 사유를 남긴다. 스크린 스페이스 UI는 전부 UI Toolkit이다.
- **세로(portrait) 레이아웃을 만들지 않는다.** landscape 고정이다.
- **색·간격 리터럴을 USS 규칙 안에 흩뿌리지 않는다.** 화면 루트 클래스의
  `--{화면}-*` 변수에 모으고 주석을 붙인다.
- **`NHNAI.Game`에서 `UnityEngine.UIElements`를 참조하지 않는다.**
- **`width` / `height` / `left` / `top`을 애니메이션하지 않는다.** `translate` / `scale` / `rotate`를 쓴다.
- **`PanelSettings.asset`과 `.unity` 씬을 텍스트로 직접 쓰지 않는다.** GUID 참조가 들어간
  YAML 이라 손으로 쓰면 깨진다 — `UiBootstrap.cs` · `CellRoomBootstrap.cs` 를 고치고 메뉴를 돌린다.
- **확률·배당·연출 타이밍 같은 게임 규칙 값을 추측해서 만들지 않는다.** 먼저 물어본다.
- **게임 스크립트에서 `Keyboard.current` / `Mouse.current` / `Touchscreen`을 직접 읽지 않는다.**
  전부 `PlayerInputSource`를 거친다. 직접 읽으면 PC 에서만 되는 조작이 생긴다.
- **`Assets/Scripts/`에 만능 `Utils.cs`를 만들지 않는다.** 기능별 폴더에 자기완결 파일로 둔다.
- **프로토타입 JS에만 있는 동작을 만들지 않는다.** 정본은 항상 C#이다.
- **화면에 나가는 문구를 한글로 쓰지 않는다.** 기본 폰트에 글리프가 없어 빌드에서만
  빈칸이 된다 (`unity-ui-authoring` 「폰트 준비」). 주석·문서는 한국어 그대로다.
- **출처를 모르는 에셋을 넣지 않는다.** 이 저장소는 public 이라 담는 것이 곧 재배포다.
  받아 온 오디오·이미지는 `Assets/Audio/README.md` 처럼 출처와 라이선스를 **같은 커밋에** 적는다.
- **로컬 절대 경로를 커밋하지 않는다.** 문서·주석·스크립트 전부. 개인 경로가 필요하면
  환경변수나 저장소 밖 설정 파일로 뺀다 (`ArtPipeline` 이 `%APPDATA%` 를 쓰는 방식).

---

## 커밋 컨벤션

**Conventional Commits.** `.githooks/commit-msg`가 실제로 검사한다 — 형식이 틀리면 커밋이 막힌다.

```
<type>(<scope>): <제목>          ← 제목은 한국어, 마침표 없이, 72자 이내

<본문 — 선택. "무엇"보다 "왜">
```

- **type**: `feat` `fix` `docs` `style` `refactor` `perf` `test` `build` `ci` `chore` `revert`.
  **화면 색·간격 변경은 `style`이 아니다** — `style`은 코드 서식이다. `feat(screen)` / `fix(screen)` 을 쓴다.
- **scope**: `ui`(UI 공통) `screen`(Assets/UI/Screens) `game`(NHNAI.Game) `editor`(Assets/Editor)
  `art`(ArtPipeline·Assets/Art) `prototype` `unity`(설정·패키지) `webgl`.
  scope 를 늘리려면 이 줄과 `.gitmessage` · `.githooks/commit-msg` 를 **같이** 고친다.
- 파괴적 변경은 type 뒤에 `!` — `feat(game)!: 배당 규칙 전면 변경`
- **화면 값을 바꾼 커밋은 `prototype/{화면}.css` 와 `{화면}.uss` 를 함께 담는다.**
- `Co-Authored-By:` 같은 트레일러는 본문 맨 끝에 붙인다.
- `Merge` / `Revert` / `fixup!` / `squash!` 로 시작하는 자동 생성 메시지는 검사에서 제외된다.
  훅을 건너뛰는 `--no-verify` 는 규칙이 잘못됐을 때만 쓰고, 반복되면 훅과 이 절을 같이 고친다.
- 클론 후 1회: `git config core.hooksPath .githooks` · `git config commit.template .gitmessage`
  (`core.hooksPath`는 저장소 로컬 설정이라 버전 관리되지 않는다).
