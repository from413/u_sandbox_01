using UnityEngine;

namespace MyGame.Runtime.Modules
{
    /// <summary>
    /// 원격 플레이어의 고급 동기화를 지원하는 보조 클래스.
    /// 선형 보간(Linear Interpolation), 외삽(Extrapolation), 라그 보정(Lag Compensation) 등을 제공합니다.
    /// 
    /// 사용 예:
    /// RemoteActorSyncHelper syncHelper = new RemoteActorSyncHelper(moveSpeed: 5.0f);
    /// Vector3 syncedPosition = syncHelper.CalculatePosition(lastPos, targetPos, deltaTime);
    /// </summary>
    public class RemoteActorSyncHelper
    {
        /// <summary>
        /// 보간/외삽 모드
        /// </summary>
        public enum SyncMode
        {
            Lerp,           // 선형 보간: 부드러운 이동
            Extrapolate,    // 외삽: 마지막 속도로 계속 이동 추측
            Snap            // 즉시 이동: 지연 없음
        }

        private SyncMode _syncMode = SyncMode.Lerp;
        private float _moveSpeed = 5.0f;
        private Vector3 _lastVelocity = Vector3.zero;
        private Vector3 _lastPosition = Vector3.zero;
        private float _lastUpdateTime = 0f;
        private float _rttEstimate = 0.1f; // 추정된 RTT (초)

        public SyncMode Mode => _syncMode;
        public Vector3 LastVelocity => _lastVelocity;
        public Vector3 LastPosition => _lastPosition;
        public float EstimatedRTT => _rttEstimate;

        public RemoteActorSyncHelper(float moveSpeed = 5.0f, SyncMode mode = SyncMode.Lerp)
        {
            _moveSpeed = moveSpeed;
            _syncMode = mode;
            _lastUpdateTime = Time.time;
        }

        /// <summary>
        /// 동기화된 위치를 계산합니다.
        /// </summary>
        public Vector3 CalculatePosition(Vector3 currentPosition, Vector3 targetPosition, float deltaTime)
        {
            switch (_syncMode)
            {
                case SyncMode.Lerp:
                    return CalculateLerpPosition(currentPosition, targetPosition, deltaTime);
                
                case SyncMode.Extrapolate:
                    return CalculateExtrapolatePosition(currentPosition, targetPosition, deltaTime);
                
                case SyncMode.Snap:
                default:
                    return targetPosition;
            }
        }

        /// <summary>
        /// 선형 보간으로 위치를 계산합니다.
        /// </summary>
        private Vector3 CalculateLerpPosition(Vector3 currentPosition, Vector3 targetPosition, float deltaTime)
        {
            Vector3 direction = (targetPosition - currentPosition).normalized;
            float distance = Vector3.Distance(currentPosition, targetPosition);

            // 목표까지의 거리가 매우 작으면 즉시 도달
            if (distance < 0.001f)
                return targetPosition;

            float moveDistance = _moveSpeed * deltaTime;
            
            // 남은 거리보다 이동 거리가 크면 목표 위치로 설정
            if (moveDistance >= distance)
                return targetPosition;

            return currentPosition + direction * moveDistance;
        }

        /// <summary>
        /// 외삽으로 위치를 계산합니다 (마지막 속도로 계속 이동).
        /// </summary>
        private Vector3 CalculateExtrapolatePosition(Vector3 currentPosition, Vector3 targetPosition, float deltaTime)
        {
            // 목표 위치가 현재 위치와 같으면 외삽하지 않음 (정지 상태)
            if (Vector3.Distance(currentPosition, targetPosition) < 0.001f)
                return currentPosition;

            // 마지막 속도 계산 (이전 업데이트 이후의 속도)
            Vector3 displacement = targetPosition - _lastPosition;
            if (displacement.sqrMagnitude > 0.001f)
            {
                float timeSinceUpdate = Time.time - _lastUpdateTime;
                if (timeSinceUpdate > 0.001f)
                {
                    _lastVelocity = displacement / timeSinceUpdate;
                }
            }

            // 현재 위치에서 속도를 적용하여 계속 이동
            Vector3 extrapolated = currentPosition + _lastVelocity * deltaTime;
            
            // 외삽이 너무 멀어지는 것을 방지
            float maxExtrapolationDistance = Vector3.Distance(currentPosition, targetPosition) * 1.5f;
            if (Vector3.Distance(currentPosition, extrapolated) > maxExtrapolationDistance)
            {
                return currentPosition + (extrapolated - currentPosition).normalized * maxExtrapolationDistance;
            }

            return extrapolated;
        }

        /// <summary>
        /// 업데이트 신호를 받아 내부 상태를 업데이트합니다.
        /// </summary>
        public void OnPositionUpdated(Vector3 newPosition)
        {
            _lastPosition = newPosition;
            _lastUpdateTime = Time.time;
        }

        /// <summary>
        /// 동기화 모드를 변경합니다.
        /// </summary>
        public void SetSyncMode(SyncMode mode)
        {
            _syncMode = mode;
        }

        /// <summary>
        /// 이동 속도를 변경합니다.
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// 추정된 RTT를 업데이트합니다 (라그 보정용).
        /// </summary>
        public void UpdateRTTEstimate(float rtt)
        {
            _rttEstimate = Mathf.Max(0.01f, rtt);
        }

        /// <summary>
        /// 라그 보정을 고려한 예측 위치를 계산합니다.
        /// RTT 기간 동안 어느 위치에 있었을 것인지를 추정합니다.
        /// </summary>
        public Vector3 CalculateLagCompensatedPosition(Vector3 currentPosition)
        {
            // RTT 동안 이동했을 거리
            Vector3 compensatedOffset = _lastVelocity * _rttEstimate;
            return currentPosition + compensatedOffset;
        }
    }
}