# ArtPipeline kit — 다른 프로젝트에서 재사용하기

이 폴더의 Blender 파이프라인은 두 층으로 갈라져 있다. **kit**은 게임과 무관한 제네릭 층이고,
**프로젝트 레이어**는 Dungeon Miner의 색·에셋·경로다. 다른 프로젝트로 가져가는 것은 kit뿐이다.

> 📄 **처음 깔아보는 거라면 [NEW-PROJECT.md](NEW-PROJECT.md)** 를 먼저 볼 것 — 이식부터 첫 에셋까지
> 순서대로 따라가는 절차서다. 이 문서는 경계·스키마·계약을 다루는 레퍼런스다.

| | 파일 | 프로젝트에 묶이나 |
|---|---|---|
| **kit** | `blender_common.ps1`, `run_blender.ps1`, `setup.ps1`, `install_kit.ps1` | ✗ |
| **kit** | `lib/lowpoly_lib/*.py` (config·paths·palette·builders·export·preview·rigging·animation) | ✗ |
| **kit** | `kit_templates/`, `KIT.md` | ✗ |
| 프로젝트 | `pipeline.json` — 출력 경로·레지스트리 위치 | ✓ |
| 프로젝트 | `project/palette_registry.py` — 색 64셀 + 공유 머티리얼 슬롯 | ✓ |
| 프로젝트 | `assets/**` — 에셋 생성 스크립트 | ✓ |
| 프로젝트 | `CONVENTIONS.md` — 이 게임의 규약(치수·리그·팔레트 예외) | ✓ |
| 프로젝트 | `audio/**` — 사운드 파이프라인 (kit과 무관) | ✓ |

kit 파일에는 Dungeon Miner라는 단어도, `Assets/Art`라는 경로도 하드코딩되지 않는다.
kit에 게임 고유 값을 넣고 싶어지면 그건 `pipeline.json`이나 레지스트리로 가야 한다는 신호다.

---

## 1. Blender 설치 위치는 개발자가 고른다

드라이브 용량도, 이미 깔린 Blender가 있는지도 사람마다 다르다. 그래서 위치는 코드에 박지 않고
**레포 밖 사용자 설정 파일**에 저장한다. 환경변수와 달리 셸을 닫아도 남고, 레포에 커밋돼
남의 경로를 덮어쓰지도 않는다. 같은 kit을 쓰는 **모든 프로젝트가 이 설정 하나를 공유**하므로
Blender를 프로젝트마다 다시 받지 않는다.

```
%APPDATA%\BlenderArtKit\config.json      ($env:BLENDER_KIT_CONFIG로 이동 가능)
{ "installDir": "D:\\BlenderArtKit", "blenderExe": "..." }
```

### 무엇을 먼저 하든 `-Status`

지금 어디를 보고 있는지 출력만 하고 아무것도 바꾸지 않는다. 뭘 할지 정하기 전에 이걸 먼저 본다.

```powershell
.\setup.ps1 -Status
```

```
Lookup order (first hit wins):
  [ ] $env:BLENDER_EXE   (not set)
  [ ] config blenderExe  (not set)
  [x] config installDir  D:\BlenderArtKit
       -> D:\BlenderArtKit\blender-4.5.11-windows-x64\blender.exe
  [ ] repo tools/        ...\ArtPipeline\tools
  [ ] default cache      C:\Users\<user>\AppData\Local\BlenderArtKit
  [ ] Program Files      C:\Program Files\Blender Foundation\Blender 4.5\blender.exe

RESOLVED: D:\BlenderArtKit\blender-4.5.11-windows-x64\blender.exe
  via   : config installDir
```

### 위치 정하기

| 상황 | 명령 |
|---|---|
| 원하는 드라이브에 설치 | `.\setup.ps1 -InstallDir D:\BlenderArtKit` |
| 이미 Blender 4.5가 깔려 있음 | `.\setup.ps1 -BlenderExe "C:\Program Files\...\blender.exe"` |
| 아무거나 좋음 | `.\setup.ps1` → `%LOCALAPPDATA%\BlenderArtKit\` |
| 이 레포 안에 두고 싶음 | `.\setup.ps1 -Local` → `ArtPipeline\tools\` (설정에 저장하지 않음) |
| 나중에 옮기고 싶음 | `.\setup.ps1 -InstallDir E:\Tools` — **이미 받아둔 같은 버전이 있으면 다시 받지 않고 옮긴다** (`-Copy`면 복사) |
| 설정을 없던 걸로 | `.\setup.ps1 -Reset` |

`-InstallDir`과 `-BlenderExe`는 선택을 설정 파일에 저장한다. 그다음부터는 인자 없이 `run_blender.ps1`만
쓰면 된다.

**설정을 바꿀 때도 같은 명령을 그냥 다시 쓴다.** 둘은 "어느 Blender를 쓸 것인가"에 대한 서로 경쟁하는
답이라 하나를 정하면 다른 하나를 지운다 — 둘 다 남으면 우선순위가 높은 옛 설정이 계속 이겨서
"바꿨는데 안 바뀐다"가 되기 때문. 이 설정들은 전부 레포 밖에 있으므로 **setup.ps1을 돌린 결과로
git 워킹트리가 변하는 일은 없다.**

### 해석 순서

`blender_common.ps1`의 `Get-BlenderCandidates` 하나가 정한다 — `-Status` 출력과 실제 해석이
**같은 목록**을 쓰므로 "표시와 실제가 다른" 상황이 생기지 않는다.

1. `$env:BLENDER_EXE` — 이 셸 세션에서만 쓰는 임시 오버라이드
2. 설정 `blenderExe` — 직접 등록한 기존 설치
3. 설정 `installDir` — 직접 고른 포터블 설치 위치
4. `<ArtPipeline>/tools/blender-*/` — 레포에 박아둔 포터블
5. 기본 캐시 (`$env:BLENDER_KIT_HOME` 또는 `%LOCALAPPDATA%\BlenderArtKit`)
6. `C:\Program Files\Blender Foundation\Blender 4.5\`

**설정을 아직 안 만든 사람에게는 4번이 이긴다** → 레포 `tools/`에 이미 Blender가 있던 기존
협업자는 아무 변화 없이 그대로 쓴다. 설정을 만들면 그게 더 명시적인 의사표시라 2·3번이 앞선다.

⚠️ `setup.ps1`은 작업 후 **실제로 쓰일 경로를 다시 확인**해서, 방금 설치·등록한 것이 우선순위가
더 높은 후보에 가려지면 경고한다(조용히 무시되는 사고 방지).

### 이 레포의 자족성

클론 → `.\setup.ps1` → 동작이라는 조건은 그대로다. 설정 파일과 Blender 설치는 **setup이 만드는 것**이지
미리 있어야 하는 준비물이 아니다. 새 협업자는 여전히 아무것도 사전 설치할 필요가 없고,
kit 파일도 전부 레포 안에 있다 — 이식은 **사본을 내보내는 것**이지 참조로 바꾸는 게 아니다.

거꾸로, **이 레포를 지워도 파이프라인은 이어진다**: Blender는 레포 밖에 있고(설정 파일이
가리키는 위치), 이식된 프로젝트는 `install_kit.ps1`과 `kit_templates/`까지 포함한 **완전한 kit
사본**을 갖는다. 그래서 이식된 프로젝트가 다시 다음 프로젝트의 씨앗이 될 수 있고, 어느 한 레포가
사라져도 단절되지 않는다.

대신 사본이 늘면 오래된 kit이 퍼질 수 있다. 각 사본은 `ArtPipeline/KIT-ORIGIN.txt`에 출처
(경로·commit·날짜)를 기록하니, 의심되면 그것부터 비교하고 최신 원본에서 `-Force`로 다시 깔면 된다.
**개선은 되도록 한 곳에서 하고 내보내는 쪽으로 굴릴 것.**

---

## 2. 새 프로젝트에 kit 깔기

`<...>`는 각자 환경에 맞게 채우는 자리다 — `<원본 레포>`는 이 레포를 클론한 위치,
`<새 프로젝트>`는 kit을 깔 폴더(미리 존재해야 한다).

```powershell
cd <원본 레포>\ArtPipeline
.\install_kit.ps1 -Target <새 프로젝트>
```

`<새 프로젝트>\ArtPipeline\`에 kit + 스캐폴딩이 생긴다. 그다음:

```powershell
cd <새 프로젝트>\ArtPipeline
.\setup.ps1                                   # 공유 Blender가 있으면 즉시 통과
.\run_blender.ps1 assets\_smoke\kit_check.py  # 설정→팔레트→메시→렌더→FBX 전 계층 확인
```

주요 옵션:

| 옵션 | 뜻 |
|---|---|
| `-ProjectName "Other Game"` | `pipeline.json`의 표시 이름 (기본: 대상 폴더명) |
| `-AssetsRoot "Assets/Art"` | 에셋 출력 루트 (리포 루트 기준) |
| `-WithUnityEditorScripts` | Unity 임포트 후처리 C# 2종을 **참고 구현**으로 함께 복사 |
| `-Force` | 이미 있는 kit 파일 덮어쓰기 (= kit 업데이트) |

**`-Force`는 kit 파일에만 걸린다.** `pipeline.json`과 `project/palette_registry.py`는
대상 프로젝트 소유라 이미 있으면 절대 덮어쓰지 않는다 — kit 업데이트가 출력 경로 설정이나
팔레트(= 익스포트된 전 에셋의 UV 계약)를 날리는 사고를 구조적으로 막는다.

### kit 업데이트

kit을 고친 뒤 `install_kit.ps1 -Target <새 프로젝트> -Force`를 다시 돌리면 kit 파일만
갱신되고 프로젝트 레이어는 그대로 남는다. 반대 방향(다른 프로젝트에서 kit을 고친 경우)은
수동으로 되가져와야 한다 — 개선은 되도록 한 곳에서 하고 내보내는 쪽으로 굴릴 것.

각 사본이 어느 시점의 kit인지는 `ArtPipeline/KIT-ORIGIN.txt`(경로·commit·날짜)로 확인한다.

---

## 3. `pipeline.json` 스키마

전부 선택 항목이고, 없으면 `lib/lowpoly_lib/config.py`의 `DEFAULTS`가 쓰인다.

```json
{
  "project": "Dungeon Miner",
  "engine": "unity",
  "repoRoot": "..",
  "assetsRoot": "Assets/Art",
  "paletteRegistry": "project/palette_registry.py",
  "dirs": {
    "palette": "Palette",
    "weapons": "Weapons",
    "characters": "Characters",
    "environment": "Environment"
  },
  "previews": "previews",
  "build": "build",
  "reports": "reports"
}
```

경로 해석 기준:

- `repoRoot` — `ArtPipeline/` 기준 상대 경로
- `assetsRoot` — `repoRoot` 기준 상대 경로
- `previews` / `build` / `reports` / `paletteRegistry` — `ArtPipeline/` 기준 상대 경로

`paths.py`가 노출하는 것: `REPO`, `ASSETS_ART`, `PALETTE_PNG`, `WEAPONS_DIR`,
`CHARACTERS_DIR`, `ENVIRONMENT_DIR`, `PREVIEWS`, `BUILD`, `REPORTS`, 그리고 `paths.art(*parts)`.

---

## 4. 팔레트 레지스트리 계약

`palette.py`(kit)는 색을 하나도 모른다. 레지스트리 모듈이 아래를 제공한다.

| 이름 | 필수 | 뜻 |
|---|---|---|
| `COLORS` | ✓ | `{이름: (r, g, b)}` sRGB 0~1. **선언 순서 = 셀 인덱스** |
| `GRID` | | 한 변의 셀 수 (기본 8 → 64칸) |
| `CELL_PX` | | 셀당 픽셀 (기본 8 → 64×64 PNG) |
| `MAT_NAME` | | 기본 머티리얼 이름 (기본 `M_Palette`) |
| `UNUSED_CELL_RGB` | | 미할당 셀 색 (기본 마젠타 — UV 실수 즉시 발견용) |
| `SLOTS` | | 팔레트 외 공유 머티리얼 슬롯 |

`SLOTS`의 각 항목:

```python
"M_Crystal": {
    "alias": "use_crystal_material",  # palette.use_crystal_material(obj) 로 노출
    "base": "M_Palette",              # 복제 원본 (기본 = MAT_NAME)
    "default_cell": None,             # 주면 슬롯 교체 전에 그 색을 UV로 먼저 먹인다
    "roughness": 0.15,                # 이하 셋은 Blender 미리보기 근사치일 뿐
    "alpha": 0.6,
    "blended": True,
}
```

kit은 `alias`가 선언된 슬롯을 `palette` 모듈 함수로 자동 생성한다. 그래서 생성 스크립트는
슬롯 문자열을 몰라도 되고, kit 코드에는 `M_Crystal` 같은 게임 고유 이름이 남지 않는다.

⚠️ **`COLORS` 순서는 영구 계약이다.** 순서를 바꾸거나 중간에 끼워 넣으면 이미 익스포트된
전 에셋의 UV가 어긋난다. 색 추가는 맨 뒤에만, 안 쓰게 된 색도 지우지 말고 주석만 남길 것.

---

## 5. 엔진 연동은 kit 밖이다

kit은 **FBX 출력까지**만 책임진다. 팔레트 텍스처를 실제 머티리얼로 묶고 슬롯 이름을
셰이더에 리맵하는 일은 엔진 쪽 몫이다. 이 레포의 참고 구현:

- `Assets/Art/Editor/ArtAssetPostprocessor.cs` — 임포트 시 스케일 1 강제, 리그 타입 결정,
  머티리얼 슬롯 이름 → `.mat` 리맵, 클립 루프 플래그, 팔레트 텍스처 Point 필터
- `Assets/Art/Editor/ArtPipelineSetup.cs` — 공유 머티리얼(`M_Palette` 등) 생성

`install_kit.ps1 -WithUnityEditorScripts`로 복사할 수 있지만 **그대로 쓰면 안 된다.**
머티리얼 슬롯 이름·루프 클립 목록·셰이더 참조가 전부 이 게임 것이라 대상 프로젝트에 맞춰
손봐야 한다. 리맵 메커니즘(슬롯 이름으로 식별)만 가져가는 것이 요점이다.

---

## 6. 생성 스크립트 작성 규약

kit을 임포트하는 부트스트랩은 어느 프로젝트에서나 동일하다:

```python
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
PIPELINE = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(PIPELINE, "lib"))
sys.path.insert(0, HERE)

from lowpoly_lib import builders, export, palette, paths, preview
```

즉 생성 스크립트는 `ArtPipeline/assets/<에셋>/` 깊이에 둔다.

**kit이 강제하는 규약** (어느 프로젝트에서나 동일):

- **1 Blender unit = 1 m**, 캐릭터 발바닥 Z=0
- **전방 = −Y** — FBX 익스포트 레시피가 엔진 +Z 전방으로 변환한다
- FBX는 `export.export_static` / `export.export_skinned`만 사용. `bpy.ops.export_scene.fbx`
  직접 호출 금지 (축·스케일·본 설정이 어긋나면 이미 뽑은 에셋과 안 맞는다)
- 색은 팔레트 UV로만. 버텍스 컬러·개별 머티리얼 금지
- 스크립트 시작은 `builders.reset_scene()` (빈 씬에서 출발)

그 밖의 것(팔레트 색 목록, 리그 본 구성, 캐릭터 비율 같은 아트 디렉션)은 프로젝트마다
다시 정한다. 원본 레포는 그것을 `ArtPipeline/CONVENTIONS.md`에 두고 있다 —
이 파일은 프로젝트 레이어라 이식되지 않으므로, 새 프로젝트도 자기 것을 따로 세우면 된다.
