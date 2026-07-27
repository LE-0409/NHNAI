"""Phase 0 스모크 테스트: 헤드리스 Workbench 렌더가 이 머신에서 동작하는지 검증.

큐브 하나를 만들어 PNG로 렌더한다. 성공 기준: previews/_smoke/smoke_cube.png에
빨간 큐브가 보일 것.
"""
import os

import bpy
from mathutils import Vector

print("Blender:", bpy.app.version_string)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "MATERIAL"

bpy.ops.mesh.primitive_cube_add(size=1.0)
cube = bpy.context.active_object
mat = bpy.data.materials.new("SmokeRed")
mat.diffuse_color = (0.8, 0.1, 0.1, 1.0)
cube.data.materials.append(mat)

cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
cam.location = (2.5, -2.5, 1.8)
look_dir = Vector((0.0, 0.0, 0.0)) - cam.location
cam.rotation_euler = look_dir.to_track_quat("-Z", "Y").to_euler()
scene.camera = cam

out_dir = os.path.normpath(
    os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "previews", "_smoke")
)
os.makedirs(out_dir, exist_ok=True)
scene.render.filepath = os.path.join(out_dir, "smoke_cube.png")
bpy.ops.render.render(write_still=True)
print("WROTE:", scene.render.filepath)
