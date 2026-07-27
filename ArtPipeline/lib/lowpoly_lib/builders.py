"""로우폴리 프리미티브 빌더. 모든 치수는 미터, 회전은 도(degree)."""
import math

import bpy
from mathutils import Euler

from . import palette

# 꼭짓점 순서: ix*4+iy*2+iz (비트 0=음수쪽). 면은 바깥 방향 CCW.
_CUBE_FACES = [
    (0, 1, 3, 2),  # -X
    (4, 6, 7, 5),  # +X
    (0, 4, 5, 1),  # -Y
    (2, 3, 7, 6),  # +Y
    (0, 2, 6, 4),  # -Z
    (1, 5, 7, 3),  # +Z
]


def _finish(obj, loc, rot, color, bone):
    obj.location = loc
    obj.rotation_euler = Euler([math.radians(a) for a in rot], "XYZ")
    bpy.context.scene.collection.objects.link(obj)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    if color:
        palette.apply_color(obj, color)
    if bone:
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    return obj


def box(name, size, loc=(0, 0, 0), rot=(0, 0, 0), color=None, bone=None):
    """원점이 중심인 직육면체. size=(x,y,z) 전체 폭."""
    sx, sy, sz = size[0] / 2, size[1] / 2, size[2] / 2
    verts = [(x, y, z) for x in (-sx, sx) for y in (-sy, sy) for z in (-sz, sz)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], _CUBE_FACES)
    mesh.update()
    return _finish(bpy.data.objects.new(name, mesh), loc, rot, color, bone)


def prism(name, radius, depth, n=8, radius_top=None, loc=(0, 0, 0), rot=(0, 0, 0), color=None, bone=None, squash=1.0):
    """Z축 방향 n각기둥 (원점 중심). radius_top=0이면 원뿔(스파이크).
    squash<1이면 로컬 Y(sin 성분)를 눌러 단면을 납작하게 만든다 — 지네 배복(dorsoventral) 편평용."""
    radius_top = radius if radius_top is None else radius_top
    hz = depth / 2
    bot = [(radius * math.cos(2 * math.pi * i / n), radius * math.sin(2 * math.pi * i / n) * squash, -hz) for i in range(n)]
    faces = []
    if radius_top <= 1e-6:
        verts = bot + [(0.0, 0.0, hz)]
        apex = n
        faces += [(i, (i + 1) % n, apex) for i in range(n)]
        faces.append(tuple(reversed(range(n))))  # 바닥 캡
    else:
        top = [(radius_top * math.cos(2 * math.pi * i / n), radius_top * math.sin(2 * math.pi * i / n) * squash, hz) for i in range(n)]
        verts = bot + top
        faces += [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
        faces.append(tuple(reversed(range(n))))          # 바닥 캡
        faces.append(tuple(range(n, 2 * n)))             # 천장 캡
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    return _finish(bpy.data.objects.new(name, mesh), loc, rot, color, bone)


def icosphere(name, radius, subdiv=1, loc=(0, 0, 0), rot=(0, 0, 0), color=None, bone=None, scale=(1.0, 1.0, 1.0)):
    """저해상 다면체 구 (icosphere). subdiv=1이면 42v/80f — 로우폴리 규약에 맞는 둥근 덩어리용.
    scale은 로컬 축별 반경 배율 (타원체) — 거미 복부처럼 길쭉/납작한 벌브에 사용."""
    import bmesh

    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius)
    bm.to_mesh(mesh)
    bm.free()
    if scale != (1.0, 1.0, 1.0):
        for v in mesh.vertices:
            v.co.x *= scale[0]
            v.co.y *= scale[1]
            v.co.z *= scale[2]
    mesh.update()
    return _finish(bpy.data.objects.new(name, mesh), loc, rot, color, bone)


def plate(name, length, chord_root, chord_tip, chord_mid=None, camber=0.0, thickness=0.03,
          sweep=0.0, loc=(0, 0, 0), rot=(0, 0, 0), color=None, bone=None):
    """로컬 +X=스팬 방향의 얇은 테이퍼 판 (날개·지느러미·잎). 원점 = 뿌리 chord 중심.

    chord(Y 폭)는 뿌리→중간→끝 3단계(chord_mid 기본 = 평균)로 변해 나뭇잎 실루엣을 만들고,
    sweep은 끝 chord 중심을 +Y로 밀어 후퇴각, camber>0는 중간 단면을 +Z로 들어 길이방향 휨.
    12v/10면. 면 순서: 0-1 윗면, 2-3 아랫면, 4 뿌리(−X), 5 끝(+X), 6-7 앞전(−Y), 8-9 뒷전(+Y).
    """
    cm = (chord_root + chord_tip) / 2 if chord_mid is None else chord_mid
    ht = thickness / 2
    stations = ((0.0, 0.0, chord_root, 0.0), (length / 2, sweep / 2, cm, camber), (length, sweep, chord_tip, 0.0))
    verts = []
    for z_sign in (1.0, -1.0):  # 윗면 6v(0..5) → 아랫면 6v(6..11). 각 스테이션 A=−Y(앞전), B=+Y(뒷전)
        for x, yc, c, zc in stations:
            verts.append((x, yc - c / 2, zc + z_sign * ht))
            verts.append((x, yc + c / 2, zc + z_sign * ht))
    faces = [
        (0, 2, 3, 1), (2, 4, 5, 3),      # 윗면 (+Z)
        (6, 7, 9, 8), (8, 9, 11, 10),    # 아랫면 (−Z)
        (0, 1, 7, 6),                    # 뿌리 rim (−X)
        (4, 10, 11, 5),                  # 끝 rim (+X)
        (0, 6, 8, 2), (2, 8, 10, 4),     # 앞전 (−Y)
        (1, 3, 9, 7), (3, 5, 11, 9),     # 뒷전 (+Y)
    ]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    return _finish(bpy.data.objects.new(name, mesh), loc, rot, color, bone)


def bake_wire(obj):
    """모든 면 경계(sharp edge)에 barycentric 와이어 데이터를 굽는다 — 셰이더가 ~1px 불투명 선을 긋는 근거.

    왜 필요한가: flat-shaded 메시는 면 안에서 법선이 상수라 화면 공간 미분(fwidth normal)이 0 →
    면 경계를 못 잡는다. 반면 barycentric은 삼각형 안에서 선형 변하므로 fwidth로 경계를 뽑을 수 있다.
    삼각분할 대각선(coplanar, 인접 면 법선이 같음)은 마스크 0으로 걸러 실제 모서리만 선이 된다.

    - UV 레이어 'wireBary' : 코너별 barycentric (x,y). z = 1-x-y는 셰이더에서 복원.
    - 컬러 'wireMask'      : 삼각형당 상수 (m0,m1,m2). 코너 i 반대편 edge가 sharp면 1, coplanar면 0.
    셰이더는 d = min(bary + (1-mask)*BIG) 로 sharp edge까지의 거리를 재 ~1px 선을 만든다.

    면 경계선을 발광시키는 커스텀 셰이더를 쓰는 메시는 반드시 이걸 거쳐야 한다 —
    안 구우면 wireD.min이 0이라 **전 표면이 '엣지'로 판정**돼 불투명 발광 덩어리가 된다.
    (어떤 머티리얼 슬롯이 이걸 요구하는지는 프로젝트 규약 문서에 — 이 레포는 CONVENTIONS.md.)
    """
    import bmesh

    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.normal_update()
    bm.faces.ensure_lookup_table()
    bm.edges.ensure_lookup_table()

    col = bm.loops.layers.color.new("wireMask")
    uvw = bm.loops.layers.uv.new("wireBary")
    bary = ((1.0, 0.0), (0.0, 1.0), (0.0, 0.0))  # 코너 0/1/2 → bary.x/y/z=1 (z는 셰이더가 복원)
    SHARP = 0.999  # 인접 두 면 법선 내적이 이보다 크면 coplanar → 삼각분할 대각선 → 숨김

    for f in bm.faces:
        loops = f.loops[:]
        vs = [lp.vert for lp in loops]
        mask = []
        for i in range(3):
            va, vb = vs[(i + 1) % 3], vs[(i + 2) % 3]  # 코너 i 반대편 edge
            e = bm.edges.get((va, vb))
            drawn = 1.0
            if e is not None and len(e.link_faces) == 2:
                if e.link_faces[0].normal.dot(e.link_faces[1].normal) > SHARP:
                    drawn = 0.0  # 같은 평면 = 대각선 → 선 없음
            mask.append(drawn)
        for i, lp in enumerate(loops):
            lp[col] = (mask[0], mask[1], mask[2], 1.0)
            lp[uvw].uv = bary[i]

    bm.to_mesh(me)
    bm.free()
    return obj


def join_all(objs, name):
    """트랜스폼을 적용(bake)한 뒤 하나의 메시로 합친다. 버텍스 그룹은 이름 기준 병합."""
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = name
    joined.data.name = name
    return joined


def reset_scene():
    """빈 씬에서 시작 (factory settings, 기본 오브젝트 없음)."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    return bpy.context.scene
