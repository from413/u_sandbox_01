namespace MyGame.Runtime.Core
{
    [System.Serializable]
    public struct ServerStatePacket
    {
        public string PlayerId;
        public uint LastProcessedTick; // 서버가 마지막으로 처리한 클라이언트의 틱 번호
        public UnityEngine.Vector3 Position; // 서버가 계산한 공인 위치
        public UnityEngine.Quaternion Rotation; // 서버가 계산한 회전 (향후 확장)

        public ServerStatePacket(string id, uint tick, UnityEngine.Vector3 pos)
        {
            PlayerId = id;
            LastProcessedTick = tick;
            Position = pos;
            Rotation = UnityEngine.Quaternion.identity; // 기본값
        }

        public ServerStatePacket(string id, uint tick, UnityEngine.Vector3 pos, UnityEngine.Quaternion rot)
        {
            PlayerId = id;
            LastProcessedTick = tick;
            Position = pos;
            Rotation = rot;
        }
    }
}