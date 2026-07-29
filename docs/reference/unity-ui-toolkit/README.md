# docs/reference/unity-ui-toolkit/ — UI Toolkit 참조 문서

이 저장소만 클론해도 개발에 필요한 UI Toolkit 지식을 참조할 수 있도록,
작성자 개인 위키(Coding-Inventory)에서 자주 쓰는 페이지만 골라 복사해 둔 것이다.
원문은 Unity 6.3 공식 문서·E-Book을 정리한 것이다 (2026-07 기준).

## 읽는 규칙

- **웹 검색 전에 여기부터 본다.** 여기에 없으면 웹 검색으로 보충하고,
  알아낸 내용은 이 폴더에 새 페이지로 추가하거나 `CLAUDE.md`에 반영해
  저장소를 자기완결로 유지한다.
- 이 문서들은 **일반 지식**이다. 프로젝트 특화 결정과 충돌하면
  `CLAUDE.md`와 `prototype/README.md`가 우선한다.

## 페이지 목록

### USS 스타일링

| 파일 | 내용 |
|---|---|
| `css-to-uss-support.md` | CSS 속성별 USS 지원 여부 전체 표 (✅/⚠️/❌) |
| `uss-workarounds.md` | 미지원 CSS 패턴 우회 방법 12종 |
| `uss-exclusive-properties.md` | `-unity-*` 전용 속성 레퍼런스 |
| `uss-naming-conventions.md` | BEM 네이밍 규칙 |
| `uss-transitions.md` | Transition · Transform 애니메이션 |
| `uss-layout-engine.md` | Flexbox(Yoga) 레이아웃 엔진 |

### HTML → UXML 변환

| 파일 | 내용 |
|---|---|
| `html-to-uxml-guide.md` | HTML/CSS → UXML/USS 변환 가이드 개요 |
| `html-to-uxml-elements.md` | HTML 태그 → UXML 요소 대응표 |
| `html-to-uxml-layout.md` | 레이아웃(Flexbox) 변환 가이드 |

### 컴포넌트 · 성능

| 파일 | 내용 |
|---|---|
| `custom-controls.md` | 커스텀 컨트롤 제작 (`[UxmlElement]`, `BaseField<T>`) |
| `performance-optimization.md` | 성능 최적화 (드로우 콜, `usageHints`, 셀렉터 비용) |
