# Voxel Derived Surface Shading (DSS)

Blocky voxel geometry stays as cubes, but the lighting normal is derived from the
voxel occupancy field V(x,y,z) baked into a Texture3D. A voxelized sphere lights as
a sphere with no 6-direction face phasing under a moving light. Geometry is never
remeshed (this is NOT Marching Cubes / Surface Nets) - only the normal source changes.

Files:
- `VoxelDSS.shader` - URP Lit shader (ForwardLit, ShadowCaster, DepthOnly, custom DepthNormals).
- `VoxelDSSInput.hlsl` - SRP-Batcher CBUFFER + Texture3D/sampler.
- `VoxelDSSCommon.hlsl` - reusable DSS math (gradient A.1, centroid A.2, sign, fallback, AO).
- `VoxelDSSForwardPass.hlsl`, `VoxelDSSDepthNormalsPass.hlsl` - the two passes that derive normals.
- `VoxelOccupancyBaker.cs`, `VoxelDssMath.cs` - editor baker (Project.Scripts.Editor).

## Bake the occupancy volume

1. `LOTW > Bake Voxel Occupancy (DSS)` opens the baker window.
2. For a real asset: drag a VoxelImporter `VoxelObject` into `Voxel Object`, drag the
   `MAT_VoxelDSS` material into `Target Material`, press `Bake From Voxel Object`.
3. For an instant demo: press `Bake Test Sphere`. It writes the mapping uniforms for a
   unit cube `[-0.5,0.5]` mesh so you can demo on a default Cube + `MAT_VoxelDSS`.
4. The baker writes `Assets/Project/Art/Textures/Voxel/TEX3D_<name>_Occupancy.asset`
   (R8_UNorm, linear, Clamp, Trilinear, optional mip chain) and sets the material's
   `_OccupancyTex`, `_VoxelDims`, `_ImportScale`, `_ImportOffset`, `_LocalOffset`.

## Object-space -> voxel mapping (must match the baker)

    grid = (objectPos / _ImportScale) - (_LocalOffset + _ImportOffset)   // VoxelImporter inverse
    uvw  = (grid + 0.5) / _VoxelDims                                       // cell center at +0.5

The baker copies the SAME importScale/importOffset/localOffset VoxelImporter used to
build the mesh, so the volume aligns regardless of non-uniform scale or axis flips.

## Material knobs

- `_KernelRadius` / `_StepScale` - gradient reach. Small = local detail (reacts to
  stair-steps), large = global form. Bake mips and the large-r form comes cheap.
- `_Sigma` - Gaussian width for the exact `(2r+1)^3` loop (enable `_DSS_WEIGHTED_LOOP`).
  Default off: cheap 6-tap central differences on the trilinear volume.
- `_FallbackThreshold` - below this `|gradient|` (thin sheet / lone voxel) the normal
  falls back to the geometric cube face, easing into DSS as thickness rises.
- `_DSSStrength` (0..1) - blend from pure cube faces (0) to full DSS (1).
- `_AOStrength` / `_AORadius` - optional derived-surface AO from neighborhood occupancy.
- `_DSS_CENTROID` toggle - use occupancy centroid (A.2) instead of gradient (A.1).
  Centroid(r) ~= Gradient(r-1).

## Notes

- Occupancy data is EDIT-TIME ONLY in VoxelImporter; re-bake after editing the model.
- The custom DepthNormals pass feeds the DSS normal to SSAO (active on PC and Mobile
  renderers) so occlusion matches the derived surface, not the cube faces.
- Volume is linear R8 (occupancy is data, not color) - do not import it as sRGB.
- Mobile is plain Forward; keep `_KernelRadius` modest or rely on the mip LOD form.
  Escalation path (not built): compute-bake RGB normals into a 2nd Texture3D and sample
  one texel instead of looping - zero shader-interface change.
- Lighting uses main + Forward+ (cluster) additional lights, reflection probes, soft shadows,
  and SSAO. Adaptive Probe Volume (APV) GI is NOT sampled by this shader yet - use regular
  lights/lightmaps for ambient. Validated against URP 17.3 / Unity 6000.3.
- The test-sphere demo on a primitive Cube is a smoke test (each flat face shows a rounded
  bump); the convincing demonstration is a real VoxelImporter voxel mesh via Bake From Voxel Object.
