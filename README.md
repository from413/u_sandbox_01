# u_sandbox_01 - 실시간 멀티플레이 (2026-03-19)

---

## ✅ 최신 변경 사항 (실시간 멀티플레이 계속 개발)

- **패킷 손실 시뮬레이션 추가**: `MyNetworkLatencySimulator`에 손실율 및 드롭 통계가 도입되어 네트워크 품질 저하 상황을 테스트할 수 있습니다.
- **진단 UI 확장**: `NetworkDiagnostics`에 패킷 손실 통계가 추가되어 RTT / 보정 오류 / 패킷 통계와 함께 확인 가능합니다.
- **기존 흐름 유지**: 로컬 예측, 보정, 서버 권위 상태 브로드캐스트, 원격 플레이어 보간은 기존 구조를 그대로 유지하면서 더 안정적으로 동작합니다.

---

## 🛠 Unity에서 해야 할 작업 (최신)

1. **씬에 필요한 오브젝트 확인**
   - `MyNetworkManager`, `MinimalServer`, `InputBufferManager`, `RemoteActorManager`, `ActorSpawner`, `NetworkDiagnostics`, `MyNetworkLatencySimulator` 등이 씬에 존재해야 합니다.
   - `RemotePlayerTester`를 사용하려면 빈 GameObject를 만들고 컴포넌트를 추가하세요.

2. **프리팹/참조 연결**
   - `RemoteActorManager.remotePlayerPrefab`에 원격 플레이어 프리팹을 연결합니다.
   - `ActorSpawner.playerPrefab`에 로컬 플레이어 프리팹을 연결합니다.
   - (선택) `NetworkDiagnostics`의 `Show Diagnostics UI`를 켜면 즉시 네트워크 상태를 확인할 수 있습니다.

3. **네트워크 시뮬레이션 설정**
   - 씬의 `MyNetworkLatencySimulator`에서 `Base Latency`, `Latency Variance`를 조정합니다.
   - `Enable Packet Loss`를 활성화하고 `Packet Loss Rate`를 설정하면 패킷 드롭을 시뮬레이션할 수 있습니다.
   - `RemotePlayerTester`를 키면 봇이 입력을 서버에 전송하며 서버 큐 작동 여부를 확인할 수 있습니다.

---

## 🧪 확인 포인트 (테스트 체크리스트)

- **로그 확인**
  - `MyNetworkManager`가 연결 성공 후 `OnConnectionSuccess`를 호출하는지.
  - `MinimalServer`가 클라이언트 입력을 정상적으로 처리하고 상태 브로드캐스트 로그를 출력하는지.
  - 패킷 손실 시에도 게임이 멈추지 않고 보정/예측이 지속되는지.

- **원격 플레이어 동작 확인**
  - `RemoteActorManager`가 서버 상태를 받아 원격 플레이어를 생성/보간하는지.
  - 업데이트가 없을 때 (연결 끊김) 8초 뒤 원격 플레이어가 삭제되는지.

- **진단 UI 확인**
  - `NetworkDiagnostics` UI에서 RTT, 송/수신/손실 패킷, 보정 오류가 표시되는지.
  - `MyNetworkLatencySimulator` 설정 변경이 RTT/손실 통계에 반영되는지.

---

## 📌 다음 작업 (로드맵)

- **ACK/재전송 + Sequencing**: 손실된 입력 패킷 재전송/재정렬 로직 추가
- **틱 동기화 (Tick Sync)**: 서버/클라이언트 틱을 일치시키는 네트워크 타임 소유성 구현
- **대역폭 최적화**: 상태 차분 전송, 회전 압축, 메시지 배치 개선

---