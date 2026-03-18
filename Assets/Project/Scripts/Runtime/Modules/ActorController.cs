using UnityEngine;

namespace MyGame.Runtime.Modules
{
    public class ActorController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        public float MoveSpeed => moveSpeed;

        // InputBufferManager로부터 패킷을 전달받아 실행
        public void ApplyInput(InputPacket packet)
        {
            ApplyInput(packet, Time.deltaTime);
        }

        // deltaTime을 명시적으로 전달할 수 있어 재생(replay) 및 고정 틱 시뮬레이션에서 일관된 결과를 얻습니다.
        public void ApplyInput(InputPacket packet, float deltaTime)
        {
            Vector3 moveDirection = new Vector3(packet.Horizontal, 0, packet.Vertical).normalized;
            transform.Translate(moveDirection * moveSpeed * deltaTime);

            if (packet.IsJump)
            {
                Debug.Log($"[Actor] Tick:{packet.Tick} - Jump Action Executed!");
                // Rigidbody 점프 로직 등을 여기에 추가 가능
            }
        }

        // 서버/네트워크에서 전달된 공인 위치를 설정할 때 사용 (원격 플레이어 보정)
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}