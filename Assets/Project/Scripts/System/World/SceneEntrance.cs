using UnityEngine;
using Project.Scripts.Content.World;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Scripts.System.World
{
    /// <summary>
    /// 씬 내 플레이어 진입 지점을 정의합니다.
    /// SceneTransitionManager가 씬 로드 후 이 지점에 플레이어를 배치합니다.
    /// </summary>
    public class SceneEntrance : MonoBehaviour
    {
        [SerializeField] private string entranceId = "Default";

        [Tooltip("플레이어가 스폰될 위치 (비워두면 이 오브젝트 위치 사용)")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("플레이어가 시작할 Rail 노드 (Rail 시스템 사용 시 필수)")]
        [SerializeField] private RailNode startNode;

        public string EntranceId => entranceId;
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        public RailNode StartNode => startNode;

        /// <summary>맵 빌더가 런타임에 역직렬화된 값을 주입합니다.</summary>
        public void SetStartNode(RailNode node) => startNode = node;
        public void SetEntranceId(string id) => entranceId = id;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 pos = SpawnPosition;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pos, 0.25f);
            Gizmos.DrawLine(pos, pos + Vector3.up * 1.5f);

            Handles.Label(pos + Vector3.up * 1.8f, $"[Entrance]\n{entranceId}");

            if (startNode != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
                Gizmos.DrawLine(pos, startNode.transform.position);
            }
        }
#endif
    }
}
