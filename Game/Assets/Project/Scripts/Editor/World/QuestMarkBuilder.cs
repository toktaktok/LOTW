// PF_NPC_Base 에 의뢰 표시(땀) 자식 오브젝트를 추가 (에디터 전용).
// NPC 스프라이트 렌더러 자식으로 두어 빌보드 회전을 같이 받고, 스프라이트 윗변 위에 놓는다. 이미 있으면 아무것도 하지 않는다.
using Project.Scripts.Content.World;
using Project.Scripts.Editor.Data;
using UnityEditor;
using UnityEngine;
using static Project.Scripts.Editor.UI.UIBuildUtil;

namespace Project.Scripts.Editor.World
{
    public static class QuestMarkBuilder
    {
        [MenuItem("LOTW/UI/Add Quest Mark To NPC Base")]
        private static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ToolDefines.NpcBasePrefabPath);
            try
            {
                if(root.GetComponentInChildren<QuestMarkIndicator>(true) != null)
                {
                    Debug.Log("[QuestMarkBuilder] Quest mark already exists");
                    return;
                }

                NPC npc = root.GetComponent<NPC>();
                SpriteRenderer body = root.GetComponentInChildren<SpriteRenderer>(true);
                if(npc == null || body == null)
                {
                    Debug.LogError("[QuestMarkBuilder] PF_NPC_Base needs NPC and a SpriteRenderer child");
                    return;
                }

                var mark = new GameObject("QuestMark");
                mark.transform.SetParent(body.transform, false);
                float top = body.sprite != null ? body.sprite.bounds.max.y : 0f;
                mark.transform.localPosition = new Vector3(0f, top + ToolDefines.QuestMarkMargin, 0f);

                var markRenderer = mark.AddComponent<SpriteRenderer>();
                markRenderer.sortingLayerID = body.sortingLayerID;
                markRenderer.sortingOrder = body.sortingOrder + 1;
                markRenderer.sharedMaterial = body.sharedMaterial;
                markRenderer.enabled = false;

                var indicator = mark.AddComponent<QuestMarkIndicator>();
                var so = new SerializedObject(indicator);
                Set(so, "npc", npc);
                Set(so, "markRenderer", markRenderer);
                SetArray(so, "frames", new[]
                {
                    AssetDatabase.LoadAssetAtPath<Sprite>(ToolDefines.QuestMarkSpritePaths[0]),
                    AssetDatabase.LoadAssetAtPath<Sprite>(ToolDefines.QuestMarkSpritePaths[1])
                });
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, ToolDefines.NpcBasePrefabPath);
                Debug.Log("[QuestMarkBuilder] Added quest mark to PF_NPC_Base");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
