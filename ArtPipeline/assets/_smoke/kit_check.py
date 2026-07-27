"""kit 설치 검증: 설정 로드 → 팔레트 굽기 → 메시 생성 → 미리보기 렌더 → FBX 익스포트.

스모크(smoke_test.py)가 "Blender가 헤드리스로 도는가"만 보는 반면, 이건 kit 전 계층이
이 프로젝트 설정으로 실제로 물리는지 본다. 새 프로젝트에 kit을 깐 직후 한 번 돌릴 것.

    ..\\run_blender.ps1 assets\\_smoke\\kit_check.py
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PIPELINE = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(PIPELINE, "lib"))

from lowpoly_lib import builders, config, export, palette, paths, preview  # noqa: E402

print("project    :", config.get("project"))
print("assets root:", paths.ASSETS_ART)
print("colors     :", len(palette.COLORS), "/", palette.GRID * palette.GRID, "cells")
print("slots      :", sorted(palette.SLOTS) or "(none)")

builders.reset_scene()

png = palette.write_palette_png()
print("PALETTE:", png)

first_color = next(iter(palette.COLORS))
box = builders.box("KitCheck", (0.6, 0.4, 0.3), color=first_color)

preview.render_turnaround("_kitcheck", [box])
print("PREVIEW:", os.path.join(paths.PREVIEWS, "_kitcheck"))

fbx = export.export_static([box], os.path.join(paths.BUILD, "kit_check.fbx"))
print("FBX:", fbx)

print("KIT CHECK OK")
