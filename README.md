# u_sandbox_01 - 실시간 멀티플레이 진행 노트

---

## ✅ 지금 구현된 것 (2026-03-17)

### 클라이언트 예측 + 서버 보정 (Reconciliation)
- 로컬 입력은 즉시 적용되어 부드러운 조작감을 유지합니다.
- 서버에서 받은 공인 위치가 도착하면 **그 틱 이후 입력을 재생**하여 로컬 상태를 보정합니다.
- 보정된 최종 위치는 SmoothDamp로 부드럽게 이동하도록 처리합니다.

### 로컬 서버 시뮬레이션 (고정 틱)
- `MinimalServer`가 고정 틱(기본 30Hz)으로 입력을 처리합니다.
- 클라이언트 입력은 서버 내부 큐에 쌓였다가 틱마다 처리됩니다.
- 처리된 상태는 다시 클라이언트로 브로드캐스트되어 동기화됩니다.

### 네트워크 지연 시뮬레이션
- `MyNetworkLatencySimulator`가 **지연 + 변동(±)** 을 모사하여 네트워크 환경을 재현합니다.

---

## 🔧 코드 변경 / 추가된 핵심 요소
- `Assets/Project/Scripts/Runtime/Modules/InputBufferManager.cs` (예측 + 보정 재생)
- `Assets/Project/Scripts/Runtime/Server/MinimalServer.cs` (틱 기반 입력 큐 + 처리)
- `Assets/Project/Scripts/Runtime/Modules/ActorController.cs` (deltaTime 오버로드로 재생 일관성 확보)
- `Assets/Project/Scripts/Runtime/Core/MyNetworkManager.cs` (로컬 네트워크 허브)
- `Assets/Project/Scripts/Runtime/Core/ServerStatePacket.cs` (공인 패킷, 회전 포함)
- `Assets/Project/Scripts/Runtime/Core/MyNetworkLatencySimulator.cs` (지연 시뮬레이터)

---

## 🧩 Unity에서 필요한 작업

### 1) 씬 구성
- 아래 컴포넌트를 포함한 GameObject가 씬에 존재해야 합니다:
  - `MyNetworkManager` (MonoBehaviour)
  - `GameManager` (MonoBehaviour)
  - `InputBufferManager` (MonoBehaviour)
  - `MinimalServer` (MonoBehaviour)
  - `ActorSpawner` (MonoBehaviour)
  - `CameraControl` (MonoBehaviour)
  - `RemoteActorManager` (MonoBehaviour)
  - `MyNetworkLatencySimulator` (MonoBehaviour)

### 2) 프리팹/인스펙터 설정
- `ActorSpawner.playerPrefab`: 로컬 플레이어 프리팹 (ActorController 포함)
- `RemoteActorManager.remotePlayerPrefab`: 원격 플레이어 프리팹 (ActorController 포함)

### 3) 주요 설정값 확인
- `NetworkLatencySimulator`: `baseLatency`, `latencyVariance` (네트워크 지연 모사)
- `InputBufferManager`:
  - `sendInterval` (틱 레이트, 기본 0.033s)
  - `moveSpeed` (로컬 예측 이동 속도)
  - `correctionSmoothTime` (보정 부드러움)
- `MinimalServer`:
  - `serverTickInterval` (서버 틱 속도)
  - `serverMoveSpeed` (서버 계산 이동 속도)

### 4) 입력 설정 확인
- `Project Settings > Input Manager`에서 `Horizontal`, `Vertical` 축이 정상 등록되어 있는지 확인

---

## 🚀 다음에 작업할 것 (확장 방향)
- 실제 네트워크 라이브러리(Mirror / Netcode / Photon 등)로 대체
- 다중 클라이언트 동기화 (서버가 다수 클라이언트 상태를 브로드캐스트)
- 원격 플레이어 보간/예측 (Interpolation / Extrapolation)
- 손실/순서 변경 시나리오를 위한 ACK/재전송/시계 동기화
