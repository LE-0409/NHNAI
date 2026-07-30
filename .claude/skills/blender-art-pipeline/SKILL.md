---
name: blender-art-pipeline
description: >
  NHNAI 의 3D 에셋을 Blender 헤드리스로 생성·수정할 때 읽는 스킬. ArtPipeline 환경 세팅
  (setup.ps1 · 저장소 밖 config), 생성 스크립트 실행(run_blender.ps1), 팔레트 레지스트리 규칙
  (선언 순서 = UV 셀 인덱스), FBX 재익스포트 노이즈 처리, Unity 쪽 머티리얼 리맵
  (ArtMaterialLibrary · NHNAI > Setup > 1) 을 다룬다.
  "3D 모델 만들어줘", "방 크기 바꿔줘", "에셋 다시 뽑아줘", "Blender", "FBX", "팔레트",
  "머티리얼이 회색", "색이 마젠타", "generate_*.py" 같은 요청이면 트리거한다.
---

# blender-art-pipeline — Blender 에셋 파이프라인

CLAUDE.md 「3D 에셋 — Blender 파이프라인」에서 옮겨 왔다.

3D 에셋은 스토어 에셋이나 외부 AI 서비스를 쓰지 않고 **Blender를 헤드리스로 돌려 코드로 생성**한다.
파이프라인은 `ArtPipeline/`에 있다. 게임을 열어보기만 한다면 Blender는 필요 없다.

`Assets/Art/` 는 이 파이프라인의 산출물이다. **손으로 만들지 않는다.**

---

## 환경 세팅 — **로컬 설정이다. 커밋 대상이 아니다**

Blender 설치 위치는 개발자마다 다르다(드라이브 용량·기존 설치 여부). 그래서 선택은
**저장소 밖** `%APPDATA%\BlenderArtKit\config.json`에 저장되고, 같은 파이프라인을 쓰는
다른 저장소와도 공유된다.

무엇을 하든 **먼저 상태를 본다.** 아무것도 바꾸지 않고 지금 무엇이 쓰이는지만 출력한다.

```powershell
cd ArtPipeline
.\setup.ps1 -Status
```

| 요청 | 명령 |
|---|---|
| 설치해줘 | `.\setup.ps1` |
| 특정 위치에 설치해줘 | `.\setup.ps1 -InstallDir <사용자가 말한 경로>` |
| 이미 깔린 Blender 쓰게 해줘 | `.\setup.ps1 -BlenderExe <그 blender.exe 경로>` |
| 위치 바꿔줘 | 같은 명령을 다시 실행 (이미 받아둔 것은 다시 받지 않고 옮긴다) |
| 설정 되돌려줘 | `.\setup.ps1 -Reset` |

⚠️ **경로를 저장소 파일에 하드코딩하지 않는다.** `blender_common.ps1`·`run_blender.ps1`·
`setup.ps1`은 공용 kit이라 모든 개발자가 공유한다. 개인 경로를 여기 박으면 남의 클론이 깨지고,
무엇보다 로컬 설정이 커밋에 남는다. 위치 관련 요청은 **전부 위 명령으로 처리**한다 —
`setup.ps1`을 돌린 결과로 git 워킹트리가 변하면 그건 비정상이다.

---

## 에셋 생성

```powershell
.\run_blender.ps1 assets\<에셋>\generate_<에셋>.py
```

생성 스크립트는 `ArtPipeline/assets/<에셋>/` 깊이에 둔다(그래야 라이브러리 임포트 경로가 맞는다).
결과 FBX는 `Assets/Art/` 밑으로, 확인용 턴어라운드 렌더는 `ArtPipeline/previews/`로 나온다.
**렌더를 눈으로 확인하고 다음 단계로 간다** — 이게 이 파이프라인의 핵심 루프다.

- 색은 `ArtPipeline/project/palette_registry.py`가 정본이다. **선언 순서 = 팔레트 셀 인덱스**라
  순서를 바꾸거나 중간에 끼워 넣으면 이미 익스포트된 에셋의 UV가 전부 어긋난다. 추가는 맨 뒤에만.
- 생성 스크립트 하나가 FBX 여러 개를 다시 export하고, FBX는 헤더 타임스탬프와 오브젝트 UID가
  매번 새로 생성돼 **손대지 않은 파일까지 `M`으로 뜬다.** 커밋 전에 실제로 바꾼 것만 남기고
  나머지는 `git checkout --`으로 되돌린다.
- Unity 쪽 연동(팔레트 텍스처 설정 → `.mat` 생성 → FBX 머티리얼 리맵)은
  `Assets/Editor/ArtMaterialLibrary.cs` 가 한다. **자동 임포트 후처리가 아니라 메뉴다** —
  `NHNAI > Setup > 1` 을 눌러야 돈다. 새 FBX 를 넣으면 회색 단색으로 보이는데,
  그건 리맵을 아직 안 돌린 것이다.

---

## 문서

`ArtPipeline/` 은 다른 저장소에서 가져온 **벤더링 kit** 이다. 아래 셋은 kit 자체의
문서라서 이 프로젝트가 쓰지 않는 기능(리깅·애니메이션·kit 설치)도 설명한다 —
이 저장소가 실제로 쓰는 것은 메시 빌더 · 팔레트 · 익스포트 · 프리뷰 네 모듈이다.

| 주제 | 파일 |
|---|---|
| 파이프라인 설치·사용 절차 | `ArtPipeline/NEW-PROJECT.md` |
| 경계·설정 스키마·팔레트 계약 | `ArtPipeline/KIT.md` |
| 이 kit이 어디서 왔는지 | `ArtPipeline/KIT-ORIGIN.txt` |

---

FBX 색이 마젠타·회색으로 나오는 등 증상은 `nhnai-troubleshooting` 스킬의 표를 본다.
씬에 놓는 것과 조명은 `unity-scene-bootstrap` 스킬이다.
