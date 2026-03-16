using UnityEngine;
using MyGame.Runtime.Core;

namespace MyGame.Runtime.Modules
{
    public class ActorSpawner : MonoBehaviour
    {
        // 변수명 컨벤션 (private은 _ 붙이기)
        private GameManager _gameMgr;

        [SerializeField] private GameObject playerPrefab; // 스폰할 캐릭터 프리팹 할당

        private void Start()
        {
            // 싱글톤 패턴 (캐싱)
            _gameMgr = GameManager.Instance;

            // GameManager의 상태 변화를 관찰
            if(_gameMgr != null)
            {
                _gameMgr.OnStateChanged += HandleStateChanaged;
            }
        }

        private void OnDestroy()
        {
            if (_gameMgr != null)
            {
                _gameMgr.OnStateChanged -= HandleStateChanaged;
            }
        }

        private void HandleStateChanaged(GameState newState)
        {
            // Lobby 상태를 거쳐 InGame으로 진입할 때 스폰
            if (newState == GameState.InGame)
            {
                SpawnPlayer();
            }
        }

        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab이 할당되지 않았습니다!");
                return;
            }

            // 서버의 위치 데이터를 받았다고 가정하고 생성
            Debug.Log("서버로부터 스폰 데이터를 받았다고 가정하고 캐릭터 생성");
            GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            player.name = "LocalPlayer_Actor";

            Debug.Log("캐릭터 스폰 완료: InGame 진입");
        }
    }
}