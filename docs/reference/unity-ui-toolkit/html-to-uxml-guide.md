# HTML/CSS → UXML/USS 변환 가이드

> HTML/CSS 웹 레이아웃을 Unity UI Toolkit으로 변환할 때의 전체 흐름과 핵심 제약

## 변환 흐름

```text
HTML/CSS 프로토타입
        │
        ▼
[1] 요소 대응 확인 → HTML 태그를 UXML 컨트롤로 매핑
        │
        ▼
[2] 레이아웃 검토 → Grid/Float 사용 여부 확인, Flexbox로 재설계
        │
        ▼
[3] CSS 속성 필터링 → 미지원 속성 제거 또는 표시
        │
        ▼
[4] 미지원 패턴 처리 → box-shadow, @keyframes 등 우회 방법 적용
        │
        ▼
[5] USS 전용 속성 추가 → -unity-* 속성으로 Unity 고유 스타일 지정
        │
        ▼
UXML/USS 완성
```

## 핵심 제약 요약

| 항목 | 웹(CSS) | USS(Unity) |
|------|---------|-----------|
| 레이아웃 | Flexbox, Grid, Block, Float | **Flexbox만** |
| position | static/relative/absolute/fixed/sticky | **relative, absolute만** |
| display | flex, grid, block, inline, none 등 | **flex, none만** |
| 애니메이션 | @keyframes + animation | **transition만** (keyframe은 C# 필요) |
| 가상 요소 | ::before, ::after | **미지원** |
| 미디어 쿼리 | @media | **미지원** |
| z-index | 지원 | **미지원** (트리 순서로 제어) |
| 필터 효과 | filter: blur, brightness 등 | **미지원** |
| 스크립트 | JavaScript | **C# (RegisterCallback)** |

## HTML/CSS 작성 10대 원칙 (변환 최소화)

변환 전 HTML/CSS 프로토타입 단계에서 이 원칙을 지키면 변환 비용이 최소화된다.

1. **레이아웃은 Flexbox만** — `display: grid`, `float`, `inline-block` 금지
2. **position은 relative/absolute만** — `fixed`, `sticky` 금지
3. **z-index 대신 DOM 순서** — 나중에 오는 요소가 위에 렌더링됨
4. **@keyframes 대신 transition** — 복잡한 애니메이션은 별도 C# 계획 필요
5. **::before / ::after 미사용** — 실제 자식 요소로 대체
6. **box-shadow, text-shadow, filter 미사용** — 대안 설계 필요
7. **calc(), clamp() 미사용** — 고정값 또는 flex 비율로 대체
8. **@media 미사용** — 단일 해상도 기준 설계 후 C#으로 반응형 처리
9. **CSS 변수(var())는 사용 가능** — USS도 CSS 변수 지원
10. **색상은 rgb() 사용** — hsl() 미지원, hex는 일부 상황에서 미지원

## 관련 페이지

- [HTML → UXML 요소 대응표](html-to-uxml-elements.md)
- [CSS 속성 USS 지원 여부](css-to-uss-support.md)
- [레이아웃 변환 가이드](html-to-uxml-layout.md)
- [미지원 CSS 패턴 우회 방법](uss-workarounds.md)
- [USS 전용 속성 (-unity-*)](uss-exclusive-properties.md)
