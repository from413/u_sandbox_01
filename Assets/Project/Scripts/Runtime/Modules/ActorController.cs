using UnityEngine;

namespace MyGame.Runtime.Modules
{
    public class ActorController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        
        // 회전 기능 (향후 확장용)
        [SerializeField] private bool enableRotation = false;
        [SerializeField] private float rotationSpeed = 180f; // 도/초

        public float MoveSpeed => moveSpeed;
        public float RotationSpeed => rotationSpeed;

        // 캐시된 이동 방향 (최적화용)
        private Vector3 _lastMoveDirection = Vector3.zero;

        // 입력 버퍼 충돌 방지
        private Vector3 _cachedMovementDelta = Vector3.zero;

        public void ApplyInput(InputPacket packet)
        {
            ApplyInput(packet, Time.deltaTime);
        }

        /// <summary>
        /// deltaTime을 명시적으로 전달할 수 있어 재생(replay) 및 고정 틱 
        /// 시뮬레이션에서 일관된 결과를 얻습니다.
        /// </summary>
        public void ApplyInput(InputPacket packet, float deltaTime)
        {
            if (packet.Tick == 0) return; // 빈 패킷 무시

            // 이동 방향 계산
            Vector3 moveDirection = new Vector3(packet.Horizontal, 0, packet.Vertical).normalized;
            
            // 이동 적용
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                transform.Translate(moveDirection * moveSpeed * deltaTime);
                _lastMoveDirection = moveDirection;

                // 회전 활성화 시 플레이어 방향 업데이트
                if (enableRotation)
                {
                    ApplyRotation(moveDirection, deltaTime);
                }
            }

            // 점프 로직
            if (packet.IsJump)
            {
                ExecuteJumpAction();
            }
        }

        /// <summary>
        /// 회전을 이동 방향에 따라 업데이트합니다.
        /// </summary>
        private void ApplyRotation(Vector3 moveDirection, float deltaTime)
        {
            if (moveDirection.sqrMagnitude < 0.01f) return;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            float step = rotationSpeed * deltaTime;
            
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                step
            );
        }

        /// <summary>
        /// 점프 액션 실행 (기본 구현: 로그 출력)
        /// 실제 물리 기반 점프는 Rigidbody를 사용하여 구현 가능
        /// </summary>
        private void ExecuteJumpAction()
        {
            Debug.Log($"[Actor:{gameObject.name}] Jump Action!");
            
            // Rigidbody 기반 점프 예시:
            // Rigidbody rb = GetComponent<Rigidbody>();
            // if (rb != null) rb.velocity += Vector3.up * jumpForce;
        }

        /// <summary>
        /// 서버/네트워크에서 전달된 공인 위치를 설정할 때 사용
        /// (원격 플레이어 보정 또는 강제 초기화)
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// 회전을 직접 설정합니다 (향후 네트워크 동기화 시 사용)
        /// </summary>
        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        /// <summary>
        /// 리셋 (게임 재시작 등)
        /// </summary>
        public void Reset()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            _lastMoveDirection = Vector3.zero;
        }
    }
}