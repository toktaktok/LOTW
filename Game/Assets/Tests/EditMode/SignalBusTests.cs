using System;
using System.Collections.Generic;
using NUnit.Framework;
using Project.Scripts.Core;

namespace Tests.EditMode
{
    public class SignalBusTests
    {
        // 테스트용 신호 타입들. 신호 타입 자체가 채널 키 역할을 한다.
        private struct PingSignal
        {
            public int value;
        }

        private struct OtherSignal
        {
        }

        [Test]
        public void Publish_NoSubscribers_DoesNothing()
        {
            var bus = new SignalBus();
            Assert.DoesNotThrow(() => bus.Publish(new PingSignal { value = 1 }));
        }

        [Test]
        public void Publish_DeliversSignalToSubscriber()
        {
            var bus = new SignalBus();
            int received = 0;
            bus.Subscribe<PingSignal>(s => received = s.value);

            bus.Publish(new PingSignal { value = 42 });

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_DeliversToAllSubscribers()
        {
            var bus = new SignalBus();
            int count = 0;
            bus.Subscribe<PingSignal>(_ => count++);
            bus.Subscribe<PingSignal>(_ => count++);
            bus.Subscribe<PingSignal>(_ => count++);

            bus.Publish(new PingSignal());

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Publish_OnlyDeliversToMatchingSignalType()
        {
            var bus = new SignalBus();
            bool pingFired = false;
            bool otherFired = false;
            bus.Subscribe<PingSignal>(_ => pingFired = true);
            bus.Subscribe<OtherSignal>(_ => otherFired = true);

            bus.Publish(new PingSignal());

            Assert.IsTrue(pingFired);
            Assert.IsFalse(otherFired);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            var bus = new SignalBus();
            int count = 0;
            Action<PingSignal> handler = _ => count++;
            bus.Subscribe(handler);

            bus.Publish(new PingSignal());
            bus.Unsubscribe(handler);
            bus.Publish(new PingSignal());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Unsubscribe_UnknownHandler_Ignored()
        {
            var bus = new SignalBus();
            Action<PingSignal> handler = _ => { };
            Assert.DoesNotThrow(() => bus.Unsubscribe(handler));
        }

        [Test]
        public void Unsubscribe_RemovesOnlyOneOfDuplicates()
        {
            var bus = new SignalBus();
            int count = 0;
            Action<PingSignal> handler = _ => count++;
            bus.Subscribe(handler);
            bus.Subscribe(handler);

            bus.Unsubscribe(handler);
            bus.Publish(new PingSignal());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Subscribe_NullHandler_Ignored()
        {
            var bus = new SignalBus();
            bus.Subscribe<PingSignal>(null);
            Assert.AreEqual(0, bus.SubscriberCount<PingSignal>());
        }

        [Test]
        public void HandlerException_DoesNotBlockOtherHandlers()
        {
            var bus = new SignalBus();
            var order = new List<int>();
            bus.Subscribe<PingSignal>(_ => { order.Add(1); throw new InvalidOperationException("boom"); });
            bus.Subscribe<PingSignal>(_ => order.Add(2));

            bus.Publish(new PingSignal());

            CollectionAssert.AreEqual(new[] { 1, 2 }, order);
        }

        [Test]
        public void HandlerException_ReportedToHook()
        {
            var bus = new SignalBus();
            Exception captured = null;
            bus.OnHandlerException = ex => captured = ex;
            bus.Subscribe<PingSignal>(_ => throw new InvalidOperationException("boom"));

            bus.Publish(new PingSignal());

            Assert.IsInstanceOf<InvalidOperationException>(captured);
        }

        [Test]
        public void HandlerException_NoHook_SwallowedAndContinues()
        {
            var bus = new SignalBus();
            int reached = 0;
            bus.Subscribe<PingSignal>(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe<PingSignal>(_ => reached++);

            Assert.DoesNotThrow(() => bus.Publish(new PingSignal()));
            Assert.AreEqual(1, reached);
        }

        [Test]
        public void SubscriberCount_ReflectsSubscriptions()
        {
            var bus = new SignalBus();
            Assert.AreEqual(0, bus.SubscriberCount<PingSignal>());

            Action<PingSignal> handler = _ => { };
            bus.Subscribe(handler);
            Assert.AreEqual(1, bus.SubscriberCount<PingSignal>());

            bus.Unsubscribe(handler);
            Assert.AreEqual(0, bus.SubscriberCount<PingSignal>());
        }

        [Test]
        public void Unsubscribe_DuringPublish_DoesNotAffectCurrentDelivery()
        {
            var bus = new SignalBus();
            int secondFired = 0;
            Action<PingSignal> second = _ => secondFired++;
            // 첫 핸들러가 게시 도중 둘째 핸들러를 해제하지만, 스냅샷 전달이므로 둘째는 이번 회차에 실행되어야 한다.
            bus.Subscribe<PingSignal>(_ => bus.Unsubscribe(second));
            bus.Subscribe(second);

            bus.Publish(new PingSignal());
            Assert.AreEqual(1, secondFired);

            // 다음 게시에서는 해제가 반영되어 더 이상 실행되지 않는다.
            bus.Publish(new PingSignal());
            Assert.AreEqual(1, secondFired);
        }

        [Test]
        public void Subscribe_DuringPublish_DoesNotReceiveCurrentSignal()
        {
            var bus = new SignalBus();
            int lateFired = 0;
            Action<PingSignal> late = _ => lateFired++;
            bus.Subscribe<PingSignal>(_ => bus.Subscribe(late));

            bus.Publish(new PingSignal());
            Assert.AreEqual(0, lateFired);

            bus.Publish(new PingSignal());
            Assert.AreEqual(1, lateFired);
        }

        [Test]
        public void Clear_RemovesAllSubscribers()
        {
            var bus = new SignalBus();
            int count = 0;
            bus.Subscribe<PingSignal>(_ => count++);
            bus.Subscribe<OtherSignal>(_ => count++);

            bus.Clear();
            bus.Publish(new PingSignal());
            bus.Publish(new OtherSignal());

            Assert.AreEqual(0, count);
            Assert.AreEqual(0, bus.SubscriberCount<PingSignal>());
        }
    }
}
