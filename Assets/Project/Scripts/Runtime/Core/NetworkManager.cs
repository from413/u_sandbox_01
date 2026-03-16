using UnityEngine;
using System.Collections;
using System;

namespace MyGame.Runtime.Core
{
    public class MyNetworkManager : MonoBehaviour
    {
        // 싱글톤
        public static MyNetworkManager Instance { get; private set; }

        // 로컬 플레이어의 세션 정보 저장소
        public PlayerSession LocalSession { get; private set; }

        // 연결 완료 시 발생할 이벤트
        public event Action OnConnectionSuccess;

        private void Awake() => Instance = this;
        public void ConnectToServer()
        {
            Debug.Log("서버와 핸드셰이크 시도 중...");
            // 실제 네트워크 라이브러리(Mirror, Netcode 등) 대신 코루틴으로 시뮬레이션
            StartCoroutine(SimulateConnection());
        }

        private IEnumerator SimulateConnection()
        {
            // 2초간 연결 시도 시뮬레이션
            yield return new WaitForSeconds(2.0f);

            // 서버로부터 받은 데이터라고 가정 (가짜 ID 생성)
            string fakeId = "User_" + UnityEngine.Random.Range(1000, 9999);
            LocalSession = new PlayerSession(fakeId, "Player_01");

            Debug.Log("서버 연결 성공! 부여받은 ID: {LocalSession.PlayerId}");
            OnConnectionSuccess?.Invoke();
        }
    }
}