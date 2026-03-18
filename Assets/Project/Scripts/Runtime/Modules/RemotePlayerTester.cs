using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Runtime.Core;

namespace MyGame.Runtime.Modules
{
    /// <summary>
    /// 테스트용 가상 클라이언트(봇)를 생성하여 서버의 입력 큐가 정상 작동하는지 확인합니다.
    /// - 각 봇은 주기적으로 랜덤 입력을 생성하여 서버에 전송합니다.
    /// - 연결/끊김(Churn)을 시뮬레이션할 수 있습니다.
    /// </summary>
    public class RemotePlayerTester : MonoBehaviour
    {
        [Header("Bot Configuration")]
        [SerializeField] private int botCount = 3;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float connectDelay = 0.5f;
        [SerializeField] private float minChurnInterval = 6f;
        [SerializeField] private float maxChurnInterval = 16f;

        [Header("Network Simulation")]
        [SerializeField] private bool useNetworkSimulation = true;
        [SerializeField] private float sendInterval = 0.033f; // 30Hz

        private MyNetworkManager _networkMgr;
        private MinimalServer _server;
        private readonly List<BotClient> _bots = new List<BotClient>();

        private void Start()
        {
            _networkMgr = MyNetworkManager.Instance;
            _server = MinimalServer.Instance;

            if (autoStart)
            {
                StartTesting();
            }
        }

        private void OnDestroy()
        {
            StopTesting();
        }

        /// <summary>
        /// 테스트 시작 (외부에서 수동으로 호출 가능)
        /// </summary>
        public void StartTesting()
        {
            StopTesting();

            for (int i = 0; i < botCount; i++)
            {
                var bot = new BotClient($"Bot_{i + 1:00}", sendInterval);
                _bots.Add(bot);
                StartCoroutine(RunBotLifecycle(bot));
            }

            Debug.Log($"[RemotePlayerTester] {botCount} bots started.");
        }

        /// <summary>
        /// 테스트 종료 (유니티 객체 파괴 시 자동 호출)
        /// </summary>
        public void StopTesting()
        {
            foreach (var bot in _bots)
            {
                bot.Stop();
            }

            _bots.Clear();
            StopAllCoroutines();
        }

        private IEnumerator RunBotLifecycle(BotClient bot)
        {
            while (true)
            {
                // 연결 대기
                yield return new WaitForSeconds(connectDelay + Random.Range(0f, 1f));

                bot.Connect();
                Debug.Log($"[RemotePlayerTester] Bot connected: {bot.Id}");
                StartCoroutine(SendBotInputLoop(bot));

                // 일정 시간 후 끊기 (Churn)
                float liveTime = Random.Range(minChurnInterval, maxChurnInterval);
                yield return new WaitForSeconds(liveTime);

                bot.Disconnect();
                Debug.Log($"[RemotePlayerTester] Bot disconnected: {bot.Id} (lived {liveTime:F1}s)");

                // 잠시 휴식 후 다시 연결
                yield return new WaitForSeconds(Random.Range(1f, 3f));
            }
        }

        private IEnumerator SendBotInputLoop(BotClient bot)
        {
            while (bot.IsConnected)
            {
                var packet = bot.CreateNextPacket();

                if (useNetworkSimulation && _networkMgr != null)
                {
                    // 클라이언트와 동일한 경로를 통해 서버로 전송
                    var batch = new InputPacketBatch { Packets = new List<InputPacket> { packet } };
                    string json = JsonUtility.ToJson(batch);
                    _networkMgr.SendRawPacket(json);
                }
                else if (_server != null)
                {
                    // 바로 서버 큐에 입력 추가 (네트워크 레이어 우회)
                    _server.ReceiveInputFromClient(new List<InputPacket> { packet });
                }

                yield return new WaitForSeconds(sendInterval);
            }
        }

        private class BotClient
        {
            public string Id { get; }
            public bool IsConnected { get; private set; }

            private uint _tick;
            private readonly float _sendInterval;

            public BotClient(string id, float sendInterval)
            {
                Id = id;
                _sendInterval = sendInterval;
                _tick = 0;
            }

            public void Connect()
            {
                IsConnected = true;
                _tick = 0;
            }

            public void Disconnect()
            {
                IsConnected = false;
            }

            public InputPacket CreateNextPacket()
            {
                _tick++;

                // 랜덤한 이동 입력 (양방향)
                float h = Random.Range(-1f, 1f);
                float v = Random.Range(-1f, 1f);

                // 가끔 점프
                bool jump = Random.value < 0.1f;

                // 회전은 이동 방향 기반
                Quaternion rotation = Quaternion.identity;
                Vector3 dir = new Vector3(h, 0, v);
                if (dir.sqrMagnitude > 0.01f)
                    rotation = Quaternion.LookRotation(dir.normalized);

                return new InputPacket(Id, _tick, h, v, jump, rotation);
            }

            public void Stop()
            {
                IsConnected = false;
            }
        }
    }
}
