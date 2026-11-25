"""Renders a contact sheet of a rigged boss's clips for review: a few frames of each clip from three-quarter front.

    blender -b <rigged .blend> --python preview_clips.py -- <out folder> <armature name> [clip:frame,frame ...]
"""

import math
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
OUT, ARM = argv[0], argv[1]
scene = bpy.context.scene
arm = bpy.data.objects[ARM]
for o in scene.objects:
    if o.type == "MESH" and not o.name.endswith("LOD0"):
        o.hide_render = True

scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 8
scene.cycles.use_denoising = False
scene.render.resolution_x = scene.render.resolution_y = 360
scene.render.film_transparent = False
world = bpy.data.worlds.new("preview") if scene.world is None else scene.world
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.6, 0.68, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.8
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
sun.data.energy = 3.5
sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
scene.collection.objects.link(sun)

mesh = next(o for o in scene.objects if o.type == "MESH" and o.name.endswith("LOD0"))
size = max(mesh.dimensions)
centre = Vector((0, 0, mesh.dimensions.z * 0.5))
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
cam.data.type = "ORTHO"
cam.data.ortho_scale = size * 1.5
scene.collection.objects.link(cam)
scene.camera = cam
direction = Vector((0.55, -1.0, 0.25)).normalized()
cam.location = centre + direction * size * 4
cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()

wanted = {}
for spec in argv[2:]:
    clip, frames = spec.split(":")
    wanted[clip] = [int(f) for f in frames.split(",")]
if not wanted:
    for action in bpy.data.actions:
        if action.name.startswith("_"):
            continue
        end = int(action.frame_range[1])
        wanted[action.name] = sorted({0, end // 3, end // 2, (2 * end) // 3, end})

for clip, frames in wanted.items():
    arm.animation_data.action = bpy.data.actions[clip]
    for f in frames:
        scene.frame_set(f)
        scene.render.filepath = f"{OUT}/{clip}_{f:03d}.png"
        bpy.ops.render.render(write_still=True)
