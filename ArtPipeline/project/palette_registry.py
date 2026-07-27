"""팔레트 레지스트리 — 이 프로젝트의 색과 공유 머티리얼 슬롯.

kit(`lowpoly_lib/palette.py`)은 셀 UV 계산·PNG 생성·머티리얼 슬롯 규약만 제공하고,
"무슨 색이 있는가"는 전부 이 파일이 정한다. `pipeline.json`의 `paletteRegistry` 키가
이 파일을 가리킨다.

⚠️ COLORS의 **선언 순서 = 셀 인덱스**다. 순서를 바꾸거나 중간에 끼워 넣으면 이미
익스포트된 전 에셋의 UV가 어긋난다. 색 추가는 반드시 맨 뒤에만. 안 쓰게 된 색도
지우지 말 것(뒤 인덱스가 전부 한 칸씩 밀린다) — 주석으로 '미사용' 표시만 남긴다.

아래는 시작용 최소 세트다. 프로젝트 색으로 갈아끼우되 GRID×GRID(기본 64)칸을 넘지 말 것.
"""

GRID = 8          # 8×8 셀
CELL_PX = 8       # 셀당 픽셀 → 64×64 이미지

MAT_NAME = "M_Palette"
UNUSED_CELL_RGB = (0.85, 0.30, 0.75)  # 미할당 셀은 눈에 띄는 마젠타 — UV 실수 즉시 발견용

# 이름 → sRGB(0~1). 셀 인덱스는 선언 순서 (좌상단부터 가로 방향).
COLORS = {
    "white": (0.92, 0.92, 0.92),
    "grey": (0.55, 0.55, 0.56),
    "black": (0.08, 0.08, 0.09),
    "skin": (0.94, 0.76, 0.62),
    "wood": (0.55, 0.36, 0.20),
    "iron": (0.62, 0.64, 0.68),
    "rock": (0.64, 0.62, 0.59),
    "accent": (0.85, 0.30, 0.20),
}

# 팔레트 외 공유 머티리얼 슬롯 (선택). 이름만 다른 슬롯을 만들어 두면 엔진 임포트 시
# 이름으로 식별해 실제 셰이더 머티리얼에 리맵할 수 있다. Blender 쪽 값은 미리보기 근사치일 뿐.
#
#   base        : 복제 원본 슬롯 (기본 = MAT_NAME)
#   alias       : `palette.<alias>(obj)` 로 노출할 헬퍼 이름
#   default_cell: 지정 시 슬롯 교체 전에 그 색을 UV로 먼저 먹인다
#   roughness / alpha / blended: Principled BSDF 미리보기 근사
#
# 예시:
# SLOTS = {
#     "M_Glass": {"alias": "use_glass_material", "roughness": 0.15, "alpha": 0.6, "blended": True},
# }
SLOTS = {}
