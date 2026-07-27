"""유일한 공식 FBX 익스포트 경로. 생성 스크립트에서 bpy.ops.export_scene.fbx 직접 호출 금지.

규약(축/스케일/본 설정)의 근거는 CONVENTIONS.md 참조. 여기 설정을 바꾸면
이미 익스포트된 모든 에셋과 어긋나므로 함부로 수정하지 말 것.
"""
import os

import bpy

_COMMON = dict(
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",   # Unity에서 스케일 1.0
    use_space_transform=True,
    axis_forward="-Z",
    axis_up="Y",
    mesh_smooth_type="FACE",
    use_mesh_modifiers=True,
    use_tspace=False,
    path_mode="AUTO",
    embed_textures=False,
    use_selection=True,
)


def _select(objs):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]


def export_static(objs, fbx_path):
    """정적 메시 (도구/환경). bake_space_transform으로 임포트 회전 0 보장."""
    os.makedirs(os.path.dirname(fbx_path), exist_ok=True)
    _select(objs)
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        object_types={"MESH"},
        bake_space_transform=True,
        bake_anim=False,
        **_COMMON,
    )
    return fbx_path


def export_skinned(objs, fbx_path):
    """스킨 캐릭터 (메시+아마추어+NLA 스태시된 전체 액션).

    - bake_space_transform=False: True면 아마추어가 손상됨
    - add_leaf_bones=False: Unity에 잡동사니 `_end` 본 방지
    - use_armature_deform_only=False: ToolSocket_R/Root처럼 버텍스 없는 본도 유지
    """
    os.makedirs(os.path.dirname(fbx_path), exist_ok=True)
    _select(objs)
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        object_types={"MESH", "ARMATURE"},
        bake_space_transform=False,
        add_leaf_bones=False,
        use_armature_deform_only=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        armature_nodetype="NULL",
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=True,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=1.0,
        **_COMMON,
    )
    return fbx_path
