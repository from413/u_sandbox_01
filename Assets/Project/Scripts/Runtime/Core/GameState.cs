namespace MyGame.Runtime.Core
{
    public enum GameState
    {
        Intro,      // Title 화면, 서버 연결 대기
        Connecting, // 핸드셰이크 진행 중
        Lobby,      // 연결 완료, 매칭 대기 (필요 시)
        InGame,     // 실제 플레이 (Play)
        Outro       // 결과 화면 및 종료
    }
}