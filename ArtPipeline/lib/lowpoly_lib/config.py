"""프로젝트 설정 로더 — kit이 특정 게임에 묶이지 않게 하는 유일한 지점.

`lowpoly_lib`(kit)은 어떤 프로젝트에도 그대로 복사해 쓸 수 있어야 한다. 그래서
프로젝트마다 달라지는 값(출력 루트·팔레트 레지스트리 위치)은 코드가 아니라
`ArtPipeline/pipeline.json`에 둔다.

설정 파일이 없어도 아래 기본값으로 동작한다 — kit만 복사해 놓고 바로 스모크를
돌릴 수 있게 하기 위한 것이며, 실제 프로젝트는 pipeline.json을 두는 것이 규약이다.

경로 해석 기준:
- `repoRoot`  : ArtPipeline 디렉터리 기준 상대 경로 (기본 "..")
- `assetsRoot`: repoRoot 기준 상대 경로
- 그 외 출력  : ArtPipeline 디렉터리 기준
"""
import json
import os

LIB_DIR = os.path.dirname(os.path.abspath(__file__))              # ArtPipeline/lib/lowpoly_lib
PIPELINE = os.path.normpath(os.path.join(LIB_DIR, "..", ".."))    # ArtPipeline
CONFIG_PATH = os.path.join(PIPELINE, "pipeline.json")

DEFAULTS = {
    "project": "Untitled",
    "repoRoot": "..",
    "assetsRoot": "Assets/Art",
    "paletteRegistry": "project/palette_registry.py",
    "dirs": {
        "palette": "Palette",
        "weapons": "Weapons",
        "characters": "Characters",
        "environment": "Environment",
    },
    "previews": "previews",
    "build": "build",
    "reports": "reports",
}


def _load():
    cfg = dict(DEFAULTS)
    cfg["dirs"] = dict(DEFAULTS["dirs"])
    if os.path.exists(CONFIG_PATH):
        # utf-8-sig: Windows 편집기(메모장·PS Out-File -Encoding utf8)가 붙이는 BOM을
        # 그냥 흡수한다. utf-8로 읽으면 BOM 하나에 "Unexpected UTF-8 BOM"으로 죽는다.
        with open(CONFIG_PATH, "r", encoding="utf-8-sig") as fh:
            user = json.load(fh)
        dirs = user.pop("dirs", None)
        cfg.update(user)
        if dirs:
            cfg["dirs"].update(dirs)
    return cfg


DATA = _load()


def get(key):
    return DATA[key]


def rel_to_pipeline(relpath):
    """ArtPipeline 기준 상대 경로 → 절대 경로 (슬래시/역슬래시 모두 허용)."""
    return os.path.normpath(os.path.join(PIPELINE, *relpath.replace("\\", "/").split("/")))
