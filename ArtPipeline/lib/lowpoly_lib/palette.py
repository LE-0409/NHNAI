"""공유 팔레트 엔진 — 색 지정 방식의 단일 소스 (kit 제네릭).

GRID×GRID 단색 셀 PNG를 하나 굽고, 면 UV를 셀 중심 한 점에 모아 색을 지정한다.
엔진에서는 게임 하나 몫의 머티리얼이 딱 하나(M_Palette)면 충분하고, 특수 룩은
"이름만 다른 슬롯"으로 표시해 두면 엔진 임포트 시 실제 셰이더로 리맵된다.

⚠️ 이 파일에는 색 목록이 없다. "무슨 색이 있는가"·"어떤 슬롯을 쓰는가"는 전부
프로젝트 레지스트리(`pipeline.json`의 `paletteRegistry`)가 정한다 — kit을 다른
프로젝트에 복사해도 이 파일은 그대로 쓸 수 있어야 하기 때문.
레지스트리 계약은 KIT.md 참조.
"""
import importlib.util
import os

import bpy

from . import config, paths

# --- 프로젝트 레지스트리 로드 -------------------------------------------------
# sys.path를 건드리지 않으려고 파일 경로로 직접 로드한다 (kit이 어디에 복사되든 동작).


def _load_registry():
    rel = config.get("paletteRegistry")
    path = config.rel_to_pipeline(rel)
    if not os.path.exists(path):
        raise RuntimeError(
            "팔레트 레지스트리를 찾을 수 없습니다: {}\n"
            "  pipeline.json의 'paletteRegistry'가 가리키는 파일을 만들어야 합니다 "
            "(계약은 ArtPipeline/KIT.md 참조).".format(path)
        )
    spec = importlib.util.spec_from_file_location("_palette_registry", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    if not getattr(mod, "COLORS", None):
        raise RuntimeError("팔레트 레지스트리에 COLORS가 비어 있습니다: {}".format(path))
    return mod


REGISTRY = _load_registry()

COLORS = REGISTRY.COLORS
GRID = getattr(REGISTRY, "GRID", 8)
CELL_PX = getattr(REGISTRY, "CELL_PX", 8)
MAT_NAME = getattr(REGISTRY, "MAT_NAME", "M_Palette")
UNUSED_CELL_RGB = getattr(REGISTRY, "UNUSED_CELL_RGB", (0.85, 0.30, 0.75))
SLOTS = getattr(REGISTRY, "SLOTS", {})

_NAMES = list(COLORS)

if len(_NAMES) > GRID * GRID:
    raise RuntimeError(
        "팔레트 색이 셀 수를 넘었습니다: {}색 > {}칸. GRID를 키우면 기존 UV가 전부 "
        "어긋나므로, 색을 정리하거나 팔레트를 새로 시작해야 합니다.".format(len(_NAMES), GRID * GRID)
    )


def cell_uv(name):
    """색 이름 → 해당 셀 중심 UV. 셀 0은 이미지 좌상단."""
    idx = _NAMES.index(name)
    cx, cy = idx % GRID, idx // GRID
    return (cx + 0.5) / GRID, 1.0 - (cy + 0.5) / GRID


def write_palette_png(path=None):
    path = path or paths.PALETTE_PNG
    os.makedirs(os.path.dirname(path), exist_ok=True)
    size = GRID * CELL_PX
    px = list(UNUSED_CELL_RGB + (1.0,)) * (size * size)
    for idx, rgb in enumerate(COLORS.values()):
        cx, cy = idx % GRID, idx // GRID
        for py in range(CELL_PX):
            y = size - 1 - (cy * CELL_PX + py)  # bpy 픽셀은 아래줄부터
            for pxi in range(CELL_PX):
                o = (y * size + cx * CELL_PX + pxi) * 4
                px[o], px[o + 1], px[o + 2], px[o + 3] = rgb[0], rgb[1], rgb[2], 1.0
    img = bpy.data.images.new("palette_write", size, size, alpha=False)
    img.pixels[:] = px
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    return path


def ensure_palette_material():
    """미리보기 렌더용 Blender 머티리얼 (엔진에는 익스포트되지 않음 — 엔진 쪽은 M_Palette.mat)."""
    mat = bpy.data.materials.get(MAT_NAME)
    if mat is not None:
        return mat
    if not os.path.exists(paths.PALETTE_PNG):
        write_palette_png()
    mat = bpy.data.materials.new(MAT_NAME)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 1.0
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    img = bpy.data.images.load(paths.PALETTE_PNG, check_existing=True)
    img.colorspace_settings.name = "sRGB"
    tex.image = img
    tex.interpolation = "Closest"
    mat.node_tree.links.new(bsdf.inputs["Base Color"], tex.outputs["Color"])
    return mat


def ensure_material(slot):
    """레지스트리 SLOTS에 선언된 공유 머티리얼을 만들어 반환 (팔레트 머티리얼 복제 + 미리보기 근사).

    여기 설정(투명/광택)은 Blender 미리보기 근사치일 뿐 — 실제 룩은 엔진 쪽 머티리얼이 결정한다.
    """
    if slot == MAT_NAME:
        return ensure_palette_material()
    mat = bpy.data.materials.get(slot)
    if mat is not None:
        return mat
    if slot not in SLOTS:
        raise KeyError("레지스트리에 없는 머티리얼 슬롯: {} (선언된 슬롯: {})".format(slot, sorted(SLOTS)))
    spec = SLOTS[slot]
    mat = ensure_material(spec.get("base", MAT_NAME)).copy()
    mat.name = slot
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    if "roughness" in spec:
        bsdf.inputs["Roughness"].default_value = spec["roughness"]
    if "alpha" in spec:
        bsdf.inputs["Alpha"].default_value = spec["alpha"]
    if spec.get("blended"):
        if hasattr(mat, "surface_render_method"):   # Blender 4.2+ (EEVEE Next)
            mat.surface_render_method = "BLENDED"
        elif hasattr(mat, "blend_method"):
            mat.blend_method = "BLEND"
    return mat


def use_material(obj, slot, cell=None):
    """오브젝트의 머티리얼 슬롯0을 지정 슬롯으로 교체한다.

    cell을 주면 교체 전에 그 색을 UV로 먼저 먹인다 (부품만 별도 오브젝트로 만들고 이 함수를
    부른 뒤 join_all하면, 합친 메시가 2슬롯을 유지해 엔진이 서브메시별로 리맵한다 — join 시
    활성 오브젝트 = 팔레트 부품이어야 슬롯0이 M_Palette로 남는다).
    """
    if cell is not None:
        apply_color(obj, cell)
    mat = ensure_material(slot)
    if not obj.data.materials:
        obj.data.materials.append(mat)
    else:
        obj.data.materials[0] = mat


def apply_color(obj, name, faces=None):
    """오브젝트(또는 지정한 면 인덱스들)의 UV를 색 셀 중심으로 모은다."""
    mat = ensure_palette_material()
    if not obj.data.materials:
        obj.data.materials.append(mat)
    uv_layer = obj.data.uv_layers.active or obj.data.uv_layers.new(name="UVMap")
    u, v = cell_uv(name)
    polys = obj.data.polygons if faces is None else [obj.data.polygons[i] for i in faces]
    for poly in polys:
        for li in poly.loop_indices:
            uv_layer.data[li].uv = (u, v)


# --- 슬롯 별칭 생성 -----------------------------------------------------------
# 레지스트리가 alias를 선언한 슬롯은 `palette.use_crystal_material(obj)`처럼
# 모듈 함수로 노출한다. 생성 스크립트가 슬롯 문자열을 몰라도 되게 하는 편의 계층.


def _make_alias(slot, default_cell):
    if default_cell is None:
        def _alias(obj):
            use_material(obj, slot)
    else:
        def _alias(obj, cell=default_cell):
            use_material(obj, slot, cell)
    _alias.__name__ = SLOTS[slot]["alias"]
    _alias.__doc__ = "오브젝트를 '{}' 슬롯으로 표시한다 (레지스트리 선언).".format(slot)
    return _alias


def _install_aliases():
    for slot, spec in SLOTS.items():
        name = spec.get("alias")
        if name:
            globals()[name] = _make_alias(slot, spec.get("default_cell"))


_install_aliases()
