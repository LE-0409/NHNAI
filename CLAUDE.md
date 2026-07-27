# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## 프로젝트 개요

NHNAI는 Unity로 만드는 게임이다. UI는 전부 **UI Toolkit**으로 만든다.
uGUI(Canvas·RectTransform·Image·TextMeshPro)는 쓰지 않는다.

이 저장소의 코드는 **LLM이 작성한다는 전제**로 구조가 잡혀 있다.
그래서 다음을 지킨다.

- 값은 한 곳에만 둔다 (토큰은 `DESIGN.md`, 배치 수식은 C#).
- 파일 하나만 읽어도 그 파일이 뭘 하는지 알 수 있게 쓴다.
- 결과를 눈으로 확인할 수 있는 경로(HTML 프로토타입)를 항상 확보한다.

### 확정된 것

| 항목 | 값 |
|---|---|
| Unity | 6000.3.10f1 (Unity 6.3) |
| 렌더 파이프라인 | URP 2D |
| UI | UI Toolkit 전용 |
| 빌드 타깃 | PC (Windows/Mac) + 모바일 (Android/iOS) |
| 화면 방향 | **landscape 고정.** 세로 모드 미지원 |
| 기준 해상도 | 1920 x 1080 |

### 아직 안 정한 것

**이 저장소는 지금 개발 세팅만 되어 있고 게임 구현물이 없다.**

- **디자인 시스템.** `DESIGN.md`가 아직 없다. 색·간격·타이포 토큰의 정본이 없는 상태다.
- **게임 장르와 소재.** 무엇을 만들지 정해지지 않았다.

두 가지가 정해지기 전에는 다음을 만들지 않는다.

- 게임 규칙 코드 (`Assets/Scripts/`)
- 화면·컴포넌트 USS (토큰이 없으면 리터럴을 쓰게 되고, 나중에 전부 다시 고쳐야 한다)

**정해지지 않은 것을 추측해서 코드로 만들지 않는다.** 먼저 물어본다.

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
└── Assets/                    ← Unity URP 2D 템플릿 그대로. 직접 만든 것 없음
    ├── Scenes/SampleScene.unity
    ├── Settings/              ← URP 렌더러·볼륨 프로파일
    ├── DefaultVolumeProfile.asset
    └── InputSystem_Actions.inputactions
```

### 앞으로 만들 구조

아래는 **계획**이다. 아직 하나도 없다. 만들 때 이 배치와 이름을 따른다.

```
NHNAI/
├── DESIGN.md                  ← 디자인 토큰 원본 (single source of truth)
│
├── prototype/                 ← HTML/CSS 프로토타입 (Unity 밖, 빌드에 포함 안 됨)
│   ├── README.md              ← 변환 규칙 · CSS→USS 치환표
│   ├── tokens.css             ← tokens.uss 와 쌍
│   ├── base.css               ← base.uss 와 쌍
│   └── {화면}.html + .css     ← 화면별 쌍
│
└── Assets/
    ├── UI/                    ← 어셈블리 NHNAI.UI
    │   ├── NHNAI.UI.asmdef
    │   ├── Theme/
    │   │   ├── tokens.uss     ← 색·간격·타이포·크기 변수. 여기 없는 값은 쓰지 않는다
    │   │   ├── base.uss       ← 타이포 스케일, 레이아웃 유틸, 공용 컴포넌트
    │   │   └── GameTheme.tss  ← 위를 묶는 런타임 테마. 새 컴포넌트 USS 는 여기 등록
    │   ├── Components/{이름}/ ← 재사용 컴포넌트 (커스텀 컨트롤). .cs + .uss
    │   └── Screens/{화면}/    ← 화면. {화면}.uxml + .uss + .cs
    │
    ├── Scripts/               ← 어셈블리 NHNAI.Game — UI 에 의존하지 않는 게임 코드
    │   └── NHNAI.Game.asmdef
    │
    ├── Editor/                ← 에디터 툴 (asmdef 없음 = Assembly-CSharp-Editor)
    │   └── UiBootstrap.cs     ← PanelSettings · 씬 생성
    │
    ├── Settings/UI/
    │   └── GamePanelSettings.asset   ← UiBootstrap 이 생성
    │
    └── Scenes/{화면}.unity    ← UiBootstrap 이 생성. 손으로 쓰지 않는다
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

## 실행 · 검증

씬 생성 메뉴와 테스트 씬은 아직 없다. `Assets/Editor/UiBootstrap.cs`를 만들 때 함께 붙인다.
씬을 만들면 `EditorBuildSettings`에도 자동으로 등록되게 하고,
`ProjectSettings/EditorBuildSettings.asset`은 손으로 고치지 않는다.

### 브라우저 프로토타입

`prototype/{화면}.html`을 브라우저로 직접 연다. 같은 값·같은 수식이라 Unity와 화면이
일치해야 한다. 일치하지 않으면 **어느 쪽이 틀렸는지 먼저 특정**하고 고친다.

### 디버깅 도구

| 문제 | 도구 |
|---|---|
| 요소가 안 보임 / 스타일이 안 먹음 | Window > UI Toolkit > Debugger |
| 드로우 콜이 많음 | Frame Debugger |
| 프레임 저하 | Unity Profiler |

**"스타일이 안 먹는다"의 1순위 원인은 컴포넌트 USS를 `GameTheme.tss`에 등록하지 않은 것이다.**

---

## 금지 사항

- **uGUI를 쓰지 않는다.** Canvas / RectTransform / Image / Text / TextMeshPro 금지.
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
