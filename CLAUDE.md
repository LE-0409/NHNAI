# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## 프로젝트 개요

NHNAI는 Unity로 만드는 3D 게임이다. UI는 **UI Toolkit이 기본**이다.
uGUI는 UI Toolkit으로 되지 않는 기능에 한해서만 쓴다 (아래 「UI — 무엇으로 만드나」 참조).

이 저장소의 코드는 **LLM이 작성한다는 전제**로 구조가 잡혀 있다.
그래서 다음을 지킨다.

- 값은 한 곳에만 둔다 (토큰은 `DESIGN.md`, 배치 수식은 C#).
- 파일 하나만 읽어도 그 파일이 뭘 하는지 알 수 있게 쓴다.
- 결과를 눈으로 확인할 수 있는 경로(HTML 프로토타입)를 항상 확보한다.

### 확정된 것

| 항목 | 값 |
|---|---|
| Unity | 6000.3.10f1 (Unity 6.3) |
| 렌더 파이프라인 | URP 3D (PC · Mobile 렌더러 분리) |
| UI | UI Toolkit 기본, uGUI 예외 허용 |
| 입력 | Input System (`activeInputHandler: 1`) |
| 빌드 타깃 | PC (Windows/Mac) + 모바일 (Android/iOS) |
| 화면 방향 | **landscape 고정.** 세로 모드 미지원 |
| 기준 해상도 | 1920 x 1080 |

화면 방향과 기준 해상도는 문서에만 있는 규칙이 아니라 `ProjectSettings/ProjectSettings.asset`에
실제로 박혀 있다 — `allowedAutorotateToPortrait`/`PortraitUpsideDown`이 `0`,
landscape 둘만 `1`이다. 세로를 지원하게 되면 이 표와 설정을 **같이** 고친다.

### 아직 안 정한 것

**이 저장소는 지금 개발 세팅만 되어 있고 게임 구현물이 없다.**

- **디자인 시스템.** `DESIGN.md`가 아직 없다. 색·간격·타이포 토큰의 정본이 없는 상태다.
- **게임 장르와 소재.** 무엇을 만들지 정해지지 않았다.

두 가지가 정해지기 전에는 다음을 만들지 않는다.

- 게임 규칙 코드 (`Assets/Scripts/`)
- 화면·컴포넌트 USS (토큰이 없으면 리터럴을 쓰게 되고, 나중에 전부 다시 고쳐야 한다)

**정해지지 않은 것을 추측해서 코드로 만들지 않는다.** 먼저 물어본다.

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

### 지금 있는 것

```
NHNAI/
├── CLAUDE.md                  ← 이 파일
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
    ├── Scripts/               ← 어셈블리 NHNAI.Game — UI 에 의존하지 않는 게임 코드
    │   ├── NHNAI.Game.asmdef
    │   └── Player/PlayerLook.cs   ← 마우스 시점
    │
    ├── Editor/                ← 에디터 툴 (asmdef 없음 = Assembly-CSharp-Editor)
    │   ├── ArtMaterialLibrary.cs  ← .mat 생성 + FBX 머티리얼 리맵
    │   └── CellRoomBootstrap.cs   ← 독방 씬의 정본
    │
    ├── Shaders/LightCone.shader   ← 전등 아래 빛 기둥
    │
    ├── Scenes/                ← 전부 부트스트랩이 생성. 손으로 쓰지 않는다
    │   ├── CellRoom.unity
    │   └── SampleScene.unity  ← URP 템플릿 잔재. 안 쓴다
    │
    ├── Settings/              ← PC_ / Mobile_ 렌더러 쌍, URP 글로벌 설정
    │   └── CellRoomVolume.asset   ← 포스트 프로세싱. 부트스트랩이 생성
    │
    └── InputSystem_Actions.inputactions   ← URP 템플릿 기본. 아직 안 쓴다
```

`Assets/Settings/`의 `PC_RPAsset` · `Mobile_RPAsset`은 품질 레벨과 짝지어져 있다.
렌더 설정을 바꿀 때 **둘 다** 봐야 한다 — 한쪽만 고치면 플랫폼에 따라 화면이 달라진다.

### 앞으로 만들 구조

아래는 **계획**이다. 아직 없다. 만들 때 이 배치와 이름을 따른다.

```
NHNAI/
├── DESIGN.md                  ← 디자인 토큰 원본 (single source of truth)
│
├── docs/game-concepts.md      ← 게임 규칙의 정본
│
├── prototype/                 ← HTML/CSS 프로토타입 (Unity 밖, 빌드에 포함 안 됨)
│   ├── README.md              ← 변환 규칙 · CSS→USS 치환표
│   ├── tokens.css · base.css  ← tokens.uss · base.uss 와 쌍
│   └── {화면}.html + .css
│
└── Assets/
    └── UI/                    ← 어셈블리 NHNAI.UI
        ├── NHNAI.UI.asmdef
        ├── Theme/             ← tokens.uss · base.uss · GameTheme.tss
        ├── Components/{이름}/ ← 재사용 컴포넌트 (커스텀 컨트롤). .cs + .uss
        └── Screens/{화면}/    ← 화면. {화면}.uxml + .uss + .cs
```

### 어셈블리 의존 방향

```
NHNAI.Game  ←  NHNAI.UI  ←  Assembly-CSharp-Editor (Assets/Editor)
```

- `NHNAI.Game`은 `UnityEngine.UIElements`를 **참조하지 않는다.** 게임 로직에 UI 타입이 들어오면 안 된다.
- `NHNAI.UI`는 `NHNAI.Game`을 참조한다. 역방향은 금지다.
- `Assets/Editor/`에는 asmdef를 두지 않는다. 사전 정의 어셈블리라 모든 패키지를 참조 설정 없이 쓸 수 있다.

---

## 개발 파이프라인

### 0단계 — 기반 만들기 (아직 안 됨. 첫 화면보다 먼저 한다)

```
1. DESIGN.md 를 정한다                    ← 팔레트·간격·타이포 스케일의 정본
        ▼
2. prototype/README.md 를 쓴다            ← USS 호환 규칙 · CSS→USS 치환표의 정본
        ▼
3. tokens.uss + tokens.css 를 DESIGN.md 에서 뽑는다
   base.uss  + base.css   를 만든다       ← flex 리셋, 타이포 스케일, .u-* / .t-* 유틸
        ▼
4. Assets/UI/Theme/GameTheme.tss 를 만들고 위 둘을 @import 한다
        ▼
5. Assets/Editor/UiBootstrap.cs 로 PanelSettings 와 첫 씬을 만든다
```

`.claude/skills/unity-ui-prototype/` 스킬은 `prototype/README.md` · `DESIGN.md` ·
`prototype/tokens.css` · `base.css`를 읽어서 동작한다. **0단계가 끝나기 전에는 이 스킬이
제대로 돌지 않는다.** 그 전에 프로토타입이 필요하면 스킬 없이 직접 쓰고,
0단계를 마친 뒤 규칙에 맞게 정리한다.

### 새 화면을 만들 때 (0단계 이후)

```
1. prototype/{화면}.html + .css 작성      ← 브라우저에서 레이아웃 확정
        │  prototype/README.md 의 USS 호환 규칙을 지킨다
        ▼
2. Assets/UI/Screens/{화면}/{화면}.uxml + .uss 로 변환
        │  치환표는 prototype/README.md
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

### 토큰을 바꿀 때

세 파일을 **항상 같이** 고친다. 하나라도 빠지면 프로토타입과 게임 화면이 달라진다.

```
DESIGN.md  →  Assets/UI/Theme/tokens.uss  →  prototype/tokens.css
```

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

### 값은 토큰에서만 가져온다

```uss
/* O */
.panel { padding: var(--space-xl); background-color: var(--color-surface); }

/* X — 토큰에 없는 값을 직접 씀 */
.panel { padding: 22px; background-color: #2a2750; }
```

토큰에 없는 값이 필요하면 **먼저 `DESIGN.md`에 토큰을 추가**하고 쓴다.

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
`tokens.uss`의 `--motion-*`는 **문서용 참조값**이다.

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

폰트는 `DESIGN.md`가 정해진 뒤에 고른다. 폰트 에셋이 없으면 Unity 기본 런타임 폰트로
렌더링된다. 웨이트를 여러 개 쓰려면 웨이트마다 FontAsset을 따로 만들고
`-unity-font-definition`으로 지정한다 (`uss-exclusive-properties.md` 참조).

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
- 아직 Unity 임포트 후처리(팔레트 텍스처 → 머티리얼 리맵)는 붙이지 않았다. 필요해지면
  `ArtPipeline/KIT.md`의 "엔진 연동" 절을 참고해 이 저장소용으로 만든다.

### 문서

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
NHNAI > Scenes > 독방 (CellRoom)                ← 위 둘을 포함해 씬을 통째로 새로 만든다
```

씬을 만들면 `EditorBuildSettings`에 **자동으로 등록**된다.
`ProjectSettings/EditorBuildSettings.asset`은 손으로 고치지 않는다.

### 조작

| 입력 | 동작 |
|---|---|
| 마우스 | 시야. **좌우는 몸통(`Player`)이, 상하는 카메라가** 돈다 |
| WASD · 방향키 | 걷기. 몸통 정면 기준이다 |
| 좌클릭 | 조준한 것과 상호작용 (커서가 잠긴 상태에서만) |
| Esc | 커서 해제. 다시 클릭하면 잠긴다 |

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

**UI 쪽 "스타일이 안 먹는다"의 1순위 원인은 컴포넌트 USS를 `GameTheme.tss`에 등록하지 않은 것이다.**

---

## 금지 사항

- **편의를 이유로 uGUI를 쓰지 않는다.** 「UI — 무엇으로 만드나」의 표에 해당할 때만 쓰고,
  쓸 때는 파일 맨 위에 사유를 남긴다. 스크린 스페이스 UI는 전부 UI Toolkit이다.
- **세로(portrait) 레이아웃을 만들지 않는다.** landscape 고정이다.
- **`DESIGN.md`에 없는 색·간격을 하드코딩하지 않는다.** 먼저 토큰을 추가한다.
- **`NHNAI.Game`에서 `UnityEngine.UIElements`를 참조하지 않는다.**
- **`width` / `height` / `left` / `top`을 애니메이션하지 않는다.** `translate` / `scale` / `rotate`를 쓴다.
- **`PanelSettings.asset`과 `.unity` 씬을 텍스트로 직접 쓰지 않는다.** `UiBootstrap.cs`를 쓴다.
- **디자인 시스템과 게임 장르가 정해지기 전에 화면 USS·게임 규칙 코드를 추측해 만들지 않는다.**
- **`Assets/Scripts/`에 만능 `Utils.cs`를 만들지 않는다.** 기능별 폴더에 자기완결 파일로 둔다.
- **프로토타입 JS에만 있는 동작을 만들지 않는다.** 정본은 항상 C#이다.

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

**디자인 토큰 변경은 `style`이 아니다.** Conventional Commits의 `style`은 코드 서식을 뜻한다.
토큰은 화면을 바꾸므로 `feat(theme)` / `fix(theme)` / `refactor(theme)`를 쓴다.

### scope (선택, 소문자·숫자·하이픈)

| scope | 범위 |
|---|---|
| `theme` | 디자인 토큰 — `DESIGN.md`, `tokens.uss`, `tokens.css` |
| `ui` | UI Toolkit 공통 — `base.uss`, `GameTheme.tss` |
| `screen` | 화면 — `Assets/UI/Screens/` |
| `game` | 게임 로직 — `NHNAI.Game` |
| `editor` | 에디터 툴 — `Assets/Editor/` |
| `art` | Blender 파이프라인·3D 에셋 — `ArtPipeline/`, `Assets/Art/` |
| `prototype` | HTML 프로토타입 — `prototype/` |
| `unity` | 프로젝트 설정·패키지 |

컴포넌트가 늘어나 별도 scope가 필요해지면 여기와 `.gitmessage` ·
`.githooks/commit-msg`를 **같이** 고친다. 세 곳이 어긋나면 커밋이 막힌다.

### 규칙

- 제목은 **한국어**, 마침표 없이, 72자 이내 (초과 시 경고만 뜬다).
- 파괴적 변경은 type 뒤에 `!` — `feat(theme)!: 토큰 이름을 signal 계열로 변경`
- **토큰을 바꾼 커밋은 `DESIGN.md` / `tokens.uss` / `tokens.css` 세 파일을 함께 담는다.** 하나만 담기면 세 곳이 어긋난다.
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
| 데이터 바인딩 | `data-binding-overview.md` |
| Flexbox 레이아웃 | `uss-layout-engine.md` |
| HTML→UXML 변환 | `html-to-uxml-guide.md` · `html-to-uxml-elements.md` · `html-to-uxml-layout.md` |

저장소 문서에 없으면 웹 검색으로 보충하고, 알아낸 내용은
`docs/reference/unity-ui-toolkit/`에 새 페이지로 추가하거나 이 파일에 남긴다.
**저장소 밖 경로를 참조 문서로 인용하지 않는다** — 다른 개발자의 클론에서 깨진다.

원본 자료(작성자 로컬 전용, `D:\00git\_Main\` 아래 — Coding-Inventory 위키의 UI Toolkit 46페이지·
LLM 설계 패턴 12페이지, awesome-design-md의 DESIGN.md 원본 73종)는 저장소에 없는 주제를
찾을 때만 쓰고, 인용한 내용은 반드시 `docs/reference/`로 복사해 저장소를 자기완결로 유지한다.
