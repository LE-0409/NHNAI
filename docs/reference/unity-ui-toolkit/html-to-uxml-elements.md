# HTML → UXML 요소 대응표

> HTML 태그를 UXML 컨트롤로 변환할 때의 1:1 매핑 레퍼런스

## 구조 요소

| HTML | UXML | 비고 |
|------|------|------|
| `<div>`, `<section>`, `<article>`, `<main>`, `<header>`, `<footer>`, `<nav>`, `<aside>` | `<VisualElement>` | 시맨틱 구분 없음 |
| `<span>` | `<VisualElement>` 또는 `<Label>` | 텍스트면 Label |
| `<ul>` / `<ol>` | `<VisualElement>` + 자식 | 항목 많으면 `<ListView>` 권장 |
| `<li>` | `<VisualElement>` 또는 `<Label>` | |
| `<table>` / `<tr>` / `<td>` | `<VisualElement>` (Flexbox 행/열) | table display 미지원 |
| `<fieldset>` | `<GroupBox>` | |
| `<details>` / `<summary>` | `<Foldout>` | |
| `<hr>` | `<VisualElement>` (높이 1px + 배경색) | |

## 텍스트 요소

| HTML | UXML | 비고 |
|------|------|------|
| `<h1>` ~ `<h6>` | `<Label>` | font-size로 크기 구분 |
| `<p>`, `<span>` | `<Label>` | |
| `<strong>` / `<b>` | `<Label>` | `-unity-font-style: bold` |
| `<em>` / `<i>` | `<Label>` | `-unity-font-style: italic` |
| `<a>` | `<Label>` + `RegisterCallback<ClickEvent>` | |
| `<br>` | `\n` in Label text | |
| `<pre>` / `<code>` | `<Label>` | 고정폭 폰트 에셋 지정 |

## 폼 / 입력 요소

| HTML | UXML | 비고 |
|------|------|------|
| `<input type="text">` | `<TextField>` | |
| `<input type="password">` | `<TextField>` | `isPasswordField="true"` |
| `<input type="number">` | `<IntegerField>` / `<FloatField>` | |
| `<input type="checkbox">` | `<Toggle>` | |
| `<input type="radio">` | `<RadioButton>` | |
| `<input type="range">` | `<Slider>` | |
| `<input type="button">`, `<button>` | `<Button>` | |
| `<input type="color">` | `<ColorField>` | Editor 전용 |
| `<textarea>` | `<TextField multiline="true">` | |
| `<select>` | `<DropdownField>` | |
| `<progress>` | `<ProgressBar>` | |
| `<input type="file">` | 없음 | C# File 다이얼로그 직접 구현 |
| `<form>` | `<VisualElement>` | form 제출 개념 없음, C#으로 처리 |

## 미디어 요소

| HTML | UXML | 비고 |
|------|------|------|
| `<img>` | `<Image>` | C#에서 Texture2D/Sprite 할당 |
| `<svg>` | `<Image>` + VectorImage 에셋 | |
| `<canvas>` | `<IMGUIContainer>` | IMGUI로 직접 드로잉 |
| `<video>` | `<Image>` + VideoPlayer (C#) | |
| `<audio>` | 없음 | C# AudioSource |

## 복합 UI 패턴

| 웹 패턴 | Unity 대응 | 구현 방식 |
|---------|-----------|---------|
| 모달 다이얼로그 | `<VisualElement>` (position: absolute) | UXML 트리 마지막에 배치 |
| 탭 UI | `<Button>` 목록 + C# display 전환 | |
| 무한 스크롤 리스트 | `<ListView>` | virtualization 내장 |
| 그리드 카드 | `<VisualElement>` (flex-wrap: wrap) | |
| 드롭다운 메뉴 | `<DropdownField>` 또는 커스텀 | |
| 아코디언 | `<Foldout>` | |
| 토스트 / 알림 | position: absolute + USS transition | |

## 지원하지 않는 HTML 요소

| HTML | 대안 |
|------|------|
| `<iframe>` | 없음 |
| `<script>` | C# 코드 |
| `<style>` (인라인) | .uss 파일 |
| `<map>` / `<area>` | C# PointerEvent |
| `<embed>` / `<object>` | 없음 |

## UXML 전용 요소 (HTML 대응 없음)

| UXML | 용도 |
|------|------|
| `<MinMaxSlider>` | 범위 슬라이더 |
| `<ListView>` | 가상화 스크롤 리스트 |
| `<TreeView>` | 계층형 트리 |
| `<Foldout>` | 접기/펼치기 |
| `<GroupBox>` | 그룹 컨테이너 |
| `<IMGUIContainer>` | IMGUI 임베딩 |
| `<TemplateContainer>` | UXML 템플릿 인스턴스 |

## 관련 페이지

- [HTML/CSS → UXML/USS 변환 가이드](html-to-uxml-guide.md)
