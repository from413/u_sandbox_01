using UnityEngine;

namespace MyGame.Runtime.Modules
{
    public class ActorController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        // InputBufferManager로부터 패킷을 전달받아 실행
        public void ApplyInput(InputPacket packet)
        {
            Vector3 moveDirection = new Vector3(packet.Horizontal, 0, packet.Vertical).normalized;
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime);

            if (packet.IsJump)
            {
                Debug.Log($"[Actor] Tick:{packet.Tick} - Jump Action Executed!");
                // Rigidbody 점프 로직 등을 여기에 추가 가능
            }
        }
    }
}