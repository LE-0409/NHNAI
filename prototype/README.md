# prototype/ — HTML/CSS 프로토타입

브라우저에서 레이아웃을 확정한 뒤 `Assets/UI/` 로 옮기는 자리다.
Unity 밖이고 빌드에 포함되지 않는다.

**왜 두는가**: UXML/USS 는 Unity 를 켜야 보이고 반복이 느리다. 브라우저는 저장하고
새로고침하면 끝이라, 배치를 잡는 단계에서는 여기가 훨씬 빠르다.

## 화면 짝

| 프로토타입 | Unity 쪽 대응 |
|---|---|
| `main-menu.html` + `.css` | `Assets/UI/Screens/MainMenu/` |
| `mobile-controls.html` + `.css` | `Assets/UI/Screens/MobileControls/` + `Assets/UI/Components/` |
| `rotate-gate.html` + `.css` | `Assets/UI/Screens/RotateGate/` |

HUD(`Assets/UI/Screens/Hud/`)는 프로토타입이 없다. 조준점 하나와 숫자 하나뿐이라
브라우저에서 볼 것이 없었다 — 「건너뛰어도 되는 경우」에 해당한다.

## ⚠️ 짝의 CSS 변수는 이름과 값이 같다

각 화면의 값은 루트 클래스의 `--{화면}-*` 로컬 변수에 모여 있고,
**프로토타입과 USS 가 같은 이름·같은 값을 쓴다.**

```
prototype/main-menu.css    .main-menu { --menu-scrim: rgba(4, 4, 6, 0.62); ... }
Assets/UI/Screens/MainMenu/MainMenu.uss   .main-menu { --menu-scrim: rgba(4, 4, 6, 0.62); ... }
```

색이나 간격을 바꿀 때는 **두 파일을 같이 고친다.** 한쪽만 고치면 브라우저에서 본 것과
게임 화면이 달라지고, 프로토타입이 거짓말을 하기 시작한다.

전역 토큰 파일은 두지 않았다 — 이유는 `.claude/skills/unity-ui-authoring/` 스킬의
「디자인 값은 화면마다 로컬 변수로 모은다」.

## CSS → USS 치환표

USS 는 CSS 의 부분집합이다. **속성별 지원 여부 전체 표는
`docs/reference/unity-ui-toolkit/css-to-uss-support.md` 가 정본이다.**
아래는 이 폴더의 프로토타입이 실제로 지키는 것만이다.

| 프로토타입에서 쓴 것 | UXML/USS 로 옮길 때 |
|---|---|
| `<div>` | `<engine:VisualElement>` |
| `<span>` · `<p>` · `<h1>` | `<engine:Label text="..." />` |
| `<button>` | `<engine:Button>` |
| `class="..."` | 그대로 |
| `--foo: 1px` / `var(--foo)` | 그대로 (색·길이 한정) |
| `:hover` · `:active` | 그대로 |
| `transition-*` | 그대로. **단 `var()` 를 쓰지 않고 리터럴** (`unity-ui-authoring` 스킬) |
| `translate` · `scale` | 그대로. 단 계산으로 정해지는 값은 C# 인라인 |

**프로토타입에서 쓰지 않은 것** (USS 에 없어서 애초에 안 썼다):
`display:grid`/`block`, `position:fixed`/`sticky`, `z-index`, `gap`, `box-shadow`,
`line-height`, `@media`, `@keyframes`, `::before`/`::after`, `:nth-child()`,
`calc()`, `linear-gradient`, `text-transform`, `hsl()`, 숫자 `font-weight`.

`gap` 대신 `margin` 을 쓰고, 겹침 순서는 트리 순서로 만든다. 대문자 제목은
`text-transform` 없이 **문자열 자체를 대문자로** 쓴다.

각 `.css` 맨 위에는 브라우저 전용 리셋 블록이 있고 **USS 로 옮기지 않는다** 고 표시해
두었다. UI Toolkit 은 기본이 flex 라 그 리셋이 하는 일을 이미 하고 있다.

## 보는 법

브라우저로 `.html` 을 그냥 연다 (서버 불필요). 창을 **가로로 넓게** 두고 본다 —
이 프로젝트는 landscape 고정이고 기준 해상도가 1920×1080 이다.

`mobile-controls.html` 의 조이스틱·시점 패드는 **위치와 크기만** 보는 용도다.
끌어서 움직이는 동작은 C# (`VirtualJoystick.cs` · `TouchLookPad.cs`) 이 정본이다 —
CLAUDE.md 금지 사항: 프로토타입 JS 에만 있는 동작을 만들지 않는다. 그래서 JS 가 없다.

## 새 화면을 추가할 때

절차는 `.claude/skills/unity-ui-authoring/` 스킬의 「새 화면을 만들 때」를 따른다.
`.claude/skills/unity-ui-prototype/` 스킬이 그 1단계를 담당한다.
