# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## 프로젝트 개요

NHNAI는 Unity로 만든 1인칭 3D 게임이다. 독방 하나에 슬롯머신 하나가 있다.
**게임 내용과 만든 방법은 `README.md`가 정본이다** — 여기는 작업 규칙만 다룬다.

UI는 **UI Toolkit이 기본**이다. uGUI는 UI Toolkit으로 되지 않는 기능에 한해서만 쓴다
(아래 「UI — 무엇으로 만드나」 참조. 지금까지 uGUI를 쓴 곳은 없다).

이 저장소의 코드는 **LLM이 작성한다는 전제**로 구조가 잡혀 있다.
그래서 다음을 지킨다.

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
| 렌더 파이프라인 | URP 3D (PC · Mobile 렌더러 분리) |
| UI | UI Toolkit 기본, uGUI 예외 허용 |
| 입력 | Input System (`activeInputHandler: 1`) |
| 빌드 타깃 | PC (Windows/Mac) + 모바일 (Android/iOS) + WebGL (GitHub Pages) |
| 화면 방향 | **landscape 고정.** 세로 모드 미지원 |
| 기준 해상도 | 1920 x 1080 |
| 저장소 공개 | **public.** 담는 것이 곧 공개다 — 「금지 사항」의 에셋 출처·로컬 경로 항목을 본다 |

화면 방향과 기준 해상도는 문서에만 있는 규칙이 아니라 `ProjectSettings/ProjectSettings.asset`에
실제로 박혀 있다 — `allowedAutorotateToPortrait`/`PortraitUpsideDown`이 `0`,
landscape 둘만 `1`이고 `defaultScreenOrientation`이 `3`(LandscapeLeft)이라
자동 회전조차 아니다. 세로를 지원하게 되면 이 표와 설정을 **같이** 고친다.

### 디자인 값은 화면마다 로컬 변수로 모은다

전역 디자인 토큰 파일(`tokens.uss` 같은 것)은 **두지 않았다.** 화면이 네 개뿐이라
각 화면을 파일 하나로 자기완결하게 읽는 쪽을 택했다.

화면 USS 는 리터럴을 규칙 안에 흩뿌리지 않고 **루트 클래스의 `--{화면}-*` 로컬 변수**
한곳에 모으고, 변수마다 무엇을 조절하는지 주석을 붙인다.
`Hud.uss` · `MainMenu.uss` · `MobileControls.uss` · `RotateGate.uss` 가 전부 이 방식이다.

⚠️ **이 선택의 비용을 알고 있어야 한다.** 흰색 `rgb(226, 226, 226)` 은 네 화면
**전부**에, 보조 회색 `rgb(198, 198, 202)` 은 두 화면에 값째로 반복된다. 지금은
`/* HUD 조준점과 같은 흰색 */` 같은 주석이 서로를 가리켜 연결을 대신하고 있다.

**그래서 이 두 색을 바꿀 때는 네 화면(+ 짝이 되는 `prototype/*.css`)을 다 고쳐야 한다.**
화면이 더 늘어 반복이 관리되지 않으면 그때 공용 토큰 파일을 뺀다 — 그 전에 미리
만들지 않는다.

---

## UI — 무엇으로 만드나

**기본은 UI Toolkit이다.** uGUI는 *UI Toolkit으로 되지 않는 기능*에 한해 쓴다.
"익숙해서" / "예제가 uGUI라서" / "빨라서"는 사유가 아니다.

### uGUI를 써도 되는 경우

| 경우 | 이유 |
|---|---|
| **월드 스페이스 UI** — 3D 오브젝트에 붙는 체력바·이름표·말풍선·조작 패널 | UI Toolkit의 world-space UIDocument 는 Unity 6.3에서도 제약이 크다. uGUI의 World Space Canvas 가 확실하다 |
| UI 위에 **파티클·VFX**를 섞어 그려야 할 때 | UI Toolkit 패널은 파티클 렌더러와 같은 레이어에 못 낀다 |
| **3D 카메라 이펙트와 정렬**되어야 하는 오버레이 (Screen Space - Camera) | UI Toolkit 패널은 카메라 스택 중간에 끼우기 어렵다 |
| 사각형이 아닌 **복잡한 마스킹** (Mask / RectMask2D) | USS 는 `overflow: hidden` 뿐이다 |
| 도입한 **에셋스토어 UI 패키지**가 uGUI 기반일 때 | 다시 만드는 비용이 더 크면 그대로 쓴다 |

**HUD·메뉴·설정·인벤토리·다이얼로그 같은 스크린 스페이스 UI는 전부 UI Toolkit이다.**
위 표에 없으면 UI Toolkit으로 만든다. 표에 추가하고 싶으면 먼저 물어본다.

### uGUI를 쓸 때 지키는 것

- **파일 맨 위에 사유를 한 줄 남긴다.** `// uGUI 사용: 월드 스페이스 체력바 — UI Toolkit world-space 제약`
- **격리한다.** uGUI Canvas 하나가 UI Toolkit 패널 영역까지 넘어오지 않게 한다.
- **섞어 겹치지 않는다.** UIDocument(PanelSettings 의 sort order)와 uGUI Canvas(`sortingOrder`)는
  렌더 경로가 달라 겹치면 순서 제어가 까다롭다. 화면 영역을 나눠 쓴다.
- 텍스트는 uGUI 쪽에서만 TextMeshPro 를 쓴다. UI Toolkit 쪽에 끌고 오지 않는다.

---

## 디렉터리 구조

```
NHNAI/
├── README.md                  ← 저장소 얼굴. 게임 소개 · 만든 방법 · AI 활용 방식
├── CLAUDE.md                  ← 이 파일. AI 작업 규칙의 정본
│
├── .githooks/commit-msg       ← 커밋 메시지 검사. .gitmessage 와 쌍
├── .gitmessage                ← 커밋 템플릿
│
├── .claude/
│   └── skills/
│       └── unity-ui-prototype/    ← 프로토타입 제작 스킬. 저장소에 포함 — 클론하면 바로 쓴다
│
├── docs/
│   └── reference/
│       └── unity-ui-toolkit/  ← UI Toolkit 참조 문서 (README.md 가 목록)
│
├── prototype/                 ← HTML/CSS 프로토타입 (Unity 밖, 빌드에 포함 안 됨)
│   ├── README.md              ← USS 호환 규칙 · 치환표 · 짝 파일 규약
│   ├── main-menu.html + .css
│   ├── mobile-controls.html + .css
│   └── rotate-gate.html + .css
│
├── Tools/
│   └── deploy-webgl.ps1       ← WebGL 빌드를 GitHub Pages 브랜치로 올린다
│
├── ArtPipeline/               ← Blender 에셋 파이프라인 (Unity 밖)
│   ├── lib/lowpoly_lib/       ← 메시 빌더 · 팔레트 · 익스포트 · 프리뷰
│   ├── project/palette_registry.py  ← 이 프로젝트의 색. 선언 순서 = UV 셀 인덱스
│   └── assets/{에셋}/generate_{에셋}.py
│
└── Assets/
    ├── Art/                   ← ArtPipeline 산출물. 손으로 만들지 않는다
    │   ├── Environment/*.fbx  ← CellRoom · SlotMachine · CeilingLamp · LightCone
    │   ├── Materials/*.mat    ← ArtMaterialLibrary 가 생성
    │   └── Palette/palette.png
    │
    ├── Audio/                  ← 효과음 · 배경 앰비언스 (전부 Pixabay)
    │   ├── *.mp3
    │   └── README.md           ← 출처·라이선스. **소리를 추가하면 여기 한 줄 같이 쓴다**
    │
    ├── Scripts/               ← 어셈블리 NHNAI.Game — UI 에 의존하지 않는 게임 코드
    │   ├── NHNAI.Game.asmdef
    │   ├── App/ControlMode.cs      ← PC / 모바일 선택. 씬을 넘어 사는 정적 값
    │   ├── Player/                 ← 입력 · 이동 · 시점 · 상호작용 · 동전 손과 인벤토리
    │   │   └── PlayerInputSource.cs   ← **조작이 들어오는 유일한 문**
    │   ├── Interaction/Interactable.cs
    │   ├── Coins/Coin.cs
    │   └── Slot/                   ← 슬롯머신 규칙 · 릴 · 레버 · 배출기 · 전원 · 연출
    │
    ├── UI/                    ← 어셈블리 NHNAI.UI (NHNAI.Game 을 참조. 역방향 금지)
    │   ├── NHNAI.UI.asmdef
    │   ├── Theme/GameTheme.tss     ← 컴포넌트 USS 를 @import 하는 곳
    │   ├── Components/             ← 재사용 커스텀 컨트롤 (.cs + .uss)
    │   │   ├── VirtualJoystick/    ← 모바일 이동
    │   │   └── TouchLookPad/       ← 모바일 시점
    │   └── Screens/           ← 넷 다 CellRoom 씬 위에 겹쳐 뜬다 (sortingOrder 순)
    │       ├── Hud/                ← 조준점 · 동전 개수 (0)
    │       ├── MobileControls/     ← 조이스틱 · 버튼 (10). PC 를 고르면 접힌다
    │       ├── MainMenu/           ← 제목 · PC / MOBILE 선택 (20). 고르면 페이드 아웃
    │       └── RotateGate/         ← 세로로 들면 덮는 안내 (30). 가로면 display:none
    │
    ├── WebGLTemplates/NHNAI/  ← WebGL 페이지. 캔버스가 뷰포트를 꽉 채운다
    │                             빌드에는 안 들어간다 — 페이지를 감싸는 껍데기다
    │
    ├── Editor/                ← 에디터 툴 (asmdef 없음 = Assembly-CSharp-Editor)
    │   ├── ArtMaterialLibrary.cs  ← .mat 생성 + FBX 머티리얼 리맵
    │   ├── UiBootstrap.cs         ← PanelSettings 의 정본
    │   ├── SceneBuildList.cs      ← 빌드 씬 목록. 순서(0번 = 시작 씬)를 여기서 정한다
    │   └── CellRoomBootstrap.cs   ← 독방 씬의 정본. UI 세 층도 여기서 붙인다
    │
    ├── Shaders/LightCone.shader   ← 전등 아래 빛 기둥
    │
    ├── Scenes/
    │   └── CellRoom.unity     ← 빌드 씬 목록 0번. **게임의 유일한 씬**
    │                             부트스트랩이 생성한다. 손으로 쓰지 않는다
    │
    ├── Settings/              ← PC_ / Mobile_ 렌더러 쌍, URP 글로벌 설정
    │   ├── UI/GamePanelSettings.asset  ← UiBootstrap 이 생성
    │   └── CellRoomVolume.asset   ← 포스트 프로세싱. 부트스트랩이 생성
    │
    └── InputSystem_Actions.inputactions
```

⚠️ `InputSystem_Actions.inputactions` 는 **지우면 안 된다.** 게임 코드는 이 에셋을
쓰지 않지만(`PlayerInputSource` 가 저수준 API 를 직접 읽는다), UI Toolkit 런타임이
포인터 입력을 이 에셋의 `UI` 액션 맵에서 가져간다. 지우면 모바일 조작 UI 가
손가락을 못 받는다 — 화면은 그려지는데 아무 반응이 없어 원인을 찾기 어렵다.

`Assets/Settings/`의 `PC_RPAsset` · `Mobile_RPAsset`은 품질 레벨과 짝지어져 있다.
렌더 설정을 바꿀 때 **둘 다** 봐야 한다 — 한쪽만 고치면 플랫폼에 따라 화면이 달라진다.

### 어셈블리 의존 방향

```
NHNAI.Game  ←  NHNAI.UI  ←  Assembly-CSharp-Editor (Assets/Editor)
```

- `NHNAI.Game`은 `UnityEngine.UIElements`를 **참조하지 않는다.** 게임 로직에 UI 타입이 들어오면 안 된다.
- `NHNAI.UI`는 `NHNAI.Game`을 참조한다. 역방향은 금지다.
- `Assets/Editor/`에는 asmdef를 두지 않는다. 사전 정의 어셈블리라 모든 패키지를 참조 설정 없이 쓸 수 있다.

---

## 개발 파이프라인

### 새 화면을 만들 때

```
1. prototype/{화면}.html + .css 작성      ← 브라우저에서 레이아웃 확정
        │  prototype/README.md 의 USS 호환 규칙을 지킨다
        ▼
2. Assets/UI/Screens/{화면}/{화면}.uxml + .uss 로 변환
        │  치환표는 prototype/README.md
        │  ⚠️ --{화면}-* 변수는 **이름과 값을 프로토타입과 똑같이** 옮긴다
        ▼
3. 재사용할 부분을 Assets/UI/Components/{이름}/ 커스텀 컨트롤로 뽑는다
        │  구조는 C# 생성자, 스타일은 .uss
        ▼
4. 컴포넌트 .uss 를 Assets/UI/Theme/GameTheme.tss 에 @import 등록
        │  ← 빠뜨리면 스타일이 안 먹는다. 가장 흔한 실수다
        ▼
5. {화면}.cs (MonoBehaviour) 로 요소를 잡고 이벤트를 붙인다
        ▼
6. Assets/Editor/UiBootstrap.cs 에 씬 생성 메뉴를 추가한다
        ▼
7. Unity 에서 Play 해 브라우저와 비교한다
```

### 프로토타입을 건너뛰어도 되는 경우

- 기존 컴포넌트의 색·간격 조정 → USS 직접 수정
- 라벨 문구·아이콘 교체 → UXML 직접 수정
- 새 화면, 3겹 이상 레이어, 시안 비교 → **반드시 프로토타입을 거친다**

### 화면 값을 바꿀 때

짝 파일 **둘을 항상 같이** 고친다. 하나만 고치면 브라우저에서 본 것과 게임 화면이
달라지고, 프로토타입이 거짓말을 시작한다.

```
prototype/{화면}.css  ←→  Assets/UI/Screens/{화면}/{화면}.uss
```

둘은 같은 `--{화면}-*` 변수 이름과 같은 값을 쓴다. 프로토타입이 없는 화면(`Hud`)은
USS 만 고친다.

### 왜 `.asset` / `.unity` 를 코드로 만드나

`.uss` `.uxml` `.tss` `.asmdef`는 전부 텍스트라 직접 쓸 수 있다.
`PanelSettings.asset`과 `.unity` 씬은 GUID 참조가 들어간 YAML이라 손으로 쓰면 깨진다.
그래서 `Assets/Editor/UiBootstrap.cs`가 만든다.
**이 두 종류를 직접 텍스트로 쓰려고 시도하지 않는다.**

---

## UI Toolkit 컨벤션

### 네이밍 — BEM

```
block-name__element-name--modifier-name
```

- 소문자 kebab-case. 라틴 문자·숫자·대시만.
- 의미 기반으로 짓는다. `button--quit` (O) / `button--red` (X)
- 상태는 수정자로. `panel--selected` (O) / `panel--purple-border` (X)

```uss
.panel { }                     /* 블록 */
.panel__title { }              /* 요소 */
.panel--selected { }           /* 수정자 */
```

유틸 클래스만 예외로 접두사를 쓴다: `.t-*` (타이포) `.u-*` (레이아웃).

### 값은 루트 클래스 변수에 모은다

```uss
/* O — 화면 루트에 모아 두고 규칙은 var() 로 참조한다 */
.main-menu {
    /* 메뉴가 방을 덮는 정도 */
    --menu-scrim: rgba(4, 4, 6, 0.62);
    background-color: var(--menu-scrim);
}

/* X — 리터럴을 규칙 안에 흩뿌림. 색을 바꿀 때 어디를 고쳐야 할지 알 수 없다 */
.main-menu { background-color: rgba(4, 4, 6, 0.62); }
```

변수마다 **무엇을 조절하는 값인지 주석을 한 줄** 붙인다. 그 주석이 없으면 다음에
읽는 사람이 값을 만지기를 두려워한다.

프로토타입이 있는 화면은 `prototype/{화면}.css` 가 **같은 이름·같은 값**을 쓴다.
한쪽만 고치지 않는다.

### `var()`를 쓰지 않는 예외

`transition-duration` / `transition-timing-function`에는 **리터럴을 쓴다.**

```uss
/* O */
transition-duration: 200ms;
transition-timing-function: ease-out-cubic;

/* 쓰지 않는다 */
transition-duration: var(--motion-base);
```

이유: `var()` 해석이 색·길이에서는 확실하지만 transition 계열에서는 Unity 버전에 따라
불안정한 사례가 있다. 애니메이션이 조용히 죽는 것보다 값을 두 번 적는 쪽이 낫다.

### 애니메이션 — 레이아웃을 건드리지 않는다

```csharp
// O — GPU 처리, 레이아웃 재계산 없음
element.style.translate = new Translate(x, y);
element.style.scale = new Scale(new Vector2(1.06f, 1.06f));
element.style.rotate = new Rotate(Angle.Degrees(12f));

// X — 레이아웃 재계산 발생
element.style.left = x;
element.style.width = w;
```

자주 움직이는 요소에는 생성자에서 `usageHints`를 준다.

```csharp
usageHints = UsageHints.DynamicTransform;   // 개별 요소가 자주 이동
container.usageHints = UsageHints.GroupTransform;   // 그룹 전체가 이동
```

### 인라인 스타일 vs USS — 역할 분담

| 담당 | 쓰는 것 |
|---|---|
| USS | 정적 외형, `:hover` / `:active` / 상태 수정자에 따른 색·scale |
| C# 인라인 | 계산으로 정해지는 `translate` / `rotate` |

**인라인이 USS를 이긴다.** 그래서 C#이 인라인으로 쓰는 속성을 USS에서 또 건드리면
USS 쪽이 조용히 무시된다. 배치 로직이 `translate`/`rotate`를 쓴다면 그 컴포넌트의
USS는 둘을 정의하지 않고 `scale`만 다룬다.

### 가시성 토글

| 방법 | 언제 |
|---|---|
| `style.display = DisplayStyle.None` | **잦은 토글 (기본 선택)** |
| `visible = false` | 자리는 남기고 숨길 때 |
| `opacity = 0` | 페이드 트랜지션 중일 때만 |
| `RemoveFromHierarchy()` | 거의 안 쓰는 요소 |

### 셀렉터 성능

```uss
/* O */
.panel__title { }
.menu > .menu__item { }

/* X */
* { }
.container .panel .item Label { }
```

### 요소 캐싱

```csharp
void OnEnable()
{
    var root = GetComponent<UIDocument>().rootVisualElement;
    _scoreLabel = root.Q<Label>("score");   // 한 번만 조회해 필드에 보관
}
```

`Update()`에서 `Q<T>()`를 호출하지 않는다.

### 콜백 해제

`OnEnable`에서 등록한 콜백은 **반드시** `OnDisable`에서 해제한다. 안 하면 누수된다.

---

## 컴포넌트 작성 규칙

### element-first — 구조를 C# 생성자에서 만든다

```csharp
[UxmlElement]
public partial class Badge : VisualElement
{
    readonly Label _titleLabel;
    string _title = string.Empty;

    public Badge()
    {
        AddToClassList("badge");
        _titleLabel = new Label { name = "title", pickingMode = PickingMode.Ignore };
        _titleLabel.AddToClassList("badge__title");
        Add(_titleLabel);
    }

    [UxmlAttribute("title")]
    public string Title
    {
        get => _title;
        set { _title = value ?? string.Empty; _titleLabel.text = _title; }
    }
}
```

**UXML + `CloneTree()` 방식을 쓰지 않는 이유**: 구조·상태·동작이 한 파일에 모여 있어야
LLM이 파일 하나만 읽고 수정할 수 있다. UXML로 쪼개면 구조와 로직이 갈라지고,
`Q<Label>("title")` 조회 실패가 런타임까지 안 잡힌다.

컴포넌트에서 지키는 것:

- `[UxmlElement]`를 붙이려면 클래스가 `public partial`이어야 한다.
- `VisualElement`에는 `Awake`/`OnEnable`이 없다. **초기화는 생성자**에서 한다.
- 자식은 `pickingMode = PickingMode.Ignore`로 두고 히트 판정은 루트가 받는다.
- 상태 전환은 `EnableInClassList()`로 한다. `AddToClassList`/`RemoveFromClassList` 분기를 쓰지 않는다.
- `BaseField<T>` 계열에서 값을 바꿀 때는 `SetValueWithoutNotify()`로 무한 루프를 막는다.

### 컴포넌트 USS는 TSS에 등록한다

새 컴포넌트를 만들면 `Assets/UI/Theme/GameTheme.tss`에 한 줄 추가한다.

```
@import url("project://database/Assets/UI/Components/{이름}/{이름}.uss");
```

화면(Screens) 전용 USS는 TSS에 넣지 않는다. 화면 UXML의 `<Style src="..." />`로 붙인다.

### 커스텀 컨트롤을 UXML에서 쓸 때

```xml
<engine:UXML xmlns:engine="UnityEngine.UIElements" xmlns:nhn="NHNAI.UI">
    <nhn:Badge title="일격" />
</engine:UXML>
```

---

## USS 제약 — 자주 걸리는 것

| CSS에 있는데 USS에 없는 것 | 대응 |
|---|---|
| `display: grid` / `block` | Flexbox만 |
| `position: fixed` / `sticky` | `relative` / `absolute`만 |
| `z-index` | 트리 순서. `BringToFront()` / `PlaceInFront()` |
| `gap` | `margin` (`.u-gap-*` 유틸) |
| `box-shadow` | 그림자 역할의 형제 요소 |
| `line-height` | `-unity-paragraph-spacing` 또는 `margin` |
| `@media` | `GeometryChangedEvent` → 루트 클래스 토글 |
| `@keyframes` | `transition`, 복잡하면 C# 코루틴 |
| `::before` / `::after` | 실제 자식 요소 |
| `:nth-child()` / `:not()` | 명시적 클래스 |
| `calc()` / `clamp()` | 고정값 또는 `flex-grow` |
| `linear-gradient` | 단색 |
| `overflow: auto` | `overflow: hidden` 또는 `ScrollView` |
| `hsl()` | `rgb()` |
| 숫자 `font-weight` | 웨이트별 FontAsset + `-unity-font-definition` |

`filter()`는 Unity 6.3부터 지원되지만 셰이더와 `FilterFunctionDefinition` 자산이 필요하다.
기본 UI에는 쓰지 않는다.

전체 표는 `docs/reference/unity-ui-toolkit/css-to-uss-support.md` 참조.

---

## 폰트 준비

**전용 폰트 에셋을 두지 않았다.** `GamePanelSettings.asset` 에 FontAsset 을 지정하지
않아서 UI Toolkit 이 Unity 기본 런타임 폰트로 그린다. 화면 문구가 짧고 전부 영어
대문자라 기본 폰트로 충분했다.

폰트를 도입하게 되면 웨이트마다 FontAsset 을 따로 만들고 `-unity-font-definition` 으로
지정한다 (`uss-exclusive-properties.md` 참조). USS 에 숫자 `font-weight` 는 없다.

### ⚠️ 화면에 보이는 글자는 **영어로 쓴다**

`GamePanelSettings.asset` 에 FontAsset 을 지정하지 않은 상태라, UI Toolkit 은 기본
폰트로 그린다 — **라틴 문자만 들어 있다.** 에디터에서는 OS 폰트가 뒤를 받쳐 줘서
한글이 그대로 보이지만, **네이티브·WebGL 빌드에는 그 폴백이 없다.** 오류도 로그도
없이 글자만 사라져서, 버튼이 빈 테두리로 남는다 (모바일 조작 버튼에서 실제로 겪었다).

그래서 UXML·C# 이 화면에 내보내는 문구는 전부 영어다 (`USE` · `STORE` · `TAKE` ·
`DRAG TO LOOK` · `ROTATE YOUR DEVICE`). 주석과 문서는 한국어 그대로 둔다 — 화면에
안 나가는 글자는 폰트와 무관하다.

한글 문구가 필요해지면 **먼저** 한글 글리프가 있는 FontAsset 을 만들어
`UiBootstrap` 이 PanelSettings 에 물리게 하고, 그 다음에 문구를 넣는다.
순서를 바꾸면 빌드에서만 빈칸이 되는데 에디터에서는 재현되지 않는다.

---

## 3D 에셋 — Blender 파이프라인

3D 에셋은 스토어 에셋이나 외부 AI 서비스를 쓰지 않고 **Blender를 헤드리스로 돌려 코드로 생성**한다.
파이프라인은 `ArtPipeline/`에 있다. 게임을 열어보기만 한다면 Blender는 필요 없다.

### 환경 세팅 — **로컬 설정이다. 커밋 대상이 아니다**

Blender 설치 위치는 개발자마다 다르다(드라이브 용량·기존 설치 여부). 그래서 선택은
**저장소 밖** `%APPDATA%\BlenderArtKit\config.json`에 저장되고, 같은 파이프라인을 쓰는
다른 저장소와도 공유된다.

무엇을 하든 **먼저 상태를 본다.** 아무것도 바꾸지 않고 지금 무엇이 쓰이는지만 출력한다.

```powershell
cd ArtPipeline
.\setup.ps1 -Status
```

| 요청 | 명령 |
|---|---|
| 설치해줘 | `.\setup.ps1` |
| 특정 위치에 설치해줘 | `.\setup.ps1 -InstallDir <사용자가 말한 경로>` |
| 이미 깔린 Blender 쓰게 해줘 | `.\setup.ps1 -BlenderExe <그 blender.exe 경로>` |
| 위치 바꿔줘 | 같은 명령을 다시 실행 (이미 받아둔 것은 다시 받지 않고 옮긴다) |
| 설정 되돌려줘 | `.\setup.ps1 -Reset` |

⚠️ **경로를 저장소 파일에 하드코딩하지 않는다.** `blender_common.ps1`·`run_blender.ps1`·
`setup.ps1`은 공용 kit이라 모든 개발자가 공유한다. 개인 경로를 여기 박으면 남의 클론이 깨지고,
무엇보다 로컬 설정이 커밋에 남는다. 위치 관련 요청은 **전부 위 명령으로 처리**한다 —
`setup.ps1`을 돌린 결과로 git 워킹트리가 변하면 그건 비정상이다.

### 에셋 생성

```powershell
.\run_blender.ps1 assets\<에셋>\generate_<에셋>.py
```

생성 스크립트는 `ArtPipeline/assets/<에셋>/` 깊이에 둔다(그래야 라이브러리 임포트 경로가 맞는다).
결과 FBX는 `Assets/Art/` 밑으로, 확인용 턴어라운드 렌더는 `ArtPipeline/previews/`로 나온다.
**렌더를 눈으로 확인하고 다음 단계로 간다** — 이게 이 파이프라인의 핵심 루프다.

- 색은 `ArtPipeline/project/palette_registry.py`가 정본이다. **선언 순서 = 팔레트 셀 인덱스**라
  순서를 바꾸거나 중간에 끼워 넣으면 이미 익스포트된 에셋의 UV가 전부 어긋난다. 추가는 맨 뒤에만.
- 생성 스크립트 하나가 FBX 여러 개를 다시 export하고, FBX는 헤더 타임스탬프와 오브젝트 UID가
  매번 새로 생성돼 **손대지 않은 파일까지 `M`으로 뜬다.** 커밋 전에 실제로 바꾼 것만 남기고
  나머지는 `git checkout --`으로 되돌린다.
- Unity 쪽 연동(팔레트 텍스처 설정 → `.mat` 생성 → FBX 머티리얼 리맵)은
  `Assets/Editor/ArtMaterialLibrary.cs` 가 한다. **자동 임포트 후처리가 아니라 메뉴다** —
  `NHNAI > Setup > 1` 을 눌러야 돈다. 새 FBX 를 넣으면 회색 단색으로 보이는데,
  그건 리맵을 아직 안 돌린 것이다.

### 문서

`ArtPipeline/` 은 다른 저장소에서 가져온 **벤더링 kit** 이다. 아래 셋은 kit 자체의
문서라서 이 프로젝트가 쓰지 않는 기능(리깅·애니메이션·kit 설치)도 설명한다 —
이 저장소가 실제로 쓰는 것은 메시 빌더 · 팔레트 · 익스포트 · 프리뷰 네 모듈이다.

| 주제 | 파일 |
|---|---|
| 파이프라인 설치·사용 절차 | `ArtPipeline/NEW-PROJECT.md` |
| 경계·설정 스키마·팔레트 계약 | `ArtPipeline/KIT.md` |
| 이 kit이 어디서 왔는지 | `ArtPipeline/KIT-ORIGIN.txt` |

---

## 실행 · 검증

### 씬 생성 메뉴

```
NHNAI > Setup  > 1. 아트 머티리얼 생성 · 갱신   ← 머티리얼만 다시 만든다
NHNAI > Setup  > 2. PanelSettings 생성 · 갱신   ← UI Toolkit 런타임 기반
NHNAI > Scenes > 독방 (CellRoom)                ← 위 둘과 UI 세 층을 포함해 통째로 만든다
NHNAI > Build  > WebGL → WebGLBuild             ← 배포 스크립트가 보는 폴더로 뱉는다
```

씬을 만들면 `EditorBuildSettings`에 **자동으로 등록**되고, 파일이 사라진 씬은
목록에서 정리된다 (`SceneBuildList`). `ProjectSettings/EditorBuildSettings.asset`은
손으로 고치지 않는다. **`CellRoom`이 0번**이다 — 빌드된 게임은 목록의 첫 씬으로 열린다.

### 시작 흐름 — 메인메뉴는 씬이 아니라 층이다

**씬은 `CellRoom` 하나뿐이다.** 메인메뉴는 그 위에 겹치는 UIDocument(`sortingOrder: 20`)다.
(그 위에 하나 더 있다 — 세로로 들면 `RotateGate`(30)가 메뉴까지 덮는다. 세로로 고른 뒤
그대로 시작하면 조작 UI 가 안 맞는 자리에 놓인 채 첫 화면을 맞기 때문이다.)
씬을 나누지 않은 이유:

- **배경음이 끊기지 않는다.** `BuildAmbience()`가 씬이 열리는 순간부터 틀고 있어서
  메뉴가 떠 있는 동안에도 그대로 울린다. 씬을 나누면 전환에서 한 번 끊긴다.
- **뒤로 방이 비친다.** 메뉴 배경은 불투명한 판이 아니라 스크림(`--menu-scrim`)이다.
  고르는 순간 이 층만 걷혀 방이 드러나므로 "화면이 바뀌었다"가 아니라
  "메뉴가 걷혔다"로 읽힌다.
- 씬 하나면 조작 방식을 씬 너머로 들고 다닐 필요가 없다. 정적 보관소 없이
  `ControlMode`를 인자로 넘긴다.

```
씬 열림 ─ 방·조명·배경음 살아 있음. 메뉴 층이 그 위를 덮음
   │      PlayerInputSource 는 아직 아무것도 내보내지 않는다 (_running = false)
   ▼
PC / MOBILE 클릭
   │  ① 메뉴 층에 --hidden → 420ms 페이드 아웃 (SetEnabled(false) 로 입력도 끊는다)
   │  ② 같은 순간 HudScreen.Begin(mode) · MobileControlsScreen.Begin(mode)
   │     → 두 층이 520ms 페이드 인. 메뉴가 걷히는 동안 겹쳐 떠오른다
   │  ③ 같은 순간 PlayerInputSource.ClaimCursor(mode) ─ 커서 잠금**만** 한다
   │     조작은 아직 안 산다. 이것만 클릭 핸들러 안에 있는 이유는 아래 ⚠️
   ▼ (420ms 뒤)
메뉴 층 display:none · PlayerInputSource.Begin(mode) ─ 여기서부터 조작이 산다
```

⚠️ **페이드 길이가 두 곳에 있다.** `MainMenu.uss`의 `transition-duration: 420ms`와
`MainMenuScreen.FadeOutMs`. USS 는 그림을 그리고 C# 은 그 뒤에 무엇을 할지를 정한다 —
어긋나면 아직 보이는 채로 접히거나(짧음), 투명해진 메뉴가 남아 첫 조작을 먹는다(김).

⚠️ **커서 잠금(`ClaimCursor`)을 `Begin` 안으로 되돌리지 않는다.** 둘을 합치면 코드는
짧아지지만 WebGL 에서 깨진다 — 브라우저는 포인터 잠금을 **사용자 조작 직후에만**
허용하는데 `Begin` 은 페이드가 끝난 420ms 뒤에 불린다. 거부는 예외도 로그도 없이
조용해서, PC 를 골랐는데 시야만 안 도는 상태로 나타난다. 클릭에 가장 가까운 시점에
요청하려고 나눠 둔 것이다.

모드에 따라 달라지는 것은 셋뿐이다.

| | PC | 모바일 |
|---|---|---|
| 커서 | 잠근다 (Esc 로 풀기) | 잠그지 않는다 — UI 를 눌러야 한다 |
| `MobileControls` 층 | `display: none` 으로 접힌다 | 페이드 인 |
| HUD 동전 개수 | 우측 **하단** | 우측 **상단** (`.hud--mobile`) — 하단은 버튼 자리다 |

에디터에서도 MOBILE 을 눌러 마우스로 터치 조작을 테스트할 수 있다.

### 조작

| 동작 | PC | 모바일 |
|---|---|---|
| 시야 | 마우스. **좌우는 몸통(`Player`)이, 상하는 카메라가** 돈다 | 화면 오른쪽 영역을 끈다. **쓴 방향으로 시야가 따라간다** (마우스와 같다) |
| 걷기 | WASD · 방향키. 몸통 정면 기준 | 왼쪽 아래 조이스틱 |
| 사용 — 조준한 것과 상호작용 / 들고 있으면 놓기 | 좌클릭 | 오른쪽 아래 큰 버튼 (`USE`) |
| 넣기 — 들고 있는 동전을 인벤토리로 | E | `STORE` 버튼 |
| 꺼내기 — 인벤토리에서 1개 꺼내 들기 (들고 있으면 그건 바닥에 버린다) | Q | `TAKE` 버튼 |
| 커서 해제 | Esc | — |

**게임에 들어가면 메인메뉴로 돌아가는 길이 없다.** 양쪽 다 그렇다 — 다시 고르려면
실행을 껐다 켠다. 나중에 일시정지 화면을 만들면 그때 붙인다.

**입력을 추가할 때는 `PlayerInputSource`에 먼저 넣는다.** 게임 스크립트가
`Keyboard.current` / `Mouse.current` 를 직접 읽으면 PC 에서만 되는 조작이 생기고,
"커서가 잠긴 동안만 받는다" 같은 규칙이 복제된다 — 복제되면 한 곳만 고쳐진 채 남는다.
모바일 쪽 값은 `NHNAI.UI`의 조작 화면이 `PressXxx()` / `SetMoveAxis()` 로 밀어 넣는다.

조준점은 **항상 떠 있고**, 쓸 수 있는 것을 보면 **커지고 또렷해진다.** 강조가 안 되면
거리가 `PlayerInteractor.reach` 를 넘었거나 그 오브젝트에 `Collider` 가 없는 것이다 —
`Interactable` 과 `Collider` 는 **같은 오브젝트**에 있어야 한다.

조준용 Collider 는 **보이는 모양대로 감싸지 않는다.** 레버 팔처럼 가는 것은 실루엣대로
잡으면 조준이 바늘구멍이 된다. 노리기 쉬운 크기의 캡슐·박스를 씌운다.

⚠️ **시점 높이는 `CellRoomBootstrap.EyeHeight` 로 바꾼다.** `CharacterController` 의
`Center` 를 올려도 시점이 내려가지만, 그건 캡슐을 통째로 들어 올려 Transform 원점을
바닥 아래로 잠기게 하는 것이라 **원점 = 발밑**이라는 전제가 깨진다. 당장은 티가 안 나도
발소리·스폰·바닥 판정이 걸리기 시작한다. `center = height / 2` 식은 건드리지 않는다.

⚠️ **씬에서 직접 만진 것은 다음 생성 때 전부 날아간다.** `.unity`·`.mat`·`VolumeProfile.asset`은
GUID 참조가 들어간 YAML 이라 손으로 쓰지 않고 에디터 코드로 만든다 — 그래서 정본은 씬이
아니라 `Assets/Editor/CellRoomBootstrap.cs`다. 조명·카메라·포스트 프로세싱 값을 바꾸려면
그 파일을 고치고 메뉴를 다시 실행한다.

인스펙터에서 값을 굴려 보며 찾는 것 자체는 정상적인 작업 방식이다. **찾은 값을
부트스트랩에 옮겨 적고 메뉴를 다시 돌려 확정한다.** 씬에만 남기면 다음 생성에서 사라진다.

### WebGL 배포 — GitHub Pages

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

WebGL 에서만 달라지는 것들 — **모두 서버 헤더를 못 주는 환경 때문이다.**

| 항목 | 값 | 이유 |
|---|---|---|
| `webGLCompressionFormat` | `0` (Brotli) | 전송량이 가장 작다 |
| `webGLDecompressionFallback` | **`1` (필수)** | Pages 는 `Content-Encoding: br` 을 못 준다. 끄면 `Unable to parse Build/*.br!` 로 죽는다 |
| `webGLThreadsSupport` | `0` | 스레드는 COOP/COEP 헤더가 필요한데 Pages 는 못 준다 |
| `webGLTemplate` | `PROJECT:NHNAI` | 기본 템플릿은 고정 크기 캔버스 + 로고 푸터라 페이지 가운데 작은 박스로 뜬다 |
| 품질 레벨 | `0` = **Mobile** | `QualitySettings.asset` 의 `m_PerPlatformDefaultQuality: WebGL: 0`. **WebGL 이 무거우면 `PC_RPAsset` 이 아니라 `Mobile_RPAsset` 을 만진다** |

배포 스크립트가 압축·스레드 설정을 푸시 전에 검사한다. 어긋나면 올라가기 전에 막힌다.

**웹 페이지 껍데기는 `Assets/WebGLTemplates/NHNAI/index.html` 이다.** 이 파일이 하는 일
셋 — 캔버스를 뷰포트에 꽉 채우고, 브라우저의 터치 제스처를 막고, 렌더 해상도를 죈다.

| 만지는 곳 | 무엇이 달라지나 |
|---|---|
| `touch-action: none` · `overscroll-behavior: none` | **지우면 모바일 조작이 죽는다.** 시점 패드를 끄는 순간 페이지가 스크롤되거나 당겨서-새로고침이 걸린다 |
| `MaxPixelRatio` (기본 2) | 폰의 DPR 은 3~4 다. 올리면 선명하고 느려진다. 포스트 프로세싱을 다 켠 상태라 이 값이 프레임을 가장 크게 좌우한다 |
| `#if` 블록과 `{{{ }}}` 매크로 | **이름을 지어내지 않는다.** Unity 6.3 의 Minimal 템플릿에서 그대로 가져온 것이고, 틀리면 빌드가 조용히 빈 URL 을 넣는다. 원본은 `<Unity>/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/WebGLTemplates/Base/Minimal` |

WebGL 에서 재현되지 않는 전제 셋 — 코드 문제가 아니라 브라우저 정책이다.

- **배경음이 첫 클릭 전까지 무음이다.** 「시작 흐름」의 "씬이 열리는 순간부터 틀고 있어
  메뉴가 떠 있는 동안에도 울린다" 가 WebGL 에서는 안 맞는다. 브라우저 자동재생 정책상
  AudioContext 가 멈춘 채로 시작해서, PC/MOBILE 을 누른 뒤부터 들린다.
- **landscape 고정이 안 걸린다.** `defaultScreenOrientation: 3` 은 네이티브 모바일
  빌드용이고 브라우저는 보지 않는다 (Screen Orientation API 는 전체화면에서만 잠글 수
  있다). 강제할 수단이 없어서 **막고 안내한다** — `RotateGate` 층이 세로일 때 화면을
  덮는다. 세로 레이아웃을 만들지 않는다는 규칙은 그대로다.
- **커서 잠금 타이밍이 빡빡하다.** 「시작 흐름」의 `ClaimCursor` ⚠️ 참조.

### 룩을 조정할 때 어디를 만지나

| 바꾸고 싶은 것 | 만지는 곳 |
|---|---|
| 방·기계의 형태·치수 | `ArtPipeline/assets/*/generate_*.py` → 다시 돌린다 |
| 표면 밝기 (어떤 부품이 몇 번 명도인가) | 같은 생성 스크립트의 `color=` 인자. **팔레트 순서는 건드리지 않는다** |
| 빛의 세기·퍼짐·그림자 | `CellRoomBootstrap.BuildLighting()` |
| 어둠의 깊이 (벽이 얼마나 녹는가) | `ConfigureRenderSettings()` 의 `fogDensity`·`ambientLight` |
| 앤틱한 톤 (대비·입자·비네트) | `BuildPostProcessing()` |
| 빛 기둥의 룩 | `M_LightCone` 파라미터 → 확정되면 `ArtMaterialLibrary.CreateLightCone()` |

빛의 **경계**가 어색할 때는 원인이 셋이라 만지는 곳이 다르다. 눈으로 어느 경계인지 먼저 가린다.

| 어떤 경계가 어색한가 | 만지는 곳 |
|---|---|
| 빛 기둥이 바닥·벽을 뚫고 지나가며 생긴 **직선 교차선** | `M_LightCone` 의 `_DepthFade` (크게 = 더 흐림, 0 = 끔) |
| 빛 기둥의 **옆면 윤곽**이 칼로 자른 듯함 | `_EdgeSoftness` (크게 = 뭉근함), `_RimBoost` (작게 = 윤곽 약함) |
| 바닥에 생긴 **광원 웅덩이의 원 테두리** | `CellRoomBootstrap.SpotEdgeSoftness` (작게 = 넓게 번짐) |

`_DepthFade` 는 카메라의 `requiresDepthTexture` 가 켜져 있어야 동작한다.
꺼지면 조용히 효과만 사라지므로, 교차선이 다시 나타나면 여기부터 확인한다.

### 디버깅 도구

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
| **빌드에서만** 버튼 글자가 안 보임 (테두리는 있는데 빈칸) | 문구에 한글이 들어갔다. 기본 폰트에 글리프가 없고 빌드에는 OS 폰트 폴백이 없다 — 「폰트 준비」 참조. 에디터에서는 재현되지 않는다 |
| 메뉴를 골랐는데 조작이 안 먹음 | `MainMenu.uss` 의 페이드 길이와 `MainMenuScreen.FadeOutMs` 가 어긋났다 |
| **WebGL** — 빌드가 `Preprocessor error "TypeError: Cannot read property 'toString' of undefined"` 로 실패 | 템플릿 `index.html` 에 값 없는 매크로가 있다. **HTML 주석도 검사 대상이다** — 전처리기는 주석을 가리지 않고 파일 전체를 정규식으로 훑는다(`BuildTools/Preprocess.js:63`). 매크로 문법을 설명하는 주석을 쓸 때 중괄호 세 겹을 그대로 적으면 그게 평가된다 |
| **WebGL** — 페이지가 비었고 콘솔에 `Unable to parse Build/*.br!` | Decompression Fallback 이 꺼진 채 빌드됐다. 켜고 **다시 빌드**한다 — 설정만 고치면 이전 빌드가 그대로 올라간다. 산출물 확장자가 `.br` 이 아니라 **`.unityweb`** 이면 fallback 이 켜진 것이다 |
| **WebGL** — 페이지에 `index.html` 만 뜨고 404 뿐 | `Build/` 가 `.gitignore` 에 걸려 빠졌다. 손으로 add 하지 말고 `Tools/deploy-webgl.ps1` 을 쓴다 |
| **WebGL** — PC 를 골랐는데 시야가 안 돌아감 | 포인터 잠금이 거부됐다. 화면을 한 번 클릭하면 되잡힌다(그 클릭은 상호작용으로 안 센다). 반복되면 `ClaimCursor` 가 클릭 핸들러 안에서 불리는지 본다 |
| **WebGL** — 손가락을 끄니 게임 대신 페이지가 스크롤됨 | 템플릿의 `touch-action: none` / `overscroll-behavior: none` 이 빠졌다 |
| **WebGL** — 폰에서만 프레임이 안 나옴 | 템플릿의 `MaxPixelRatio` 를 낮춘다. 그 다음이 `Mobile_RPAsset` 과 SMAA 품질이다 |
| 세로로 들었는데 안내가 안 뜸 / 가로인데 안 걷힘 | `RotateGateScreen` 이 콜백을 **문서 루트**가 아니라 `gate-root` 에 걸었다. 접힌 요소에는 `GeometryChangedEvent` 가 오지 않아 한 번 숨으면 못 돌아온다 |
| 게임 UI 가 안 나타남 | `HudScreen.Begin` / `MobileControlsScreen.Begin` 이 안 불렸다 — 메뉴가 셋을 다 Bind 받았는지 본다 |

**UI 쪽 "스타일이 안 먹는다"의 1순위 원인은 컴포넌트 USS를 `GameTheme.tss`에 등록하지 않은 것이다.**

---

## 금지 사항

- **편의를 이유로 uGUI를 쓰지 않는다.** 「UI — 무엇으로 만드나」의 표에 해당할 때만 쓰고,
  쓸 때는 파일 맨 위에 사유를 남긴다. 스크린 스페이스 UI는 전부 UI Toolkit이다.
- **세로(portrait) 레이아웃을 만들지 않는다.** landscape 고정이다.
- **색·간격 리터럴을 USS 규칙 안에 흩뿌리지 않는다.** 화면 루트 클래스의
  `--{화면}-*` 변수에 모으고 주석을 붙인다.
- **`NHNAI.Game`에서 `UnityEngine.UIElements`를 참조하지 않는다.**
- **`width` / `height` / `left` / `top`을 애니메이션하지 않는다.** `translate` / `scale` / `rotate`를 쓴다.
- **`PanelSettings.asset`과 `.unity` 씬을 텍스트로 직접 쓰지 않는다.** `UiBootstrap.cs`를 쓴다.
- **확률·배당·연출 타이밍 같은 게임 규칙 값을 추측해서 만들지 않는다.** 먼저 물어본다.
  그럴듯한 값이 한 번 코드에 박히면 왜 그 값인지 아무도 모르게 된다.
- **게임 스크립트에서 `Keyboard.current` / `Mouse.current` / `Touchscreen`을 직접 읽지 않는다.**
  전부 `PlayerInputSource`를 거친다. 직접 읽으면 PC 에서만 되는 조작이 생긴다.
- **`Assets/Scripts/`에 만능 `Utils.cs`를 만들지 않는다.** 기능별 폴더에 자기완결 파일로 둔다.
- **프로토타입 JS에만 있는 동작을 만들지 않는다.** 정본은 항상 C#이다.
- **화면에 나가는 문구를 한글로 쓰지 않는다.** 기본 폰트에 글리프가 없어 빌드에서만
  빈칸이 된다 (「폰트 준비」 참조). 한글이 필요하면 FontAsset 을 먼저 물린다.
  주석·문서는 한국어 그대로다.
- **출처를 모르는 에셋을 넣지 않는다.** 이 저장소는 public 이라 담는 것이 곧 재배포다.
  받아 온 오디오·이미지는 `Assets/Audio/README.md` 처럼 출처와 라이선스를 **같은 커밋에** 적는다.
- **로컬 절대 경로를 커밋하지 않는다.** 문서·주석·스크립트 전부. 개인 경로가 필요하면
  환경변수나 저장소 밖 설정 파일로 뺀다 (`ArtPipeline` 이 `%APPDATA%` 를 쓰는 방식).

---

## 커밋 컨벤션

**Conventional Commits.** `.githooks/commit-msg`가 실제로 검사한다 — 형식이 틀리면 커밋이 막힌다.

```
<type>(<scope>): <제목>

<본문 — 선택. "무엇"보다 "왜">
```

### type (필수)

Conventional Commits 표준 type(`feat` `fix` `docs` `style` `refactor` `perf` `test` `build`
`ci` `chore` `revert`)을 그대로 쓴다. 허용 목록은 `.githooks/commit-msg`가 검사하고,
`.gitmessage` 템플릿에 요약이 있다.

**화면 색·간격 변경은 `style`이 아니다.** Conventional Commits의 `style`은 코드 서식을
뜻한다. 화면이 달라지므로 `feat(screen)` / `fix(screen)` 을 쓴다.

### scope (선택, 소문자·숫자·하이픈)

| scope | 범위 |
|---|---|
| `ui` | UI Toolkit 공통 — `GameTheme.tss`, `Assets/UI/Components/` |
| `screen` | 화면 — `Assets/UI/Screens/` |
| `game` | 게임 로직 — `NHNAI.Game` |
| `editor` | 에디터 툴 — `Assets/Editor/` |
| `art` | Blender 파이프라인·3D 에셋 — `ArtPipeline/`, `Assets/Art/` |
| `prototype` | HTML 프로토타입 — `prototype/` |
| `unity` | 프로젝트 설정·패키지 |
| `webgl` | WebGL 빌드·배포 — `Assets/WebGLTemplates/`, `Tools/deploy-webgl.ps1` |

컴포넌트가 늘어나 별도 scope가 필요해지면 여기와 `.gitmessage` ·
`.githooks/commit-msg`를 **같이** 고친다. 세 곳이 어긋나면 커밋이 막힌다.

### 규칙

- 제목은 **한국어**, 마침표 없이, 72자 이내 (초과 시 경고만 뜬다).
- 파괴적 변경은 type 뒤에 `!` — `feat(game)!: 배당 규칙 전면 변경`
- **화면 값을 바꾼 커밋은 `prototype/{화면}.css` 와 `{화면}.uss` 를 함께 담는다.** 하나만 담기면 둘이 어긋난다.
- `Co-Authored-By:` 같은 트레일러는 본문 맨 끝에 붙인다. 제목 형식과 충돌하지 않는다.
- `Merge` / `Revert` / `fixup!` / `squash!` 로 시작하는 자동 생성 메시지는 검사에서 제외된다.

### 활성화

`core.hooksPath`는 저장소 로컬 설정이라 **버전 관리되지 않는다.** 새로 클론하면 1회 실행한다.

```bash
git config core.hooksPath .githooks
git config commit.template .gitmessage
```

훅을 건너뛰려면 `git commit --no-verify` — 규칙이 잘못됐을 때만 쓰고,
반복되면 `.githooks/commit-msg`와 `.gitmessage`, 이 섹션을 함께 고친다.

---

## 참조 문서

UI Toolkit 참조 문서는 **저장소 안에** 있다. 모르는 것이 있으면 **웹 검색 전에 여기부터 본다.**

```
docs/reference/unity-ui-toolkit/    ← 자주 쓰는 페이지 벤더링. README.md 가 목록이다
```

자주 쓰는 페이지 (`docs/reference/unity-ui-toolkit/` 기준):

| 주제 | 파일 |
|---|---|
| CSS→USS 지원 여부 전체 표 | `css-to-uss-support.md` |
| 미지원 CSS 우회 12종 | `uss-workarounds.md` |
| `-unity-*` 전용 속성 | `uss-exclusive-properties.md` |
| BEM 네이밍 | `uss-naming-conventions.md` |
| Transitions & Transform | `uss-transitions.md` |
| 커스텀 컨트롤 | `custom-controls.md` |
| 성능 최적화 | `performance-optimization.md` |
| Flexbox 레이아웃 | `uss-layout-engine.md` |
| HTML→UXML 변환 | `html-to-uxml-guide.md` · `html-to-uxml-elements.md` · `html-to-uxml-layout.md` |

여기 있는 것은 **일반 지식**이다. 프로젝트 특화 결정과 충돌하면 이 파일과
`prototype/README.md`가 우선한다.

저장소 문서에 없으면 웹 검색으로 보충하고, 알아낸 내용은
`docs/reference/unity-ui-toolkit/`에 새 페이지로 추가하거나 이 파일에 남긴다.
**저장소 밖 경로를 참조 문서로 인용하지 않는다** — 다른 개발자의 클론에서 깨지고,
공개 저장소에서는 작성자의 디렉터리 구조가 드러난다.
