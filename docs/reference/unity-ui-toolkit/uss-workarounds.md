# 미지원 CSS 패턴 USS 우회 방법

> USS에서 지원하지 않는 CSS 패턴별 Unity UI Toolkit 대안 구현

## box-shadow

**방법 1: VisualElement 중첩**
```uxml
<VisualElement class="card-wrapper">
  <VisualElement class="card-shadow"/>
  <VisualElement class="card"/>
</VisualElement>
```
```uss
.card-shadow {
  position: absolute;
  top: 4px; left: 4px;
  width: 100%; height: 100%;
  background-color: rgba(0, 0, 0, 0.15);
  border-radius: 8px;
}
```

**방법 2: border로 단순화**
```uss
.card { border-width: 1px; border-color: rgba(0, 0, 0, 0.12); }
```

## text-shadow

`-unity-text-outline`으로 근사 (방향성 없음):
```uss
.title {
  -unity-text-outline-width: 1px;
  -unity-text-outline-color: rgba(0, 0, 0, 0.4);
}
```

## @keyframes 애니메이션

**방법 1: USS transition (단순 두 상태)**
```uss
.element {
  opacity: 0;
  translate: 0 -10px;
  transition: opacity 0.3s ease, translate 0.3s ease;
}
.element.visible { opacity: 1; translate: 0 0; }
```
```csharp
element.AddToClassList("visible");
```

**방법 2: C# 코루틴 (복잡한 키프레임)**
```csharp
IEnumerator FadeIn(VisualElement el, float duration)
{
    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / duration;
        el.style.opacity = Mathf.Lerp(0f, 1f, t);
        yield return null;
    }
}
```

**방법 3: 실험적 Animation API (Unity 2022.2+)**
```csharp
using UnityEngine.UIElements.Experimental;
element.experimental.animation
    .Start(new StyleValues { opacity = 1f }, 300)
    .Ease(Easing.InOutSine);
```

## ::before / ::after

```csharp
/* ::before 대체 */
var icon = new Label("★");
icon.AddToClassList("badge-icon");
badge.Insert(0, icon);

/* ::after 대체 */
var line = new VisualElement();
line.AddToClassList("divider-line");
container.Add(line);
```
```uss
.badge-icon { color: rgb(255, 215, 0); }
.divider-line { height: 1px; background-color: rgb(204, 204, 204); }
```

## filter (blur, brightness, grayscale)

- `brightness(0.5)` → 반투명 검정 오버레이 (position: absolute)
- `blur` → 미리 blur 처리된 텍스처 에셋 사용 또는 커스텀 셰이더
- `grayscale` → 커스텀 셰이더 Material

## @media 미디어 쿼리

```csharp
root.RegisterCallback<GeometryChangedEvent>(evt =>
{
    bool isMobile = evt.newRect.width < 768f;
    root.EnableInClassList("layout-mobile", isMobile);
    root.EnableInClassList("layout-desktop", !isMobile);
});
```

## calc()

**방법 1: flex-grow 활용**
```uss
/* calc(100% - 240px) 대체 */
.sidebar { width: 240px; }
.main    { flex-grow: 1; }
```

**방법 2: C# 계산**
```csharp
root.RegisterCallback<GeometryChangedEvent>(evt =>
{
    panel.style.width = evt.newRect.width - 240f;
});
```

## z-index

```csharp
element.BringToFront();
element.SendToBack();
element.PlaceInFront(referenceElement);
element.PlaceBehind(referenceElement);
```

## text-decoration (underline, line-through)

**방법 1: Rich Text Tags**
```csharp
label.text = "<u>밑줄 텍스트</u>";
label.text = "<s>취소선 텍스트</s>";
```

**방법 2: border 구분선으로 밑줄**
```uss
.underline-wrapper { flex-direction: column; }
.underline { height: 1px; background-color: currentColor; }
```

## pointer-events: none

```csharp
element.pickingMode = PickingMode.Ignore;   // pointer-events: none
element.pickingMode = PickingMode.Position; // pointer-events: auto
```
```uxml
<VisualElement picking-mode="Ignore"/>
```

## hsl() 색상

USS 미지원. 사전에 rgb()로 변환:
```uss
/* hsl(210, 100%, 56%) → */
.element { color: rgb(33, 150, 243); }
```

## CSS 변수 색상 포맷

```uss
/* hex 대신 rgb() 사용 권장 */
:root { --primary: rgb(33, 150, 243); }
.btn  { background-color: var(--primary); }
```

## 관련 페이지

- [변환 가이드 개요](html-to-uxml-guide.md)
- [CSS 속성 USS 지원 여부](css-to-uss-support.md)
- [USS Transitions & Transform](uss-transitions.md)
