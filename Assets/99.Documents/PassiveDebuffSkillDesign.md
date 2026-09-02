# 적 스탯 감소 패시브 설계

## 1. 목적

영웅에게 적용되는 `PassiveBuffSkillData`와 별도로, 적의 스탯을 지속적으로 감소시키는 `PassiveDebuffSkillData`를 등급별 스킬 구성에 등록하고 런타임에서 안전하게 적용·해제한다.

- 패시브 디버프는 지속시간이 없는 영웅 보유 효과다.
- 대상은 전체 적 또는 영웅 주변의 적이다.
- 기본 패시브와 `+`, `++`는 기존 패시브 성장 규칙처럼 누적 적용한다.
- 영웅 진화, 합성, 판매, Scene 종료 시 기존 `SkillController.Dispose()` 경로에서 모두 해제한다.
- 적이 사망하거나 풀로 반환될 때 기존 디버프가 다음 생명주기에 남지 않아야 한다.
- 잘못된 데이터나 적용 실패는 복구 가능한 상황으로 취급하지 않고 게임 진행을 중단한다.

## 2. 기존 구조 분석

현재 영웅 패시브 버프는 `PassiveBuffSkillData`의 `BuffData`를 `IHeroStatModifier.AddMultiplier()`로 적용하고, `Release()`에서 반대 값을 더해 원복한다.

기존 `TimeEffectManager`의 슬로우와 방어력 감소는 다음 특성이 있다.

- 지속시간 효과와 영구 효과의 실제 적용 Handle을 제공한다.
- 같은 종류의 지속시간 디버프는 가장 강한 값 하나만 적용한다.
- 패시브 기본/`+`/`++`의 가산 누적 규칙과는 다르다.

따라서 패시브 디버프는 `TimeEffectManager`의 지속시간 디버프에 포함하지 않는다. 영웅 패시브 버프와 대칭인 별도 패시브 런타임으로 구성한다.

## 3. 데이터 구조

### 3.1 공통 패시브 타입

패시브 슬롯과 검증 코드가 개별 구현 타입을 반복해서 확인하지 않도록 공통 Marker 타입을 추가한다.

```text
SkillBaseData
└─ PassiveSkillData
   ├─ PassiveBuffSkillData
   └─ PassiveDebuffSkillData
```

```csharp
public abstract class PassiveSkillData : SkillBaseData
{
}
```

- 기존 `PassiveBuffSkillData` 에셋의 Script GUID는 변경하지 않는다.
- 상속 타입만 `SkillBaseData`에서 `PassiveSkillData`로 변경한다.
- `PassiveDebuffSkillData`도 `PassiveSkillData`를 상속한다.

### 3.2 적 대상 타입 제한

`FinderData`를 그대로 사용하면 영웅 대상 Finder를 패시브 디버프에 잘못 등록할 수 있다. 이를 타입 단계에서 차단하기 위해 적 대상 Marker 타입을 추가한다.

```text
FinderData
├─ HeroTargetData
└─ EnemyTargetData
   ├─ NearEnemyTargetData
   └─ AllEnemyTargetData
```

- `NearEnemyTargetData`와 `AllEnemyTargetData`의 Script GUID는 유지한다.
- 두 클래스의 상속 타입만 `EnemyTargetData`로 변경한다.

### 3.3 PassiveDebuffSkillData

```csharp
public class PassiveDebuffSkillData : PassiveSkillData
{
    [Header("탐색")]
    public EnemyTargetData Target;

    [Header("효과")]
    public List<DebuffData> Effects;
}
```

- 기존 `FindData`는 버프 패시브와 명칭을 맞춰 `Target`으로 변경한다.
- 기존 에셋이 존재한다면 `[FormerlySerializedAs("FindData")]`를 사용해 참조를 보존한다.
- 현재 파일의 깨진 한글 `Header` 문자열을 정상 문자열로 교체한다.

### 3.4 DebuffData

`DebuffData`는 `BuffData.cs`에서 분리하여 디버프 데이터 파일로 관리한다.

```csharp
[Serializable]
public class DebuffData
{
    public EnemyStatType StatType;

    [Range(0f, 1f)]
    public float ReductionRatio;
}
```

- `IncreaseRatio`는 의미가 반대이므로 `ReductionRatio`로 변경한다.
- 기존 에셋이 존재한다면 `[FormerlySerializedAs("IncreaseRatio")]`를 사용한다.
- 감소율은 양수로 저장하고 적용할 때 음수로 변환한다.
- 초기 지원 스탯은 `MoveSpeed`, `Armor`다.
- `MaxHP` 감소는 지원하지 않는다.

`MaxHP` 감소는 해제할 때 현재 체력을 함께 복구할지, 최대 체력만 복구할지 별도 규칙이 필요하다. 해당 규칙을 확정하기 전까지 데이터 검증에서 오류로 처리한다.

## 4. 런타임 구조

```text
PassiveSkill
├─ BuffPassiveSkill
└─ EnemyDebuffPassive
   ├─ AllEnemyDebuffPassive
   └─ NearEnemyDebuffPassive
```

### 4.1 패시브 갱신 계약

범위 디버프는 적이 계속 이동하므로 최초 `Apply()`만으로 대상을 확정할 수 없다. `IPassiveSkill`에 Tick을 추가한다.

```csharp
public interface IPassiveSkill
{
    void Apply();
    void Tick(float deltaTime);
    void Release();
}
```

- `PassiveSkill` 기본 구현의 `Tick()`은 아무 동작도 하지 않는다.
- 기존 자기/주변/전체 영웅 버프는 수정된 계약만 구현하고 동작은 유지한다.
- `NearEnemyDebuffPassive`만 `Tick()`에서 대상을 갱신한다.

### 4.2 대상별 적용 객체

패시브 디버프는 대상마다 적용 객체를 하나씩 보관한다.

```text
Dictionary<Enemy, DebuffApplication>
```

`DebuffApplication` 책임:

1. 대상 적과 적용된 `DebuffData` 목록을 보관한다.
2. 적용 시 `AddMultiplier(StatType, -ReductionRatio)`를 호출한다.
3. 해제 시 `AddMultiplier(StatType, +ReductionRatio)`를 호출한다.
4. 적용 도중 실패하면 이미 적용된 항목을 역순으로 원복하고 예외를 다시 던진다.
5. `Dispose()` 또는 `Release()`는 여러 번 호출되어도 한 번만 원복한다.

생성자에서는 스탯 변경이나 이벤트 구독을 하지 않는다. 실제 부작용은 패시브의 `Apply()` 이후에만 발생한다.

여러 대상에게 적용하는 `Apply()`와 범위 갱신도 하나의 적용 작업으로 취급한다.

- 대상 적용을 시작하기 전에 패시브 활성 상태와 필요한 이벤트 구독을 기록한다.
- 중간 대상에서 실패하면 앞서 적용한 모든 대상의 `DebuffApplication`을 해제한다.
- 실패 시 이벤트 구독도 함께 해제한다.
- 정리 과정의 추가 예외는 별도로 기록하고 원래 적용 예외를 다시 던진다.
- 패시브 자체의 `Apply()`와 `Release()`도 멱등성을 보장한다.

### 4.3 적 생명주기 처리

디버프가 적용된 적의 `OnActiveOff`를 구독한다.

- 사망 및 강제 Despawn 시 즉시 해당 적의 `DebuffApplication`을 해제한다.
- 이벤트 구독을 제거하고 Dictionary에서도 삭제한다.
- 적이 풀에 반환되기 전에 원래 스탯으로 복구한다.
- 동일한 `Enemy` 인스턴스가 재사용되어도 이전 생명주기의 디버프가 남지 않는다.

`FieldEnemyService.OnEnemyDeath`만으로 정리하지 않는다. 강제 Despawn도 처리해야 하므로 `ICombatant.OnActiveOff`를 기준으로 한다.

`OnSpawnEnemy`와 `OnActiveOff`는 `SkillController`의 보호된 실행 구간 밖에서 호출된다. 따라서 적 디버프 패시브는 `IFatalStopService`를 직접 전달받고, 두 이벤트 콜백에서 발생한 예외를 `FatalStop()`에 보고한 뒤 다시 던진다. 이벤트 콜백 실패 시에도 가능한 전체 패시브 정리를 먼저 시도한다.

## 5. 대상별 동작

### 5.1 전체 적 패시브

`AllEnemyDebuffPassive.Apply()`:

1. `IFieldEnemyService.OnSpawnEnemy`를 구독한다.
2. `IFieldEnemyService.GetAllFieldEnemy`의 활성 적 전체에 적용한다.
3. 이후 생성되는 적에게 즉시 적용한다.

`Release()`:

1. `OnSpawnEnemy` 구독을 해제한다.
2. 보관 중인 모든 `DebuffApplication`을 역순 또는 안전한 복사본 기준으로 해제한다.
3. 대상 Dictionary를 비운다.

전체 적 패시브는 매 프레임 대상을 검색하지 않는다.

### 5.2 범위 적 패시브

`NearEnemyDebuffPassive`는 영웅 위치와 적 위치를 기준으로 매 Tick 대상을 갱신한다.

- `IFieldEnemyService.GetAllFieldEnemy`를 순회한다.
- `Vector3.sqrMagnitude`와 `Radius * Radius`를 비교한다.
- 현재 적용 대상은 적용 객체 Dictionary로 보관하고, 다음 대상 계산에는 재사용 가능한 `HashSet<Enemy>`를 사용한다.
- 진입한 적에게 적용한다.
- 범위를 벗어난 적은 해제한다.
- 비활성 적은 대상에서 제외한다.

`Physics2D.OverlapCircleAll`은 매 호출마다 배열을 생성하므로 패시브 범위 갱신에는 사용하지 않는다. 별도의 Refresh Interval은 두지 않고 매 Tick 정확하게 갱신하되, 프로파일링에서 문제가 확인될 때만 최적화한다.

## 6. SkillFactory 변경

`SkillFactory.CreateSkill()`은 두 패시브 데이터 타입을 모두 처리한다.

```text
PassiveBuffSkillData   -> 기존 영웅 버프 패시브 생성
PassiveDebuffSkillData -> 적 디버프 패시브 생성
```

`PassiveDebuffSkillData.Target` 분기:

- `AllEnemyTargetData` -> `AllEnemyDebuffPassive`
- `NearEnemyTargetData` -> `NearEnemyDebuffPassive`
- 그 외 타입 -> `InvalidOperationException`

필요 의존성은 기존 `SkillRuntimeContext`의 `FieldEnemy`를 사용한다. 별도 Manager나 Scene 컴포넌트는 추가하지 않는다.

패시브 생성자는 이벤트를 연결하거나 스탯을 변경하지 않아야 한다. 따라서 `SkillFactory` 생성 과정이 실패해도 활성화되지 않은 패시브를 별도로 해제할 필요가 없다.

## 7. SkillController 변경

`SkillController.Tick()`의 보호된 실행 구간에서 활성화된 패시브들의 `Tick(deltaTime)`을 호출한다.

- 패시브 Tick은 `_isUsingSkill` 조기 반환보다 먼저 실행한다.
- 범위 디버프는 영웅이 공격 또는 스킬 애니메이션을 실행하는 동안에도 계속 갱신한다.
- 패시브 Tick은 마나 충전과 액티브 스킬 판정 전에 수행한다.
- 패시브 Tick 실패는 기존 `FatalStop` 경로로 전달하고 게임 진행을 중단한다.
- `Activate()` 중 패시브 적용 실패는 이미 시작된 패시브를 역순 해제한다.
- `Dispose()`는 패시브를 역순 해제하고 첫 번째 해제 예외를 다시 던진다.

진화로 컨트롤러를 교체할 때 기존 패시브 디버프가 모두 해제된 후 새 등급 패시브가 적용된다.

## 8. 중첩 규칙

패시브 디버프는 기존 패시브 버프 성장 방식과 동일하게 가산 누적한다.

```text
최종 패시브 감소율 = 기본 + '+' 증가분 + '++' 증가분 + 다른 영웅의 패시브
```

예시:

```text
기본 20% + 강화 15% + 강화 15% = 50% 감소
```

지속시간 디버프와의 관계:

- 패시브 디버프끼리는 가산 누적한다.
- 기존 지속시간 슬로우끼리는 가장 강한 값만 적용한다.
- 기존 지속시간 방어력 감소끼리는 가장 강한 값만 적용한다.
- 패시브 감소 합계와 선택된 지속시간 감소는 함께 적용된다.

여러 효과의 합으로 스탯 배율이 음수가 될 수 있으므로 `EnemyStats`에서 최종 `MoveSpeed`와 `Armor`의 하한을 `0`으로 보장한다. 실제 누적 값은 유지하므로 일부 패시브가 해제되면 남아 있는 감소량에 맞게 정상 복구된다.

별도의 최대 감소율 설정은 이번 범위에 추가하지 않는다. 필요한 경우 이후 밸런스 설정 데이터로 분리한다.

## 9. SkillSetContainer 검증

`SkillSetValidator.ValidateGroup()`은 `PassiveBuffSkillData` 대신 공통 `PassiveSkillData`를 인식한다.

`ValidateEnhancements()`의 두 번째 순회에서도 `PassiveSkillData`를 강화 데이터 검사 대상에서 제외한다. 이 처리가 없으면 `PassiveDebuffSkillData`가 액티브 강화 데이터로 오인된다.

공통 패시브 검증:

- Level 0에는 등록할 수 없다.
- 동일 등급 그룹에서 동일한 패시브 에셋을 두 번 등록할 수 없다.
- 서로 다른 등급 그룹에서는 같은 용도의 동일 에셋 참조를 공유할 수 있다.
- 지원하지 않는 `PassiveSkillData` 하위 타입은 오류다.

버프 패시브 검증:

- 기존 `Target` 필수 검증을 유지한다.
- `Effects`는 null 또는 빈 목록일 수 없다.
- null Effect를 허용하지 않는다.
- `BuffType`은 정의된 값이어야 한다.
- `IncreaseRatio`는 유한한 값이어야 한다.

디버프 패시브 검증:

- `Target`은 필수다.
- `Target`은 `AllEnemyTargetData` 또는 `NearEnemyTargetData`여야 한다.
- `NearEnemyTargetData.Radius`는 유한한 양수여야 한다.
- `Effects`는 null 또는 빈 목록일 수 없다.
- null Effect를 허용하지 않는다.
- `StatType`은 정의된 `EnemyStatType`이어야 한다.
- `ReductionRatio`는 유한한 `0 초과, 1 이하` 값이어야 한다.
- 같은 패시브 안에 같은 `StatType`을 중복 등록할 수 없다.
- `EnemyStatType.MaxHP`는 오류로 처리한다.

검증 실패는 게임 Scene 초기 전체 스킬 데이터 검증에서 예외를 던지고 진행을 중단한다. 런타임에서는 잘못된 데이터를 Clamp하여 계속 진행하지 않는다.

## 10. Custom Inspector 변경

`SkillSetContainerEditor`의 패시브 성장 슬롯은 다음 타입을 허용한다.

```text
PassiveSkillData 또는 기존 지원 강화 데이터
```

변경 사항:

- `PassiveBuffSkillData` 직접 타입 검사 제거
- `PassiveSkillData` 공통 타입 검사 사용
- 오류 문구를 실제 타입명인 `PassiveSkillData`와 일치시킨다.
- 기존 등급 간 같은 용도 자동 참조 연결은 버프와 디버프 모두 동일하게 유지한다.
- 상세 데이터 오류는 `SkillSetValidator` 결과로 표시한다.

## 11. 실패 및 정리 정책

- 패시브 디버프 생성 실패: 스킬 생성 실패로 처리하고 게임 진행을 중단한다.
- `Apply()` 실패: 부분 적용을 원복한 뒤 `SkillController`의 FatalStop으로 전달한다.
- `Tick()` 실패: FatalStop 후 게임 진행을 중단한다.
- `OnSpawnEnemy` 적용 실패: 전체 패시브 정리를 시도하고 FatalStop 후 예외를 다시 던진다.
- `OnActiveOff` 해제 실패: 나머지 정리를 시도하고 FatalStop 후 예외를 다시 던진다.
- `Release()` 실패: 가능한 나머지 대상 정리를 계속하고 첫 번째 예외를 보고한다.
- 적 비활성화: 해당 적의 적용 객체를 즉시 해제한다.
- 영웅 컨트롤러 교체 및 Dispose: 모든 이벤트 구독과 대상 적용을 해제한다.
- Scene 종료: 모든 영웅 컨트롤러 Dispose 경로를 통해 정리한다.

## 12. 구현 범위

주요 수정 대상:

- `PassiveSkillData.cs` 신규 추가
- `PassiveBuffSkillData.cs`
- `PassiveDebuffSkillData.cs`
- `BuffData.cs` 및 신규 `DebuffData.cs`
- `FinderData.cs`
- `NearEnemyTargetData.cs`
- `AllEnemyTargetData.cs`
- `PassiveSkill.cs`
- `SkillInterfaces.cs`
- `SkillController.cs`
- `SkillFactory.cs`
- `SkillSetContainer.cs`
- `SkillSetContainerEditor.cs`
- `EnemyStats.cs`

추가 Manager, 프리팹, Scene 오브젝트는 만들지 않는다.

## 13. 검증 시나리오

1. 전체 적 패시브 활성화 시 현재 필드의 모든 적에게 즉시 적용된다.
2. 전체 적 패시브 활성화 후 생성된 적에게도 적용된다.
3. 범위 적이 진입하면 적용되고 이탈하면 즉시 해제된다.
4. 적 사망 및 강제 Despawn 시 풀 반환 전에 원복된다.
5. 같은 `Enemy` 인스턴스가 재사용될 때 이전 디버프가 남지 않는다.
6. 기본/`+`/`++`가 증가분 기준으로 누적된다.
7. 같은 패시브를 가진 영웅이 여러 명이면 가산 누적된다.
8. 패시브 하나를 제거하면 해당 기여분만 정확히 해제된다.
9. 지속시간 디버프와 패시브 디버프가 정해진 중첩 규칙으로 함께 적용된다.
10. 진화 시 이전 등급 디버프가 해제되고 새 등급 구성이 적용된다.
11. 영웅 판매, 합성, Scene 종료 시 이벤트 구독과 디버프가 모두 해제된다.
12. 잘못된 Target, Radius, StatType, ReductionRatio, 중복 Effect가 초기 검증에서 차단된다.
13. 기존 `PassiveBuffSkillData`의 적용·누적·해제 동작이 변경되지 않는다.

## 14. 구현 완료 기준

- 버프와 디버프 패시브가 동일한 등급별 패시브 슬롯에 등록된다.
- 전체/범위 적 패시브가 적 생성, 이동, 사망, Despawn에 맞춰 정확히 적용·해제된다.
- 오브젝트 풀 재사용 후 스탯 오염이 없다.
- 패시브 누적과 지속시간 디버프의 중첩 정책이 문서대로 동작한다.
- 모든 데이터 오류와 런타임 적용 오류가 예외 및 FatalStop 경로로 전달된다.
- Editor 포함 C# 빌드와 관련 런타임 검증 시나리오를 통과한다.
