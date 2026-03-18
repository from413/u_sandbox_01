# u_sandbox_01 - 실시간 멀티플레이 (2026-03-19)

---

## ✅ 최신 변경 사항 (실시간 멀티플레이 계속 개발)

- **패킷 송수신을 JSON 기반으로 통합**
  - `InputPacketBatch`를 만들어 `JsonUtility` 직렬화/역직렬화 처리 지원.
  - 네트워크 이벤트를 통해 JSON 송신/수신을 처리하도록 구조 개선.

- **구조 개선: 네트워크 이벤트 중심 설계**
  - `MyNetworkManager`가 JSON 패킷 발신/수신 이벤트를 제공하여 서버/클라이언트 모듈이 느슨하게 결합됨.
  - 이 구조는 나중에 실제 네트워크 라이브러리(예: Mirror/Netcode)로 교체하기 쉽도록 설계됨.

- **진행 중인 문제**
  - **순환 참조(Cyclic Dependency)**: `RemotePlayerTester.cs`가 `MinimalServer.cs`를 직접 참조하지 못하는 문제(설계/의존성 구조 재검토 필요).

---

## 🛠 Unity에서 필요한 작업 (최신)

1. **씬 구성 확인**
   - `MyNetworkManager`, `MinimalServer`, `RemoteActorManager`, `InputBufferManager`, `ActorSpawner` 등이 씬에 존재해야 합니다.
   - `RemotePlayerTester`를 테스트하려면 빈 GameObject를 만들고 컴포넌트를 추가하세요.

2. **프리팹/참조 연결**
   - `RemoteActorManager.remotePlayerPrefab`에 원격 플레이어 프리팹을 연결합니다.
   - `ActorSpawner.playerPrefab`에 로컬 플레이어 프리팹을 연결합니다.
   - `NetworkDiagnostics`를 씬에 배치해 두면 진단 데이터 표시가 바로 활성화됩니다.

3. **실행 흐름**
   1) 플레이 모드로 실행.
   2) 아무 키 입력 시 `NetworkManager` 연결 시뮬레이션 시작.
   3) `GameState.InGame` 진입 시 로컬 플레이어 스폰 및 입력 수집/전송 루프 동작.
   4) 서버는 입력을 처리하고 `ServerStatePacket`을 브로드캐스트.

---

## 🧪 확인 포인트 (테스트 체크리스트)

- `MyNetworkManager`가 연결 성공 후 `OnConnectionSuccess`를 호출하는지.
- `MinimalServer`가 JSON 입력을 받아 파싱하고, 상태를 브로드캐스트하는지.
- 로컬 예측 + 보정 로직이 정상 작동하는지 (InputBufferManager + RemoteActorManager).
- `RemoteActorManager`가 원격 플레이어를 생성/보간하는지.

---

## 📌 다음 작업 (로드맵)

- **순환 참조 해결**: `RemotePlayerTester` ↔ `MinimalServer` 의존성 재설계 (인터페이스/이벤트 기반).
- **ACK/재전송**: 패킷 손실 시 재전송 로직 추가.
- **틱 동기화 (Tick Sync)**: 서버/클라이언트 틱 동기화 및 입력 시점 일관성 강화.
- **대역폭 최적화**: 상태 차분 전송, 회전 압축, 패킷 배치 등.
