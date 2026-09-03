# Summon 실행 대상 정책 설계

## 1. 목적

지연 실행되는 Summon이 다음 두 종류의 효과를 안전하게 지원하도록 대상 결정 규칙을 정의한다.

- Summon의 현재 위치를 기준으로 실행하는 범위 또는 무대상 효과
- Summon이 현재 추적 중인 단일 대상에게 실행하는 효과

Priest의 Varius는 `SummonApproachMove`로 선택한 Enemy를 추적한 뒤 `OnExpireExecutionSummon`에서 `DamageEffect`를 실행한다. 현재 `SummonSpawner`는 실행용 `DamageContext`를 만들 때 모든 Summon에 `WithoutTarget()`을 적용하므로 Varius의 실행 시점에는 대상이 없다.

단순히 원래 `DamageContext.Target`을 유지하면 비추적 Summon도 장기 Target 참조를 보관하게 되고, 풀에서 재사용된 전투 객체를 오래된 대상으로 오인할 수 있다. 따라서 실행 컨텍스트는 Target을 보관하지 않고 실행 시점에 유효한 대상을 다시 결합한다.

## 2. 범위 결정

1차 구현에서는 현재 실제 요구가 있는 `OnExpireExecutionSummon`에만 대상 출처를 적용한다.

```csharp
public enum SummonExecutionTargetSource
{
    SummonPosition = 0,
    TrackedTarget = 1
}
```

- `SummonPosition`: Summon의 실행 시점 위치를 사용한다.
- `TrackedTarget`: 이동 전략이 실행 시점까지 유효하게 추적 중인 대상을 사용한다.
- `OnExpireExecutionSummon`에 `TargetSource` 필드를 추가한다.
- 기본값은 `SummonPosition`으로 두어 기존 TimeBomb 자산의 동작을 유지한다.
- `OnEnterExecutionSummon`과 `SummonOnStayExecution`은 기존처럼 실제 충돌 대상을 사용한다.
- `OnTimeOnceExecutionSummon`과 `OnTickExecutionSummon`은 1차 구현에서 변경하지 않는다.

`ContactTarget`을 공통 enum에 추가하지 않는다. 접촉 대상은 `OnEnter`와 `OnStay` 자체가 이미 결정하므로 데이터로 다시 선택하게 하면 무효 조합만 증가한다.

현재 `TrackedTarget`이 필요한 자산은 Varius의 `ApproachMove + OnExpire` 조합뿐이다. `OnTimeOnce` 또는 `OnTick`에 같은 요구가 생기면 그 시점에 공통 예약 실행 데이터로 승격한다. 1차 구현에서는 사용되지 않는 공통 기반 클래스를 만들지 않는다.

## 3. 소유권 원칙

- `DamageContext`는 실행 효과의 값 스냅샷이며 장기 Target 생명주기를 소유하지 않는다.
- 실행 전략에 전달하는 기본 컨텍스트에는 항상 `WithoutTarget()`을 적용한다.
- `AttachMoveStrategy`와 `ApproachMoveStrategy`만 추적 대상 참조와 `OnActiveOff` 구독을 소유한다.
- 실행 전략은 Target 이벤트를 별도로 구독하지 않는다.
- 실행 전략은 효과를 발생시키는 순간에만 이동 전략에서 현재 Target을 조회한다.
- Target이 한 번 비활성화되면 이동 전략은 참조를 null로 만들며, 같은 객체가 풀에서 다시 활성화돼도 이전 Summon의 대상으로 사용하지 않는다.

이 원칙으로 이동 전략과 실행 전략이 같은 Target의 생명주기를 중복 관리하지 않게 한다.

## 4. Target 제공 인터페이스

추적 이동 전략에 조회 전용 기능을 추가한다.

```csharp
public interface ISummonTargetProvider
{
    bool TryGetActiveTarget(out ICombatant target);
}
```

- `AttachMoveStrategy`와 `ApproachMoveStrategy`가 구현한다.
- `NoneMoveStrategy`는 구현하지 않는다.
- 반환 조건은 내부 Target이 null이 아니고 `IsActive`인 경우다.
- 이 인터페이스는 Target 구독과 해제를 수행하지 않는다. 소유권은 계속 이동 전략에 있다.

`ISummonMoveStrategy` 자체에 Target 속성을 추가하지 않는다. Target이 필요 없는 이동 전략까지 불필요한 계약을 갖지 않도록 기능 인터페이스를 분리한다.

## 5. 런타임 실행 흐름

```text
Summon 생성
-> 원래 Target으로 MoveStrategy 생성
-> 실행 기본 DamageContext에는 WithoutTarget 적용
-> MoveStrategy가 ISummonTargetProvider인지 확인
-> ExecutionStrategy 생성
-> 기존 Update와 Move 처리 유지
-> 자연 만료 또는 OnExecute 요청
-> ExecuteAndExpire에서 현재 Transform으로 SourcePosition 갱신
-> OnExpireExecution 실행
-> TargetSource에 따라 실행 컨텍스트 생성
-> CombatService.RegisterAttack
-> Summon 만료 및 모든 구독 정리
```

`SummonItem.ExecuteAndExpire()`는 이미 `OnExpire()` 직전에 현재 Transform으로 SourcePosition을 갱신한다. 따라서 Varius와 TimeBomb을 위해 전역 `Update()` 실행 순서를 바꿀 필요가 없다. 이동형 `OnTimeOnce`, 이동형 `OnTick`, 이동형 위치 기반 효과가 실제로 추가될 때 별도 요구로 검토한다.

### SummonPosition

1. `SummonItem`이 실행 직전 현재 Transform 위치를 전달한다.
2. 실행 전략은 기본 컨텍스트에 `WithImpactPosition(currentPosition)`을 적용한다.
3. `RangeEffect`, `GoldEffect`, Target을 요구하지 않는 Move를 사용하는 `SummonSpawnEffect`처럼 직접 Target이 필요 없는 효과를 실행한다.

### TrackedTarget

1. 실행 전략이 `ISummonTargetProvider.TryGetActiveTarget()`을 호출한다.
2. 유효한 Target이 있으면 `baseContext.WithTarget(target)`을 만든다.
3. 새 컨텍스트의 `ImpactPosition`은 실행 시점 Target 위치가 된다.
4. Target이 없거나 비활성 상태면 효과 없이 정상 만료한다.

Target 부재는 전투 중 정상적으로 발생할 수 있으므로 치명적 예외로 처리하지 않는다. 데이터 조합 자체가 잘못된 경우만 초기 검증에서 차단한다.

## 6. Target 상실 정책

| LostTargetEvent | TargetSource | 처리 |
|---|---|---|
| `Disappear` | `TrackedTarget` | 효과 없이 즉시 만료 |
| `Disappear` | `SummonPosition` | 효과 없이 즉시 만료 |
| `OnExecute` | `SummonPosition` | 마지막 Summon 위치에서 효과 실행 후 만료 |
| `OnExecute` | `TrackedTarget` | 허용하지 않음 |

현재 이동 전략은 Target 참조를 해제한 뒤 `OnTargetLost`를 발생시킨다. 따라서 `OnExecute + TrackedTarget`에는 실행할 Target이 없으며, 이 조합을 지원하려고 비활성 Target을 별도로 보관하지 않는다.

## 7. 데이터 검증

정적 데이터 검증은 `SkillSetValidator.WalkEffect()`가 `SummonSpawnEffect`를 방문하는 시점에 수행한다. `SummonSpawner`는 Effect 그래프를 다시 순회하지 않고 런타임 상태만 방어한다.

정적 데이터 검증:

- `TargetSource`가 정의된 enum 값인지 확인한다.
- `TrackedTarget`은 `ISummonTargetProvider`를 제공하는 이동 전략과만 조합할 수 있다.
- `TrackedTarget`과 `LostTargetEvent.OnExecute` 조합은 금지한다.
- `SummonPosition` 실행 목록의 최상위에는 직접 Target이 필요한 효과를 등록할 수 없다.
- `DamageEffect` 등 Target 의존 효과를 위치 기준으로 실행하려면 `RangeEffect` 내부에 배치한다. `RangeEffect` 내부 효과는 범위 검색으로 얻은 Target과 실행되므로 허용한다.
- `SummonPosition`의 최상위 `SummonSpawnEffect`는 자식 Summon의 Move가 Target을 요구하지 않을 때만 허용한다.
- `SummonPosition`의 최상위 `SummonSpawnEffect`가 `AttachMove` 또는 `ApproachMove`를 사용하면 생성 시 전달할 Target이 없으므로 초기 검증에서 차단한다.
- `OnEnter`와 `OnStay`는 예약 실행 대상 출처를 사용하지 않고 충돌한 활성 Target을 사용한다.
- 실행 데이터, 이동 데이터, 효과 목록의 null은 기존 정책대로 초기 검증에서 차단한다.

런타임 방어:

- Target 추적 Move를 생성할 때 원래 Target이 null이면 구성 오류로 처리한다.
- 생성 시 Target이 이미 비활성 상태면 기존 정책대로 Summon을 생성하지 않는다.
- `TrackedTarget`인데 생성된 Move가 `ISummonTargetProvider`를 구현하지 않으면 내부 구성 오류로 예외를 발생시키고 FatalStop한다.
- Provider가 정상적으로 존재하지만 실행 시 Target을 반환하지 못하면 정상적인 Target 상실로 처리하고 효과 없이 만료한다.
- 알 수 없는 `TargetSource` 값은 실행하지 않고 구성 오류로 처리한다.

현재 Target 없이 직접 실행할 수 있는 최상위 효과는 `RangeEffect`, `GoldEffect`, 조건을 만족하는 `SummonSpawnEffect`다. 나머지 효과는 직접 Target을 요구한다.

Target 필요 여부 판정은 Validator와 `EffectProcessor`가 공유하는 단일 판정 함수로 분리한다. 다만 `SummonSpawnEffect`의 Move 조합 검사는 Summon 데이터 검증에서 추가로 수행한다. 런타임에서 효과 타입을 보고 Target 출처를 자동 선택하지 않으며, `TargetSource` 데이터에 실행 의도를 명시한다.

## 8. 현재 자산 매핑

| 스킬 | 실행 | 대상 출처 | 효과 형태 |
|---|---|---|---|
| Priest Varius | `OnExpireExecutionSummon` | `TrackedTarget` | 단일 `DamageEffect` |
| Boomber TimeBomb | `OnExpireExecutionSummon` | `SummonPosition` | `RangeEffect` |
| MagicSwords Explosion | `OnTimeOnceExecutionSummon` | 정책 적용 대상 아님 | 기존 `RangeEffect` |
| Wizard Junakrion | `OnTimeOnceExecutionSummon` | 정책 적용 대상 아님 | 기존 `RangeEffect` |

Varius의 `LostTargetEvent`는 `Disappear`를 유지한다. 추적 대상이 먼저 사라지면 공격하지 않고 Varius도 제거된다.

## 9. 수정 예상 범위

| 영역 | 예상 복잡도 | 내용 |
|---|---|---|
| 데이터 | 낮음 | enum, `OnExpireExecutionSummon` 필드, Varius 자산 값 추가 |
| 이동 런타임 | 낮음 | Target provider 인터페이스와 두 추적 전략 구현 |
| 실행 런타임 | 중간 | `OnExpire` 실행 시점 컨텍스트 해석과 Target 부재 처리 |
| 생명주기 | 중간에서 높음 | Target 상실, 풀 재사용, 이벤트 실행 순서 검증 |
| 에디터 | 낮음 | 현재 범용 SerializedProperty 출력으로 신규 필드 자동 표시 |
| 검증 | 중간 | 현재 Summon 전용 자동화 테스트 어셈블리가 없어 컴파일, 데이터 검증, Play Mode 확인 필요 |

예상 변경 대상은 데이터 및 런타임 코드 5~7개 파일과 Varius 자산이다. 자동화 테스트 어셈블리 도입은 생산 코드의 assembly 경계 검토가 필요하므로 1차 구현에 포함하지 않는다.

## 10. 제외한 대안

### 추적 이동이면 원래 DamageContext 유지

`RequiresTarget(move) ? damageContext : damageContext.WithoutTarget()` 분기는 구현이 가장 작지만 사용하지 않는다.

- 이동 방식이 효과 대상 정책을 암묵적으로 결정한다.
- 추적형 범위 효과가 원래 Target의 과거 위치를 사용할 수 있다.
- 비활성화된 Target 참조가 실행 컨텍스트에 남는다.
- `LostTargetEvent.OnExecute`에서 이미 잃은 Target을 다시 사용할 위험이 있다.

### 모든 실행에 3종 TargetSource 제공

`SummonPosition`, `TrackedTarget`, `ContactTarget`을 모든 실행 데이터에 제공하는 방식은 1차 구현에서 제외한다.

- `OnEnter`와 `OnStay`는 이미 접촉 대상이 정해져 있다.
- `OnExpire + ContactTarget`, `OnStay + SummonPosition` 같은 무효 조합이 생긴다.
- Inspector와 Validator의 분기가 불필요하게 증가한다.

### 모든 예약 실행에 TargetSource 제공

`OnExpire`, `OnTimeOnce`, `OnTick`이 상속하는 공통 예약 실행 데이터를 바로 추가하는 방식은 1차 구현에서 제외한다.

- 현재 `TrackedTarget` 요구는 `OnExpire` 하나뿐이다.
- 사용하지 않는 `OnTimeOnce`와 `OnTick` 실패 상태까지 미리 정의해야 한다.
- 실제 요구가 생길 때 `TargetSource` 필드를 공통 기반 클래스로 승격할 수 있다.

### 전역 Update 순서 변경

`Move.Tick`과 `Execution.OnTick`의 순서를 바꾸는 작업은 제외한다.

- `OnExpire`는 `ExecuteAndExpire()`에서 현재 위치를 다시 설정한다.
- 현재 이동형 `OnTimeOnce`와 `OnTick` 자산이 없다.
- 기존 Summon 전체의 실행 시점을 바꾸는 회귀 위험이 현재 효용보다 크다.

### Varius 전용 분기

스킬 이름이나 자산을 검사하는 특수 처리는 사용하지 않는다. 같은 형태의 추적 후 단일 대상 실행 스킬이 추가될 때 재사용할 수 없다.

## 11. 테스트 항목

1. Varius가 살아 있는 추적 대상에게 만료 시 한 번만 피해를 준다.
2. Varius의 Target이 먼저 비활성화되면 피해 없이 Summon이 사라진다.
3. 비활성화된 Target 객체가 풀에서 다시 활성화돼도 기존 Varius가 공격하지 않는다.
4. TimeBomb이 기존처럼 현재 Summon 위치에서 범위 효과를 실행한다.
5. Explosion과 Junakrion의 `OnTimeOnce` 동작이 변경되지 않는다.
6. `NoneMove + TrackedTarget` 조합이 초기 검증에서 실패한다.
7. `OnExecute + TrackedTarget` 조합이 초기 검증에서 실패한다.
8. 정의되지 않은 `TargetSource` 값이 초기 검증에서 실패한다.
9. `SummonPosition`에 직접 `DamageEffect`를 등록하면 초기 검증에서 실패한다.
10. `SummonPosition`에서 Target 추적형 자식 `SummonSpawnEffect`를 직접 실행하면 초기 검증에서 실패한다.
11. `RangeEffect` 내부의 Target 의존 효과와 Target 추적형 자식 Summon은 범위 검색 대상과 정상 실행된다.
12. `OnEnter`와 `OnStay`가 기존처럼 실제 충돌 대상을 사용한다.
13. 효과 실행 중 Target이 사망해도 Summon이 중복 반환되지 않는다.
14. 정상 만료, Target 상실, 풀 반환, `OnDisable` 경로에서 Target 이벤트 구독이 모두 해제된다.

## 12. 구현 상태

1차 구현이 반영됐다.

- `OnExpireExecutionSummon`에 `SummonPosition`과 `TrackedTarget` 대상 출처를 추가했다.
- 실행용 기본 `DamageContext`는 계속 `WithoutTarget()`을 사용한다.
- `AttachMoveStrategy`와 `ApproachMoveStrategy`가 현재 활성 Target을 제공한다.
- `OnExpireExecution`은 실행 시점에만 추적 Target을 새 컨텍스트에 결합한다.
- TimeBomb은 `SummonPosition`, Varius는 `TrackedTarget`으로 명시했다.
- `SkillSetValidator`에 TargetSource, Move, Effect 조합 검증을 추가했다.
- `SummonItem.Update()`, `OnTimeOnceExecutionSummon`, `OnTickExecutionSummon`은 변경하지 않았다.
- `Assembly-CSharp.csproj` 빌드가 오류 없이 완료됐다. Unity Play Mode 동작 확인은 별도로 필요하다.
