using UnityEngine;
using System.Collections.Generic;
using System;

namespace MyGame.Runtime.Core
{
    /// <summary>
    /// 네트워크 패킷의 지연을 시뮬레이션합니다.
    /// 발신 시간을 기록했다가 일정 시간이 지나면 수신을 처리합니다.
    /// </summary>
    public class MyNetworkLatencySimulator : MonoBehaviour
    {
        public static MyNetworkLatencySimulator Instance { get; private set; }

        [Header("Latency Settings")]
        [SerializeField] private float baseLatency = 0.1f; // 기본 지연 (초)
        [SerializeField] private float latencyVariance = 0.02f; // 지연 변동폭

        private Queue<DelayedPacket> _delayedPackets = new Queue<DelayedPacket>();

        private void Awake() => Instance = this;

        private void Update()
        {
            ProcessDelayedPackets();
        }

        /// <summary>
        /// 패킷을 지연 큐에 추가합니다.
        /// </summary>
        public void EnqueueDelayedPacket(Action callback)
        {
            if (callback == null) return;

            float randomLatency = baseLatency + UnityEngine.Random.Range(-latencyVariance, latencyVariance);
            _delayedPackets.Enqueue(new DelayedPacket
            {
                callback = callback,
                arrivalTime = Time.time + randomLatency
            });
        }

        private void ProcessDelayedPackets()
        {
            while (_delayedPackets.Count > 0 && _delayedPackets.Peek().arrivalTime <= Time.time)
            {
                var packet = _delayedPackets.Dequeue();
                packet.callback?.Invoke();
            }
        }

        private class DelayedPacket
        {
            public Action callback;
            public float arrivalTime;
        }
    }
}
