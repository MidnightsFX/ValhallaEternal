"""Exports Modgud's rigged, animated AssetForge model for AesirLineageUnity, as AssetForge's own package would, until the
asset is published.

    blender -b <AssetForge work>/modgud/06_anim/v003/animated.blend --python export_modgud.py -- <out.fbx>
"""

import sys

import bpy

OUT_FBX = sys.argv[sys.argv.index("--") + 1]
SOURCE = "modgud"
NAME = "Modgud"
# Unity takes a Humanoid's rest pose (its T-pose) from the first take in the file, and Blender writes takes in name
# order with "_" ahead of letters.
REST_TAKE = "_TPose"


def main():
    for name in (f"{SOURCE}_Source", f"{SOURCE}_Collider"):
        if name in bpy.data.objects:
            bpy.data.objects.remove(bpy.data.objects[name])
    arm = bpy.data.objects[f"{SOURCE}_Rig"]
    lods = sorted((o for o in bpy.data.objects if o.type == "MESH" and "_LOD" in o.name), key=lambda o: o.name)
    for o in lods:
        o.name = o.data.name = f"{NAME}_LOD{o.name[-1]}"
    arm.name = arm.data.name = NAME
    arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.location, pb.rotation_euler, pb.scale = (0, 0, 0), (0, 0, 0), (1, 1, 1)
    rest = bpy.data.actions.new(REST_TAKE)
    arm.animation_data.action = rest
    for frame in (0, 1):
        for pb in arm.pose.bones:
            for path in ("location", "rotation_euler", "scale"):
                pb.keyframe_insert(path, frame=frame)
    arm.animation_data.action = None
    for o in bpy.context.scene.objects:
        o.select_set(o in lods or o == arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=OUT_FBX, use_selection=True, object_types={"MESH", "ARMATURE"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE",
        use_mesh_modifiers=False, add_leaf_bones=False, use_armature_deform_only=True,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0.0, path_mode="STRIP", embed_textures=False)
    print("EXPORTED", [o.name for o in lods], sorted(a.name for a in bpy.data.actions))


main()
