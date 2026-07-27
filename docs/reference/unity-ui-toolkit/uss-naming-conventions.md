# USS 네이밍 컨벤션 (BEM)

> Unity UI Toolkit에서 권장하는 BEM(Block Element Modifier) 기반 CSS 네이밍 표준

## BEM이란?

CSS/웹 개발 표준 네이밍 방식. Unity 공식 e-book이 강력 권장.

형식: `block-name__element-name--modifier-name`

### Block (블록)
최상위 독립 UI 컴포넌트.
```text
navbar-menu          ← 내비게이션 바 전체
character-stats      ← 캐릭터 스탯 패널
inventory-panel      ← 인벤토리 패널
```
일반 요소는 블록 생략 가능: `button--small`

### Element (요소)
블록의 자식 — `__`로 연결.
```text
navbar-menu__shop-button    ← 내비게이션 바 안의 쇼핑 버튼
character-stats__health-bar ← 스탯 패널 안의 체력바
inventory-panel__item-slot  ← 인벤토리 패널의 아이템 슬롯
```

### Modifier (수정자)
변형 또는 상태 — `--`로 연결.
```text
navbar-menu__shop-button--small      ← 작은 버전
button--quit                         ← 종료 버튼 변형
character-stats__health-bar--low     ← 체력 낮음 상태
```

## 전체 예시

```uss
/* 블록 */
.character-card { ... }

/* 요소 */
.character-card__portrait { ... }
.character-card__name-label { ... }
.character-card__level-badge { ... }

/* 수정자 */
.character-card--selected { ... }
.character-card--legendary { ... }
.character-card__level-badge--max { ... }
```

## C#에서 적용

```csharp
public class CharacterCard : VisualElement
{
    public CharacterCard()
    {
        AddToClassList("character-card");
        
        var portrait = new Image();
        portrait.AddToClassList("character-card__portrait");
        Add(portrait);
        
        var nameLabel = new Label();
        nameLabel.AddToClassList("character-card__name-label");
        Add(nameLabel);
    }
    
    public void SetSelected(bool selected)
    {
        if (selected)
            AddToClassList("character-card--selected");
        else
            RemoveFromClassList("character-card--selected");
    }
}
```

## 핵심 가이드라인

| 규칙 | 좋은 예 | 나쁜 예 |
|------|---------|---------|
| 의미론적 네이밍 | `button--quit` | `button--red` |
| 역할 우선 | `nav-menu__link` | `div__anchor` |
| 상태 수정자 | `item--selected` | `item--blue-border` |
| Kebab case | `shop-button` | `shopButton` |

## 추가 가이드라인
- 허용 문자: 라틴 문자, 숫자, 대시만
- 짧지만 설명적인 이름
- 변경 가능한 속성 기반 이름 지양 (색상, 위치 등)
- 아트 에셋(스프라이트, 텍스처)까지 컨벤션 확장
- 멀티 프로젝트 환경: 클래스 충돌 방지를 위해 접두사 추가 (`proj-`)
- 코드와 에셋 전반에 일관성 유지

## 주의사항
- `class`/`name`/`style` 속성은 UXML 템플릿 AttributeOverrides로 변경 불가
- 생성자에서 `AddToClassList()` 사용 권장 (UXML의 `class` 속성과 동일 효과)

## 출처
- [Naming conventions (E-Book)](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/naming-conventions.html)
