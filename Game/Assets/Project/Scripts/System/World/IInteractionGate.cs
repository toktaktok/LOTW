using UnityEngine;

namespace Project.Scripts.System.World
{
    /// <summary>
    /// 이동형 상호작용(RailConnector, SceneExitZone)과 같은 오브젝트에 붙여 통과 여부를 정합니다.
    /// 막으면 구현 쪽이 이유(대사 등)를 보여주고 false 를 돌려줍니다.
    /// </summary>
    public interface IInteractionGate
    {
        bool TryPass(GameObject interactor);
    }
}
