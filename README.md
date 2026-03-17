# u_sandbox_01

## 현재 상태 (2026-03-17 - 고급 기능 추가)
- 로컬 클라이언트가 `GameManager` → `MyNetworkManager`로 연결을 시뮬레이션하고 `InGame`으로 진입합니다.
- `InputBufferManager`가 입력을 수집하고 **입력 히스토리**를 저장하며, 로컬 예측을 위해 `ActorController`에 즉시 적용합니다.
- 네트워크 시뮬레이션:
  - `InputBufferManager`는 입력을 `InputPacketBatch`로 묶어 **JSON 직렬화** 후 `MyNetworkManager`로 전송합니다.
  - `NetworkLatencySimulator`가 패킷 지연을 시뮬레이션합니다 (기본: 100ms ± 20ms).
  - `MinimalServer`는 JSON을 받아서 `InputPacket`을 복원하고, 서버 권위 위치를 계산합니다.
  - `MyNetworkManager`가 `ServerStatePacket`을 클라이언트에 전달합니다.
- **부드러운 위치 보정**: `InputBufferManager`는 서버 위치를 **SmoothDamp**로 부드럽게 보정합니다 (지연 없이 튀는 현상 방지).
- 원격 플레이어: `RemoteActorManager`가 다른 플레이어의 상태를 받아 원격 캐릭터를 생성하고 위치를 업데이트합니다.

---

## 변경 사항 (코드)
### 새로 추가된 기능
- **NetworkLatencySimulator**: 패킷 지연을 시뮬레이션합니다.
  - `baseLatency`: 기본 지연 시간 (기본값: 0.1초 = 100ms)
  - `latencyVariance`: 지연 변동폭 (기본값: 0.02초 = ±20ms)
  - 지연된 콜백을 큐에 저장했다가 시간이 되면 수행합니다.
  
- **InputHistoryEntry**: 각 틱별 입력과 예측 위치를 저장합니다.
  - 서버 보정 시 클라이언트 예측 오차를 추적하는 데 사용됩니다.

- **개선된 InputBufferManager**:
  - 입력 히스토리 관리 (`_inputHistory`)
  - SmoothDamp 기반 부드러운 위치 보정
  - 서버 위치 오차 로깅 (디버깅)
  - `correctionSmoothTime` 파라미터로 보정 속도 조절 가능

- **개선된 ServerStatePacket**: 회전(Quaternion) 필드 추가 (향후 회전 동기화 확장 준비)

### 주요 파일
- `Assets/Project/Scripts/Runtime/Core/NetworkLatencySimulator.cs` (新)
- `Assets/Project/Scripts/Runtime/Core/MyNetworkManager.cs` (수정)
- `Assets/Project/Scripts/Runtime/Core/ServerStatePacket.cs` (수정)
- `Assets/Project/Scripts/Runtime/Modules/InputBufferManager.cs` (수정)
- `Assets/Project/Scripts/Runtime/Modules/InputHistoryEntry.cs` (新)

---

## Unity에서 필요한 작업
1. **씬 구성**
   - 한 씬에 아래 컴포넌트를 가진 GameObject들이 있어야 합니다:
     - `MyNetworkManager` (MonoBehaviour)
     - `GameManager` (MonoBehaviour)
     - `InputBufferManager` (MonoBehaviour)
     - `MinimalServer` (MonoBehaviour)
     - `ActorSpawner` (MonoBehaviour)
     - `CameraControl` (MonoBehaviour)
     - `RemoteActorManager` (MonoBehaviour)
     - **`NetworkLatencySimulator` (MonoBehaviour)** ← 새로 추가
     
2. **프리팹 설정**
   - `ActorSpawner.playerPrefab`: **로컬 플레이어 프리팹**
   - `RemoteActorManager.remotePlayerPrefab`: **원격 플레이어 프리팹**
   
3. **NetworkLatencySimulator 설정** (선택)
   - 계층구조에 빈 GameObject 추가하고 `NetworkLatencySimulator` 스크립트 부착
   - 인스펙터에서 `baseLatency`, `latencyVariance` 조정
   - 기본값 (100ms + ±20ms 변동)으로 현실적인 네트워크 지연 시뮬레이션
   
4. **InputBufferManager 설정** (선택)
   - `sendInterval`: 틱 레이트 (기본: 0.033초 = 30FPS)
   - `moveSpeed`: 로컬 예측 이동 속도
   - `correctionSmoothTime`: 서버 위치 보정 부드러움 (기본: 0.2초)
   
5. **입력 확인**
   - `Project Settings > Input Manager`에서 `Horizontal`, `Vertical` 축 정상 등록

6. **실행/테스트**
   - 플레이 ▶ AnyKey 누르면 `Connecting` → `Lobby` → `InGame` 진입
   - 플레이어 이동 시:
     - 콘솔에 `[Tick:X] 입력 감지` 로그 출력
     - `[Network] Raw packet sent` 로그 (지연 후 처리)
     - `[Server] ID:... Tick:... AuthPos:...` 로그
     - `[Correction] Tick:... / Predicted:... / Server:... / Error:...` 로그로 보정 추적

---

## 다음 단계 제안
- **클라이언트 예측 재시뮬레이션**: 입력 히스토리를 활용해 서버 보정 시 재시뮬레이션 로직 추가
- **패킷 드롭 시뮬레이션**: 일부 패킷을 의도적으로 손실시켜 손실 복구 로직 테스트
- **플레이어 애니메이션 동기화**: 이동 상태(Idle/Walk/Run)를 ServerStatePacket에 추가
- **다중 클라이언트 테스트 환경**: 로컬에서 여러 프로세스로 클라이언트 띄워 동시 테스트
- **INetworkTransport 인터페이스**: 실제 네트워크 라이브러리(Mirror/Netcode) 교체 준비