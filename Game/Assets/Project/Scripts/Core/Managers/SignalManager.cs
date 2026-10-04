using System;
using UnityEngine;

namespace Project.Scripts.Core.Managers
{
    /// <summary>
    /// 전역 신호 버스를 노출하는 매니저. 순수 로직은 SignalBus에 위임합니다.
    /// 매니저/콘텐츠가 서로의 구체 타입을 참조하지 않고 신호로 통신하는 채널을 제공합니다
    /// (FlagManager가 FlagStore를 감싸는 것과 동일한 파사드 패턴).
    /// 핸들러 예외는 삼켜지지 않고 Debug.LogException으로 보고됩니다.
    /// </summary>
    public class SignalManager : Singleton<SignalManager>
    {
        #region Fields

        private readonly SignalBus _bus = new();

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _bus.OnHandlerException = ex => Debug.LogException(ex);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _bus.Clear();
        }

        #endregion

        #region Public API

        /// <summary>신호 타입 TSignal에 대한 핸들러를 구독합니다.</summary>
        public void Subscribe<TSignal>(Action<TSignal> handler) => _bus.Subscribe(handler);

        /// <summary>구독을 해제합니다.</summary>
        public void Unsubscribe<TSignal>(Action<TSignal> handler) => _bus.Unsubscribe(handler);

        /// <summary>신호를 게시해 구독자에게 전달합니다.</summary>
        public void Publish<TSignal>(TSignal signal) => _bus.Publish(signal);

        #endregion
    }
}
