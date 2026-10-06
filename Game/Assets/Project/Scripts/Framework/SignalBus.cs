using System;
using System.Collections.Generic;

namespace Project.Scripts.Framework
{
    /// <summary>
    /// 타입별 신호(이벤트)를 게시/구독하는 순수 C# 메시지 버스입니다.
    /// 신호 타입 자체가 채널 키이므로, 게시자와 구독자가 서로의 구체 타입을 참조하지 않고
    /// 통신할 수 있습니다(.Instance 직접 호출 / 발행자별 static event 대체).
    /// UnityEngine 의존이 없어 EditMode 테스트에서 단독 검증이 가능합니다.
    ///
    /// 한 핸들러에서 예외가 발생해도 나머지 핸들러로의 전달은 계속됩니다.
    /// Publish 도중의 Subscribe/Unsubscribe는 현재 전달에 영향을 주지 않습니다(스냅샷 전달).
    /// </summary>
    public class SignalBus
    {
        // 신호 타입 -> 해당 타입을 구독한 핸들러 목록.
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        /// <summary>
        /// 게시 중 핸들러가 발생시킨 예외를 보고받는 선택적 훅입니다.
        /// 미설정이면 예외는 조용히 삼켜집니다(전달은 계속됨).
        /// 파사드(예: SignalManager)에서 Debug.LogException 등으로 연결할 수 있습니다.
        /// </summary>
        public Action<Exception> OnHandlerException { get; set; }

        /// <summary>
        /// 신호 타입 TSignal에 대한 핸들러를 구독합니다.
        /// 같은 핸들러를 중복 등록하면 그 횟수만큼 호출됩니다(호출자 책임).
        /// </summary>
        public void Subscribe<TSignal>(Action<TSignal> handler)
        {
            if(handler == null)
                return;

            Type key = typeof(TSignal);
            if(!_handlers.TryGetValue(key, out var list))
            {
                list = new List<Delegate>();
                _handlers[key] = list;
            }

            list.Add(handler);
        }

        /// <summary>
        /// 구독을 해제합니다. 등록되지 않은 핸들러는 무시합니다.
        /// 중복 등록된 경우 하나만 제거합니다.
        /// </summary>
        public void Unsubscribe<TSignal>(Action<TSignal> handler)
        {
            if(handler == null)
                return;

            Type key = typeof(TSignal);
            if(!_handlers.TryGetValue(key, out var list))
                return;

            list.Remove(handler);
            if(list.Count == 0)
                _handlers.Remove(key);
        }

        /// <summary>
        /// 신호를 게시해 구독자에게 전달합니다. 구독자가 없으면 아무 일도 하지 않습니다.
        /// 한 핸들러의 예외는 OnHandlerException으로 보고하고 나머지 전달을 계속합니다.
        /// </summary>
        public void Publish<TSignal>(TSignal signal)
        {
            Type key = typeof(TSignal);
            if(!_handlers.TryGetValue(key, out var list) || list.Count == 0)
                return;

            // 게시 도중 구독/해제가 일어나도 안전하도록 스냅샷을 순회합니다.
            var snapshot = list.ToArray();
            foreach(var entry in snapshot)
            {
                if(entry is not Action<TSignal> handler)
                    continue;

                try
                {
                    handler(signal);
                }
                catch(Exception ex)
                {
                    OnHandlerException?.Invoke(ex);
                }
            }
        }

        /// <summary>특정 신호 타입의 구독자 수를 반환합니다. 진단/테스트용.</summary>
        public int SubscriberCount<TSignal>()
        {
            return _handlers.TryGetValue(typeof(TSignal), out var list) ? list.Count : 0;
        }

        /// <summary>모든 구독을 제거합니다. 씬 전환/리셋 시 누수 방지용.</summary>
        public void Clear()
        {
            _handlers.Clear();
        }
    }
}
