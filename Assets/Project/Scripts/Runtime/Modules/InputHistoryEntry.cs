using UnityEngine;

namespace MyGame.Runtime.Modules
{
    /// <summary>
    /// 특정 틱에서의 입력과 그로 인한 예측 위치를 저장합니다.
    /// 서버 보정 시 히스토리를 다시 시뮬레이션하여 보정량을 계산합니다.
    /// </summary>
    [System.Serializable]
    public struct InputHistoryEntry
    {
        public uint Tick;
        public InputPacket InputPacket;
        public Vector3 PredictedPosition; // 이 입력으로 예측한 위치

        public InputHistoryEntry(uint tick, InputPacket packet, Vector3 position)
        {
            Tick = tick;
            InputPacket = packet;
            PredictedPosition = position;
        }
    }
}
