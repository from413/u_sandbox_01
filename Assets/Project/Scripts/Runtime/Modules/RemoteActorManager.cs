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
        [SerializeField] private float interpolationSpeed = 5.0f; // 보간 이동 속도

        private MyNetworkManager _NetworkMgr;
        private string _localPlayerId;

        // 원격 플레이어 정보 저장
        private Dictionary<string, ActorController> _remotePlayers = new Dictionary<string, ActorController>();
        private Dictionary<string, Vector3> _targetPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, Vector3> _lastPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, uint> _lastUpdateTicks = new Dictionary<string, uint>();

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
            foreach (var kvp in _remotePlayers)
            {
                string playerId = kvp.Key;
                ActorController actor = kvp.Value;

                if (actor == null || !_targetPositions.ContainsKey(playerId)) continue;

                Vector3 targetPos = _targetPositions[playerId];
                Vector3 currentPos = actor.transform.position;

                // 목표 위치에 도달하지 않았으면 보간
                if (Vector3.Distance(currentPos, targetPos) > 0.001f)
                {
                    Vector3 newPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * interpolationSpeed);
                    actor.SetPosition(newPos);
                }
                else
                {
                    // 목표 위치에 도달했으면 정확히 설정
                    actor.SetPosition(targetPos);
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

                GameObject go = Instantiate(remotePlayerPrefab, state.Position, Quaternion.identity);
                go.name = $"RemotePlayer_{state.PlayerId}";
                actor = go.GetComponent<ActorController>() ?? go.AddComponent<ActorController>();
                _remotePlayers[state.PlayerId] = actor;
                _lastPositions[state.PlayerId] = state.Position;
                _targetPositions[state.PlayerId] = state.Position;
                _lastUpdateTicks[state.PlayerId] = state.LastProcessedTick;

                Debug.Log($"[RemoteActor] 플레이어 생성: {state.PlayerId} @ {state.Position}");
                return;
            }

            // 서버 상태 업데이트
            Vector3 serverPos = state.Position;

            // 새로운 업데이트인지 확인 (틱이 이전보다 크면 새 상태)
            if (state.LastProcessedTick > _lastUpdateTicks[state.PlayerId])
            {
                _lastUpdateTicks[state.PlayerId] = state.LastProcessedTick;
                
                // 목표 위치 업데이트 (보간이 시작됨)
                _lastPositions[state.PlayerId] = actor.transform.position;
                _targetPositions[state.PlayerId] = serverPos;

                float distance = Vector3.Distance(_lastPositions[state.PlayerId], serverPos);
                Debug.Log($"[RemoteActor] {state.PlayerId} 위치 업데이트 - Tick:{state.LastProcessedTick} / 거리:{distance:F3} → {serverPos}");
            }
        }
    }
}
