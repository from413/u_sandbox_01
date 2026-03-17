using UnityEngine;
using System.Collections.Generic;
using MyGame.Runtime.Core;
using MyGame.Runtime.Modules;

namespace MyGame.Server
{
    public class MinimalServer : MonoBehaviour
    {
        // 싱글톤 패턴
        public static MinimalServer Instance { get; private set; }

        // 서버가 관리하는 플레이어들의 공인(Authoritative) 위치
        private Dictionary<string, Vector3> _playerPositions = new Dictionary<string, Vector3>();
        private float _serverMoveSpeed = 5f;

        private void Awake() => Instance = this;

        // 클라이언트(InputBufferManager)로부터 패킷 수신 (시뮬레이션)
        public void ReceiveInputFromClient(List<InputPacket> packets)
        {
            foreach (var packet in packets)
            {
                ProcessInput(packet);
            }
        }

        private void ProcessInput(InputPacket packet)
        {
            if (!_playerPositions.ContainsKey(packet.PlayerId))
            {
                _playerPositions[packet.PlayerId] = Vector3.zero;
            }

            // [검증 및 계산] 서버 사이드 시뮬레이션
            // 실제 서버라면 여기서 '이동 거리가 물리적으로 가능한가?' 등을 체크합니다.
            Vector3 moveDir = new Vector3(packet.Horizontal, 0, packet.Vertical).normalized;

            // 서버 틱(30fps) 기준으로 위치 계산
            // 실제 서버라면 여기서 deltaTime 대신 고정 틱(Tick) 시간을 사용함
            _playerPositions[packet.PlayerId] += moveDir * _serverMoveSpeed * 0.033f;

            // 계산된 공인 위치를 로그로 출력 (추후 클라이언트에게 브로드캐스팅)
            Debug.Log($"[Server] ID:{packet.PlayerId} / Tick:{packet.Tick} / AuthPos:{_playerPositions[packet.PlayerId]}");

            // 결과 브로드캐스팅 시뮬레이션
            BroadcastState(packet.PlayerId, packet.Tick);
        }

        private void BroadcastState(string playerId, uint tick)
        {
            ServerStatePacket state = new ServerStatePacket(playerId, tick, _playerPositions[playerId]);

            // 모든 클라이언트에게 이 정보를 보냄 (지금은 로컬 매니저에 전달)
            // NetworkManager.Instance.OnServerStateReceived(state);
            Debug.Log($"[Server] Tick:{tick} 처리 완료 -> 위치:{state.Position}");

        }
    }
}