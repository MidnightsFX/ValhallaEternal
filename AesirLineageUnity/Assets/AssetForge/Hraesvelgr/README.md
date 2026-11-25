# Hraesvelgr

A huge menacing bat seen from the front with its wings spread wide and flat, each wing twice as long as its body, the wing membranes torn and ragged between long bony fingers ending in hooked claws, a lean body with a row of bone spikes down its back, a skull-like snout with long fangs and tall pointed ears, short clawed hind legs hanging below, storm-grey leathery hide with pale ice-blue glowing eyes.. valheim style, static prop, 0.9 m tall.

| Variant | Unity / Unreal | Godot / glTF |
|---------|----------------|--------------|
| base | `Hraesvelgr.fbx` | `Hraesvelgr.glb` |

- **LODs**: `_LOD0` 7970 triangles, `_LOD1` 3978 triangles, `_LOD2` 1988 triangles. Unity builds an LOD Group from the `_LOD0`.. names when the FBX imports.
  The GLB holds LOD0 only.
- **Collider**: `_Collider` (96 triangles), a convex hull for a mesh collider; turn its renderer off. Unreal expects the name `UCX_<mesh>` if you want it picked up automatically.
- **Material**: one per variant (`M_Hraesvelgr`), each with its own base colour (`Textures/Hraesvelgr_BaseColor.png`, ...) and the shared normal map `Textures/Hraesvelgr_Normal.png` (tangent space, OpenGL +Y, as Unity and Godot read it). 1024 px, bilinear filtering with mipmaps, smoothness 0.15, not metallic. LOD0 to LOD2 share the texture.
- **Source**: `Hraesvelgr.blend` keeps each variant's material.

Usable in: noncommercial, commercial, mods, packs. Made with AI models; raw AI output may not be protected by copyright.

AI disclosure: 3D model generated with AI (TRELLIS.2 from a Z-Image Turbo concept), reduced and painted by AssetForge scripts in Blender, with human review at each stage.

Made by AssetForge from workspace `hraesvelgr`; see `asset.json` and `NOTICES.txt`.
