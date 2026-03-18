using System.Collections.Generic;
using UnityEngine;
using MyGame.Runtime.Core;

namespace MyGame.Runtime.Modules
{
    /// <summary>
    /// 서버로부터 들어오는 상태 패킷을 받아 원격 플레이어(다른 클라이언트)의
    /// 캐릭터를 생성/갱신합니다.
    /// 선형 보간(Linear Interpolation)을 통해 서버 상태 업데이트 사이의 
    /// 부드러운 이동을 제공합니다.
    /// </summary>
    public class RemoteActorManager : MonoBehaviour
    {
        [SerializeField] private GameObject remotePlayerPrefab;
        [SerializeField] private float baseInterpolationSpeed = 5.0f; // 기본 보간 이동 속도
        [SerializeField] private bool enableDynamicInterpolation = true; // RTT 기반 동적 보간 속도 조절
        [SerializeField] private float disconnectTimeout = 8.0f; // 업데이트가 없을 때 원격 플레이어 제거 (초)

        private MyNetworkManager _NetworkMgr;
        private string _localPlayerId;

        // 원격 플레이어 정보 저장
        private Dictionary<string, ActorController> _remotePlayers = new Dictionary<string, ActorController>();
        private Dictionary<string, Vector3> _targetPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, Quaternion> _targetRotations = new Dictionary<string, Quaternion>(); // 🆕 회전 목표
        private Dictionary<string, Vector3> _lastPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, uint> _lastUpdateTicks = new Dictionary<string, uint>();
        private Dictionary<string, float> _lastUpdateTimes = new Dictionary<string, float>();
        private Dictionary<string, float> _dynamicInterpolationSpeeds = new Dictionary<string, float>(); // 🆕 플레이어별 동적 속도

        private void Start()
        {
            _NetworkMgr = MyNetworkManager.Instance;
            if (_NetworkMgr == null) return;

            _NetworkMgr.OnServerStateReceived += HandleServerState;
            _NetworkMgr.OnConnectionSuccess += HandleConnectionSuccess;

            if (_NetworkMgr.LocalSession != null)
                _localPlayerId = _NetworkMgr.LocalSession.PlayerId;
        }

        private void OnDestroy()
        {
            if (_NetworkMgr != null)
            {
                _NetworkMgr.OnServerStateReceived -= HandleServerState;
                _NetworkMgr.OnConnectionSuccess -= HandleConnectionSuccess;
            }
        }

        private void Update()
        {
            // 모든 원격 플레이어의 보간 업데이트
            List<string> playersToRemove = null;

            foreach (var kvp in _remotePlayers)
            {
                string playerId = kvp.Key;
                ActorController actor = kvp.Value;

                if (actor == null || !_targetPositions.ContainsKey(playerId)) continue;

                Vector3 targetPos = _targetPositions[playerId];
                Vector3 currentPos = actor.transform.position;

                // 동적 보간 속도 계산 (RTT 기반)
                float currentInterpolationSpeed = GetInterpolationSpeed(playerId);

                // 목표 위치에 도달하지 않았으면 보간
                if (Vector3.Distance(currentPos, targetPos) > 0.001f)
                {
                    Vector3 newPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * currentInterpolationSpeed);
                    actor.SetPosition(newPos);
                }
                else
                {
                    // 목표 위치에 도달했으면 정확히 설정
                    actor.SetPosition(targetPos);
                }

                // 회전 보간 (SLERP 사용)
                if (_targetRotations.ContainsKey(playerId))
                {
                    Quaternion targetRot = _targetRotations[playerId];
                    Quaternion currentRot = actor.transform.rotation;
                    
                    if (Quaternion.Angle(currentRot, targetRot) > 0.1f)
                    {
                        Quaternion newRot = Quaternion.Slerp(currentRot, targetRot, Time.deltaTime * currentInterpolationSpeed);
                        actor.SetRotation(newRot);
                    }
                    else
                    {
                        actor.SetRotation(targetRot);
                    }
                }

                // 업데이트가 일정 시간 없으면 해당 플레이어를 제거 (연결 끊김 대응)
                if (_lastUpdateTimes.TryGetValue(playerId, out float lastTime))
                {
                    if (Time.time - lastTime > disconnectTimeout)
                    {
                        if (playersToRemove == null)
                            playersToRemove = new List<string>();
                        playersToRemove.Add(playerId);
                    }
                }
            }

            if (playersToRemove != null)
            {
                foreach (var playerId in playersToRemove)
                {
                    if (_remotePlayers.TryGetValue(playerId, out var actor))
                    {
                        if (actor != null)
                            Destroy(actor.gameObject);
                    }

                    _remotePlayers.Remove(playerId);
                    _targetPositions.Remove(playerId);
                    _lastPositions.Remove(playerId);
                    _lastUpdateTicks.Remove(playerId);
                    _lastUpdateTimes.Remove(playerId);

                    Debug.Log($"[RemoteActor] 플레이어 제거: {playerId} (업데이트 없음 {disconnectTimeout:F1}s)");
                }
            }
        }

        private void HandleConnectionSuccess()
        {
            if (_NetworkMgr.LocalSession != null)
            {
                _localPlayerId = _NetworkMgr.LocalSession.PlayerId;
            }
        }

        private void HandleServerState(ServerStatePacket state)
        {
            // 1단계: 패킷 수신 로그 (이게 안 찍히면 NetworkManager 문제)
            Debug.Log($"[RemoteActorManager] 패킷 수신됨. / PlayerId: {state.PlayerId} / Tick: {state.LastProcessedTick}");

            // 자신의 상태는 무시 (로컬에서 처리됨)
            if (string.IsNullOrEmpty(state.PlayerId) || state.PlayerId == _localPlayerId)
                return;

            // 이미 생성한 원격 플레이어가 있는지 확인
            if (!_remotePlayers.TryGetValue(state.PlayerId, out var actor))
            {
                if (remotePlayerPrefab == null)
                {
                    Debug.LogWarning($"[RemoteActorManager] remotePlayerPrefab이 할당되지 않았습니다.");
                    return;
                }

                GameObject go = Instantiate(remotePlayerPrefab, state.Position, state.Rotation);
                go.name = $"RemotePlayer_{state.PlayerId}";
                actor = go.GetComponent<ActorController>() ?? go.AddComponent<ActorController>();
                _remotePlayers[state.PlayerId] = actor;
                _lastPositions[state.PlayerId] = state.Position;
                _targetPositions[state.PlayerId] = state.Position;
                _targetRotations[state.PlayerId] = state.Rotation; // 초기 회전 설정
                _lastUpdateTicks[state.PlayerId] = state.LastProcessedTick;
                _lastUpdateTimes[state.PlayerId] = Time.time;
                _dynamicInterpolationSpeeds[state.PlayerId] = baseInterpolationSpeed; // 초기 속도

                Debug.Log($"[RemoteActor] 플레이어 생성: {state.PlayerId} @ {state.Position} (Rotation:{state.Rotation.eulerAngles.y:F1}°)");
                return;
            }

            // 서버 상태 업데이트
            Vector3 serverPos = state.Position;
            Quaternion serverRot = state.Rotation;

            // 새로운 업데이트인지 확인 (틱이 이전보다 크면 새 상태)
            if (state.LastProcessedTick > _lastUpdateTicks[state.PlayerId])
            {
                _lastUpdateTicks[state.PlayerId] = state.LastProcessedTick;
                _lastUpdateTimes[state.PlayerId] = Time.time;

                // 목표 위치 및 회전 업데이트 (보간이 시작됨)
                _lastPositions[state.PlayerId] = actor.transform.position;
                _targetPositions[state.PlayerId] = serverPos;
                _targetRotations[state.PlayerId] = serverRot;

                // 동적 보간 속도 업데이트 (RTT 기반)
                if (enableDynamicInterpolation)
                {
                    UpdateDynamicInterpolationSpeed(state.PlayerId);
                }

                float distance = Vector3.Distance(_lastPositions[state.PlayerId], serverPos);
                Debug.Log($"[RemoteActor] {state.PlayerId} 업데이트 - Tick:{state.LastProcessedTick} / 거리:{distance:F3} / 회전:{serverRot.eulerAngles.y:F1}°");
            }
        }

        /// <summary>
        /// 동적 보간 속도 계산 (네트워크 지연에 따른 자동 조정)
        /// </summary>
        private float GetInterpolationSpeed(string playerId)
        {
            if (!enableDynamicInterpolation || !_dynamicInterpolationSpeeds.ContainsKey(playerId))
                return baseInterpolationSpeed;

            return _dynamicInterpolationSpeeds[playerId];
        }

        /// <summary>
        /// RTT를 기반으로 동적 보간 속도를 조정합니다
        /// </summary>
        private void UpdateDynamicInterpolationSpeed(string playerId)
        {
            if (NetworkDiagnostics.Instance == null) return;

            // 현재 RTT 가져오기
            float currentRtt = NetworkDiagnostics.Instance.GetLatestRTT();

            // RTT 기반 보간 속도 계산
            // RTT가 높을수록 보간 속도를 높여서 다음 업데이트까지 거리를 덜 벌린다
            // RTT가 낮을수록 기본 속도 유지
            float adjustedSpeed = baseInterpolationSpeed;

            if (currentRtt > 100f)
            {
                // 높은 지연: 더 빠르게 보간
                adjustedSpeed = baseInterpolationSpeed * (1f + (currentRtt - 100f) / 100f);
            }
            else if (currentRtt < 50f)
            {
                // 낮은 지연: 더 부드럽게 보간
                adjustedSpeed = baseInterpolationSpeed * 0.8f;
            }

            _dynamicInterpolationSpeeds[playerId] = adjustedSpeed;

            if (currentRtt > 80f)
            {
                Debug.Log($"[RemoteActor] {playerId} 보간 속도 조정: {baseInterpolationSpeed:F2} → {adjustedSpeed:F2} (RTT:{currentRtt:F1}ms)");
            }
        }
    }
}
