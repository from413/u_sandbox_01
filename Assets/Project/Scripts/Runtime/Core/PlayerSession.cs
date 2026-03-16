namespace MyGame.Runtime.Core
{
    [System.Serializable]
    public class PlayerSession
    {
        public string PlayerId; // 서버가 부여한 고유 ID
        public string Nickname; // 플레이어 이름
        public int Ping; // 지연 시간 (선택 사항)

        public PlayerSession(string id, string name)
        {
            PlayerId = id;
            Nickname = name;
        }
    }
}