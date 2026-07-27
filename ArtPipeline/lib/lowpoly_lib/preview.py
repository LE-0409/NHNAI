"""Workbench 미리보기 렌더 — Claude가 결과물을 눈으로 확인하는 수단."""
import os

import bpy
from mathutils import Vector

from . import paths

RES = 640

# 뷰 이름 → 카메라 방향 (피사체 중심 기준). 전방 = -Y 규약이므로 front 카메라는 -Y쪽.
VIEWS = {
    "front": Vector((0.0, -1.0, 0.0)),
    "back": Vector((0.0, 1.0, 0.0)),
    "left": Vector((-1.0, 0.0, 0.0)),
    "persp34": Vector((-0.8, -1.0, 0.55)),
}


def setup_render(scene=None):
    scene = scene or bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = RES
    scene.render.resolution_y = RES
    scene.view_settings.view_transform = "Standard"  # Filmic 톤매핑 없이 팔레트 원색 확인
    sh = scene.display.shading
    sh.light = "STUDIO"
    sh.color_type = "TEXTURE"
    sh.show_object_outline = True
    sh.background_type = "VIEWPORT"
    sh.background_color = (0.9, 0.9, 0.9)
    return scene


def scene_bounds(objs):
    bpy.context.view_layer.update()  # 갓 생성된 오브젝트의 matrix_world/bound_box 갱신
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        if o.type != "MESH":
            continue
        for corner in o.bound_box:
            wc = o.matrix_world @ Vector(corner)
            mn = Vector(map(min, mn, wc))
            mx = Vector(map(max, mx, wc))
    return mn, mx


def _aim(cam, target):
    look = target - cam.location
    cam.rotation_euler = look.to_track_quat("-Z", "Y").to_euler()


def render_turnaround(asset_name, objs=None, out_dir=None, views=None):
    """4방향 스틸 렌더: previews/<asset>/<asset>_{front,back,left,persp34}.png

    views로 뷰 dict를 넘기면 기본 VIEWS 대신/추가로 렌더 (예: 탑뷰 실루엣 검수)."""
    scene = setup_render()
    objs = objs or [o for o in scene.objects if o.type == "MESH"]
    mn, mx = scene_bounds(objs)
    center = (mn + mx) / 2
    dim = mx - mn
    radius = max(max(dim) / 2, 0.25)
    out_dir = out_dir or os.path.join(paths.PREVIEWS, asset_name)
    os.makedirs(out_dir, exist_ok=True)
    written = []
    for view, direction in (views or VIEWS).items():
        ortho = view != "persp34"
        cam_data = bpy.data.cameras.new("PreviewCam_" + view)
        cam_data.type = "ORTHO" if ortho else "PERSP"
        cam = bpy.data.objects.new("PreviewCam_" + view, cam_data)
        scene.collection.objects.link(cam)
        dist = radius * 4 + 1.0
        cam.location = center + direction.normalized() * dist
        _aim(cam, center)
        if ortho:
            cam_data.ortho_scale = max(dim) * 1.3
        else:
            cam_data.lens = 50
        cam_data.clip_start = 0.01
        cam_data.clip_end = dist * 4
        scene.camera = cam
        scene.render.filepath = os.path.join(out_dir, f"{asset_name}_{view}.png")
        bpy.ops.render.render(write_still=True)
        written.append(scene.render.filepath)
    return written


def render_action_strip(rig, action, out_dir, frames=6):
    """액션을 rig에 걸고 6프레임을 고정 3/4 카메라로 렌더 (애니메이션 검수용)."""
    scene = setup_render()
    rig.animation_data_create()
    rig.animation_data.action = action
    f0, f1 = (int(action.frame_range[0]), int(action.frame_range[1]))
    meshes = [o for o in scene.objects if o.type == "MESH"]
    mn, mx = scene_bounds(meshes)
    center = (mn + mx) / 2
    dim = mx - mn
    radius = max(max(dim) / 2, 0.25)

    cam_data = bpy.data.cameras.new("StripCam")
    cam_data.type = "PERSP"
    cam_data.lens = 50
    cam = bpy.data.objects.new("StripCam", cam_data)
    scene.collection.objects.link(cam)
    dist = radius * 4.5 + 1.0
    # 측면 위주 각도: 전방(−Y) 숙임/스윙이 화면에서 압축되지 않고 읽히도록
    cam.location = center + Vector((-1.0, -0.35, 0.3)).normalized() * dist
    _aim(cam, center)
    cam_data.clip_end = dist * 4
    scene.camera = cam

    os.makedirs(out_dir, exist_ok=True)
    written = []
    for i in range(frames):
        frame = round(f0 + (f1 - f0) * i / max(frames - 1, 1))
        scene.frame_set(frame)
        scene.render.filepath = os.path.join(out_dir, f"f{frame:03d}.png")
        bpy.ops.render.render(write_still=True)
        written.append(scene.render.filepath)
    return written
