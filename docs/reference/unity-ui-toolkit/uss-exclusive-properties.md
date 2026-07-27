# USS 전용 속성 (-unity-*)

> CSS에 없고 USS에서만 존재하는 속성들. Unity UI Toolkit 고유 기능을 스타일로 제어할 때 사용

### `-unity-font`
Unity Font 에셋 참조. CSS `font-family` 대체.
```uss
.label { -unity-font: resource("Fonts/Roboto"); }
```

### `-unity-font-definition`
TextCore FontAsset(SDF 렌더링) 참조. 고품질 텍스트 렌더링 필요 시 사용.
```uss
.label { -unity-font-definition: resource("Fonts/Roboto SDF"); }
```

### `-unity-font-style`
| 값 | 설명 |
|----|------|
| `normal` | 기본 |
| `bold` | 굵게 (CSS font-weight: bold 대체) |
| `italic` | 기울임 (CSS font-style: italic 대체) |
| `bold-and-italic` | 굵게 + 기울임 |

```uss
.title { -unity-font-style: bold; }
```

### `-unity-text-align`
CSS `text-align`을 대체하며 수직 정렬도 포함하는 9방향 정렬.

```uss
.centered { -unity-text-align: middle-center; }
.top-left { -unity-text-align: upper-left; }
```

방향 값: `upper-left` / `upper-center` / `upper-right` / `middle-left` / `middle-center` / `middle-right` / `lower-left` / `lower-center` / `lower-right`

### `-unity-background-scale-mode`
CSS `background-size` 대체.

| 값 | CSS 대응 | 설명 |
|----|----------|------|
| `stretch-to-fill` | `100% 100%` | 비율 무시 |
| `scale-and-crop` | `cover` | 비율 유지, 채움 |
| `scale-to-fit` | `contain` | 비율 유지, 완전히 들어옴 |

```uss
.banner {
  background-image: url("project://Assets/banner.png");
  -unity-background-scale-mode: scale-and-crop;
}
```

### `-unity-background-image-tint-color`
배경 이미지에 색상 곱셈 적용. 흰색 아이콘을 다양한 색상으로 재사용할 때 유용.
```uss
.icon {
  background-image: url("project://Assets/icon-white.png");
  -unity-background-image-tint-color: rgb(33, 150, 243);
}
```

## 9-슬라이스

크기가 변해도 모서리가 왜곡되지 않는 배경 이미지 분할.

```uss
.button {
  background-image: url("project://Assets/button-bg.png");
  -unity-slice-left: 12;
  -unity-slice-right: 12;
  -unity-slice-top: 12;
  -unity-slice-bottom: 12;
  -unity-slice-scale: 1.0; /* DPI 배율 */
}
```

## 텍스트 외곽선

CSS `text-shadow`의 근사 대체재 (방향성 없음).

```uss
.outlined {
  color: white;
  -unity-text-outline-width: 1.5px;
  -unity-text-outline-color: rgb(0, 0, 0);
}
```

### `-unity-text-overflow-position`
`text-overflow: ellipsis` 적용 시 말줄임표 위치.

| 값 | 설명 |
|----|------|
| `end` | 끝 (기본값) |
| `start` | 앞 |
| `middle` | 중간 — 파일명 생략에 유용 |

```uss
.filename {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  -unity-text-overflow-position: middle;
}
```

### `-unity-overflow-clip-box`
`overflow: hidden` 클리핑 기준. `padding-box`(기본) / `content-box`.

### `-unity-paragraph-spacing`
여러 줄 텍스트에서 단락 간 추가 간격(px).
```uss
.body { -unity-paragraph-spacing: 8px; }
```

## USS 개별 Transform 속성

CSS `transform: translate/rotate/scale()`을 USS에서 개별 속성으로 분리. transition 세부 지정이 쉬워진다.

```uss
/* CSS: transform: translate(20px, 10px) rotate(45deg) scale(1.2) */
.element {
  translate: 20px 10px;
  rotate: 45deg;
  scale: 1.2 1.2;
}

/* hover 애니메이션 */
.card {
  translate: 0 0;
  scale: 1 1;
  transition: translate 0.2s ease, scale 0.2s ease;
}
.card:hover {
  translate: 0 -4px;
  scale: 1.02 1.02;
}
```

## UsageHints (C# 전용)

CSS `will-change`의 대체재. USS 속성이 아니라 C# API로만 설정.

```csharp
element.usageHints = UsageHints.DynamicTransform; // 자주 이동하는 요소
element.usageHints = UsageHints.DynamicColor;     // 자주 색상 변경
container.usageHints = UsageHints.GroupTransform; // 자식 전체 레이어 처리
```

## 관련 페이지

- [CSS 속성 USS 지원 여부](css-to-uss-support.md)
- [USS Transitions & Transform](uss-transitions.md)
