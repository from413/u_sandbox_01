$content = @'
# u_sandbox_01

## 현재 상태 (2026-03-17)
- 클라이언트는 `GameManager` → `MyNetworkManager` 연결 시뮬레이션 후 `InGame`으로 전환.
- `InputBufferManager`가 입력을 수집하고 로컬 예측(ActorController)에 적용.
- `MinimalServer`가 입력을 받아서 권위 있는 위치를 계산하지만, 네트워크 연결(전달)이 구현되지 않은 상태.

---

## 변경 사항 (코드)
### 핵심 변경
- `MyNetworkManager`에 **Raw JSON 패킷 송수신 이벤트** 추가 (`OnRawPacketSent`, `OnServerStateReceived`) 및 관련 메서드(`SendRawPacket`, `ReceiveServerState`).
- `InputBufferManager`가 입력을 **JSON으로 직렬화**하여 `MyNetworkManager`로 전송하도록 변경.
- `InputBufferManager`가 **서버 상태 패킷 수신**을 구독하고, 로컬 플레이어 위치를 **서버 권위 위치로 보정**하도록 추가.
- `MinimalServer`가 `OnRawPacketSent`를 구독하여 JSON을 파싱하고, 기존 로직대로 처리 후 **`MyNetworkManager.ReceiveServerState` 호출**로 결과 브로드캐스트.
- `InputPacketBatch` 타입 추가: `List<InputPacket>`를 감싸서 `JsonUtility` 직렬화/역직렬화 지원.

### 주요 파일
- `Assets/Project/Scripts/Runtime/Core/MyNetworkManager.cs`
- `Assets/Project/Scripts/Runtime/Modules/InputBufferManager.cs`
- `Assets/Project/Scripts/Runtime/Server/MinimalServer.cs`
- `Assets/Project/Scripts/Runtime/Modules/InputPacketBatch.cs`

---

## Unity에서 필요한 작업
1. **씬에 필요한 매니저 오브젝트 배치**
   - `MyNetworkManager`, `GameManager`, `InputBufferManager`, `MinimalServer`, `ActorSpawner`, `CameraControl`가 한 씬에 존재해야 합니다.
   - `ActorSpawner`의 `playerPrefab` 필드에 **이동 가능한 프리팹**을 할당하세요.
2. **인풋 설정 확인**
   - `Project Settings > Input Manager`에서 `Horizontal`, `Vertical` 축이 정상 등록되어 있는지 확인.
3. **테스트**
   - 플레이 ▶ `Any Key`를 누르면 `Connecting` → `Lobby` → `InGame` 상태로 진입.
   - 플레이어 이동 입력 시 `InputBufferManager`가 로그를 남기고, 서버 시뮬레이션 로그와 위치 보정 로그가 찍혀야 합니다.
4. **추가 권장**
   - `InputBufferManager`의 `sendInterval` 값을 조정하여 틱 레이트(네트워크 전송 빈도)를 실험하세요.
   - `MinimalServer`에서 처리되는 이동 계산에 `Time.deltaTime` 대신 고정 틱(예: 1/30초)을 사용하도록 변경하면 서버 시뮬레이션이 보다 일관됩니다.

---

## 다음 단계 (추가 구현 제안)
- `ServerStatePacket`을 **다른 클라이언트에게 브로드캐스트**하여 멀티플레이어 동기화 구현.
- **클라이언트 예측 + 서버 보정**: 입력 로그를 보관하고, 서버 위치 도착 시점에 맞춰 재시뮬레이션.
- **패킷 손실 / 지연 시나리오** 시뮬레이션 (지연 추가, 패킷 드롭 등).
- 실제 네트워크 라이브러리(Mirror/Netcode/ENet 등)로 교체 가능한 **추상화 인터페이스** 추가.
'@
Set-Content -LiteralPath 'c:\u2026\u_sandbox_01\README.md' -Value $content