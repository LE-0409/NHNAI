"""파이프라인 표준 경로. 모든 입출력 경로는 여기서만 계산한다.

실제 값은 `ArtPipeline/pipeline.json`에서 온다 (없으면 config.DEFAULTS). 이 모듈에
프로젝트별 경로를 하드코딩하지 말 것 — kit은 그대로 복사돼 다른 프로젝트에서도 돈다.
"""
import os

from . import config

LIB_DIR = config.LIB_DIR                                          # ArtPipeline/lib/lowpoly_lib
PIPELINE = config.PIPELINE                                        # ArtPipeline
REPO = config.rel_to_pipeline(config.get("repoRoot"))             # 리포 루트

PREVIEWS = config.rel_to_pipeline(config.get("previews"))
BUILD = config.rel_to_pipeline(config.get("build"))
REPORTS = config.rel_to_pipeline(config.get("reports"))

_DIRS = config.get("dirs")

ASSETS_ART = os.path.normpath(
    os.path.join(REPO, *config.get("assetsRoot").replace("\\", "/").split("/"))
)


def art(*parts):
    """에셋 루트(Assets/Art) 하위 경로."""
    return os.path.join(ASSETS_ART, *parts)


PALETTE_DIR = art(_DIRS["palette"])
PALETTE_PNG = os.path.join(PALETTE_DIR, "palette.png")
WEAPONS_DIR = art(_DIRS["weapons"])
CHARACTERS_DIR = art(_DIRS["characters"])
ENVIRONMENT_DIR = art(_DIRS["environment"])
