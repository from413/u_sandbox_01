using System.Collections.Generic;
using UnityEngine;
using MyGame.Runtime.Core;

namespace MyGame.Runtime.Modules
{
    /// <summary>
    /// 서버로부터 들어오는 상태 패킷을 받아 원격 플레이어(다른 클라이언트)의
    /// 캐릭터를 생성/갱신합니다.
    /// </summary>
    public class RemoteActorManager : MonoBehaviour
    {
        [SerializeField] private GameObject remotePlayerPrefab;

        private MyNetworkManager _networkMgr;
        private string _localPlayerId;

        // 원격 플레이어 목록
        private Dictionary<string, ActorController> _remotePlayers = new Dictionary<string, ActorController>();

        private void Start()
        {
            _networkMgr = MyNetworkManager.Instance;
            if (_networkMgr == null) return;

            _networkMgr.OnServerStateReceived += HandleServerState;
            _networkMgr.OnConnectionSuccess += HandleConnectionSuccess;

            if (_networkMgr.LocalSession != null)
                _localPlayerId = _networkMgr.LocalSession.PlayerId;
        }

        private void OnDestroy()
        {
            if (_networkMgr != null)
            {
                _networkMgr.OnServerStateReceived -= HandleServerState;
                _networkMgr.OnConnectionSuccess -= HandleConnectionSuccess;
            }
        }

        private void HandleConnectionSuccess()
        {
            if (_networkMgr.LocalSession != null)
            {
                _localPlayerId = _networkMgr.LocalSession.PlayerId;
            }
        }

        private void HandleServerState(ServerStatePacket state)
        {
            if (string.IsNullOrEmpty(state.PlayerId) || state.PlayerId == _localPlayerId) return;

            // 이미 생성한 원격 플레이어가 있는지 확인
            if (!_remotePlayers.TryGetValue(state.PlayerId, out var actor))
            {
                if (remotePlayerPrefab == null)
                {
                    Debug.LogWarning("RemoteActorManager: remotePlayerPrefab이 할당되지 않았습니다.");
                    return;
                }

                GameObject go = Instantiate(remotePlayerPrefab, state.Position, Quaternion.identity);
                go.name = $"RemotePlayer_{state.PlayerId}";
                actor = go.GetComponent<ActorController>() ?? go.AddComponent<ActorController>();
                _remotePlayers[state.PlayerId] = actor;
            }

            // 서버 공인 위치로 보정
            actor.SetPosition(state.Position);
        }
    }
}
