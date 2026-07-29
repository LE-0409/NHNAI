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
| `prototype/README.md` | **USS 호환 규칙과 치환표의 정본** — 쓰지 않는 속성, 짝 파일 규약 |
| 기존 화면 하나 (`prototype/rotate-gate.html` + `.css`) | 파일 구성·주석 방식의 본보기. **가장 최근에 쓴 것이라 규칙이 가장 잘 지켜져 있다** |

USS 호환 규칙(쓰지 않는 속성, 치환표)은 **이 스킬에 다시 적지 않는다.**
`prototype/README.md`가 유일한 정본이다. 속성별 지원 여부의 상세 근거가 필요하면
`docs/reference/unity-ui-toolkit/` (`css-to-uss-support.md`, `html-to-uxml-*.md`)를 본다.

## 산출물 구조

화면 하나당 두 파일. 나중에 만들 Unity 자산과 1:1로 짝을 이루도록 이름을 정한다.

```
prototype/{화면}.html   ←→  Assets/UI/Screens/{화면PascalCase}/{화면PascalCase}.uxml
prototype/{화면}.css    ←→  Assets/UI/Screens/{화면PascalCase}/{화면PascalCase}.uss
```

`<head>`에는 그 화면의 CSS 하나만 링크한다. **공용 `tokens.css` · `base.css` 는 없다** —
이 프로젝트는 전역 토큰 파일을 두지 않는다 (CLAUDE.md 「디자인 값은 화면마다 로컬
변수로 모은다」).

```html
<link rel="stylesheet" href="{화면}.css">
```

`.css` 맨 위에는 브라우저 전용 리셋 블록을 두고 **"USS 로 옮기지 않는다"** 고 표시한다.
UI Toolkit 은 기본이 flex 라 그 리셋이 하는 일을 이미 하고 있다.
`<body>` 첫 줄에는 Unity 쌍 파일 경로를 주석으로 남긴다.

## 지키는 것

- **landscape 1920×1080 기준.** 세로(portrait) 레이아웃은 만들지 않는다.
- 클래스는 **BEM** (`block__element--modifier`). 규칙은 CLAUDE.md ·
  `docs/reference/unity-ui-toolkit/uss-naming-conventions.md`.
- **값은 루트 클래스의 `--{화면}-*` 로컬 변수 한곳에 모은다.** 리터럴을 규칙 안에
  흩뿌리지 않는다. 각 변수에는 무엇을 조절하는 값인지 주석을 한 줄 붙인다.
- 2단계에서 USS 로 옮길 때 **같은 변수 이름과 같은 값을 쓴다.** 짝이 어긋나면
  프로토타입이 거짓말을 시작한다 (`prototype/README.md` 참조).
- **화면에 나가는 문구는 영어로 쓴다.** 기본 폰트에 한글 글리프가 없어 빌드에서만
  빈칸이 된다 (CLAUDE.md 「폰트 준비」).
- JS는 쓰지 않는다. 동작의 정본은 항상 C#이다 — JS에만 있는 동작을 만들지 않는다.
  움직이는 컨트롤이라면 위치와 크기만 보이고 나머지는 C# 에 맡긴다.

## 완료 기준

1. 브라우저에서 `.html`을 직접 열어 (서버 불필요) 레이아웃이 의도대로 나온다.
2. `prototype/README.md`의 「쓰지 않은 것」 목록이 CSS에 남아 있지 않다.
3. 값이 루트 클래스 변수로 모여 있고, 각 변수에 주석이 붙어 있다.
4. 시안 비교 요청이었다면 각 시안을 별도 파일 또는 전환 가능한 클래스로 제공한다.
5. `prototype/README.md` 의 「화면 짝」 표에 새 줄을 추가했다.

이후 변환(2단계~)은 CLAUDE.md 「개발 파이프라인」 절차를 따른다.
