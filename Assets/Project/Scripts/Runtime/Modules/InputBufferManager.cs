using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using MyGame.Runtime.Core;

namespace MyGame.Runtime.Modules
{
    public class InputBufferManager : MonoBehaviour
    {
        // 싱글톤 패턴
        public static InputBufferManager Instance { get; private set; }

        // 변수명 컨벤션 (private은 _ 붙이기)
        private GameManager _gameMgr;
        private MyNetworkManager _NetworkMgr;
        private ActorController _localActor;

        private Queue<InputPacket> _inputBuffer = new Queue<InputPacket>();
        private List<InputHistoryEntry> _inputHistory = new List<InputHistoryEntry>(); // 서버 보정용 히스토리
        private uint _currentTick = 0;

        [SerializeField] private float moveSpeed = 5f; // 로컬 예측 계산용

        [Header("Network Settings")]
        [SerializeField] private float sendInterval = 0.033f; // 30 FPS (Tick Rate)
        
        [Header("Correction Settings")]
        [SerializeField] private float _correctionSmoothTime = 0.2f; // 보정을 얼마나 부드럽게 할지 (초)
        private Vector3 _correctionVelocity = Vector3.zero;
        private Vector3 _targetPosition = Vector3.zero;

        private void Awake() => Instance = this;

        public void RegisterLocalActor(ActorController actor)
        {
            _localActor = actor;
        }

        void Start()
        {
            _gameMgr = GameManager.Instance;
            _NetworkMgr = MyNetworkManager.Instance;

            if (_NetworkMgr != null)
            {
                _NetworkMgr.OnServerStateReceived += HandleServerState;
            }

            // 네트워크 송신 루프 시작
            StartCoroutine(NetworkSendLoop());
        }

        private void OnDestroy()
        {
            if (_NetworkMgr != null)
            {
                _NetworkMgr.OnServerStateReceived -= HandleServerState;
            }
        }

        void Update()
        {
            // InGame 상태에서만 입력을 수집
            if (_gameMgr.CurrentState != GameState.InGame) return;

            CollectInput();

            // 보정 적용
            if (_localActor != null && _correctionSmoothTime > 0)
            {
                Vector3 current = _localActor.transform.position;
                Vector3 smoothed = Vector3.SmoothDamp(current, _targetPosition, ref _correctionVelocity, _correctionSmoothTime);
                _localActor.transform.position = smoothed;
            }
        }

        private void CollectInput()
        {
            _currentTick++;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool jump = Input.GetKeyDown(KeyCode.Space);

            // 로컬 세션 ID 가져오기
            string playerId = _NetworkMgr.LocalSession.PlayerId;

            // 패킷 생성
            InputPacket packet = new InputPacket(playerId, _currentTick, h, v, jump);

            // 1. 서버 전송용 버퍼에 저장
            _inputBuffer.Enqueue(packet);

            // 2. [로컬 예측] 서버 응답 전, 내 화면의 캐릭터에게 즉시 적용
            if (_localActor != null)
            {
                _localActor.ApplyInput(packet);
                
                // 입력 히스토리에 저장 (서버 보정용)
                var entry = new InputHistoryEntry(_currentTick, packet, _localActor.transform.position);
                _inputHistory.Add(entry);
                
                // 목표 위치 업데이트
                _targetPosition = _localActor.transform.position;
            }

            if (h != 0 || v != 0 || jump)
            {
                Debug.Log($"[Tick:{_currentTick}] 입력 감지 - H:{h}, V:{v}, Jump:{jump}");
            }
        }

        private IEnumerator NetworkSendLoop()
        {
            while (true)
            {
                // InGame 상태일 때만 전송 루프 가동
                if (_gameMgr.CurrentState == GameState.InGame)
                {
                    SendBufferedPackets();
                }

                yield return new WaitForSeconds(sendInterval);
            }
        }

        private void SendBufferedPackets()
        {
            if (_inputBuffer.Count == 0) return;

            // 현재 버퍼에 쌓인 모든 패킷을 리스트로 묶음 (Batching)
            List<InputPacket> packetsToSend = new List<InputPacket>();
            while (_inputBuffer.Count > 0)
            {
                packetsToSend.Add(_inputBuffer.Dequeue());
            }

            // JSON 변환 (네트워크 레이어가 InputPacket 타입을 알 필요가 없도록 추상화)
            InputPacketBatch batch = new InputPacketBatch { Packets = packetsToSend };
            string json = JsonUtility.ToJson(batch);

            // 시뮬레이션: 로컬 서버에 전달
            _NetworkMgr?.SendRawPacket(json);

            Debug.Log($"[Network] 서버로 {packetsToSend.Count}개의 패킷 전송 (최신 Tick: {packetsToSend[^1].Tick})");
        }

        // 서버로 보낼 패킷들을 꺼내오는 메서드
        public InputPacket GetNextPacket()
        {
            return _inputBuffer.Count > 0 ? _inputBuffer.Dequeue() : default;
        }

        // 서버의 공인 상태를 받아서 로컬 예측을 보정
        private void HandleServerState(ServerStatePacket state)
        {
            if (_localActor == null || state.PlayerId != _NetworkMgr.LocalSession.PlayerId) return;

            // 서버로부터 받은 공인 위치
            Vector3 serverAuthPosition = state.Position;
            Vector3 currentPredictedPosition = _localActor.transform.position;

            // 오차 계산
            float positionError = Vector3.Distance(currentPredictedPosition, serverAuthPosition);

            // 서버 위치를 목표로 설정 (SmoothDamp로 부드럽게 보정)
            _targetPosition = serverAuthPosition;

            Debug.Log($"[Correction] Tick:{state.LastProcessedTick} / Predicted:{currentPredictedPosition} / Server:{serverAuthPosition} / Error:{positionError:F3}");
        }
    }
}