using UnityEngine;
using System.Collections.Generic;
using MyGame.Runtime.Core;
using MyGame.Runtime.Modules;

namespace MyGame.Server
{
    /// <summary>
    /// 고정 틱 기반의 최소 서버 시뮬레이터.
    /// 클라이언트의 입력을 수신하여 권위있는 상태를 계산하고,
    /// 모든 클라이언트에게 공인 위치를 브로드캐스트합니다.
    /// </summary>
    public class MinimalServer : MonoBehaviour
    {
        // 싱글톤 패턴
        public static MinimalServer Instance { get; private set; }

        private MyNetworkManager _NetworkMgr;

        [Header("Server Settings")]
        [SerializeField] private float serverTickInterval = 0.033f; // 30Hz 시뮬레이션
        [SerializeField] private float _serverMoveSpeed = 5f;
        [SerializeField] private bool _enableDetailedLogging = false;

        private Queue<InputPacket> _incomingPackets = new Queue<InputPacket>();

        // 서버가 관리하는 플레이어들의 공인(Authoritative) 위치
        private Dictionary<string, Vector3> _playerPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, Quaternion> _playerRotations = new Dictionary<string, Quaternion>(); // 새로 추가: 회전 추적
        private Dictionary<string, uint> _lastProcessedTicks = new Dictionary<string, uint>();

        // 서버 틱 카운터 (고정 틱당 1씩 증가)
        private uint _serverTick = 0;

        // 로컬 테스트용 가상 플레이어 (봇)
        [SerializeField] private bool simulateBot = true;
        private string _botId = "Bot_01";
        private float _botAngle = 0f;

        // 성능 모니터링
        private int _inputsProcessedThisTick = 0;
        private int _totalInputsProcessed = 0;

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
            Debug.Log("[Server] 서버 시작 (30Hz 고정 틱)");
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

        /// <summary>
        /// 클라이언트(InputBufferManager)로부터 패킷 수신
        /// 수신한 패킷은 서버 내부 큐에 쌓아 두고, 고정 틱에서 처리됩니다.
        /// </summary>
        public void ReceiveInputFromClient(List<InputPacket> packets)
        {
            foreach (var packet in packets)
            {
                _incomingPackets.Enqueue(packet);
            }
            if (_enableDetailedLogging && packets.Count > 0)
            {
                Debug.Log($"[Server] 클라이언트로부터 {packets.Count}개 입력 수신 (큐 크기: {_incomingPackets.Count})");
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
            _inputsProcessedThisTick = 0;

            // 큐에 쌓인 모든 입력 처리
            int inputCount = _incomingPackets.Count;
            for (int i = 0; i < inputCount; i++)
            {
                var packet = _incomingPackets.Dequeue();
                ProcessInput(packet);
                _inputsProcessedThisTick++;
            }

            // 서버 틱마다 봇 위치 업데이트
            if (simulateBot)
            {
                UpdateBot(serverTickInterval);
            }

            // 모든 플레이어의 상태를 각 클라이언트에게 브로드캐스트
            foreach (var playerId in _playerPositions.Keys)
            {
                BroadcastState(playerId, _serverTick);
            }

            // 주기적인 로깅 (매 30틱마다)
            if (_serverTick % 30 == 0 && _serverTick > 0)
            {
                Debug.Log($"[Server] Tick:{_serverTick} / 플레이어:{_playerPositions.Count} / 이번 틱 처리:{_inputsProcessedThisTick} inputs");
                _totalInputsProcessed += _inputsProcessedThisTick;
            }
        }

        /// <summary>
        /// 클라이언트 입력을 서버 권위 상태에 반영합니다.
        /// 실제 서버에서는 이곳에서 게임 규칙 검증, 치트 방지, 충돌 검사 등을 수행합니다.
        /// </summary>
        private void ProcessInput(InputPacket packet)
        {
            // 새로운 플레이어 초기화
            if (!_playerPositions.ContainsKey(packet.PlayerId))
            {
                Debug.Log($"[Server] 새로운 플레이어 감지: {packet.PlayerId} - 초기 위치 설정");
                _playerPositions[packet.PlayerId] = Vector3.zero;
                _playerRotations[packet.PlayerId] = Quaternion.identity; // 회전도 초기화
                _lastProcessedTicks[packet.PlayerId] = 0;
            }

            // 이미 처리한 틱의 입력은 무시 (중복 방지)
            if (packet.Tick <= _lastProcessedTicks[packet.PlayerId])
            {
                if (_enableDetailedLogging)
                    Debug.LogWarning($"[Server] 중복 입력 무시: {packet.PlayerId} Tick:{packet.Tick}");
                return;
            }

            _lastProcessedTicks[packet.PlayerId] = packet.Tick;

            // [검증 및 계산] 서버 사이드 시뮬레이션
            Vector3 moveDir = new Vector3(packet.Horizontal, 0, packet.Vertical).normalized;

            // 고정 틱 간격으로 이동 계산
            if (moveDir.sqrMagnitude > 0.01f)
            {
                _playerPositions[packet.PlayerId] += moveDir * _serverMoveSpeed * serverTickInterval;
                
                // 이동 방향에 따라 회전 계산
                _playerRotations[packet.PlayerId] = Quaternion.LookRotation(moveDir);
            }
            else
            {
                // 이동이 없으면 클라이언트가 보낸 회전을 사용 (조준 방향)
                _playerRotations[packet.PlayerId] = packet.AimRotation;
            }

            if (_enableDetailedLogging)
            {
                Debug.Log($"[Server] Tick:{_serverTick} / {packet.PlayerId} / " +
                    $"Input:({packet.Horizontal:F1}, {packet.Vertical:F1}) / " +
                    $"AuthPos:{_playerPositions[packet.PlayerId]} / Rot:{_playerRotations[packet.PlayerId].eulerAngles.y:F1}°");
            }
        }

        /// <summary>
        /// 테스트용 봇을 원 궤도로 이동시킵니다.
        /// </summary>
        private void UpdateBot(float deltaTime)
        {
            if (!_playerPositions.ContainsKey(_botId))
            {
                _playerPositions[_botId] = new Vector3(3, 0, 0);
                _playerRotations[_botId] = Quaternion.identity; // 봇의 회전도 초기화
                _lastProcessedTicks[_botId] = _serverTick;
            }

            // 원을 그리며 이동하는 간단한 봇
            _botAngle += (360f / 10f) * deltaTime; // 10초에 한 바퀴
            if (_botAngle >= 360f) _botAngle -= 360f;

            float radius = 3f;
            Vector3 center = Vector3.zero;
            Vector3 botPos = center + new Vector3(
                Mathf.Cos(_botAngle * Mathf.Deg2Rad), 
                0, 
                Mathf.Sin(_botAngle * Mathf.Deg2Rad)
            ) * radius;
            
            _playerPositions[_botId] = botPos;
            
            // 봇이 이동하는 방향으로 회전
            Vector3 botDirection = botPos - center;
            if (botDirection.sqrMagnitude > 0.01f)
            {
                _playerRotations[_botId] = Quaternion.LookRotation(botDirection.normalized);
            }
        }

        /// <summary>
        /// 특정 플레이어의 상태를 모든 클라이언트에게 브로드캐스트합니다.
        /// </summary>
        private void BroadcastState(string playerId, uint tick)
        {
            // 서버가 해당 플레이어에 대해 마지막으로 처리한 입력 틱 번호를 사용.
            // 클라이언트 보정(reconciliation)이 올바르게 동작하려면 이 값이 클라이언트의 입력 틱과 일치해야 합니다.
            uint lastProcessedTick = _lastProcessedTicks.ContainsKey(playerId)
                ? _lastProcessedTicks[playerId]
                : tick;

            // 플레이어의 회전 가져오기 (없으면 항등원소)
            Quaternion playerRotation = _playerRotations.ContainsKey(playerId) 
                ? _playerRotations[playerId] 
                : Quaternion.identity;

            ServerStatePacket state = new ServerStatePacket(
                playerId, 
                lastProcessedTick, 
                _playerPositions[playerId],
                playerRotation  // 회전도 포함
            );

            // 모든 클라이언트에게 이 정보를 보냄
            if (_NetworkMgr != null)
            {
                _NetworkMgr.ReceiveServerState(state);
            }
        }

        /// <summary>
        /// 현재 서버 상태를 조회합니다 (디버깅용).
        /// </summary>
        public void PrintServerStatus()
        {
            Debug.Log($"=== 서버 상태 (Tick:{_serverTick}) ===");
            Debug.Log($"활성 플레이어: {_playerPositions.Count}");
            foreach (var kvp in _playerPositions)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value} (LastTick:{_lastProcessedTicks[kvp.Key]})");
            }
            Debug.Log($"큐 크기: {_incomingPackets.Count}");
            Debug.Log($"누적 처리 입력: {_totalInputsProcessed}");
        }
    }
}