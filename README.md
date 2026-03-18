# u_sandbox_01 - 실시간 멀티플레이 구현 (2026-03-18 업데이트)

---

## ✅ 현재 구현된 핵심 기능

### 클라이언트 예측 + 서버 보정 (Reconciliation)
- 로컬 입력은 즉시 적용되어 부드러운 조작감 유지
- 서버 공인 위치 도착 시 **그 틱 이후 입력 재생**으로 보정
- SmoothDamp를 통해 보정 이동을 부드럽게 처리

### 고정 틱 서버 시뮬레이션
- MinimalServer: 30Hz 고정 틱 입력 처리
- 클라이언트 입력 큐 기반 처리 및 브로드캐스트
- 각 틱마다 모든 플레이어의 공인 상태 생성
- **입력 검증**: 중복 틱 무시, 플레이어별 마지막 처리 틱 추적

### 네트워크 지연 시뮬레이션
- 기본 지연(baseLatency) + 변동(latencyVariance) 모사
- 실제 네트워크 환경 테스트 가능
- RTT 통계 추적 및 모니터링

### 원격 플레이어 관리 + 보간
- RemoteActorManager: 서버 상태 패킷 수신 시 원격 플레이어 생성/갱신
- **선형 보간(Linear Interpolation)**: 서버 상태 업데이트 사이의 부드러운 이동
- 목표 위치 추적으로 자연스러운 동작

---

## 🔄 변경 사항 상세 (2026-03-18)

### 1️⃣ RemoteActorManager 개선
**파일**: [Assets/Project/Scripts/Runtime/Modules/RemoteActorManager.cs](Assets/Project/Scripts/Runtime/Modules/RemoteActorManager.cs)

**항목**:
- ✅ 선형 보간(Linear Interpolation) 추가
  - `interpolationSpeed` 파라미터로 부드러움 조절
  - Update에서 매 프레임 목표 위치로 이동
- ✅ 목표 위치 추적 (`_targetPositions` Dictionary)
- ✅ 마지막 위치 저장 (`_lastPositions`)
- ✅ 틱 기반 업데이트 감지

**설정값**:
```
Remote Player Prefab: 원격 플레이어 프리팹
Interpolation Speed: 5.0 (조절 가능)
```

---

### 2️⃣ InputBufferManager 향상
**파일**: [Assets/Project/Scripts/Runtime/Modules/InputBufferManager.cs](Assets/Project/Scripts/Runtime/Modules/InputBufferManager.cs)

**항목**:
- ✅ 향상된 보정 로깅
  - 보정 전후 오류 비교 (`Error:A→B`)
  - 오류 감소율 계산 (%)
  - 재생된 입력 개수 표시
- ✅ 성능 통계 수집
  - 총 보정 횟수
  - 최대 오류값
  - 평균 오류값
- ✅ `PrintStatistics()` 메서드 추가

**로그 예시**:
```
[Input] Tick:123 - H:1.0, V:0.0, Jump:false
[Network] 서버로 1개 패킷 전송 (최신 Tick:123 / 히스토리:5)
[Correction] Tick:123 / Error:0.523m→0.045m (91.4%) / Replayed:2 inputs
⚠️ 큰 보정 오류 감지: 1.234m (서버 응답 지연 또는 네트워크 문제 가능)
```

---

### 3️⃣ ActorController 확장
**파일**: [Assets/Project/Scripts/Runtime/Modules/ActorController.cs](Assets/Project/Scripts/Runtime/Modules/ActorController.cs)

**항목**:
- ✅ 회전 처리 지원 (`enableRotation` 플래그)
- ✅ 회전 속도 설정 (`rotationSpeed`)
- ✅ `SetRotation()` 메서드 추가
- ✅ `Reset()` 메서드 추가
- ✅ 명시적 deltaTime 전달로 재생 일관성 보장
- ✅ 속도 벡터 캐싱 최적화

**사용 예**:
```csharp
actorController.SetPosition(new Vector3(5, 0, 3));
actorController.SetRotation(Quaternion.identity);
actorController.ApplyInput(packet, fixedDeltaTime);
```

---

### 4️⃣ MinimalServer 강화
**파일**: [Assets/Project/Scripts/Runtime/Server/MinimalServer.cs](Assets/Project/Scripts/Runtime/Server/MinimalServer.cs)

**항목**:
- ✅ 입력 검증 (중복 틱 무시)
- ✅ 플레이어별 마지막 처리 틱 추적
- ✅ 향상된 로깅 (`_enableDetailedLogging`)
- ✅ 성능 통계 수집
- ✅ `PrintServerStatus()` 메서드 추가
- ✅ 봇 각도 계산 개선 (각도 단위 정규화)

**로그 예시**:
```
[Server] 클라이언트로부터 3개 입력 수신 (큐 크기: 5)
[Server] Tick:30 / 플레이어:2 / 이번 틱 처리:3 inputs
중복 입력 무시: User_1234 Tick:123
```

---

### 5️⃣ MyNetworkLatencySimulator 개선
**파일**: [Assets/Project/Scripts/Runtime/Core/MyNetworkLatencySimulator.cs](Assets/Project/Scripts/Runtime/Core/MyNetworkLatencySimulator.cs)

**항목**:
- ✅ 성능 통계 추적
  - 평균 지연 (`CurrentAverageLatency`)
  - 최대/최소 지연
  - 처리된 패킷 개수
- ✅ 로깅 옵션 (`_enableLatencyLogging`)
- ✅ `PrintLatencyStatistics()` 메서드
- ✅ `SetLatency()` for 실시간 조정
- ✅ Getter 메서드들 추가

**기본값**:
```
Base Latency: 50ms (0.05s)
Latency Variance: ±10ms (0.01s)
```

---

### 6️⃣ 신규 파일: RemoteActorSyncHelper
**파일**: [Assets/Project/Scripts/Runtime/Modules/RemoteActorSyncHelper.cs](Assets/Project/Scripts/Runtime/Modules/RemoteActorSyncHelper.cs)

**용도**: 원격 플레이어의 고급 동기화 지원 (선택적)

**기능**:
- 선형 보간 (Lerp)
- 외삽 (Extrapolation): 마지막 속도로 이동 계속
- 즉시 이동 (Snap)
- 라그 보정 (Lag Compensation)

**사용 예**:
```csharp
RemoteActorSyncHelper syncHelper = new RemoteActorSyncHelper(5.0f, SyncMode.Extrapolate);
Vector3 newPos = syncHelper.CalculatePosition(currentPos, targetPos, Time.deltaTime);
syncHelper.UpdateRTTEstimate(networkRTT);
Vector3 compensatedPos = syncHelper.CalculateLagCompensatedPosition(currentPos);
```

---

### 7️⃣ 신규 파일: NetworkDiagnostics
**파일**: [Assets/Project/Scripts/Runtime/Core/NetworkDiagnostics.cs](Assets/Project/Scripts/Runtime/Core/NetworkDiagnostics.cs)

**용도**: 네트워크 성능 모니터링 및 진단 (선택적)

**기능**:
- RTT 통계 수집
- 입력 지연 추적
- 보정 오류 모니터링
- 패킷 송수신 카운팅
- 네트워크 상태 평가 (Excellent/Good/Fair/Poor/VeryPoor)
- 진단 UI (F키 또는 버튼)

**사용 예**:
```csharp
NetworkDiagnostics.Instance.RecordRTT(0.05f);
NetworkDiagnostics.Instance.RecordCorrectionError(0.1f);
var condition = NetworkDiagnostics.Instance.EvaluateNetworkCondition();
NetworkDiagnostics.Instance.PrintDiagnostics();
```

---

## 🧩 Unity 씬 설정 체크리스트

### GameObject 구조 (권장)
```
Canvas (UI)
  └─ Diagnostics Panel (선택사항)

NetworkManager
  ├─ MyNetworkManager
  ├─ MyNetworkLatencySimulator
  └─ NetworkDiagnostics (선택사항)

GameController  
  ├─ GameManager
  ├─ InputBufferManager
  └─ CameraControl

Server
  └─ MinimalServer

Players
  ├─ ActorSpawner
  └─ RemoteActorManager
```

### Inspector 설정값

**InputBufferManager:**
```
Send Interval: 0.033 (30Hz)
Move Speed: 5.0
Correction Smooth Time: 0.2
```

**RemoteActorManager:** ⭐ **신규 설정**
```
Remote Player Prefab: [AssignPrefab] ActorController 포함
Interpolation Speed: 5.0  ← 조절로 보간 속도 제어
```

**MinimalServer:**
```
Server Tick Interval: 0.033 (30Hz)
Server Move Speed: 5.0
Simulate Bot: true
Enable Detailed Logging: false (성능용)
```

**MyNetworkLatencySimulator:**
```
Base Latency: 0.05 (50ms, 시뮬레이션용)
Latency Variance: 0.01 (±10ms)
```

**ActorController:** ⭐ **신규 설정**
```
Move Speed: 5.0
Enable Rotation: false (향후 확장용)
Rotation Speed: 180 (도/초)
```

**NetworkDiagnostics (선택사항):**
```
Enable Diagnostics: true
Show Diagnostics UI: false (필요시 활성화)
```

**MyNetworkLatencySimulator:**
```
Base Latency: 0.05 (50ms)
Latency Variance: 0.01 (±10ms)
Enable Latency Logging: false
```

### Input Manager 확인
- Horizontal: ← → / A / D
- Vertical: ↑ ↓ / W / S
- Space: Jump

---

## 📊 실행 결과 로깅 및 콘솔 출력

### 정상 동작 로그 시퀀스
```
[Network Simulator] 지연 설정: 50.0ms ± 10.0ms
[Server] 서버 시작 (30Hz 고정 틱)
서버 연결 성공! 부여받은 ID: User_5678

— 플레이어 조작 —
[Input] Tick:15 - H:1.0, V:0.0, Jump:false
[Network] 서버로 1개 패킷 전송 (최신 Tick:15 / 히스토리:3)
[Server] 클라이언트로부터 1개 입력 수신 (큐 크기: 1)

— 서버 처리 —
[Server] Tick:30 / 플레이어:2 / 이번 틱 처리:1 inputs

— 클라이언트 수신 및 보정 —
[Correction] Tick:30 / Error:0.156m→0.023m (85.3%) / Replayed:1 inputs

— 원격 플레이어 —
[RemoteActor] 플레이어 생성: Bot_01 @ (3.0, 0.0, 0.0)
[RemoteActor] Bot_01 위치 업데이트 - Tick:30 / 거리:0.165 → (2.8, 0.0, -0.8)
```

### 진단 명령어 호출
**InputBufferManager 통계**:
```csharp
InputBufferManager.Instance.PrintStatistics();
// 출력:
// [Stats] 보정 횟수:45 / 평균 오류:0.087m / 최대 오류:0.523m
```

**MinimalServer 상태**:
```csharp
MinimalServer.Instance.PrintServerStatus();
// 출력:
// === 서버 상태 (Tick:2100) ===
// 활성 플레이어: 2
//   - User_1234: (5.3, 0.0, 2.1) (LastTick:120)
//   - Bot_01: (1.5, 0.0, -2.8) (LastTick:2100)
// 큐 크기: 0
// 누적 처리 입력: 540
```

**네트워크 진단**:
```csharp
NetworkDiagnostics.Instance.PrintDiagnostics();
// 출력:
// ===== 네트워크 진단 보고서 =====
// RTT: 평균 53.24ms / 범위 41.12ms ~ 68.45ms
// 패킷: 송신 120 / 수신 115
// 보정: 평균 오류 0.089m / 최대 0.523m
// 네트워크 상태: Good
```

### 성능 모니터링 Tip
| 메트릭 | 정상 범위 | 주의 | 위험 |
|--------|----------|------|------|
| RTT (지연) | < 50ms | 50-100ms | > 100ms |
| 보정 오류 | < 0.1m | 0.1-0.5m | > 0.5m |
| Replayed inputs | 1-3개 | 4-5개 | > 5개 |
| 큐 크기 | 0-1개 | 2-3개 | > 3개 |

---

## 🚀 다음 구현 단계 (우선순위)

### Phase 1: 원격 플레이어 개선 (진행 중 ✓)
- ✅ 선형 보간 추가
- ✅ RemoteActorSyncHelper (선택): 외삽, 라그 보정
- ⏳ **동적 보간 속도 조절**: 네트워크 지연에 따른 자동 조定
- ⏳ **애니메이션 동기화**: 방향 회전 애니메이션

### Phase 2: 네트워크 강화
- ⏳ ACK/재전송 메커니즘 (패킷 손실 대응)
  - 구현: 트래킹 ID + ACK 타임아웃
  - 손실률 시뮬레이션 옵션
- ⏳ 시계 동기화 (클라이언트-서버 틱 맞춤)
  - 클라이언트 틱 = 서버 틱 + RTT/2
  - 시간차 보정 (Time Warp)
- ⏳ 대역폭 최적화
  - 상태 압축 (Vector3 → Uint32)
  - 차분 전송 (변화값만 전송)
  - 쿼터니언 압축

### Phase 3: 확장성 & 실제 환경
- ⏳ 실제 네트워크 라이브러리 통합
  - Mirror Networking
  - Netcode for GameObjects
  - Photon PUN2
- ⏳ 다중 서버/영역 지원
- ⏳ 플레이어 입장/퇴장 처리
- ⏳ 게임 상태 저장/로드

---

## 📝 코드 구조 및 네임스페이스

```
Assets/Project/Scripts/Runtime/
│
├── Core/
│   ├── GameManager.cs              → 게임 상태 관리
│   ├── MyNetworkManager.cs          → 네트워크 이벤트 허브
│   ├── ServerStatePacket.cs        → 서버→클라이언트 패킷
│   ├── PlayerSession.cs             → 플레이어 세션 정보
│   ├── GameState.cs                 → 게임 상태 (Intro/InGame)
│   ├── MyNetworkLatencySimulator.cs → 지연 시뮬레이터 ⭐
│   └── NetworkDiagnostics.cs       → 진단 도구 (신규) ⭐
│
├── Modules/
│   ├── InputBufferManager.cs    → 클라이언트 입력 + 보정 ⭐ 개선됨
│   ├── RemoteActorManager.cs    → 원격 플레이어 + 보간 ⭐ 개선됨
│   ├── ActorController.cs       → 플레이어 이동 + 회전 ⭐ 확장됨
│   ├── RemoteActorSyncHelper.cs → 동기화 헬퍼 (신규) ⭐
│   ├── ActorSpawner.cs          → 로컬 플레이어 생성
│   ├── CameraControl.cs         → 카메라 제어
│   ├── InputPacket.cs           → 클라이언트→서버 패킷
│   ├── InputPacketBatch.cs      → 배치 전송
│   └── InputHistoryEntry.cs    → 입력 히스토리
│
└── Server/
    └── MinimalServer.cs         → 틱 기반 서버 시뮬레이션 ⭐ 개선됨
```

---

## 🔗 주요 메서드 및 API

### InputBufferManager
```csharp
public void RegisterLocalActor(ActorController actor)
public void PrintStatistics()
```

### RemoteActorManager
```csharp
// (자동 처리됨 - OnServerStateReceived 이벤트)
```

### ActorController
```csharp
public void ApplyInput(InputPacket packet, float deltaTime)
public void SetPosition(Vector3 position)
public void SetRotation(Quaternion rotation)      // 신규
public void Reset()                               // 신규
```

### MinimalServer
```csharp
public void ReceiveInputFromClient(List<InputPacket> packets)
public void PrintServerStatus()                   // 신규
```

### MyNetworkLatencySimulator
```csharp
public void EnqueueDelayedPacket(Action callback)
public int GetPendingPacketCount()               // 신규
public void PrintLatencyStatistics()             // 신규
public void SetLatency(float baseLatencyMs, float varianceMs)
```

### NetworkDiagnostics
```csharp
public void RecordRTT(float rtt)
public void RecordInputLatency(float latency)
public void RecordCorrectionError(float error)
public void RecordPacketSent(int count)
public NetworkCondition EvaluateNetworkCondition()
public void PrintDiagnostics()
public void ToggleDiagnosticsUI()
```

### RemoteActorSyncHelper
```csharp
public Vector3 CalculatePosition(Vector3 current, Vector3 target, float deltaTime)
public Vector3 CalculateLagCompensatedPosition(Vector3 currentPosition)
public void SetSyncMode(SyncMode mode)
public void SetMoveSpeed(float speed)
public void UpdateRTTEstimate(float rtt)
```

---

## ⚠️ 알려진 제한사항 및 향후 개선

### 현재 한계
- **싱글 플레이어만 테스트 가능**: 로컬 호스트 + 봇으로만 시뮬레이션
- **패킷 손실 미시뮬레이션**: 실제 네트워크의 손실 재현 불가
- **물리 상호작용 없음**: 단순 위치 이동만 구현
- **음성/영상**: 음성 채팅, 비디오 스트리밍 미지원

### 개선 예정
- [ ] 다중 클라이언트 지원 (실제 네트워크)
- [ ] 패킷 손실률 설정 옵션
- [ ] 리플레이 시스템
- [ ] 클라이언트 측 앞보기 (Client-side Prediction 강화)
- [ ] 서버 렉 보정 (Server-side Lag Compensation)
