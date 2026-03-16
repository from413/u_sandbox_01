using UnityEngine;
using System;

namespace MyGame.Runtime.Core
{
    public class GameManager : MonoBehaviour
    {
        // 싱글톤 패턴
        public static GameManager Instance { get; private set; }

        // 변수명 컨벤션 (private은 _ 붙이기)
        private MyNetworkManager _NetworkMgr;

        public GameState CurrentState { get; private set; } = GameState.Intro;

        // 상태 변경 시 UI나 다른 모듈에 알림
        public event Action<GameState> OnStateChanged;

        private void Awake() => Instance = this; // 자동 할당


        void Start()
        {
            // 싱글톤 패턴 (캐싱)
            _NetworkMgr = MyNetworkManager.Instance;
        }


        public void ChangeState(GameState newState)
        {
            // 상태 변경없으면 넘어감
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnStateChanged?.Invoke(newState);

            Debug.Log($"Game State Changed to: {newState}");
        }

        void Update()
        {
            // Intro 상태에서만 AnyKey 입력 감지
            if (CurrentState == GameState.Intro && Input.anyKeyDown)
            {
                StartConnection();
            }
        }

        private void StartConnection()
        {
            ChangeState(GameState.Connecting);

            // TODO: NetworkManager.Connect() 호출 로직 연결 예정
            // 로직연결
            // NetworkManager에 연결 요청
            if (_NetworkMgr != null)
            {
                _NetworkMgr.OnConnectionSuccess += HandleConnectionSuccess;
                _NetworkMgr.ConnectToServer();
            }
        }

        private void HandleConnectionSuccess()
        {
            // 이벤트 구독 해제 (일회성 연결)
            _NetworkMgr.OnConnectionSuccess -= HandleConnectionSuccess;

            // 연결 성공 시 Lobby 상태로 전이
            ChangeState(GameState.Lobby);

            // 로비 연출(페이드 등)이 끝날 시간을 고려해 1.5초 후 InGame으로 변경
            Invoke(nameof(EnterInGame), 1.5f);
        }

        private void EnterInGame()
        {
            ChangeState(GameState.InGame);
            // Debug.Log("GameManager: InGame 상태로 진입했습니다!");
        }
    }
}