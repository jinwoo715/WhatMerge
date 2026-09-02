# 주기적 골드 획득 패시브 설계

## 1. 목적

영웅이 필드에 존재하는 동안 일정한 게임 시간마다 골드를 획득하는 패시브를 등급별 스킬셋에 등록한다.

```text
10초마다 골드 5 / 10 / 15 획득
```

현재 패시브 시스템의 `Tick()`과 누적 성장 규칙을 유지하며, 액티브 스킬이나 전투 Effect로 우회하지 않는다.

## 2. 현재 구조 분석

현재 구현에는 필요한 기반 일부가 이미 존재한다.

- `IPassiveSkill.Tick(float deltaTime)`이 존재한다.
- `SkillController`는 활성화된 모든 패시브의 `Tick()`을 매 프레임 호출한다.
- 패시브 Tick은 액티브 스킬 실행 여부와 관계없이 먼저 실행된다.
- 골드는 `IGameGoldService.GainMoney(int)`로 지급할 수 있다.
- `GoldEffect`와 `GoldEffectHandler`도 존재하지만 전투 Effect 실행 경로에 속한다.

현재 상태 그대로 에셋만 추가해서는 구현할 수 없다.

- `PassiveSkillData` 하위 타입에 주기적 골드 데이터가 없다.
- `SkillFactory`가 주기적 골드 패시브를 생성하지 않는다.
- `SkillRuntimeContext`가 `IGameGoldService`를 제공하지 않는다.
- `SkillSetValidator`는 알 수 없는 `PassiveSkillData` 하위 타입을 오류로 처리한다.

## 3. 구현 방향

```text
PassiveSkillData
├─ PassiveBuffSkillData
├─ PassiveDebuffSkillData
└─ PassiveGoldSkillData

PassiveSkill
├─ 기존 버프 패시브
├─ 기존 적 디버프 패시브
└─ PeriodicGoldPassive
```

`PassiveGoldSkillData`는 설정값만 보관한다. `SkillFactory`는 활성화된 골드 패시브 데이터의 증가분을 합산하고, `PeriodicGoldPassive` 하나가 타이머와 골드 지급을 담당한다.

별도의 Manager, MonoBehaviour, Coroutine, 프리팹 또는 Scene 오브젝트는 추가하지 않는다.

## 4. 데이터 설계

### 4.1 PassiveGoldSkillData

```csharp
[CreateAssetMenu(
    fileName = "PeriodicGold",
    menuName = "Skill/Passive/Periodic Gold",
    order = 0)]
public sealed class PassiveGoldSkillData : PassiveSkillData
{
    [Min(0.01f)]
    public float IntervalTime = 10f;

    [Min(1)]
    public int GoldAmount;
}
```

필드 의미:

- `IntervalTime`: 한 번 지급한 후 다음 지급까지의 게임 시간
- `GoldAmount`: 이 패시브 데이터 하나가 주기마다 추가로 지급하는 골드

`GoldAmount`는 최종 지급량이 아니라 **누적 증가분**이다.

### 4.2 5 / 10 / 15 데이터 구성

현재 스킬셋은 영웅 레벨 이하의 모든 항목을 함께 활성화한다. 패시브의 기본, `+`, `++`도 각각 별도 패시브로 누적된다.

따라서 최종 지급량을 `5 / 10 / 15`로 만들려면 다음처럼 구성한다.

| 성장 단계 | 에셋의 GoldAmount | 누적 최종 지급량 |
| --- | ---: | ---: |
| 기본 해금 | +5 | 5 |
| `+` 강화 | +5 | 10 |
| `++` 강화 | +5 | 15 |

각 성장 단계는 서로 다른 `PassiveGoldSkillData` 에셋을 사용한다.

- 동일 등급 그룹에서 동일한 `PassiveSkillData` 에셋을 중복 등록하는 기존 오류 규칙을 유지한다.
- 세 에셋의 값이 같더라도 `기본`, `+`, `++`라는 서로 다른 성장 단계의 기여분을 나타낸다.
- 같은 성장 용도의 에셋은 기존 Custom Inspector 규칙에 따라 다른 등급 그룹에서도 같은 참조를 공유한다.

`GoldAmount`를 각각 `5 / 10 / 15`로 등록하면 합산 결과가 `5 / 15 / 30`이 되므로 허용하지 않는다.

한 등급 그룹에서는 주기 골드 패시브 계열 하나만 지원한다.

- 기본, `+`, `++`에 해당하는 데이터는 최대 3개다.
- 세 데이터의 `IntervalTime`은 모두 같아야 한다.
- 서로 다른 주기의 주기 골드 패시브 여러 종류는 현재 지원하지 않는다.
- 실제 요구가 생기기 전에는 그룹 UID나 별도 강화 데이터 타입을 추가하지 않는다.

## 5. 런타임 설계

### 5.1 PeriodicGoldPassive

```csharp
public sealed class PeriodicGoldPassive : PassiveSkill
{
    private readonly IGameGoldService _goldService;
    private readonly float _intervalSeconds;
    private readonly int _goldAmount;

    private float _elapsedTime;
    private bool _isApplied;
}
```

생성자 책임:

- `IGameGoldService` null 검사
- `IntervalTime`이 유한한 양수인지 검사
- `GoldAmount`가 1 이상인지 검사
- 이벤트 구독이나 골드 지급은 하지 않음

### 5.2 Apply

```text
1. 이미 활성 상태면 아무 작업도 하지 않는다.
2. 활성 상태로 변경한다.
3. 경과 시간을 0으로 초기화한다.
```

활성화 직후에는 골드를 지급하지 않는다. 최초 지급은 온전한 `IntervalTime`이 지난 후 발생한다.

### 5.3 Tick

개념 코드는 다음과 같다.

```csharp
if (!_isApplied)
    return;

_elapsedTime += deltaTime;

while (_elapsedTime >= _intervalSeconds)
{
    _elapsedTime -= _intervalSeconds;
    _goldService.GainMoney(_goldAmount);
}
```

- 누적 시간에서 주기를 빼므로 프레임 단위 오차가 계속 누적되지 않는다.
- `while`을 사용하여 전달된 `deltaTime`이 주기보다 큰 경우에도 지급 횟수를 보존한다.
- `SkillController`의 보호 구간 안에서 실행한다.
- `GainMoney()` 또는 `OnChangeMoney` 구독자에서 예외가 발생하면 기존 `SkillController`의 FatalStop 경로로 전달한다.

### 5.4 Release

```text
1. 비활성 상태라면 아무 작업도 하지 않는다.
2. 비활성 상태로 변경한다.
3. 남은 경과 시간을 0으로 초기화한다.
4. 지급되지 않은 일부 시간에 대한 골드 보상은 하지 않는다.
```

이미 지급된 골드는 스탯 버프처럼 원복하지 않는다.

## 6. 시간 기준

타이머는 `Hero.Update()`에서 전달되는 `Time.deltaTime`을 사용한다.

- 게임 일시정지 시 `Time.timeScale == 0`이므로 타이머도 정지한다.
- 2배속과 3배속에서는 게임 시간이 빠르게 흐르므로 실제 시간 기준 지급 간격도 짧아진다.
- 액티브 스킬 애니메이션과 공격 실행 중에도 패시브 타이머는 계속 진행한다.
- FatalStop 상태에서는 `SkillController.Tick()`이 실행되지 않으므로 지급도 중단한다.

즉, `10초`는 현실 시간 10초가 아니라 **게임 시간 10초**다.

## 7. 중첩 및 생명주기

### 7.1 성장 단계 합산

`SkillFactory`는 현재 레벨에서 활성화된 기본, `+`, `++` 데이터를 수집한다. 모든 `GoldAmount`를 `checked`로 합산한 뒤 `PeriodicGoldPassive` 하나만 생성한다.

```text
기본 +5, 강화 +5, 강화++ +5
-> PeriodicGoldPassive(10초, 15골드) 하나 생성
-> 10초 시점에 GainMoney(15) 한 번 호출
```

단계마다 독립 타이머를 생성하지 않으므로 골드 UI 갱신 이벤트도 지급 주기마다 한 번만 발생한다.

### 7.2 여러 영웅 중첩

- 패시브를 보유한 영웅마다 독립적인 타이머를 가진다.
- 각 영웅은 자신이 활성화된 시점부터 10초를 계산한다.
- 서로 다른 시간에 소환된 영웅의 지급 시점은 서로 다를 수 있다.
- 여러 영웅의 지급량은 모두 누적된다.

전역 10초 타이머로 합쳐서 처리하지 않는다. 패시브의 소유권과 `SkillController.Dispose()` 정리 경로를 유지하기 위해 영웅별 런타임으로 관리한다.

### 7.3 진화와 컨트롤러 교체

진화 시 기존 `SkillController`가 Dispose되고 새 등급의 컨트롤러가 생성된다.

- 기존 패시브의 남은 시간은 계승하지 않는다.
- 새 패시브 타이머는 0초부터 다시 시작한다.
- 컨트롤러 사이에 별도 런타임 상태 전달 구조를 추가하지 않는다.

이 규칙은 구현 단순화를 위한 것이며, 진화 직전까지 쌓인 일부 시간이 사라질 수 있다는 단점이 있다. 현재 설계에서는 이를 허용한다.

### 7.4 판매, 합성, Scene 종료

- 영웅 판매 및 합성 재료 제거: 컨트롤러 Dispose 시 타이머 제거
- 다른 UID 영웅으로 합성: 재료 영웅의 타이머를 계승하지 않음
- Scene 종료: 모든 영웅 컨트롤러 Dispose 경로로 정리
- 일부 경과 시간에 대한 정산 없음

## 8. SkillRuntimeContext 변경

`SkillFactory`가 골드 패시브를 생성할 수 있도록 게임 골드 서비스를 추가한다.

```csharp
public IGameGoldService Gold { get; }
```

생성자에도 `IGameGoldService goldService`를 추가하고 null을 허용하지 않는다.

`GameSceneBootStrapper`에서는 이미 보유한 `_economy`를 전달한다.

```text
GameSceneBootStrapper._economy
-> SkillRuntimeContext.Gold
-> SkillFactory
-> PeriodicGoldPassive
```

`SkillRuntimeContext` 생성 시점에 `_economy.Init()`이 완료되지 않았더라도 같은 객체 참조를 전달하므로 문제없다. 실제 골드 지급은 Scene 초기화가 끝나고 영웅의 Tick이 실행된 이후에 발생한다.

## 9. SkillFactory 변경

`CreateSkill()`은 `PassiveGoldSkillData`를 즉시 런타임으로 만들지 않고 별도 목록에 수집한다. 다른 `PassiveSkillData`는 기존처럼 즉시 생성한다.

```csharp
case PassiveGoldSkillData gold:
    goldPassiveData.Add(gold);
    break;

case PassiveSkillData passive:
    result.PassiveSkills.Add(CreatePassiveSkill(passive, owner));
    break;
```

전체 스킬 데이터를 순회한 뒤 private 메서드에서 다음 작업을 수행한다.

```text
1. 수집된 데이터가 없으면 종료
2. 데이터가 3개 이하인지 확인
3. 모든 IntervalTime이 같은지 확인
4. GoldAmount를 checked로 합산
5. PeriodicGoldPassive 하나 생성
6. PassiveSkills에 추가
```

합산 로직은 `SkillFactory`의 런타임 생성 책임에 포함한다. 현재 규모에서는 별도의 `GoldPassiveFactory`나 Builder를 만들지 않는다. 합산형 패시브가 추가되거나 패시브 그룹 식별이 필요해질 때 `PassiveSkillFactory` 분리를 검토한다.

런타임 생성자가 합산 결과를 다시 검증하므로 초기 전체 검증이 우회되더라도 잘못된 값으로 게임을 계속 진행하지 않는다.

## 10. SkillSetContainer 검증

`SkillSetValidator.ValidatePassive()`에 `PassiveGoldSkillData` 분기를 추가한다.

검증 조건:

- `IntervalTime`은 `NaN` 또는 무한대가 아닌 양수여야 한다.
- `GoldAmount`는 1 이상이어야 한다.
- Level 0에는 등록할 수 없다.
- 동일 등급 그룹에서 동일한 에셋 참조를 두 번 등록할 수 없다.
- 다른 패시브와 마찬가지로 영웅 최대 레벨을 벗어난 슬롯은 허용하지 않는다.
- 동일 등급 그룹의 골드 패시브 데이터는 최대 3개다.
- 동일 등급 그룹의 모든 골드 패시브 데이터는 같은 `IntervalTime`을 사용해야 한다.
- 합산 `GoldAmount`는 `int` 범위를 초과할 수 없다.

검증 실패는 게임 Scene 초기 스킬셋 전체 검증에서 예외를 던지고 진행을 중단한다.

## 11. Custom Inspector 영향

`SkillSetContainerEditor`의 패시브 성장 슬롯은 이미 모든 `PassiveSkillData` 하위 타입을 허용한다.

따라서 슬롯 등록 기능의 구조 변경은 필요 없다.

- 새 ScriptableObject 에셋을 패시브 성장 슬롯에 등록할 수 있다.
- 등급 간 같은 용도 자동 참조 연결도 기존 방식으로 동작한다.
- 상세 데이터 오류는 확장된 `SkillSetValidator` 결과로 표시한다.

전용 Custom Editor는 추가하지 않는다. 필드가 두 개뿐이므로 기본 Inspector로 충분하다.

## 12. GoldEffect를 재사용하지 않는 이유

`GoldEffect`는 액티브 스킬의 Effect Graph와 `EffectProcessor`를 통해 실행되는 전투 Effect다.

주기적 패시브에서 이를 재사용하면 다음 불필요한 의존성이 생긴다.

- 전투 대상 및 `DamageContext`
- Effect Graph와 실행 노드
- 액티브 스킬 실행 흐름
- Effect 확률 및 강화 처리

주기적 골드 지급은 대상 없는 경제 동작이므로 `PeriodicGoldPassive`가 `IGameGoldService.GainMoney()`를 직접 호출한다.

기존 `GoldEffect`, `GoldEffectHandler`, `EffectProcessor`는 수정하지 않는다.

## 13. 액티브 스킬 우회 방식을 사용하지 않는 이유

현재 Trigger는 `None`, `Mana`, `HitCount`만 지원하며 시간 간격 Trigger가 없다.

시간 Trigger를 새로 추가하더라도 액티브 스킬로 만들면 다음 문제가 생긴다.

- 기본 공격 및 다른 스킬과 우선순위 경쟁을 한다.
- 공격 주기 기반 선택 시점에 종속된다.
- 불필요한 대상 탐색과 실행 노드가 필요하다.
- 영웅 애니메이션 실행 상태에 영향을 줄 수 있다.

따라서 시간 Trigger 기반 액티브 스킬은 이 기능에 적합하지 않다.

## 14. 실패 정책

- 데이터 생성 실패: 예외를 던지고 게임 진행 중단
- 런타임 생성 실패: 스킬 생성 실패로 처리하고 게임 진행 중단
- Tick 또는 골드 지급 실패: `SkillController` FatalStop 후 예외 재전파
- Release: 지급 작업이 없으므로 활성 상태와 타이머만 초기화

골드 최대치와 `int` 오버플로 정책은 패시브가 아니라 `IGameGoldService`의 책임이다. 현재 `GameEconomySystem`에는 별도 상한과 오버플로 보호가 없으므로, 경제 시스템 전체의 최대 골드 정책을 정할 때 별도로 보강한다.

## 15. 구현 범위

신규 파일:

- `PassiveGoldSkillData.cs`
- `PeriodicGoldPassive.cs`

수정 파일:

- `SkillRuntimeContext.cs`
- `GameSceneBootStrapper.cs`
- `SkillFactory.cs`
- `SkillSetContainer.cs`

구조 변경이 필요 없는 파일:

- `SkillController.cs`
- `SkillInterfaces.cs`
- `SkillSetContainerEditor.cs`
- `GoldEffect.cs`
- `GameEconomySystem.cs`

## 16. 검증 시나리오

1. 패시브 활성화 직후 골드가 지급되지 않는다.
2. 게임 시간 9.9초에는 지급되지 않고 10초가 되면 정확히 한 번 지급된다.
3. 큰 `deltaTime`이 전달되어 여러 주기가 지나면 해당 횟수만큼 지급된다.
4. 일시정지 중에는 타이머와 지급이 멈춘다.
5. 2배속과 3배속에서 게임 시간 기준으로 지급된다.
6. 공격 및 스킬 애니메이션 중에도 타이머가 진행된다.
7. 기본/`+`/`++`가 각각 `+5`일 때 최종 지급량이 `5 / 10 / 15`가 된다.
8. `++` 단계에서 10초마다 `GainMoney(15)`와 `OnChangeMoney`가 각각 한 번 호출된다.
9. 같은 패시브 영웅이 여러 명이면 영웅별 지급량이 모두 누적된다.
10. 서로 다른 시점에 소환된 영웅은 각자의 활성화 시점부터 주기를 계산한다.
11. 진화 후 이전 타이머가 제거되고 새 컨트롤러에서 0초부터 시작한다.
12. 판매, 합성, Scene 종료 후 제거된 영웅의 골드 지급이 발생하지 않는다.
13. 잘못된 `IntervalTime` 또는 `GoldAmount`가 초기 전체 검증에서 차단된다.
14. 런타임 골드 지급 예외가 FatalStop으로 전달된다.

## 17. 구현 완료 기준

- 주기적 골드 패시브가 기존 등급별 패시브 성장 슬롯에 등록된다.
- 골드 지급량이 누적 성장 규칙에 따라 `5 / 10 / 15`로 동작한다.
- 시간 배속, 일시정지, 영웅 생명주기에 따라 타이머가 정확히 동작한다.
- 기존 버프, 디버프, 액티브 스킬 동작에 영향을 주지 않는다.
- 초기 데이터 검증과 런타임 생성 검증이 모두 적용된다.
- Editor 포함 C# 빌드와 관련 Play Mode 검증 시나리오를 통과한다.
