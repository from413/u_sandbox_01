namespace MyGame.Runtime.Core
{
    [System.Serializable]
    public struct ServerStatePacket
    {
        public string PlayerId;
        public uint LastProcessedTick; // 서버가 마지막으로 처리한 클라이언트의 틱 번호
        public UnityEngine.Vector3 Position; // 서버가 계산한 공인 위치

        public ServerStatePacket(string id, uint tick, UnityEngine.Vector3 pos)
        {
            PlayerId = id;
            LastProcessedTick = tick;
            Position = pos;
        }
    }
}