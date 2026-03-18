using UnityEngine;
using System.Collections.Generic;
using System;

namespace MyGame.Runtime.Core
{
    /// <summary>
    /// 네트워크 패킷의 지연을 시뮬레이션합니다.
    /// 발신 시간을 기록했다가 일정 시간이 지나면 수신을 처리합니다.
    /// 
    /// 지연 = baseLatency + Random(-latencyVariance, +latencyVariance)
    /// </summary>
    public class MyNetworkLatencySimulator : MonoBehaviour
    {
        public static MyNetworkLatencySimulator Instance { get; private set; }

        [Header("Latency Settings")]
        [SerializeField] private float baseLatency = 0.05f; // 기본 지연 (초) - 기본 50ms
        [SerializeField] private float latencyVariance = 0.01f; // 지연 변동폭 (기본 ±10ms)

        [Header("Debug Settings")]
        [SerializeField] private bool _enableLatencyLogging = false;

        private Queue<DelayedPacket> _delayedPackets = new Queue<DelayedPacket>();

        // 성능 통계
        private float _totalLatency = 0f;
        private int _processedPackets = 0;
        private float _maxLatency = 0f;
        private float _minLatency = float.MaxValue;

        public float CurrentAverageLatency => _processedPackets > 0 ? _totalLatency / _processedPackets : 0f;
        public float MaxLatency => _maxLatency;
        public float MinLatency => _minLatency == float.MaxValue ? 0f : _minLatency;

        private void Awake() => Instance = this;

        private void Start()
        {
            Debug.Log($"[Network Simulator] 지연 설정: {baseLatency * 1000:F1}ms ± {latencyVariance * 1000:F1}ms");
        }

        private void Update()
        {
            ProcessDelayedPackets();
        }

        /// <summary>
        /// 패킷을 지연 큐에 추가합니다.
        /// 지연 = baseLatency + Random(-latencyVariance, +latencyVariance)
        /// </summary>
        public void EnqueueDelayedPacket(Action callback)
        {
            if (callback == null) return;

            // 지연값 계산 (밀리초 변환 후 다시 초로)
            float randomLatency = baseLatency + UnityEngine.Random.Range(-latencyVariance, latencyVariance);
            randomLatency = Mathf.Max(0.001f, randomLatency); // 최소 1ms 이상

            float arrivalTime = Time.time + randomLatency;

            _delayedPackets.Enqueue(new DelayedPacket
            {
                callback = callback,
                arrivalTime = arrivalTime,
                sentTime = Time.time,
                latency = randomLatency
            });

            if (_enableLatencyLogging)
            {
                Debug.Log($"[Latency] 패킷 송신 - 지연: {randomLatency * 1000:F1}ms / 도착 예정: {arrivalTime:F3}");
            }
        }

        private void ProcessDelayedPackets()
        {
            while (_delayedPackets.Count > 0 && _delayedPackets.Peek().arrivalTime <= Time.time)
            {
                var packet = _delayedPackets.Dequeue();

                // 실제 지연값 측정
                float actualLatency = Time.time - packet.sentTime;

                // 통계 업데이트
                _totalLatency += actualLatency;
                _processedPackets++;
                _maxLatency = Mathf.Max(_maxLatency, actualLatency);
                _minLatency = Mathf.Min(_minLatency, actualLatency);

                // 네트워크 진단 기록
                NetworkDiagnostics.Instance?.RecordRTT(actualLatency);

                if (_enableLatencyLogging)
                {
                    Debug.Log($"[Latency] 패킷 도착 - 실제 지연: {actualLatency * 1000:F1}ms");
                }

                packet.callback?.Invoke();
            }
        }

        /// <summary>
        /// 현재 큐에 있는 대기 중인 패킷 개수를 반환합니다.
        /// </summary>
        public int GetPendingPacketCount() => _delayedPackets.Count;

        /// <summary>
        /// 지연 통계를 출력합니다 (디버깅용).
        /// </summary>
        public void PrintLatencyStatistics()
        {
            Debug.Log($"=== 네트워크 지연 통계 ===");
            Debug.Log($"처리된 패킷: {_processedPackets}");
            Debug.Log($"평균 지연: {CurrentAverageLatency * 1000:F2}ms");
            Debug.Log($"최소 지연: {(_minLatency == float.MaxValue ? 0 : _minLatency * 1000):F2}ms");
            Debug.Log($"최대 지연: {_maxLatency * 1000:F2}ms");
            Debug.Log($"대기 중인 패킷: {_delayedPackets.Count}");
            Debug.Log($"설정: {baseLatency * 1000:F1}ms ± {latencyVariance * 1000:F1}ms");
        }

        /// <summary>
        /// 지연 설정을 실시간으로 변경합니다 (테스트용).
        /// </summary>
        public void SetLatency(float baseLatencyMs, float varianceMs)
        {
            baseLatency = baseLatencyMs / 1000f;
            latencyVariance = varianceMs / 1000f;
            Debug.Log($"[Network Simulator] 지연 설정 변경: {baseLatency * 1000:F1}ms ± {latencyVariance * 1000:F1}ms");
        }

        private class DelayedPacket
        {
            public Action callback;
            public float arrivalTime;
            public float sentTime;
            public float latency; // 예상 지연값
        }
    }
}
