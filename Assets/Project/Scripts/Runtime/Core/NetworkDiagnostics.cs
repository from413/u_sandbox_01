using UnityEngine;
using System.Collections.Generic;

namespace MyGame.Runtime.Core
{
    /// <summary>
    /// 네트워크 성능을 모니터링하고 진단하는 유틸리티.
    /// RTT, 패킷 손실률(시뮬레이션), 보정 오류, 입력 지연 등을 추적합니다.
    /// </summary>
    public class NetworkDiagnostics : MonoBehaviour
    {
        public static NetworkDiagnostics Instance { get; private set; }

        [Header("Diagnostics Settings")]
        [SerializeField] private bool _enableDiagnostics = true;
        [SerializeField] private bool _showDiagnosticsUI = false;

        // 네트워크 지표
        private float _currentRTT = 0f;
        private float _averageRTT = 0f;
        private float _maxRTT = 0f;
        private float _minRTT = float.MaxValue;
        private int _rttSampleCount = 0;

        // 입력 지연 추적
        private float _inputLatency = 0f;
        private Queue<float> _inputLatencies = new Queue<float>();
        private const int MaxLatencySamples = 60; // 마지막 60 샘플 유지

        // 보정 오류 추적
        private float _avgCorrectionError = 0f;
        private float _maxCorrectionError = 0f;

        // 패킷 송신 추적
        private int _totalPacketsSent = 0;
        private int _totalPacketsReceived = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnGUI()
        {
            if (!_showDiagnosticsUI || !_enableDiagnostics) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 250), GUI.skin.box);
            GUILayout.Label("📊 네트워크 진단", GUI.skin.label);
            GUILayout.Label($"RTT: {_currentRTT * 1000:F1}ms (평균: {_averageRTT * 1000:F1}ms)", GUI.skin.label);
            GUILayout.Label($"패킷: 송신 {_totalPacketsSent} / 수신 {_totalPacketsReceived}", GUI.skin.label);
            GUILayout.Label($"보정 오류: {_avgCorrectionError:F3}m (최대: {_maxCorrectionError:F3}m)", GUI.skin.label);
            GUILayout.Label($"입력 지연: {_inputLatency * 1000:F1}ms", GUI.skin.label);

            if (GUILayout.Button("통계 출력"))
            {
                PrintDiagnostics();
            }

            if (GUILayout.Button("UI 토글"))
            {
                _showDiagnosticsUI = !_showDiagnosticsUI;
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// RTT 샘플을 기록합니다.
        /// </summary>
        public void RecordRTT(float rtt)
        {
            if (!_enableDiagnostics) return;

            _currentRTT = rtt;
            _totalPacketsReceived++;
            _averageRTT = (_averageRTT * _rttSampleCount + rtt) / (_rttSampleCount + 1);
            _rttSampleCount++;
            
            _maxRTT = Mathf.Max(_maxRTT, rtt);
            if (_minRTT == float.MaxValue)
                _minRTT = rtt;
            else
                _minRTT = Mathf.Min(_minRTT, rtt);
        }

        /// <summary>
        /// 입력 지연을 기록합니다.
        /// </summary>
        public void RecordInputLatency(float latency)
        {
            if (!_enableDiagnostics) return;

            _inputLatency = latency;
            _inputLatencies.Enqueue(latency);

            if (_inputLatencies.Count > MaxLatencySamples)
            {
                _inputLatencies.Dequeue();
            }
        }

        /// <summary>
        /// 보정 오류를 기록합니다.
        /// </summary>
        public void RecordCorrectionError(float error)
        {
            if (!_enableDiagnostics) return;

            _maxCorrectionError = Mathf.Max(_maxCorrectionError, error);
            _avgCorrectionError = error; // 최신값으로 업데이트
        }

        /// <summary>
        /// 패킷 송신을 기록합니다.
        /// </summary>
        public void RecordPacketSent(int count = 1)
        {
            if (!_enableDiagnostics) return;
            _totalPacketsSent += count;
        }

        /// <summary>
        /// 현재 네트워크 상황을 평가합니다 (디버깅용).
        /// </summary>
        public NetworkCondition EvaluateNetworkCondition()
        {
            if (_averageRTT < 0.05f)
                return NetworkCondition.Excellent;
            else if (_averageRTT < 0.1f)
                return NetworkCondition.Good;
            else if (_averageRTT < 0.2f)
                return NetworkCondition.Fair;
            else if (_averageRTT < 0.5f)
                return NetworkCondition.Poor;
            else
                return NetworkCondition.VeryPoor;
        }

        /// <summary>
        /// 전체 진단 정보를 출력합니다.
        /// </summary>
        public void PrintDiagnostics()
        {
            Debug.Log("===== 네트워크 진단 보고서 =====");
            Debug.Log($"RTT 통계:");
            Debug.Log($"  현재: {_currentRTT * 1000:F2}ms");
            Debug.Log($"  평균: {_averageRTT * 1000:F2}ms");
            Debug.Log($"  범위: {_minRTT * 1000:F2}ms ~ {_maxRTT * 1000:F2}ms");
            Debug.Log($"");
            Debug.Log($"패킷 통계:");
            Debug.Log($"  송신: {_totalPacketsSent}");
            Debug.Log($"  수신: {_totalPacketsReceived}");
            Debug.Log($"");
            Debug.Log($"입력 지연:");
            Debug.Log($"  현재: {_inputLatency * 1000:F2}ms");
            Debug.Log($"  최근 평균: {CalculateAverageInputLatency() * 1000:F2}ms");
            Debug.Log($"");
            Debug.Log($"보정:");
            Debug.Log($"  평균 오류: {_avgCorrectionError:F3}m");
            Debug.Log($"  최대 오류: {_maxCorrectionError:F3}m");
            Debug.Log($"");
            Debug.Log($"네트워크 상태: {EvaluateNetworkCondition()}");
            Debug.Log($"================================");
        }

        private float CalculateAverageInputLatency()
        {
            if (_inputLatencies.Count == 0) return 0f;
            float sum = 0f;
            foreach (var latency in _inputLatencies)
            {
                sum += latency;
            }
            return sum / _inputLatencies.Count;
        }

        /// <summary>
        /// 진단 UI를 토글합니다.
        /// </summary>
        public void ToggleDiagnosticsUI()
        {
            _showDiagnosticsUI = !_showDiagnosticsUI;
        }

        /// <summary>
        /// 진단을 활성화/비활성화합니다.
        /// </summary>
        public void SetDiagnosticsEnabled(bool enabled)
        {
            _enableDiagnostics = enabled;
        }
    }

    /// <summary>
    /// 네트워크 상태 평가.
    /// </summary>
    public enum NetworkCondition
    {
        Excellent,  // < 50ms
        Good,       // 50-100ms
        Fair,       // 100-200ms
        Poor,       // 200-500ms
        VeryPoor    // > 500ms
    }
}