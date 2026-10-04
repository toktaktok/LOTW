// Plaza 배치에 필요한 복셀 프리팹 목록과 일괄 생성 (에디터 전용).
using System;
using System.Collections.Generic;
using System.IO;
using Project.Scripts.Editor.Baking;
using Project.Scripts.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Editor.Plaza
{
    public static class PlazaVoxelPrefabs
    {
        private const string ModelPrefix = "M_E_";

        public static readonly string[] Models =
        {
            ToolDefines.PlazaModelRoot + "Etc/M_E_Bench_1.vox",
            ToolDefines.PlazaModelRoot + "Etc/M_E_Bench_2.vox",
            ToolDefines.PlazaModelRoot + "Etc/M_E_StreetLight_1.vox",
            ToolDefines.PlazaModelRoot + "Etc/M_E_StreetLight_2.vox",
            ToolDefines.PlazaModelRoot + "Etc/M_E_DrinkMachine_3.vox",
            ToolDefines.PlazaModelRoot + "Etc/M_E_FlowerBush.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_Bar_Outside.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_CommonBuilding_001.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_CommonBuilding_002.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_CommonBuilding_004.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_CommonBuilding_005.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_CommonBuilding_008.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_CommonHouse_001.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_CandyShop.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_PostBox.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_MilkShop_Bottom.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_RabbitHouse.vox",
            ToolDefines.PlazaModelRoot + "Central/M_E_DogHouse.vox",
            ToolDefines.PlazaModelRoot + "Middle/M_E_MiddleHouse_1.vox",
            ToolDefines.PlazaModelRoot + "Middle/M_E_OldApartment.vox",
        };

        [MenuItem("LOTW/Plaza/1 Create Voxel Prefabs")]
        public static void CreateAll()
        {
            CreateRange(0, Models.Length);
        }

        /// <summary>Models[start..start+count) 중 없는 프리팹만 만들고 DSS를 적용한다. 새로 만든 수를 반환한다.</summary>
        public static int CreateRange(int start, int count)
        {
            var created = new List<string>();
            int end = Math.Min(start + count, Models.Length);
            for(int i = Math.Max(start, 0); i < end; i++)
            {
                string prefabPath = PrefabPathFor(Models[i]);
                if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                    continue;
                if(VoxelPrefabCreator.CreatePrefab(Models[i], prefabPath) != null)
                    created.Add(prefabPath);
            }

            if(created.Count > 0)
                VoxelDssApplier.ApplyToPrefabs(created);
            return created.Count;
        }

        /// <summary>.vox 경로에 대응하는 프리팹 경로 (M_E_Bench_1.vox -> PF_Bench_1.prefab).</summary>
        public static string PrefabPathFor(string voxPath)
        {
            string name = Path.GetFileNameWithoutExtension(voxPath);
            if(name.StartsWith(ModelPrefix))
                name = name.Substring(ModelPrefix.Length);
            return ToolDefines.PlazaPrefabFolder + "/PF_" + name + ".prefab";
        }
    }
}
