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

        private MyNetworkManager _NetworkMgr;

        [Header("Server Settings")]
        [SerializeField] private float serverTickInterval = 0.033f; // 30Hz 시뮬레이션
        [SerializeField] private float _serverMoveSpeed = 5f;

        private Queue<InputPacket> _incomingPackets = new Queue<InputPacket>();

        // 서버가 관리하는 플레이어들의 공인(Authoritative) 위치
        private Dictionary<string, Vector3> _playerPositions = new Dictionary<string, Vector3>();

        // 서버 틱 카운터 (고정 틱당 1씩 증가)
        private uint _serverTick = 0;

        // 로컬 테스트용 가상 플레이어 (봇)
        [SerializeField] private bool simulateBot = true;
        private string _botId = "Bot_01";
        private float _botAngle = 0f;

        private void Awake()
        {
            Instance = this;

            _NetworkMgr = MyNetworkManager.Instance;
            if (_NetworkMgr != null)
            {
                _NetworkMgr.OnRawPacketSent += HandleRawPacket;
            }
        }

        void Start()
        {
            StartCoroutine(ServerTickLoop());
        }

        private void OnDestroy()
        {
            if (_NetworkMgr != null)
            {
                _NetworkMgr.OnRawPacketSent -= HandleRawPacket;
            }
        }

        private void HandleRawPacket(string json)
        {
            // JSON -> InputPacket 리스트 변환
            var batch = JsonUtility.FromJson<InputPacketBatch>(json);
            if (batch == null || batch.Packets == null || batch.Packets.Count == 0)
                return;

            ReceiveInputFromClient(batch.Packets);
        }

        // 클라이언트(InputBufferManager)로부터 패킷 수신 (시뮬레이션)
        public void ReceiveInputFromClient(List<InputPacket> packets)
        {
            // 네트워크 쪽으로 받은 패킷은 서버 내부 큐에 쌓아 두고,
            // 고정 틱(서버 틱)에서 처리합니다.
            foreach (var packet in packets)
            {
                _incomingPackets.Enqueue(packet);
            }
        }

        private System.Collections.IEnumerator ServerTickLoop()
        {
            while (true)
            {
                ProcessTick();
                yield return new WaitForSeconds(serverTickInterval);
            }
        }

        private void ProcessTick()
        {
            _serverTick++;

            int count = _incomingPackets.Count;
            for (int i = 0; i < count; i++)
            {
                var packet = _incomingPackets.Dequeue();
                ProcessInput(packet);
            }

            // 서버 틱마다 로컬 테스트용 봇 위치를 업데이트
            if (simulateBot)
            {
                UpdateBot(serverTickInterval);
            }

            // 전체 플레이어 상태를 브로드캐스트
            foreach (var kvp in _playerPositions)
            {
                BroadcastState(kvp.Key, _serverTick);
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

            // 고정 틱 간격으로 이동 결과 갱신
            _playerPositions[packet.PlayerId] += moveDir * _serverMoveSpeed * serverTickInterval;

            Debug.Log($"[Server] Tick:{_serverTick} / ID:{packet.PlayerId} / AuthPos:{_playerPositions[packet.PlayerId]}");
        }

        private void UpdateBot(float deltaTime)
        {
            if (!_playerPositions.ContainsKey(_botId))
            {
                _playerPositions[_botId] = new Vector3(2, 0, 0);
            }

            // 원을 그리며 이동하는 간단한 봇
            _botAngle += deltaTime;
            float radius = 3f;
            Vector3 center = Vector3.zero;
            _playerPositions[_botId] = center + new Vector3(Mathf.Cos(_botAngle), 0, Mathf.Sin(_botAngle)) * radius;
        }

        private void BroadcastState(string playerId, uint tick)
        {
            ServerStatePacket state = new ServerStatePacket(playerId, tick, _playerPositions[playerId]);

            // 모든 클라이언트에게 이 정보를 보냄 (지금은 로컬 네트워크 매니저를 통해 전달)
            if (_NetworkMgr != null)
            {
                _NetworkMgr.ReceiveServerState(state);
            }

            Debug.Log($"[Server] Tick:{tick} 처리 완료 -> 위치:{state.Position}");
        }
    }
}