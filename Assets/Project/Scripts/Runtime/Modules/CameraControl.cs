using UnityEngine;

namespace MyGame.Runtime.Modules
{
    public class CameraControl : MonoBehaviour
    {
        // 싱글톤 패턴      
        public static CameraControl Instance { get; private set; }

        // 변수명 컨벤션 (private은 _ 붙이기)
        private Transform _target;

        [SerializeField] private Vector3 offset = new Vector3(0, 5, -10);
        [SerializeField] private float smoothSpeed = 0.125f;

        private void Awake() => Instance = this;

        // ActorSpawner가 호출할 메서드
        public void SetTarget(Transform target)
        {
            _target = target; // spawn된 Player
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desiredPosition = _target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            // 항상 타겟을 바라보게 설정
            transform.LookAt(_target);
        }
    }
}
