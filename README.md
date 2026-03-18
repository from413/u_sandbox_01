# u_sandbox_01 - 실시간 멀티플레이 구현 (2026-03-18 업데이트)

---

## ✅ 최신 변경 사항 (2026-03-18 Phase 1 업데이트)

### 1) 플레이어 방향 회전 동기화 추가 ⭐ **NEW**
- **InputPacket에 회전 필드 추가**: `AimRotation (Quaternion)` - 플레이어가 바라보는 방향 저장
- **ActorController 개선**:
  - `enableRotation` 기본값을 `false` → `true`로 변경
  - 새로운 프로퍼티 추가: `CurrentRotation` - 현재 회전값 반환
  - 회전은 이동 방향에 따라 자동 업데이트 (SLERP 사용)
- **InputBufferManager 통합**:
  - 입력 수집 시 로컬 플레이어의 현재 회전을 InputPacket에 포함
  - 서버 상태 수신 시 로컬 플레이어 회전도 동기화 (`SetRotation()` 호출)
  - 재생(Replay) 시에도 회전 추적
- **MinimalServer 회전 처리**:
  - 서버는 입력의 이동 방향에 따라 `Quaternion.LookRotation()` 계산
  - 현재 움직이지 않으면 클라이언트가 보낸 회전값 사용 (조준 방향)
  - 봇도 원형 궤도상 이동 방향으로 자동 회전
- **RemoteActorManager 회전 보간**:
  - 원격 플레이어의 회전을 `Quaternion.Slerp()`로 부드럽게 보간
  - 회전 각도 차이가 0.1°보다 작으면 즉시 스냅

### 2) 동적 보간 속도 조절 (RTT 기반) ⭐ **NEW**
- **RemoteActorManager 개선**:
  - 네트워크 지연(RTT)을 감지하여 보간 속도 자동 조정
  - `enableDynamicInterpolation` 옵션 추가 (기본값: `true`)
  - `baseInterpolationSpeed` 로 기본 보간 속도 설정
  - 새로운 메서드들:
    - `GetInterpolationSpeed(playerId)` - 플레이어별 동적 속도 반환
    - `UpdateDynamicInterpolationSpeed(playerId)` - RTT 기반 속도 재계산
- **동작 원리**:
  - RTT < 50ms: 기본 속도의 80% (부드러운 보간)
  - RTT 50-100ms: 기본 속도 유지
  - RTT > 100ms: 기본 속도 × (1 + (RTT-100)/100) (빠른 따라잡기)
  - 예: RTT 150ms → 속도 1.5배 (빠르게 다음 업데이트까지 도달)
- **NetworkDiagnostics 확장**:
  - 새로운 public 메서드 추가: `GetLatestRTT()` - 최신 RTT값 반환 (밀리초)
  - RemoteActorManager가 이를 호출하여 동적 속도 계산

### 3) 기존 기능 유지 및 강화
- RemoteActorManager의 연결 끊김 처리 여전히 활성화 (8초 타임아웃)
- NetworkDiagnostics로 모든 지표 추적
- 로컬 플레이어 입력 보정(Reconciliation) 안정적으로 작동
- 입력 배칭 및 고정 틱 시뮬레이션 유지

---

## 🧩 Unity에서 필요한 작업

### ✅ 필수 작업

#### 1) ActorController 설정 (부분 변경)
- **변경 사항**: `enableRotation` 필드가 이제 기본값으로 `true`로 설정되어 있습니다
- **확인 사항**: 기존에 수동으로 씬에서 비활성화했다면, 다시 활성화하거나 프리팹을 업데이트하세요
- **검증**: 게임 플레이 중 플레이어가 이동 방향에 따라 자동으로 회전하는지 확인

#### 2) RemoteActorManager 설정 (일부 속성명 변경)
- **기존 속성 이름 변화**:
  - `interpolationSpeed` → `baseInterpolationSpeed` (기본값 유지: 5.0)
  - **씬 할당**: 진행 중이었다면 새로 재할당 필요 (또는 속성 이름만 변경)
- **새로운 옵션 추가**:
  - `enableDynamicInterpolation` (기본값: `true`) - 동적 보간 활성화 여부 (권장: 활성화)
  - `disconnectTimeout` 값 유지 (기본값: 8.0초)
- **원격 플레이어 프리팹 할당**: 여전히 `Remote Player Prefab` 에 프리팹 지정 필요

#### 3) NetworkDiagnostics 활성화 (선택 사항)
- `enableDiagnostics` 설정 확인 (기본값: `true`)
- `showDiagnosticsUI` 체크하여 진단 UI 표시 여부 결정
- 동적 보간 속도 조절이 정상 작동하려면 **반드시 활성화** 필요

#### 4) MinimalServer 설정 (자동 처리됨)
- 서버가 회전 계산을 자동으로 처리하므로 추가 설정 불필요
- `simulateBot` 활성화하면 봇도 자동으로 방향을 회전시킵니다
- `enableDetailedLogging` 활성화하면 회전 정보도 로그에 포함됩니다

### ⚙️ 선택 작업

#### 1) 씬 재구성 (권장)
새 씬에서 테스트하려면:
- 게임 매니저 (GameManager.cs)
- 로컬 플레이어 (ActorController와 InputBufferManager)
- 원격 플레이어 매니저 (RemoteActorManager - Remote Player Prefab 할당)
- 최소 서버 (MinimalServer with simulateBot enabled)
- 네트워크 매니저 (MyNetworkManager)
- 네트워크 지연 시뮬레이터 (MyNetworkLatencySimulator)
- 진단 시스템 (NetworkDiagnostics)

#### 2) 프리팹 업데이트
로컬 플레이어 프리팹:
```
✓ ActorController 컴포넌트 확인
  - moveSpeed: 5 (권장)
  - enableRotation: true (⭐ 중요)
  - rotationSpeed: 180 (도/초)
```

원격 플레이어 프리팹:
```
✓ ActorController 컴포넌트 기본값
  - moveSpeed: 5
  - enableRotation: true (필수, 회전 보간을 위해)
  - rotationSpeed: 180
```

#### 3) 진단 UI 활용 (권장)
게임플레이 중 또는 빌드 후:
```
OnGUI에 진단 정보 표시:
- RTT: 현재/평균/범위
- 패킷: 송신/수신 개수
- 보정 오류: 평균/최대
- 입력 지연: 최신값
- 네트워크 상태: Excellent/Good/Fair/Poor/VeryPoor

버튼:
[통계 출력] - 콘솔에 상세 진단 보고서 출력
[UI 토글]   - 진단 UI 표시/숨기기
```

---

## ✅ 검증 포인트 (새로운 항목 추가)

### 회전 관련
1. **로컬 플레이어 회전**: WASD 입력 시 플레이어가 이동 방향으로 회전하는가?
2. **원격 플레이어 회전**: 봇 또는 다른 클라이언트가 부드럽게 회전하는가?
3. **회전 동기화**: 회전 로그에 도/초 값이 표시되는가?
   ```
   [Input] Tick:X - H:0.0, V:1.0, Jump:false, Rotation:0.0°
   [RemoteActor] Bot_01 업데이트 - Tick:X / 거리:X.XXX / 회전:45.0°
   ```

### 동적 보간 (RTT 기반)
1. **높은 지연 상황**: RTT > 100ms일 때 보간 속도가 증가하는가?
   ```
   [RemoteActor] Bot_01 보간 속도 조정: 5.00 → 7.50 (RTT:150.0ms)
   ```
2. **낮은 지연 상황**: RTT < 50ms일 때 속도가 감소하는가?
3. **일반 상황**: RTT 50-100ms 범위에서 기본 속도 유지하는가?

### 기존 기능 (유지 확인)
1. **원격 플레이어 자동 제거**: 8초 동안 업데이트 없으면 제거되는가?
2. **Diagnostics UI**: 진단 정보가 올바르게 표시되는가?
3. **로컬 플레이어 보정**: 위치 오류가 보정되는가?

---

## 🚀 다음 구현 단계 (우선순위)

### Phase 1: 원격 플레이어 개선 (진행 중 ✓)
- ✅ 선형 보간 추가 (완료)
- ✅ RemoteActorSyncHelper (선택) - 외삽, 라그 보정 (완료)
- ✅ **동적 보간 속도 조절** (방금 구현 ⭐)
- ✅ **애니메이션 동기화**: 방향 회전 애니메이션 (방금 구현 ⭐)
- ⏳ 애니메이션 상태 동기화 (점프, 공격 등 - 향후)

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

## 📊 제어 흐름 다이어그램 (갱신됨)

```
┌─────────────────────────────────────────────────────────────────┐
│ CLIENT SIDE (Updated with Rotation)                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Update()                                                         │
│  ├→ CollectInput()                                              │
│  │   ├→ Read keyboard input (WASD)                              │
│  │   ├→ Create InputPacket (Tick++)                             │
│  │   ├→ Capture CurrentRotation from LocalActor ⭐              │
│  │   ├→ Enqueue to _inputBuffer                                 │
│  │   └→ ApplyInput() to _localActor IMMEDIATELY                 │
│  │       ├→ Movement applied (Translate)                        │
│  │       ├→ Rotation applied (LookRotation → RotateTowards)    │
│  │       └→ Store in _inputHistory for replay                  │
│  │                                                              │
│  └→ Apply Correction Smoothing                                  │
│     └→ SmoothDamp towards _targetPosition                       │
│                                                                  │
│ NetworkSendLoop() [Every 0.033s]                                │
│  ├→ Dequeue all packets from _inputBuffer                       │
│  ├→ Convert to JSON batch (include AimRotation) ⭐              │
│  └→ SendRawPacket(json)                                         │
│      └→ Latency Simulator adds random delay                     │
│         └→ Server receives after ~50ms                          │
│                                                                  │
│ OnServerStateReceived event                                      │
│  ├─→ Local player:                                              │
│  │    ├→ HandleServerState (InputBufferManager)                │
│  │    ├→ Measure position error                                 │
│  │    ├→ Correct to server position + rotation ⭐              │
│  │    ├→ Apply server rotation ⭐                               │
│  │    └→ REPLAY unprocessed inputs (with rotation tracking) ⭐  │
│  │                                                              │
│  └─→ Remote players:                                            │
│       ├→ HandleServerState (RemoteActorManager)                │
│       ├→ Create GameObject if first time (with rotation) ⭐    │
│       ├→ Calculate dynamic interpolation speed (RTT-based) ⭐  │
│       ├→ Update position target for Lerp interpolation          │
│       └→ Update rotation target for Slerp interpolation ⭐     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ SERVER SIDE (Updated with Rotation)                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ ServerTickLoop() [Every 0.033s]                                 │
│  ├→ _serverTick++                                               │
│  ├→ ProcessTick()                                               │
│  │  ├→ Dequeue all InputPackets from _incomingPackets          │
│  │  ├→ For each packet:                                         │
│  │  │   ├→ Verify Tick > _lastProcessedTicks (anti-duplicate) │
│  │  │   ├→ Calculate moveDir from Horizontal/Vertical input    │
│  │  │   ├→ Update _playerPositions[PlayerId]                  │
│  │  │   ├→ Calculate rotation from moveDir ⭐                  │
│  │  │   │   └→ If movement: Quaternion.LookRotation(moveDir)  │
│  │  │   │   └→ If no movement: Use packet.AimRotation ⭐      │
│  │  │   └→ Update _playerRotations[PlayerId] ⭐               │
│  │  │                                                          │
│  │  ├→ UpdateBot() [if simulateBot enabled]                   │
│  │  │   ├→ Move around circle for testing                      │
│  │  │   ├→ Calculate bot rotation from direction ⭐            │
│  │  │   └→ Update _playerRotations[_botId] ⭐                 │
│  │  │                                                          │
│  │  └→ BroadcastState() → all clients                          │
│  │      └→ Send ServerStatePacket with:                        │
│  │          ├→ Authoritative position                          │
│  │          └→ Authoritative rotation ⭐                       │
│  │                                                             │
│  └→ Log status every 30 ticks (~1 second)                      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ REMOTE PLAYER UPDATE (New Dynamic Interpolation)                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ RemoteActorManager.Update() → Each frame                        │
│  For each remote player:                                        │
│    1. Get current interpolation speed (RTT-based) ⭐            │
│       if RTT < 50ms: speed × 0.8 (smooth)                      │
│       else if 50ms ≤ RTT ≤ 100ms: base speed                  │
│       else (RTT > 100ms): speed × (1 + (RTT-100)/100) (fast)  │
│                                                                  │
│    2. Position interpolation (Lerp)                             │
│       newPos = Lerp(currentPos, targetPos,                      │
│                     Time.deltaTime × interpolationSpeed)        │
│                                                                  │
│    3. Rotation interpolation (Slerp) ⭐                        │
│       newRot = Slerp(currentRot, targetRot,                    │
│                      Time.deltaTime × interpolationSpeed)       │
│                                                                  │
│    4. Check timeout & remove if needed (8 sec)                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 코드 변경 요약

### InputPacket.cs
```csharp
// Before:
struct InputPacket { 
  string PlayerId, uint Tick, float H/V, bool IsJump 
}

// After: ✅ AimRotation 추가
struct InputPacket { 
  string PlayerId, uint Tick, float H/V, bool IsJump, 
  Quaternion AimRotation  // ⭐ 신규
}
```

### ServerStatePacket.cs
```csharp
// Already had Rotation field - now fully utilized ✅
struct ServerStatePacket { 
  string PlayerId, uint LastProcessedTick, 
  Vector3 Position, Quaternion Rotation  // ⭐ 이제 사용됨
}
```

### ActorController.cs
```csharp
// Changes:
- enableRotation: false → true  // ⭐ 기본값 변경
- Added property: CurrentRotation  // ⭐ 회전값 노출
- ApplyRotation() 메서드 개선
```

### InputBufferManager.cs
```csharp
// Changes:
- CollectInput(): 현재 회전값 InputPacket에 포함 ⭐
- HandleServerState(): 서버 회전값 로컬 플레이어에 적용 ⭐
- 로그에 회전값 표시
```

### MinimalServer.cs
```csharp
// Changes:
- _playerRotations Dictionary 추가 ⭐
- ProcessInput(): 입력 방향으로 회전 계산 ⭐
- UpdateBot(): 봇 회전 조정 ⭐
- BroadcastState(): 회전값 포함하여 전송 ⭐
```

### RemoteActorManager.cs
```csharp
// Changes:
- interpolationSpeed → baseInterpolationSpeed  ✏️ 명칭 변경
- enableDynamicInterpolation 옵션 추가 ⭐
- _targetRotations Dictionary 추가 ⭐
- _dynamicInterpolationSpeeds Dictionary 추가 ⭐
- Update(): 회전 보간(Slerp) 추가 ⭐, 동적 속도 적용 ⭐
- GetInterpolationSpeed() 메서드 ⭐
- UpdateDynamicInterpolationSpeed() 메서드 (RTT 기반) ⭐
- HandleServerState(): 회전 목표 업데이트 ⭐
```

### NetworkDiagnostics.cs
```csharp
// Changes:
- GetLatestRTT() 메서드 추가 ⭐
  (RemoteActorManager의 동적 보간 속도 조정에 사용)
```

---

## ⚠️ 알려진 제한사항 및 향후 개선

### 현재 한계
- **싱글 플레이어만 테스트 가능**: 로컬 호스트 + 봇으로만 시뮬레이션
- **패킷 손실 미시뮬레이션**: 실제 네트워크의 손실 재현 불가
- **물리 상호작용 없음**: 단순 위치 이동만 구현
- **음성/영상**: 음성 채팅, 비디오 스트리밍 미지원
- **애니메이션 상태**: 점프, 공격 등 상태 애니메이션 미동기화 (향후)

### 개선 예정
- [ ] 다중 클라이언트 지원 (실제 네트워크)
- [ ] 패킷 손실률 설정 옵션
- [ ] 리플레이 시스템
- [ ] 클라이언트 측 앞보기 (Client-side Prediction 강화)
- [ ] 서버 렉 보정 (Server-side Lag Compensation)
- [ ] 애니메이션 상태 동기화 (공격, 점프 등)
- [ ] 이동/회전 외 추가 상태값 (체력, 상태이상 등)

---

## 📝 커밋 메시지 예시

```
feat: Phase 1 - 플레이어 방향 회전 동기화 및 동적 보간 속도 조절

- 회전 필드를 InputPacket/ServerStatePacket에 추가
- ActorController의 회전 자동화 (enableRotation 기본값 true)
- InputBufferManager에서 플레이어 회전 수집 및 서버 동기화
- MinimalServer에서 회전값 계산 및 브로드캐스트
- RemoteActorManager에서 회전 보간(Slerp) 구현
- RTT 기반 동적 보간 속도 조절 로직 추가
  * RTT < 50ms: 속도 80% (부드러움)
  * RTT 50-100ms: 기본 속도
  * RTT > 100ms: 속도 증가 (빠른 따라잡기)
- NetworkDiagnostics.GetLatestRTT() 메서드 추가

이러한 개선으로 원격 플레이어의 시각적 매끄러움이 향상되고,
네트워크 지연에 따라 자동으로 최적화된 보간 속도가 적용됩니다.
```
