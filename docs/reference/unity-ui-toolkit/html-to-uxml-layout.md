# 레이아웃 변환 가이드 (CSS → USS Flexbox)

> USS는 Flexbox만 지원한다. Grid/Float/Block을 Flexbox로 재설계하는 방법

## 전제 조건

- USS 레이아웃 = Flexbox 전용 (CSS Grid, Float, Block, Inline, Table 미지원)
- USS 기본 `flex-direction`은 **column** (CSS는 row — 반드시 명시)

### 고정 열 그리드 (repeat(3, 1fr))

```uss
.grid {
  flex-direction: row;
  flex-wrap: wrap;
}
.grid-item {
  width: 32%;
  margin-right: 2%;
  margin-bottom: 16px;
}
```

### Header + Sidebar + Content + Footer 레이아웃

```uss
.layout   { flex-direction: column; }
.header   { height: 60px; }
.body     { flex-direction: row; flex-grow: 1; }
.sidebar  { width: 240px; }
.content  { flex-grow: 1; }
.footer   { height: 40px; }
```

```uxml
<VisualElement class="layout">
  <VisualElement class="header"/>
  <VisualElement class="body">
    <VisualElement class="sidebar"/>
    <VisualElement class="content"/>
  </VisualElement>
  <VisualElement class="footer"/>
</VisualElement>
```

### 이미지 + 텍스트 float left

```uss
.container { flex-direction: row; }
.image     { width: 200px; margin-right: 16px; flex-shrink: 0; }
.text      { flex-grow: 1; }
```

### 양쪽 정렬 (float left + float right)

```uss
.bar { flex-direction: row; justify-content: space-between; }
```

### fixed → absolute (루트 기준)

```uss
/* 루트 VisualElement의 직계 자식으로 배치 */
.modal-overlay {
  position: absolute;
  top: 0; left: 0;
  width: 100%; height: 100%;
}
```

### sticky → C# 직접 처리

```csharp
scrollView.RegisterCallback<ScrollViewEvent>(evt =>
{
    float scrollY = scrollView.scrollOffset.y;
    stickyHeader.style.top = scrollY;
});
```

## z-index → 트리 순서 + C# API

```xml
<!-- 나중에 선언된 요소가 위에 렌더링 -->
<VisualElement class="background"/>
<VisualElement class="content"/>
<VisualElement class="overlay"/>   <!-- 맨 위 -->
```

```csharp
/* 런타임 z-order 변경 */
element.BringToFront();
element.SendToBack();
element.PlaceInFront(referenceElement);
element.PlaceBehind(referenceElement);
```

## gap 대체 패턴

USS는 `gap`을 지원하지 않는다. margin으로 대체한다.

```uss
/* gap: 16px 대체 */
.item {
  margin-right: 16px;
  margin-bottom: 16px;
}
```

## @media → C# GeometryChangedEvent

```csharp
root.RegisterCallback<GeometryChangedEvent>(evt =>
{
    float width = evt.newRect.width;
    if (width < 768f)
    {
        root.AddToClassList("layout-mobile");
        root.RemoveFromClassList("layout-desktop");
    }
    else
    {
        root.AddToClassList("layout-desktop");
        root.RemoveFromClassList("layout-mobile");
    }
});
```

```uss
.layout-mobile .content  { flex-direction: column; }
.layout-desktop .content { flex-direction: row; }
```

## 검증 방법

- UI Builder에서 요소 선택 → Inspector에서 Flex 설정 확인
- 요소가 예상 위치에 없으면 `flex-direction` 기본값(column) 확인
- 가로 나열 안 되면 부모에 `flex-direction: row` 명시 확인

## 관련 페이지

- [변환 가이드 개요](html-to-uxml-guide.md)
- [레이아웃 엔진 (Flexbox)](uss-layout-engine.md)
- [미지원 CSS 패턴 우회 방법](uss-workarounds.md)
