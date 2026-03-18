# u_sandbox_01 - 실시간 멀티플레이 (2026-03-19)

---

## ✅ 최근 변경 사항

- **RemotePlayerTester 추가**: 서버 입력 큐(MinimalServer._incomingPackets)가 정상적으로 채워지는지 확인할 수 있는 가상 클라이언트를 구현했습니다.
  - 설정된 숫자만큼 봇을 생성하고, 주기적으로 랜덤 입력을 서버에 전송합니다.
  - 연결/끊김(Churn) 시나리오를 시뮬레이션하여 서버 측 입력 흐름을 스트레스 테스트할 수 있습니다.
- **기존 네트워크 흐름 유지**: InputBufferManager의 로컬 예측, 보정, 서버 패킷 송수신 로직은 그대로 두어 기존 플레이어 제어 흐름에 영향을 주지 않습니다.

---

## 🛠 Unity에서 필요한 작업 (최신)

### 1) 씬 구성 확인
- **MyNetworkManager**, **MinimalServer**, **RemoteActorManager**가 모두 씬에 존재해야 합니다.
- **RemotePlayerTester**를 테스트하려면 빈 GameObject를 만들고 해당 컴포넌트를 붙이세요.

### 2) 프리팹 / 참조 연결
- RemoteActorManager.remotePlayerPrefab에 원격 플레이어용 프리팹을 연결하세요.
- 로컬 플레이어 프리팹은 ActorSpawner.playerPrefab에 연결하세요.

### 3) 실행 흐름
1. 플레이 모드로 실행합니다.
2. 아무 키를 누르면 네트워크 연결 시뮬레이션이 시작되고, GameState가 InGame으로 전환됩니다.
3. 로컬 플레이어 캐릭터가 스폰되고, 서버가 주기적으로 상태를 브로드캐스트합니다.
4. RemotePlayerTester가 활성화되어 있다면 추가 봇이 입력을 서버에 전송하며 서버 큐가 채워지는지를 확인할 수 있습니다.

---

## 🧪 확인할 핵심 포인트

- **서버 입력 큐**: MinimalServer._incomingPackets가 주기적으로 증가하는지 확인 (로그/디버거).
- **로컬 예측 + 보정**: 로컬 입력이 즉시 적용되며, 서버에서 전달된 상태로 보정되는지 확인.
- **원격 플레이어 렌더링**: RemoteActorManager가 서버에서 보내는 상태를 수신해 봇을 생성/보간하는지 확인.
- **진단 UI**: NetworkDiagnostics의 Show Diagnostics UI를 켜면 RTT, 보정 오류, 패킷 통계가 표시됩니다.

---

## 📌 앞으로의 작업 (로드맵)

- **ACK/재전송**: 패킷 손실 시 재전송 로직 추가
- **틱 동기화**: 클라이언트/서버 간 틱 동기화를 구현하여 입력 시점 일관성 향상
- **대역폭 최적화**: 상태 차분 전송, 회전 압축, 패킷 배치 등

---