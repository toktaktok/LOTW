# LOTW Asset Naming and Layout

Applies to `Assets/Project/` only. Third-party folders (`VoxelImporter`, `TextMesh Pro`) keep their own layout.

## Placement
- `Resources/` only for assets loaded by string path (see CLAUDE.md). Everything else is referenced via Inspector.
- `Art/` source art, `Audio/` sound, `Data/` ScriptableObject data, `Prefabs/` prefabs, `Scenes/`, `Settings/` render/project assets.

## Folders
| Asset | Folder |
|---|---|
| Character sprites | `Art/Characters/{NNN}.{Name}/Textures/` |
| Character clips | `Art/Characters/{NNN}.{Name}/Animations/` |
| Animator controllers | `Art/Animations/Controllers/` |
| Environment sprites | `Art/Environments/{Decors\|Props\|Tilesets}/` |
| Voxel / mesh models | `Art/Environments/Models/{Area}/` (`Central`, `Middle`, `JerkStreet`, `Etc`) |
| Model textures | `Art/Environments/Models/{Area}/Textures/` |
| Materials | `Art/Materials/` (flat, no subfolders) |
| Shaders, HLSL | `Art/Shaders/` |
| Fonts | `Art/Fonts/{Family}/` |
| UI sprites | `Art/UI/` |
| Audio | `Audio/{BGM\|SFX}/` |
| Generated 3D textures | `Art/Textures/Voxel/` |

Character IDs: `0xx` player characters, `1xx` town NPCs, `2xx` other NPCs.

## File names
Pattern: `{PREFIX}_{Subject}[_{State}][_{Variant}]`. Segments are PascalCase, separated by `_`, no spaces.

| Asset | Prefix | Example |
|---|---|---|
| Character texture | `TX_C_` | `TX_C_Snowman_Move_Front`, `TX_C_Mouse_Idle_2` |
| Environment texture | `TX_E_` | `TX_E_Poster_Coffee_1`, `TX_E_Window_003` |
| UI texture | `TX_UI_` | `TX_UI_Notebook` |
| Animation clip | `ANIM_` | `ANIM_Snowman_Move_Intro` |
| Animator controller | `AC_` | `AC_Snowman` |
| Model (vox/fbx) | `M_E_` | `M_E_CommonBuilding_001`, `M_E_Bench_1` |
| Material | `MAT_` | `MAT_VoxelDSS`, `MAT_BusStop_Pillar` |
| Prefab | `PF_` | `PF_FlowerShop`, `PF_NPC_Test` |
| Generated 3D texture | `TEX3D_` | `TEX3D_PF_FlowerShop_Occupancy` |
| BGM / SFX | `BGM_` / `SFX_` | `BGM_Village` |

- `Subject` is the character's own name. A folder holding several characters uses each member's name (`TX_C_Cham1_Talk` in `107.TrueGang`).
- State order: action, then direction or sub-state (`Move_Front`, `Move_Intro`).
- A single unnamed pose has no state (`TX_C_Mayor`). Extra variants get `_2`, `_3`. Keep existing numbering width in a series (`_001`).
- Shaders: PascalCase, no prefix, no spaces (`VoxelDSS.shader`, `SpriteShadow.shadergraph`).
- ScriptableObject data: `{Type}_{Name}` (`Dialogue_Cat`).

## Exceptions (do not rename)
- Font files keep the distributed name (`Galmuri11.ttf`, `DungGeunMo.otf`) and license text. TMP font assets: `{Family} {Kind}.asset`.
- Tool-generated files: VoxelImporter `PF_*_mat0.mat`, `TEX3D_*`, scene lighting folders.
- URP default assets in `Settings/`.

## Import settings
- Character sprites: PPU 25, Point filter, no compression (see pixel render style).
