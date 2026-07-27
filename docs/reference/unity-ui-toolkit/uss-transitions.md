# USS Transitions & Transform

> CSS 트랜지션과 유사한 USS 애니메이션 시스템. Transform 속성(translate/scale/rotate)을 사용하면 레이아웃 재계산 없이 요소를 애니메이션할 수 있다.

## USS Transitions

USS 트랜지션은 지정된 시간 동안 속성 값을 부드럽게 변경한다.

### 핵심 속성

| 속성 | 역할 |
|------|------|
| `transition-property` | 애니메이션할 USS 속성 지정 |
| `transition-duration` | 애니메이션 길이 (s 또는 ms) |
| `transition-timing-function` | 이징 곡선 |
| `transition-delay` | 시작 지연 시간 |

### USS 예시

```uss
.my-button {
    transition-property: color, rotate;
    transition-duration: 0.2s;
    transition-timing-function: ease-in-out;
    color: white;
    background-color: #4A4A4A;
}

.my-button:hover {
    color: yellow;
    background-color: #666666;
}

.my-button:active {
    rotate: 5deg;
    scale: 0.95 0.95;
}
```

### C# 구현

```cs
element.style.transitionProperty = new List<StylePropertyName> { "rotate", "color" };
element.style.transitionDuration = new List<TimeValue> {
    new(0.2f, TimeUnit.Second)
};
element.style.transitionTimingFunction = new List<EasingFunction> {
    EasingMode.EaseInOut
};
element.style.transitionDelay = new List<TimeValue> { 0 };
```

### 이징 함수

`ease`, `ease-in`, `ease-out`, `ease-in-out`, `linear`, `ease-in-sine`, `ease-in-bounce` 등

---

## USS Transform Properties

레이아웃 계층의 다른 요소에 영향을 주지 않고 요소를 변환한다 — 재계산 비용 없음.

### 핵심 Transform 속성

| 속성 | 역할 |
|------|------|
| `transform-origin` | 회전/스케일 기준점 (피벗), 기본값 `center` |
| `translate` | 위치 이동 (px 또는 %) |
| `scale` | 크기 조정 (음수 = 반전) |
| `rotate` | 회전 (deg, grad, rad, turn 단위) |

### USS 예시

```uss
.card {
    transform-origin: center;
    transition-property: rotate, scale;
    transition-duration: 0.3s;
}

.card:hover {
    rotate: 5deg;
    scale: 1.05;
}

.card--flipped {
    rotate: 180deg;
    scale: -1 1;  /* 가로 반전 */
}
```

### C# 구현

```cs
// 변환 기준점 설정
element.style.transformOrigin = new TransformOrigin(
    Length.Percent(50), Length.Percent(50));

// 이동 (레이아웃 재계산 없음)
element.style.translate = new Translate(Length.Percent(10), 50);

// 스케일
element.style.scale = new Scale(new Vector2(1.2f, 1.2f));

// 회전
element.style.rotate = new Rotate(Angle.Degrees(45));
```

### 변환 적용 순서

**Scale → Rotate → Translate**

---

## 주의사항

- 첫 프레임에는 이전 상태가 없으므로 첫 프레임 이후부터 트랜지션이 작동한다
- 단위 일치 필수: `left` 기본값은 `auto` → 트랜지션 시 `0px`로 명시
- `width`/`height` 트랜지션 → 레이아웃 재계산 발생 → 성능 비용 큼
- 애니메이션에는 `translate`/`scale`/`rotate` 사용 권장 (레이아웃 재계산 없음)

## 언제 무엇을 쓰나

| 목적 | 권장 속성 |
|------|-----------|
| 정적 레이아웃 정의 | `position`, `width`, `height` |
| 이동 애니메이션 | `translate` |
| 크기 변경 애니메이션 | `scale` |
| 회전 애니메이션 | `rotate` |

## 관련 페이지
- [레이아웃 엔진 (Flexbox)](uss-layout-engine.md) — Yoga Flexbox 배치
- [성능 최적화](performance-optimization.md) — UsageHints, 렌더링 비용

## 출처
- `raw/unity-ui-toolkit-uss-transitions.md`
