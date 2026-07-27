# 레이아웃 엔진 (Flexbox / Yoga)

> UI Toolkit은 Yoga 엔진으로 CSS Flexbox 서브셋을 구현 — 요소의 위치·크기를 자동 계산하는 반응형 레이아웃 시스템

## Flexbox란?

"Flexbox is a method for arranging items in rows or columns." UI Toolkit은 **Yoga** (Facebook의 Flexbox 구현)을 사용.

### Flexbox 장점
| 장점 | 설명 |
|------|------|
| 반응형 | 해상도 변화에 중첩 컨테이너 자동 적응 |
| 재사용 | 스타일을 수백 요소에 일관 적용 |
| 분리 | UI 레이아웃이 코드 로직과 독립 |

### Direction & Wrap
```uss
.container {
    flex-direction: row;     /* 가로 배치 (기본) */
    flex-direction: column;  /* 세로 배치 */
    flex-wrap: nowrap;       /* 한 줄 (기본) */
    flex-wrap: wrap;         /* 여러 줄로 분배 */
}
```

### 크기 지정
```uss
.element {
    width: 200px;       /* 고정 크기 */
    width: 50%;         /* 부모 대비 % */
    width: auto;        /* 자동 (기본) */
    min-width: 100px;
    max-width: 500px;
}
```

### Flex 속성 (반응형 크기)
```uss
.flexible {
    flex-basis: 100px;  /* grow/shrink 전 기본 크기 */
    flex-grow: 1;       /* 여백 전부 차지 */
    flex-grow: 0.5;     /* 여백의 절반 */
    flex-shrink: 1;     /* 필요 시 축소 */
    flex-shrink: 0;     /* 크기 유지 (오버플로우 가능) */
}
```

### 정렬
```uss
.container {
    /* 교차축 정렬 */
    align-items: flex-start;  /* 시작 |
    align-items: center;      /* 중앙 */
    align-items: flex-end;    /* 끝 */
    align-items: stretch;     /* 늘리기 (기본) */
    
    /* 주축 간격 분배 */
    justify-content: flex-start;     /* 시작 정렬 */
    justify-content: center;         /* 중앙 */
    justify-content: space-between;  /* 양끝 정렬 */
    justify-content: space-around;   /* 균등 간격 */
}

.child {
    align-self: center;  /* 개별 정렬 오버라이드 */
}
```

### Relative (기본값)
```uss
.my-element {
    position: relative;  /* 기본값 */
    /* 부모 Flexbox 규칙 따름 */
    /* 형제에 영향을 줌 */
}
```
- 영구적·복잡한 UI 구조에 적합
- 부모 크기/스타일 변화에 자동 반응

### Absolute
```uss
.overlay {
    position: absolute;
    left: 10px;
    top: 20px;
    right: 10px;
    bottom: 20px;
    /* Grow/Shrink/Margin 무시 */
    /* 형제 레이아웃에 영향 없음 */
}
```
- 팝업, 오버레이, 장식 요소에 적합
- 동적 캐릭터 인디케이터

## 여백
```uss
.element {
    margin: 8px;          /* 외부 여백 (모든 방향) */
    margin-top: 4px;
    padding: 12px;        /* 내부 여백 */
    padding-left: 16px;
}
```

## 실용 워크플로우
1. 중첩 박스(컨테이너 + 자식)로 UI 목업
2. 부모 `flex-direction` 설정
3. `flex-grow` / `flex-shrink`으로 반응성 구성
4. `justify-content` / `align-items`으로 간격 조정
5. 플랫폼 확장성: `%` 단위 선호
6. 타겟 해상도에서 테스트

## Panel Settings — Scale Modes

| 모드 | 설명 | 용도 |
|------|------|------|
| `Constant Pixel Size` | 고정 픽셀 + 선택적 스케일 팩터 | 픽셀 아트 |
| `Constant Physical Size` | 기기 전반 물리적 크기 유지 | 모바일 |
| `Scale with Screen Size` | 기준 해상도로 동적 스케일링 | 일반적 |

## C#에서 레이아웃 설정
```csharp
var element = new VisualElement();
element.style.flexDirection = FlexDirection.Row;
element.style.flexGrow = 1;
element.style.width = new Length(50, LengthUnit.Percent);
element.style.justifyContent = Justify.Center;
element.style.alignItems = Align.Center;
```

## 주의사항
- 애니메이션에서 `width`/`height`/`position` 변경 → 레이아웃 재계산 비쌈 → `translate`/`scale`/`rotate` 사용
- Absolute 요소는 형제 레이아웃에 영향 없음
- Flex 속성은 부모 `flex-direction`에 따라 주축/교차축 방향 결정

## 관련 페이지
- [성능 최적화](performance-optimization.md)

## 출처
- [Layouts (E-Book)](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/layouts.html)
- [Position element with layout engine](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-LayoutEngine.html)
