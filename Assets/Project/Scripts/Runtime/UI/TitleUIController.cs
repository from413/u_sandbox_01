using UnityEngine;
using TMPro;
using MyGame.Runtime.Core;
using System.Collections; // 코루틴 사용

namespace MyGame.Runtime.UI
{
    public class TitleUIController : MonoBehaviour
    {
        // 변수명 컨벤션 (private은 _ 붙이기)
        private GameManager _gameMgr;
        private CanvasGroup _canvasGroup; // 페이드용

        [Header("UI Elements")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject pressAnyKeyPrompt;
        [SerializeField] private GameObject loadingSpinner; // 로딩 연출용 (선택)

        private void Awake()
        {
            // 싱글톤 패턴 (캐싱)
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            // 싱글톤 패턴 (캐싱)
            _gameMgr = GameManager.Instance;

            if (_gameMgr != null)
            {
                // 초기 상태 UI 반영
                UpdateUI(_gameMgr.CurrentState);

                // 이벤트 구독
                _gameMgr.OnStateChanged += UpdateUI;
            }
            else
            {
                Debug.LogError("GameManager를 찾을 수 없습니다! 씬에 배치했는지 확인하세요.");
            }
        }

        private void OnDestroy()
        {
            // 메모리 누수 방지를 위해 이벤트 구독 해제
            if (_gameMgr != null)
            {
                _gameMgr.OnStateChanged -= UpdateUI;
            }
        }

        private void UpdateUI(GameState state)
        {
            if (statusText == null || pressAnyKeyPrompt == null) return;

            switch (state)
            {
                case GameState.Intro:
                    statusText.text = "Status: Press Any Key";
                    pressAnyKeyPrompt.SetActive(true);
                    if (loadingSpinner) loadingSpinner.SetActive(false);
                    break;
                case GameState.Connecting:
                    statusText.text = "Status: Connecting to Server...";
                    pressAnyKeyPrompt.SetActive(false);
                    if (loadingSpinner) loadingSpinner.SetActive(true);
                    break;
                case GameState.Lobby:
                    statusText.text = "Status: Connected!";
                    // 로비 진입 시 처리 (에: 캔버스 페이드 아웃 등)
                    // 연결 성공 시 페이드 아웃 시작
                    StartCoroutine(FadeOutAndDisable());
                    break;
            }
        }

        private IEnumerator FadeOutAndDisable()
        {
            yield return new WaitForSeconds(1.0f); // "Connected!" 메시지를 잠시 보여줌

            float duration = 1.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = 0;
            gameObject.SetActive(false); // UI 비활성화하여 다음 단계 준비

            Debug.Log("Title UI 페이드 아웃 완료. 이제 무대가 준비되었습니다.");
        }
    }
}