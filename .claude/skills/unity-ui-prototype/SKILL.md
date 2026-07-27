---
name: unity-ui-prototype
description: >
  NHNAI 새 화면의 HTML/CSS 프로토타입을 만드는 스킬. "새 화면", "프로토타입",
  "레이아웃 잡아줘", "시안 비교", "Unity UI Toolkit 변환용 HTML" 같은 요청이면 트리거한다.
  CLAUDE.md 개발 파이프라인 1단계(프로토타입 작성)를 담당하며, USS 호환 CSS 규칙을 지킨
  HTML/CSS를 prototype/ 에 생성한다. 기존 컴포넌트의 색·간격 조정이나 라벨 문구 교체처럼
  프로토타입을 건너뛰는 작업(CLAUDE.md 참조)에는 사용하지 않는다.
---

# unity-ui-prototype — 화면 프로토타입 제작

새 화면을 Unity에 만들기 전에 브라우저에서 레이아웃을 확정하는 HTML/CSS를 만든다.
이 스킬은 **CLAUDE.md 개발 파이프라인의 1단계**다. 결과물은 `prototype/`에 두고,
이후 2단계(UXML/USS 변환)로 넘어간다.

## 시작 전에 반드시 읽는 파일

| 파일 | 이유 |
|---|---|
| `prototype/README.md` | **USS 호환 규칙의 정본** — 금지·허용 속성, 변환 치환표, base.css 리셋 설명 |
| `DESIGN.md` | 디자인 토큰 원본. 여기 없는 색·간격은 쓰지 않는다 |
| `prototype/tokens.css` · `base.css` | 이미 정의된 변수·유틸 클래스. 중복 정의 금지 |
| 기존 화면 (`prototype/card-spread.html`) | 파일 구성·주석·JS 작성 방식의 본보기 |

USS 호환 규칙(금지 속성, 치환표)은 **이 스킬에 다시 적지 않는다.**
`prototype/README.md`가 유일한 정본이다. 상세 근거가 필요하면
`docs/reference/unity-ui-toolkit/` (css-to-uss-support.md, html-to-uxml-*.md)를 본다.

## 산출물 구조

화면 하나당 두 파일. 나중에 만들 Unity 자산과 1:1로 짝을 이루도록 이름을 정한다.

```
prototype/{화면}.html   ←→  Assets/UI/Screens/{화면PascalCase}/{화면PascalCase}.uxml
prototype/{화면}.css    ←→  Assets/UI/Screens/{화면PascalCase}/{화면PascalCase}.uss
```

HTML `<head>`의 스타일 링크 순서는 고정이다 (USS 적용 순서와 일치시킨다):

```html
<link rel="stylesheet" href="tokens.css">
<link rel="stylesheet" href="base.css">
<!-- 사용하는 컴포넌트 CSS (예: card.css) -->
<link rel="stylesheet" href="{화면}.css">
```

`<body>` 첫 줄에는 Unity 쌍 파일 경로를 주석으로 남긴다 (`card-spread.html` 참조).

## 지키는 것

- **landscape 1920×1080 기준.** 세로(portrait) 레이아웃은 만들지 않는다.
- 클래스는 **BEM** (`block__element--modifier`). 규칙은 CLAUDE.md · `docs/reference/unity-ui-toolkit/uss-naming-conventions.md`.
- 값은 `var(--토큰)`으로만. 토큰에 없는 값이 필요하면 **먼저 `DESIGN.md`에 추가**하고
  `tokens.uss` · `tokens.css` 세 파일을 같이 고친다.
- `base.css` 맨 위의 flex 리셋을 지우거나 덮어쓰지 않는다. 이 리셋이 브라우저를 USS 동작에 맞춘다.
- JS는 단일 파일 안의 vanilla만, **동작 확인용**이다. 정본은 항상 C# — JS에만 있는 동작을 만들지 않는다.

## 완료 기준

1. 브라우저에서 `.html`을 직접 열어 (서버 불필요) 레이아웃이 의도대로 나온다.
2. `prototype/README.md`의 금지 속성이 CSS에 남아 있지 않다.
3. 시안 비교 요청이었다면 각 시안을 별도 파일 또는 전환 가능한 클래스로 제공한다.

이후 변환(2단계~)은 CLAUDE.md 「개발 파이프라인」 절차를 따른다.
