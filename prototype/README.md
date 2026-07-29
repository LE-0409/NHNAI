# prototype/ — HTML/CSS 프로토타입

브라우저에서 레이아웃을 확정한 뒤 `Assets/UI/` 로 옮기는 자리다.
Unity 밖이고 빌드에 포함되지 않는다.

## ⚠️ 지금은 임시 상태다

CLAUDE.md 「개발 파이프라인 0단계」가 아직 끝나지 않았다 — `DESIGN.md` 가 없어
`tokens.css` / `tokens.uss` 의 정본이 없다. 그래서 여기 파일들은 **토큰을 참조하지 않고**
각 화면 루트 클래스에 `--{화면}-*` 로컬 변수를 두고 `토큰 승격 후보` 주석을 달아 둔다.
`Assets/UI/Screens/Hud/Hud.uss` 가 먼저 쓴 방식과 같다.

`DESIGN.md` 가 생기면 할 일:

1. 각 파일의 `토큰 승격 후보` 주석이 붙은 값을 `DESIGN.md` 로 올린다
2. `tokens.css` · `tokens.uss` 를 뽑고 로컬 변수를 `var(--color-*)` 등으로 바꾼다
3. 이 절을 지운다

`.claude/skills/unity-ui-prototype/` 스킬은 `DESIGN.md` 와 `tokens.css` 를 읽어야 돌아가므로
0단계 전에는 쓰지 않는다. 여기 파일들은 스킬 없이 직접 썼다.

## CSS → USS 치환표

**전체 표는 `docs/reference/unity-ui-toolkit/css-to-uss-support.md` 가 정본이다.**
0단계에서 이 README 로 옮겨 온다. 지금 이 폴더의 프로토타입이 실제로 지키는 것만 적는다.

| 프로토타입에서 쓴 것 | UXML/USS 로 옮길 때 |
|---|---|
| `<div>` | `<engine:VisualElement>` |
| `<span>` · `<p>` · `<h1>` | `<engine:Label text="..." />` |
| `<button>` | `<engine:Button>` |
| `class="..."` | 그대로 |
| `--foo: 1px` / `var(--foo)` | 그대로 (색·길이 한정) |
| `:hover` · `:active` | 그대로 |
| `transition-*` | 그대로. **단 `var()` 를 쓰지 않고 리터럴** (CLAUDE.md) |
| `translate` · `scale` | 그대로. 단 계산으로 정해지는 값은 C# 인라인 |

**프로토타입에서 쓰지 않은 것** (USS 에 없어서 애초에 안 썼다):
`display:grid`/`block`, `position:fixed`/`sticky`, `z-index`, `gap`, `box-shadow`,
`line-height`, `@media`, `@keyframes`, `::before`/`::after`, `:nth-child()`,
`calc()`, `linear-gradient`, `text-transform`, `hsl()`, 숫자 `font-weight`.

`gap` 대신 `margin` 을 쓰고, 겹침 순서는 트리 순서로 만든다. 대문자 제목은
`text-transform` 없이 **문자열 자체를 대문자로** 쓴다.

## 보는 법

브라우저로 `.html` 을 그냥 연다. 창을 **가로로 넓게** 두고 본다 — 이 프로젝트는
landscape 고정이고 기준 해상도가 1920×1080 이다.

| 파일 | Unity 쪽 대응 |
|---|---|
| `main-menu.html` | `Assets/UI/Screens/MainMenu/` |
| `mobile-controls.html` | `Assets/UI/Screens/MobileControls/` + `Assets/UI/Components/` |

`mobile-controls.html` 의 조이스틱·시점 패드는 **위치와 크기만** 보는 용도다.
끌어서 움직이는 동작은 C# (`VirtualJoystick.cs` · `TouchLookPad.cs`) 이 정본이다 —
CLAUDE.md 금지 사항: 프로토타입 JS 에만 있는 동작을 만들지 않는다. 그래서 JS 가 없다.
