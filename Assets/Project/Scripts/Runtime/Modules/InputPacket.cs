using UnityEngine;

namespace MyGame.Runtime.Modules
{
    [System.Serializable]
    public struct InputPacket
    {
        public string PlayerId; // 서버에서 받은 고유 ID
        public uint Tick; // 입력이 발생한 프레임 번호 (서버 동기화용)
        public float Horizontal; // 좌우 입력 (-1 ~ 1)
        public float Vertical; // 상하 입력 (-1 ~ 1)
        public bool IsJump; // 점프 여부 (추가 확장 대비)

        public InputPacket(string id, uint tick, float h, float v, bool jump)
        {
            PlayerId = id;
            Tick = tick;
            Horizontal = h;
            Vertical = v;
            IsJump = jump;
        }
    }
}