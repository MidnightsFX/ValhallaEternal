# Sinmara

A hulking fire brute of cracked, charred black rock, with shoulders three times as wide as its waist, a small head with two swept-back horns sunk low between the shoulders, thick arms hanging to its knees and ending in three long black claws, short heavy legs, deep cracks over its whole body glowing bright blue-white as if molten inside, and jagged blue flames rising from its shoulders and the top of its head, a far bigger and more menacing version of the fire imp in the reference.. valheim style, humanoid, 1.6 m tall.

| Variant | Unity / Unreal | Godot / glTF |
|---------|----------------|--------------|
| base | `Sinmara.fbx` | `Sinmara.glb` |

- **LODs**: `_LOD0` 11984 triangles, `_LOD1` 5980 triangles, `_LOD2` 2994 triangles. Unity builds an LOD Group from the `_LOD0`.. names when the FBX imports.
  The GLB holds LOD0 only.
- **Material**: one per variant (`M_Sinmara`), each with its own base colour (`Textures/Sinmara_BaseColor.png`, ...) and the shared normal map `Textures/Sinmara_Normal.png` (tangent space, OpenGL +Y, as Unity and Godot read it). 1024 px, bilinear filtering with mipmaps, smoothness 0.15, not metallic. LOD0 to LOD2 share the texture.
- **Source**: `Sinmara.blend` keeps each variant's material.

**Rig**: the forge's `humanoid` skeleton, 53 bones, root `root`.
Bone names are Mixamo's and the rest pose is a T-pose. In Unity import as **Humanoid**; Mixamo and other Humanoid clips then play on it too. Godot maps it with a SkeletonProfileHumanoid bone map. FBX takes are named `Sinmara|<clip>`. Clips are in place: the game moves
the character.

| Clip | Frames at 30 fps | Loops | Events |
|------|------------------|-------|--------|
| Idle | 90 (3.0 s) | yes | - |
| Walk | 32 (1.067 s) | yes | - |
| Run | 20 (0.667 s) | yes | - |
| Attack | 24 (0.8 s) | no | hit at frame 13 |
| Hit | 15 (0.5 s) | no | - |
| Death | 45 (1.5 s) | no, holds its last frame | land at frame 28 |

Usable in: noncommercial, commercial, mods, packs. Made with AI models; raw AI output may not be protected by copyright.

AI disclosure: 3D model generated with AI (TRELLIS.2 from a Z-Image Turbo concept), reduced and painted by AssetForge scripts in Blender, rigged and animated by script, with human review at each stage.

Made by AssetForge from workspace `sinmara`; see `asset.json` and `NOTICES.txt`.
