# CSS 속성 USS 지원 여부

> CSS 속성별 USS 지원 여부 레퍼런스. ✅ 지원 | ⚠️ 부분 지원 | ❌ 미지원

## 박스 모델

| CSS 속성 | USS |
|----------|-----|
| width, height, min/max-width/height | ✅ |
| margin, padding (단축 + 방향별) | ✅ |
| border-width, border-color, border-radius | ✅ |
| border-style (dashed, dotted) | ❌ 항상 solid |
| box-sizing | ❌ 항상 border-box |
| outline | ❌ border로 대체 |
| box-shadow | ❌ |

## 레이아웃

| CSS 속성 | USS |
|----------|-----|
| display: flex | ✅ |
| display: none | ✅ |
| display: grid / block / inline / inline-block | ❌ |
| flex-direction / wrap / grow / shrink / basis | ✅ |
| align-items / self / content | ✅ |
| justify-content | ✅ |
| justify-items / justify-self | ❌ |
| gap / row-gap / column-gap | ❌ margin으로 대체 |
| grid-* 전체 | ❌ |
| float / clear | ❌ |
| position: relative / absolute | ✅ |
| position: fixed / sticky / static | ❌ |
| top / right / bottom / left | ✅ (absolute일 때) |
| z-index | ❌ 트리 순서로 제어 |
| overflow: hidden / visible / scroll | ✅ |
| overflow: auto | ❌ |
| overflow-x / overflow-y | ❌ |
| columns / column-* | ❌ |

## 색상 / 배경

| CSS 속성 | USS |
|----------|-----|
| color | ✅ |
| background-color | ✅ |
| background-image | ✅ (에셋 url() 참조) |
| background-size | ⚠️ -unity-background-scale-mode로 대체 |
| background-position / repeat / attachment | ❌ |
| background (단축) | ❌ |
| linear-gradient / radial-gradient | ❌ |
| opacity | ✅ |
| filter / backdrop-filter / mix-blend-mode | ❌ |

## 타이포그래피

| CSS 속성 | USS |
|----------|-----|
| font-size (px) | ✅ |
| font-size (em, rem) | ❌ |
| font-family | ⚠️ -unity-font / -unity-font-definition으로 대체 |
| font-weight | ⚠️ -unity-font-style: bold |
| font-style | ⚠️ -unity-font-style: italic |
| letter-spacing / word-spacing | ✅ |
| line-height | ❌ |
| text-align | ⚠️ -unity-text-align으로 대체 (9방향) |
| text-decoration | ❌ Rich Text Tags로 대체 |
| text-transform | ❌ |
| text-overflow | ✅ ellipsis |
| text-shadow | ❌ -unity-text-outline으로 근사 |
| white-space | ✅ normal/nowrap |
| word-break / overflow-wrap | ❌ |

## Transform / 애니메이션

| CSS 속성 | USS |
|----------|-----|
| transform: translate / rotate / scale | ✅ (USS 개별 속성) |
| transform: skew / matrix | ❌ |
| transition | ✅ |
| animation / @keyframes | ❌ C# 코루틴 또는 실험적 API |
| will-change | ❌ UsageHints C# API로 대체 |

## 가시성 / 상호작용

| CSS 속성 | USS |
|----------|-----|
| visibility: visible / hidden | ✅ |
| cursor | ✅ |
| pointer-events | ⚠️ pickingMode C# API로 대체 |
| user-select / resize | ❌ |

## 셀렉터

| CSS 셀렉터 | USS |
|-----------|-----|
| .class / #id / element / * | ✅ |
| .parent > .child / .ancestor .descendant | ✅ |
| .a.b 다중 클래스 | ✅ |
| :hover / :active / :focus / :disabled / :checked | ✅ |
| ::before / ::after / ::placeholder | ❌ |
| :nth-child() / :first-child / :last-child / :not() | ❌ |
| [attr] 속성 셀렉터 | ❌ |

## CSS 함수 / At-규칙

| CSS | USS |
|-----|-----|
| var(--name) | ✅ |
| calc() / clamp() / min() / max() | ❌ |
| rgb() / rgba() | ✅ |
| hsl() / hsla() | ❌ rgb로 변환 필요 |
| url() | ✅ |
| @media | ❌ |
| @keyframes | ❌ |
| @import | ✅ (TSS에서) |
| @font-face | ❌ Unity 폰트 에셋 사용 |

## 관련 페이지

- [HTML/CSS → UXML/USS 변환 가이드](html-to-uxml-guide.md)
- [USS 전용 속성 (-unity-*)](uss-exclusive-properties.md)
- [미지원 CSS 패턴 우회 방법](uss-workarounds.md)
