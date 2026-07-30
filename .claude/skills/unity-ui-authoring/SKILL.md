---
name: unity-ui-authoring
description: >
  NHNAI 의 UI Toolkit 화면·컴포넌트를 실제로 작성·수정할 때 읽는 스킬. UXML/USS/TSS 작성,
  화면 색·간격 변경, 커스텀 컨트롤 추가, BEM 네이밍, USS 제약 우회, 애니메이션(translate/scale),
  가시성 토글, 화면 문구·폰트, uGUI 를 써도 되는지 판단이 필요하면 트리거한다.
  "UI 만들어줘", "화면 색 바꿔줘", "스타일이 안 먹어", "컴포넌트 추가", "UXML", "USS",
  "UI Toolkit", "uGUI", "HUD", "메뉴 화면" 같은 요청이 해당한다.
  브라우저 HTML/CSS 프로토타입 단계(파이프라인 1단계)는 unity-ui-prototype 스킬이 담당한다.
---

# unity-ui-authoring — UI Toolkit 작성 규칙

CLAUDE.md 에서 옮겨 온 절: 「UI — 무엇으로 만드나」 · 「개발 파이프라인」 ·
「디자인 값은 화면마다 로컬 변수로 모은다」 · 「UI Toolkit 컨벤션」 ·
「인라인 스타일 vs USS」 · 「컴포넌트 작성 규칙」 · 「USS 제약」 · 「폰트 준비」 ·
「참조 문서」. 코드 주석이 `CLAUDE.md 「…」` 로 가리키는 것은 이 파일이다.

---

## UI — 무엇으로 만드나

**기본은 UI Toolkit이다.** uGUI는 *UI Toolkit으로 되지 않는 기능*에 한해 쓴다.
"익숙해서" / "예제가 uGUI라서" / "빨라서"는 사유가 아니다. (지금까지 uGUI를 쓴 곳은 없다.)

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

## 개발 파이프라인

### 새 화면을 만들 때

```
1. prototype/{화면}.html + .css 작성      ← 브라우저에서 레이아웃 확정
        │  prototype/README.md 의 USS 호환 규칙을 지킨다
        │  이 단계는 unity-ui-prototype 스킬이 담당한다
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
USS 만 고친다. 커밋도 둘을 함께 담는다.

### 왜 `.asset` / `.unity` 를 코드로 만드나

`.uss` `.uxml` `.tss` `.asmdef`는 전부 텍스트라 직접 쓸 수 있다.
`PanelSettings.asset`과 `.unity` 씬은 GUID 참조가 들어간 YAML이라 손으로 쓰면 깨진다.
그래서 `Assets/Editor/UiBootstrap.cs`가 만든다.
**이 두 종류를 직접 텍스트로 쓰려고 시도하지 않는다.**

---

## 디자인 값은 화면마다 로컬 변수로 모은다

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

**"스타일이 안 먹는다"의 1순위 원인이 이 등록 누락이다.**

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

## 참조 문서

UI Toolkit 참조 문서는 **저장소 안에** 있다. 모르는 것이 있으면 **웹 검색 전에 여기부터 본다.**

```
docs/reference/unity-ui-toolkit/    ← 자주 쓰는 페이지 벤더링. README.md 가 목록이다
```

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

여기 있는 것은 **일반 지식**이다. 프로젝트 특화 결정과 충돌하면 이 스킬과 CLAUDE.md ·
`prototype/README.md`가 우선한다.

저장소 문서에 없으면 웹 검색으로 보충하고, 알아낸 내용은
`docs/reference/unity-ui-toolkit/`에 새 페이지로 추가하거나 이 스킬에 남긴다.
**저장소 밖 경로를 참조 문서로 인용하지 않는다** — 다른 개발자의 클론에서 깨지고,
공개 저장소에서는 작성자의 디렉터리 구조가 드러난다.

---

UI 가 안 보이거나 스타일이 안 먹는 증상은 `nhnai-troubleshooting` 스킬의 증상 표를 본다.
