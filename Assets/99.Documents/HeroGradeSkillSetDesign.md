# 영웅 등급별 스킬 구성 설계

## 1. 목적

영웅의 현재 등급과 저장 레벨을 기준으로 스킬 구성을 생성하고, 일반 진화 시 변경된 등급에 맞게 전체 스킬 구성을 교체한다.

- 영웅마다 하나의 `SkillSetContainer`를 사용한다.
- 하나의 컨테이너에 해당 영웅이 도달 가능한 세 등급의 완전한 스킬 구성을 저장한다.
- 게임 Scene에서 영웅 레벨은 변경되지 않는다.
- 스폰, 합성 결과 생성, 일반 진화 시 런타임 `SkillSet`과 `SkillController`를 새로 생성한다.
- 스킬 생성이나 활성화 실패는 복구 가능한 상황으로 취급하지 않고 게임 진행을 중단한다.

## 2. 등급과 진화 규칙

`EvolutionLevel`은 실제 등급이 아니라 진화 횟수이며 모든 영웅이 `0~2`를 사용한다.

| 영웅 종류 | BaseGrade | EvolutionLevel 0 | EvolutionLevel 1 | EvolutionLevel 2 |
| --- | --- | --- | --- | --- |
| 일반 영웅 | D | D | C | B |
| 일반 합성 영웅 | C | C | B | A |
| 신화 영웅 | B | B | A | S |

현재 등급은 별도 상태로 저장하지 않고 다음 규칙으로 계산한다.

```text
CurrentGrade = BaseGrade + EvolutionLevel
```

`HeroGrade`는 계산 안정성을 위해 명시적인 정수 값을 사용한다.

```text
D = 0
C = 1
B = 2
A = 3
S = 4
```

- `CurrentGrade`는 get-only 계산 프로퍼티다.
- `BaseGrade`가 D/C/B 중 하나인지 검증한다.
- `BaseGrade + 2`가 S를 초과하면 데이터 오류다.
- `EvolutionLevel`이 `0~2`를 벗어나면 즉시 예외를 던진다.
- 신화 합성 UI의 단계 문구는 등급과 관계없이 계속 `1단계 / 2단계 / 3단계`를 사용한다.
- UI 단계는 내부 `EvolutionLevel 0 / 1 / 2`에 대응한다.

## 3. 영웅 레벨 규칙

- `Hero.Level`은 `HeroSaveData.Level`을 보관하는 get-only 값이다.
- 영웅 레벨은 로비에서만 상승한다.
- 게임 Scene이 시작된 이후에는 영웅 레벨이 변경되지 않는다.
- 게임 중 레벨 변경 이벤트와 런타임 스킬 재구성 경로는 만들지 않는다.
- 최대 레벨은 하드코딩하지 않고 `GameConfig.HeroProgression.MaxLevel`에서 설정한다.
- 저장 레벨의 유효 범위는 `1~MaxLevel`이다.
- 범위를 벗어난 저장 레벨은 Clamp하지 않고 초기 검증에서 예외를 던진다.
- 저장 데이터가 없는 영웅은 실제 스폰할 수 없다.

저장 데이터가 없는 합성 결과의 처리:

- 신화 합성 후보와 전체 목록에서 숨긴다.
- 일반 합성은 불가능한 것으로 처리한다.
- 어떤 재료도 소비하지 않는다.
- 잠금 상태일 수 있으므로 저장 데이터 누락 자체만으로 게임 시작 오류로 취급하지 않는다.

## 4. 등급별 스킬 해금 표

전용무기 해금은 스킬 시스템이 아니므로 `SkillSetContainer`에 포함하지 않는다.

| 등급 | Lv1 | Lv10 | Lv20 | Lv30 | Lv40 | Lv50 | Lv60 | Lv70 | Lv80 | Lv90 | Lv100 | Lv110 | Lv120 | Lv130 | Lv140 | Lv150 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| D | 스킬1 해금 | 패시브1 해금 | 스킬1+ | 패시브1+ | 스킬1++ | 전용무기 해금 | 패시브1++ | - | - | - | 마나스킬 20% 절감 | - | - | - | - | 마나스킬 20% 절감 |
| C | 스킬1 해금 | 패시브1 해금 | 스킬2 해금 | 스킬1+ | 패시브1+ | 전용무기 해금 | 스킬2+ | 스킬1++ | 패시브1++ | 스킬2++ | 마나스킬 20% 절감 | - | - | - | - | 타수스킬 20% 절감 |
| B | 스킬1 해금 | 패시브1 해금 | 스킬2 해금 | 패시브2 해금 | 스킬1+ | 전용무기 해금 | 패시브1+ | 스킬2+ | 패시브2+ | 스킬1++ | 마나스킬 20% 절감 | 패시브1++ | 스킬2++ | - | - | 타수스킬 20% 절감 |
| A | 스킬1 해금 | 패시브1 해금 | 스킬2 해금 | 패시브2 해금 | 스킬1+ | 전용무기 해금 | 패시브1+ | 스킬2+ | 패시브2+ | 스킬1++ | 마나스킬 20% 절감 | 패시브1++ | 스킬2++ | 패시브2++ | - | 타수스킬 20% 절감 |
| S | 스킬1 해금 | 패시브1 해금 | 스킬2 해금 | 패시브2 해금 | 스킬3 해금 | 전용무기 해금 | 스킬1+ | 패시브1+ | 스킬2+ | 패시브2+ | 마나 절감 + 스킬3+ | 스킬1++ | 패시브1++ | 스킬2++ | 패시브2++ | 타수 절감 + 스킬3++ |

기본 공격은 위 표와 별개로 각 등급 그룹의 Level 0에 등록한다.

## 5. SkillSetContainer 구조

영웅 UID마다 하나의 `SkillSetContainer`를 사용한다.

```text
SkillSetContainer
- UID
- GradeSets
  - Grade
  - Sets
    - Level
    - Skill
```

예상 직렬화 타입:

```csharp
public class SkillSetContainer : ScriptableObject
{
    public int UID;
    public List<HeroGradeSkillSet> GradeSets;
}

[Serializable]
public class HeroGradeSkillSet
{
    public HeroGrade Grade;
    public List<HeroSkillSet> Sets;
}

[Serializable]
public class HeroSkillSet
{
    public int Level;
    public SkillBaseData Skill;
}
```

- D 시작 영웅은 D/C/B 그룹을 갖는다.
- C 시작 영웅은 C/B/A 그룹을 갖는다.
- B 시작 영웅은 B/A/S 그룹을 갖는다.
- 각 등급 그룹은 다른 그룹을 상속하지 않는 완전한 구성이다.
- 스킬 생성 시 현재 등급 그룹 하나만 선택한다.
- 선택한 그룹에서 `Level <= Hero.Level`인 항목을 작성 순서대로 사용한다.
- 동일한 레벨에 여러 항목을 등록할 수 있다.
- 서로 다른 등급 그룹에서 같은 스킬 에셋을 재사용할 수 있다.
- 기존 10개 `SkillSetContainer`는 마이그레이션하지 않고 새 구조로 다시 작성한다.

## 6. 기본 공격과 우선순위

기본 공격은 별도의 직렬화 필드로 관리하지 않는다.

기본 공격 판정 규칙:

- Level 0
- `ActiveSkillData`
- Priority 0
- `NoneTriggerData`
- ActivationChance 1

각 등급 그룹에는 위 조건을 만족하는 기본 공격이 정확히 하나 있어야 한다.

- Level 0에는 기본 공격만 허용한다.
- 비기본 액티브 스킬의 Priority는 0보다 커야 한다.
- 액티브 스킬은 `SkillController` 초기화 시 Priority 내림차순으로 한 번 정렬한다.
- 같은 Priority는 SkillSet 작성 순서를 유지한다.
- `_basicAttack`은 별도 설정값이 아니라 위 규칙으로 찾은 런타임 캐시로만 사용할 수 있다.
- 기본 공격 범위 조회와 발동 확률 실패 폴백에 이 캐시를 사용한다.

## 7. 스킬 선택과 발동 실패

한 공격 시점의 스킬 선택 순서:

1. Priority가 높은 스킬부터 Trigger와 Target을 검사한다.
2. 처음 사용 가능한 스킬 하나를 선택한다.
3. 선택한 스킬의 ActivationChance를 판정한다.
4. 성공하면 해당 스킬의 Trigger 자원을 소비한 뒤 실행한다.
5. 실패하면 해당 스킬의 Trigger 자원을 소비하고 기본 공격을 실행한다.
6. 발동에 실패해도 다음 Priority 스킬은 검사하지 않는다.

기본 공격은 ActivationChance가 항상 1이므로 자체 발동 실패가 발생하지 않는다.

## 8. 액티브와 패시브 강화

### 액티브 스킬

- 같은 `ActiveSkillData`로 만든 하나의 런타임 스킬을 강화한다.
- `+`, `++` 강화는 현재 레벨까지 해금된 강화 데이터를 작성 순서대로 누적 적용한다.
- 효과 수치, 추가 효과, 발동 확률, SequenceCount, Trigger 요구량 감소 모두 현재 강화 구조를 사용한다.
- 같은 등급 그룹에서 동일한 `ActiveSkillData`를 기본 스킬 항목으로 두 번 등록할 수 없다.
- 강화 데이터는 같은 등급 그룹에서 자신보다 같거나 낮은 레벨에 존재하는 액티브 스킬만 대상으로 삼을 수 있다.

### 패시브 성장 슬롯

- 기획상의 패시브 슬롯과 런타임 `PassiveSkillData` 타입을 구분한다.
- 패시브 슬롯은 직접 발동하지 않는 능력의 성장 위치이며, `PassiveSkillData` 또는 기존 액티브 스킬을 변경하는 강화 데이터를 등록할 수 있다.
- 상시 버프와 오라는 해금, `+`, `++` 단계마다 해당 단계에서 추가할 효과량을 가진 별도의 `PassiveSkillData`를 등록한다.
- 현재 레벨까지 등록된 각 `PassiveSkillData`마다 별도의 런타임 `PassiveSkill`을 생성하고 Apply/Release하여 효과를 누적한다.
- 평타 적중 시 확률 디버프처럼 기존 공격을 변경하는 능력은 `ExtraEffectData`, `EffectValueEnhanceData` 등의 강화 데이터로 구성한다.
- 예를 들어 해금 단계에서 기본 공격에 Chance 0.1인 감전 Effect를 추가하고, `+` 단계에서 해당 Effect의 Chance를 0.1 더해 최종 20%로 만든다.
- 강화 데이터는 같은 등급 그룹에서 대상 `ActiveSkillData`가 자신보다 같거나 낮은 레벨에 해금되어 있어야 한다.
- 패시브 슬롯에 새로운 `ActiveSkillData` 자체를 등록하는 것은 허용하지 않는다.

### Trigger 요구량 감소

- 요구량 감소는 `Ratio`와 `Fixed` 두 방식을 지원한다.
- 최종 요구량은 `기본 요구량 * (1 - 누적 비율) - 누적 고정값`으로 계산한다.
- 마나 고정 감소는 소수를 허용하고, 타수 고정 감소는 정수만 허용한다.
- 최종 요구량은 최소 1이다.
- 강화 데이터의 감소값과 유형별 누적 결과는 초기 검증 대상이다.
- 누적 비율이 100%를 초과하는 구성은 Clamp로 숨기지 않고 데이터 오류로 처리한다.
- 레벨 100/150 전용 슬롯은 기존 규칙대로 `Ratio 0.2`만 허용한다.
- 상세 설계와 에셋 등록 방법은 `TriggerRequirementReductionDesign.md`를 따른다.

## 9. SkillUID 제거

현재 구조에서는 `SkillUID`가 런타임 기능에 필요하지 않다.

- 강화 대상은 `ActiveSkillData` 에셋 참조로 찾는다.
- 스킬 선택은 런타임 객체와 Priority를 사용한다.
- 저장 데이터와 네트워크 데이터에 스킬 상태를 저장하지 않는다.
- 패시브 UID는 실제 동작에 사용되지 않는다.
- DOT 구분은 별도의 런타임 효과 ID로 처리한다.

제거 대상:

- `SkillBaseData.UID`
- 공통 `ISkill` 인터페이스
- `ISkill.SkillUID`
- `ActiveSkill.SkillUID`
- `PassiveSkill.SkillUID`와 `SetUID()`
- `SkillExecutionContext.SkillUid`
- `DamageContext.SkillUid`
- DOT 키의 `SkillUid`

`ISkill`은 `SkillUID` 외의 공통 계약이 없으므로 함께 제거한다. `IActiveSkill`과 `IPassiveSkill`은 서로 독립된 인터페이스로 유지한다.

`SkillSetContainer.UID`는 영웅 UID와 컨테이너를 연결하므로 유지한다.

향후 스킬별 저장, 네트워크 동기화, 통계 수집처럼 외부의 안정적인 식별자가 필요해지면 별도 요구사항으로 다시 추가한다.

## 10. 런타임 객체 생성 정책

다음 객체는 풀링하지 않는다.

- `SkillSetContainer`: ScriptableObject 설정 에셋을 공유한다.
- `SkillSet`: 스폰 또는 진화 시 새로 구성한다.
- `SkillController`: 스폰 또는 진화 시 새로 생성한다.
- `ActiveSkill`, `PassiveSkill`, Trigger, Finder, Execution: 새 SkillSet에 맞춰 새로 생성한다.

생성 빈도보다 런타임 상태 초기화와 참조 정리의 정확성이 더 중요하다. 영웅 ObjectPool과 별개로 SkillController 풀을 만들지 않는다.

## 11. SkillController 생명주기

`SkillController`는 재사용하지 않는 일회성 객체다.

```text
Created -> Active -> Disposed
```

### Created

- 액티브와 패시브 목록을 보관한다.
- 기본 공격을 찾는다.
- 액티브 스킬을 Priority로 정렬한다.
- MaxMana를 계산한다.
- 패시브를 적용하지 않는다.

### Active

- `Activate()`에서 패시브를 적용한다.
- 공격 속도를 적용한 뒤 활성화한다.
- 중복 `Activate()`는 예외다.
- Active 상태에서만 Tick한다.

### Disposed

- 최초 `Dispose()` 호출에서 외부 정리 콜백보다 먼저 상태를 `Disposed`로 전환한다.
- 실행 중인 스킬 코루틴을 중단한다.
- 패시브를 역순으로 해제한다.
- 액티브 스킬과 `RuntimeExecution`을 정리한다.
- 이벤트 구독과 캐시를 해제한다.
- 하나의 패시브나 액티브 정리가 실패해도 나머지 정리를 best-effort로 계속한다.
- 최초 정리 예외를 보관하고 추가 정리 예외는 별도로 기록한 뒤, 전체 정리가 끝나면 최초 예외를 다시 던진다.
- `Dispose()`는 여러 번 호출해도 결과가 같은 멱등 동작이다.
- Disposed 상태의 컨트롤러는 다시 활성화할 수 없다.

일반 플레이 중 `Dispose()` 예외는 호출 경계에서 FatalStop으로 처리한다. Scene 종료 정리에서는 FatalStop을 다시 호출하지 않고 예외를 수집하여 출력한다.

### Activate 실패

- 패시브의 `Apply()`를 호출하기 전에 해당 패시브를 정리 대상으로 기록한다.
- `Apply()` 도중 예외가 발생하면 이미 적용을 시작한 패시브와 생성된 RuntimeExecution을 best-effort로 `Dispose()`한다.
- 정리 시도 후 컨트롤러 상태를 `Disposed`로 확정하고 `FatalStop`을 호출한 뒤 원래 예외를 다시 던진다.
- 패시브의 `Release()`는 `Apply()`가 일부만 진행된 상태에서도 안전하게 호출할 수 있어야 한다.
- 이 정리는 게임 진행 복구가 아니라 이벤트 구독과 런타임 리소스 누수를 막기 위한 처리다.

기존 `StopRunner()`는 전체 런타임을 파괴하는 동작이므로 `Dispose()`로 변경하고 `IDisposable`을 구현한다.

## 12. 스킬 생성 API와 예외 안전성

외부 런타임 진입점은 `IHeroSkillConfigurator` 하나로 고정한다.

```text
SkillController CreateSkillController(Hero hero, HeroGrade targetGrade)
```

- `HeroSpawner`가 `IHeroSkillConfigurator`를 구현한다.
- `HeroSpawner.RegisterSkillSets()`가 구성한 UID Dictionary를 생성 API가 그대로 사용한다.
- `HeroController`는 `IHeroSummonService`와 함께 동일한 `HeroSpawner` 인스턴스를 `IHeroSkillConfigurator`로 주입받는다.
- 별도의 SkillSet Repository나 별도 configurator 객체는 추가하지 않는다.
- 구현체는 Hero UID로 `SkillSetContainer`를 조회한다.
- Hero의 게임 Scene 고정 `Level`을 직접 읽는다.
- `targetGrade`에 해당하는 등급 그룹을 선택해 `Created` 상태의 `SkillController`를 반환한다.
- `SkillFactory`와 런타임 SkillSet 조립기는 `HeroSpawner` 내부 구현 세부사항으로 둔다.
- 변경 가능한 `SkillSet`을 외부 계층 사이에서 전달하는 공개 API는 만들지 않는다.

- 생성 도중 실패하면 이미 생성한 임시 `RuntimeExecution`과 액티브 스킬을 정리한다.
- 이 정리는 게임 복구가 아니라 리소스 누수 방지 목적이다.
- 원본 예외를 유지하고 Hero UID, Grade, Level, Container 이름을 추가 문맥으로 제공한다.
- 생성 실패 이후 게임 진행은 재개하지 않는다.
- 생성자는 패시브를 적용하지 않으므로 진화 상태 변경 전에 새 컨트롤러를 준비할 수 있다.

## 13. Hero와 SkillController 연결

Hero가 SkillController의 최종 소유자다.

Hero가 제공하는 생명주기 API는 다음 세 개로 고정한다.

```text
AttachSkillController(SkillController controller)
UpgradeEvolution(SkillController nextController)
DisposeSkillController()
```

초기 연결:

- `AttachSkillController()`를 사용한다.
- 기존 컨트롤러가 없어야 한다.
- 새 컨트롤러가 null이면 예외다.
- 인자와 기존 상태 검증이 끝나면 새 컨트롤러의 소유권을 Hero가 넘겨받는다.
- 공격 속도 변경 이벤트를 연결한다.
- 현재 공격 속도를 새 컨트롤러에 설정한다.
- `Activate()`를 호출한다.
- 연결 또는 활성화에 실패하면 공격 속도 이벤트를 해제하고 Hero의 컨트롤러 참조를 비운 뒤 새 컨트롤러를 best-effort로 Dispose하고 원래 예외를 다시 던진다.

교체:

- `UpgradeEvolution(nextController)`만 사용한다.
- 기존 컨트롤러가 있어야 한다.
- `nextController`가 null이면 진화 상태를 변경하기 전에 예외를 던진다.
- EvolutionLevel 상한과 다음 등급을 검증한 뒤 `nextController`의 소유권을 Hero가 넘겨받는다.
- 상태 변경 전 현재 등급의 다음 등급을 `expectedGrade`로 계산한다.
- 기존 공격 속도 이벤트를 해제한다.
- Hero의 기존 컨트롤러 참조를 비운다.
- 기존 컨트롤러를 Dispose한다.
- 진화 상태와 기본 스탯, Sprite를 갱신한다.
- 새 컨트롤러를 연결하고 공격 속도 이벤트를 다시 연결한다.
- 현재 공격 속도를 적용하고 `Activate()`를 호출한다.
- 최종 `CurrentGrade`가 `expectedGrade`와 같은지 검증한다.
- 어느 단계에서든 실패하면 새 컨트롤러의 이벤트 연결과 Hero 참조를 제거하고 새 컨트롤러를 best-effort로 Dispose한다.
- 진화 상태와 기본 스탯은 롤백하지 않으며 호출 경계에서 FatalStop한 뒤 최초 예외를 다시 던진다.
- 새 컨트롤러 정리 중 발생한 추가 예외는 최초 예외를 덮어쓰지 않는다.

`DisposeSkillController()`는 풀 반환과 Scene 종료에서 사용하는 멱등 정리 API다.

- 공격 속도 이벤트를 먼저 해제한다.
- 현재 컨트롤러를 지역 변수로 옮기고 Hero의 컨트롤러 참조를 비운다.
- 지역 변수의 컨트롤러를 Dispose한다.
- Dispose가 예외를 던져도 Hero에는 Disposed 컨트롤러 참조가 남지 않는다.

스킬이 없는 진화 상태를 만들 수 없도록 매개변수 없는 기존 `UpgradeEvolution()`과 Hero 외부에서 진화 상태만 변경하는 API는 유지하지 않는다.

### 스폰 소유권

- `HeroSpawner`는 풀에서 꺼낸 모든 Hero를 내부 활성 목록에 등록하고, 풀 반환 시 목록에서 제거한다.
- Hero를 꺼낸 시점부터 SkillController 생성·연결·활성화와 타일 점유가 완료될 때까지 `HeroSpawner`가 생성 중 Hero의 정리 책임을 가진다.
- 필드 등록 이벤트는 SkillController 활성화와 타일 점유가 성공한 뒤에만 발생시킨다.
- 필드 등록 시작 전 예외가 발생하면 점유한 타일을 해제하고, Hero의 SkillController를 Dispose한 뒤 풀에 반환한다. `HeroSpawner`는 이 예외를 FatalStop에 전달하고 원래 예외를 다시 던진다.
- `HeroController.AddFieldHero()`는 두 필드 Dictionary 등록을 먼저 완료한 뒤 `OnSpawnedHero`와 집계 이벤트를 발생시킨다.
- 필드 등록 또는 그 이후 이벤트 처리 중 예외가 발생하면 부분 등록 가능성이 있으므로 롤백하거나 즉시 풀로 반환하지 않고 FatalStop한다.
- Scene 종료 시 HeroController의 필드 Snapshot과 HeroSpawner의 활성 Snapshot을 합치고 참조 기준으로 중복 제거하여 모든 SkillController를 정리한다.

## 14. 동일 UID 일반 진화

일반 진화는 같은 UID와 같은 EvolutionLevel의 영웅 두 마리를 사용한다.

```text
재료와 생존 영웅 최종 검증
-> target EvolutionLevel과 CurrentGrade 계산
-> IHeroSkillConfigurator로 새 SkillController를 Created 상태로 생성
-> 재료 영웅 제거
-> 생존 Hero.UpgradeEvolution(nextController)
-> 필드 변경 이벤트 Commit
```

- 새 컨트롤러 생성 실패 시 영웅과 재료를 변경하지 않는다.
- 재료 제거 이후 실패하면 롤백하지 않고 FatalStop한다.
- 기존 스킬 코루틴은 즉시 중단한다.
- 아직 발생하지 않은 공격과 효과는 취소한다.
- 마나, 타수, 다음 공격 시간, 발동 확률 캐시는 초기화한다.
- 이미 생성된 투사체, 지속 효과, 소환물은 유지한다.
- 생존 영웅의 SpawnIndex는 유지한다.

## 15. 진화 시 외부 상태 계승

동일 UID 일반 진화에서는 기존 Hero 인스턴스가 생존한다.

유지하는 상태:

- 스킬 외부에서 적용된 임시 버프와 디버프
- 원소와 상태이상
- 전역 버프
- Hero 인스턴스와 SpawnIndex

초기화하는 상태:

- 기존 스킬 패시브
- SkillController 내부 마나와 타수
- 공격 예약 시간과 실행 중인 스킬
- 기존 스킬 런타임 캐시

현재 `StatValue.SetBaseValue()`처럼 기본값 변경과 전체 Modifier 초기화를 동시에 수행하면 외부 버프가 사라진다. 스탯 API를 다음 두 동작으로 분리한다.

- 풀 재사용 초기화: Base, Fixed, Multiplier 전체 초기화
- 진화 기본값 변경: Base만 변경하고 외부 Fixed와 Multiplier 유지

진화 시에는 기존 스킬 패시브를 먼저 해제한 뒤 Base를 변경하고 새 패시브를 적용한다.

## 16. 다른 UID 일반·신화 합성

다른 UID 결과 영웅은 신규 전투 객체로 취급한다.

계승하는 값:

- 조합 결과 HeroUID
- 재료와 동일한 EvolutionLevel
- 결과 영웅 자신의 HeroSaveData.Level
- 가장 낮은 SpawnIndex 재료의 소환 타일

계승하지 않는 값:

- 재료의 마나와 타수
- 공격 예약 시간과 쿨다운
- 실행 중인 스킬
- 재료에게 적용된 임시 버프와 디버프
- 원소와 상태이상
- 재료의 스탯 Modifier
- 재료의 SkillController와 패시브

여러 재료 중 어느 상태를 계승할지 기준이 없고, 특정 재료의 버프를 결과에 이전하는 악용 가능성이 있으므로 상태를 전달하지 않는다.

- 결과 영웅은 새로운 SpawnIndex를 받는다.
- 결과 영웅은 자신의 BaseGrade와 계승한 EvolutionLevel로 CurrentGrade를 계산한다.
- 결과 영웅의 SkillController는 SpawnHero 경로에서 한 번 생성한다.
- 전역 버프와 주변 오라는 결과 영웅이 필드에 등록된 뒤 새로 계산한다.
- 이미 생성된 재료의 투사체, 지속 효과, 소환물은 원래 소유자 정보로 유지하며 결과 영웅에게 이전하지 않는다.

## 17. 유지되는 효과의 불변 소유 정보

투사체, 지속 효과, 소환물이 풀로 반환된 Hero 인스턴스를 장기 참조하면 안 된다.

```text
AttackSourceSnapshot
- AttackPayload
- OwnerSpawnIndex
- SourceEvolutionLevel

DamageContext
- AttackSourceSnapshot
- SourcePosition
- Target
- ImpactPosition
- Effects
- EffectLifetime
```

- `DamageContext`에서 장기 `IAttacker` 참조를 제거한다.
- 공격 수치와 방어 관통, 치명타 정보는 AttackPayload에 스냅샷으로 보관한다.
- 소유자 식별은 OwnerSpawnIndex를 사용한다.
- 투사체 Sprite 선택에는 생성 시점의 SourceEvolutionLevel을 사용한다.
- VFX 방향과 투사체 생성 위치에는 SourcePosition을 사용한다.
- 투사체와 소환물이 하위 효과를 발생시키면 소유 정보는 유지하고 SourcePosition만 해당 런타임 객체의 현재 위치로 변경한다.
- 기존 효과는 영웅 진화나 합성 이후에도 생성 당시 공격 수치와 시각 단계로 완료된다.

대상 참조의 수명 규칙:

- 즉시 판정하는 효과는 실행 시점의 `DamageContext.Target`만 사용한다.
- `IProjectile`과 `ISummonMoveStrategy`는 `IDisposable`을 상속한다.
- `HomingMove`, `AttachMoveStrategy`, `ApproachMoveStrategy`처럼 대상을 추적하는 전략이 대상의 `ICombatant.OnActiveOff`를 직접 구독하고 구독 수명을 소유한다.
- 대상이 한 번 비활성화되면 해당 참조를 영구 무효화하고, 효과 정책에 따라 종료하거나 타겟을 해제한다.
- 풀에서 같은 대상 객체가 다시 활성화되더라도 기존 효과의 대상으로 재인식하지 않는다. 현재 `IsActive` 값만 재검사하는 방식은 사용하지 않는다.
- 전략의 정상 완료와 만료 시 `Dispose()`하고, `ProjectileItem`과 `SummonItem`의 풀 반환 및 `OnDisable()`에서도 같은 멱등 `Dispose()`를 호출한다.
- 투사체와 소환물의 장기 실행용 `DamageContext.Target`는 null로 변경하고, 생성 시 필요한 대상 종류와 위치만 값으로 복사한다.
- 추적 Summon의 예약 실행이 단일 대상을 요구하면 이동 전략이 소유한 현재 활성 Target을 실행 시점에만 조회해 새 `DamageContext`에 결합한다.
- 충돌 대상 제한에 필요한 Hero/Enemy 구분은 내부 값 타입으로 스냅샷하며 원래 `ICombatant` 참조로 판정하지 않는다.
- DOT와 상태 효과는 기존 대상 비활성화 정리 경로를 동일한 원칙으로 유지한다.

Summon 예약 실행의 대상 출처와 금지 조합은 [SummonExecutionTargetDesign.md](SummonExecutionTargetDesign.md)를 따른다.

## 18. RuntimeEffectInstanceId

Unity `GetInstanceID()`를 장기 런타임 효과 식별자로 사용하지 않는다.

- 모든 `RuntimeExecution`이 공유하는 내부 정적 카운터에서 효과 복제 시 단조 증가 `long RuntimeEffectInstanceId`를 발급한다.
- 카운터는 게임 Scene 실행 중 재설정하지 않으며 RuntimeExecution 인스턴스별 카운터를 두지 않는다.
- 각 런타임 효과 복제본은 수명 동안 같은 ID를 유지한다.
- 새 SkillController에서 생성된 효과는 새 ID를 받는다.
- `EffectBase`에는 직렬화하지 않는 내부 전용 `RuntimeEffectInstanceId` 프로퍼티를 둔다.
- 원본 Effect 에셋의 값은 0이며 `RuntimeExecution.CreateRuntimeEffect()`가 복제 직후 0보다 큰 ID를 할당한다.
- 중첩 Effect와 강화로 추가되는 ExtraEffect에도 같은 규칙으로 각각 새 ID를 할당한다.
- `DotData`는 `DotEffect.RuntimeEffectInstanceId`를 복사하며 `GetInstanceID()`를 사용하지 않는다.
- DOT 적용 시 ID가 0이면 런타임 복제 경로를 거치지 않은 오류이므로 즉시 예외를 던진다.
- 이 ID는 런타임 내부 전용 값이며 직렬화하거나 저장 데이터에 포함하지 않는다.
- UI, 분석, 콘텐츠 데이터 등 무관한 시스템에는 노출하지 않는다.

DOT 중복 키:

```text
대상별 DotProcessBundle
+ RuntimeEffectInstanceId
```

- RuntimeEffectInstanceId가 Scene 안에서 전역 유일하므로 DOT 중복 키에 OwnerSpawnIndex를 중복 포함하지 않는다.
- OwnerSpawnIndex는 공격 출처와 귀속 정보로만 유지한다.
- 같은 런타임 효과가 같은 대상에 중복 적용되는 것을 방지한다.
- 진화 전 DOT와 진화 후 새 스킬의 DOT는 서로 다른 RuntimeEffectInstanceId를 가지므로 함께 존재할 수 있다.
- SkillUID는 DOT 식별에 사용하지 않는다.

## 19. 유지 효과의 RuntimeExecution 수명

- ActiveSkill은 자신이 소유한 `RuntimeExecution`의 기본 참조를 가진다.
- 투사체와 소환물은 생성될 때 EffectLifetime lease를 획득한다.
- SkillController가 Dispose되어도 lease가 남아 있으면 런타임 효과 복제본을 유지한다.
- 투사체와 소환물이 종료되면 lease를 해제한다.
- 마지막 lease가 해제되면 RuntimeExecution의 복제 ScriptableObject를 파괴한다.
- DOT와 상태 효과처럼 이후 실행 그래프가 필요하지 않은 효과는 필요한 불변 런타임 값만 복사하고 lease를 보유하지 않는다.
- 투사체와 소환물처럼 이후 하위 효과 그래프를 실행하는 객체는 RuntimeExecution lease를 반드시 유지한다.
- 새 유지 효과를 추가할 때는 `불변 값 스냅샷`과 `RuntimeExecution lease` 중 하나를 명시적으로 선택해야 한다.

## 20. FatalStop 정책

다음 스킬 생명주기 오류는 모두 치명적 오류다.

- 초기 SkillSet 전체 검증 실패
- SkillFactory 생성 실패
- SkillController 연결 또는 교체 실패
- 패시브 활성화, 재계산, 해제 실패
- 액티브 스킬 실행 중 예외
- 투사체와 소환물의 이동·충돌·하위 효과 실행 예외
- DOT 코루틴과 시간제 상태 효과 Update 실행 예외
- RuntimeExecution 생성 또는 강화 적용 실패

FatalStop 상태의 소유자는 `TimeController`로 확정한다. 스킬 계층에는 다음 좁은 인터페이스만 노출한다.

```csharp
public interface IFatalStopService
{
    bool IsFatalStopped { get; }
    void FatalStop(Exception exception, string context);
}
```

- `TimeController`가 `ITimeService`와 `IFatalStopService`를 함께 구현한다.
- `SkillRuntimeContext`에 `IFatalStopService`를 포함하고 `SkillController`의 생성·활성화·코루틴 예외를 이 서비스로 전달한다.
- `HeroSpawner`, `HeroController`, `ProjectileSpawner`, `SummonSpawner`, `DotEffectManager`, `TimeEffectManager`에도 같은 `IFatalStopService` 인스턴스를 주입한다.
- ProjectileItem과 SummonItem은 Spawn 시 각 Spawner에서 같은 서비스를 전달받는다.
- `FatalStop()`은 최초 예외와 문맥만 저장하고, 중복 호출되어도 다시 상태를 변경하지 않는 멱등 동작이다.
- `FatalStop()` 자체는 예외를 던지지 않는다. 호출자는 원래 예외를 다시 던진다.
- 영웅 조작, 소환, 합성, 속도 변경 진입점은 주입받은 동일 서비스의 `IsFatalStopped`를 검사한다.
- `GameSceneBootStrapper`는 전체 Validator보다 먼저 `TimeController`를 준비하고, `Start()` 경계에서 잡은 초기화 예외를 `FatalStop()`에 전달한 뒤 다시 던진다.

처리 순서:

```text
임시 런타임 리소스 정리
-> 최초 예외와 문맥 기록
-> FatalStop 설정
-> Time.timeScale 0
-> 게임 명령 입력 차단
-> 원본 예외 재전달
```

- 상태 복구나 게임 진행 재개를 시도하지 않는다.
- 일반 Pause 해제로 FatalStop을 해제할 수 없다.
- 최초 예외만 주 원인으로 보관한다.
- 추가 정리 예외가 최초 예외를 덮어쓰지 않게 한다.
- Unity Console에 Hero UID, Grade, Level, SkillSet과 스킬 에셋 이름을 포함한 예외를 출력한다.
- `Application.Quit()`은 호출하지 않는다.

FatalStop 이후 차단할 진입점:

- 영웅 소환
- 영웅 선택, 드래그, 이동
- 일반 진화와 일반 합성
- 신화 합성
- 게임 속도 변경

모든 하위 메서드에 검사 코드를 반복하지 않고 사용자 명령 진입점에서 공통 게임 상태를 확인한다.

FatalStop 예외 경계:

- `HeroController`의 `OnSpawnedHero`, `OnDestroyHero`, `OnChangedHeroPosition`, `OnFieldHeroesChanged` 발생 경계에서 패시브 재계산과 구독자 예외를 잡는다.
- 필드 상태가 변경된 뒤 발생한 구독자 예외는 롤백하지 않고 FatalStop한 뒤 원래 예외를 다시 던진다.
- `ProjectileItem.Update()`, 충돌 처리, `SummonItem.Update()`와 충돌 처리에서 발생한 예외는 해당 객체의 전략·이벤트·EffectLifetime lease를 먼저 best-effort로 정리한다.
- DOT 코루틴과 `TimeEffectManager.Update()`의 상태 효과 만료 처리 예외는 해당 효과 핸들과 대상 구독을 best-effort로 정리한다.
- 비동기 유지 효과 경계도 최초 예외만 FatalStop의 주 원인으로 기록하고 정리 예외로 덮어쓰지 않는다.
- 별도의 범용 예외 처리 프레임워크는 추가하지 않고 위 런타임 경계에서 동일한 처리 순서만 적용한다.

## 21. 런타임 실행 예외 처리

Unity 코루틴의 예외 로그에만 의존하면 FatalStop 호출을 보장할 수 없다.

- 액티브 스킬 IEnumerator를 예외 감시 래퍼로 실행한다.
- 중첩 IEnumerator의 `MoveNext()` 예외도 FatalStop으로 전달한다.
- Trigger 자원 소비 후 실행이 실패해도 자원을 복구하지 않는다.
- 실행 중 스킬 실패 이후 다른 스킬이나 기본 공격을 시도하지 않는다.
- 투사체, 소환물, DOT와 시간제 상태 효과처럼 SkillController 밖에서 계속 실행되는 효과는 각자의 Update·코루틴 경계에서 같은 FatalStop 정책을 적용한다.

## 22. TimeController 설계

일반 UI 일시정지와 치명적 중단을 분리한다.

```text
일반 Pause
- MythicMergePanel 등에서 설정
- 해당 UI를 닫으면 해제 가능

FatalStop
- 해당 게임 Scene에서 해제 불가
- SetPause(false)로 재개 불가
```

- FatalStop 또는 일반 Pause 중 하나라도 활성화되면 Time.timeScale은 0이다.
- SpeedUp은 설정 속도만 변경하며 Pause/FatalStop 중에는 시간을 재개하지 않는다.
- `SetPause(false)`는 일반 Pause만 해제하며 FatalStop 상태는 변경하지 않는다.
- Scene 종료 시 TimeController를 정리하고 Time.timeScale을 1로 복구한다.

## 23. 필드 변경 배치

합성 과정의 중간 상태를 외부 Presenter와 주변 패시브에 완료 상태로 알리지 않는다.

```text
BeginBatch
-> 재료 제거
-> 결과 생성
-> SkillController 활성화
-> 필드 등록
-> Commit
-> OnChangedHeroPosition 1회
-> OnFieldHeroesChanged 1회
```

- 재료마다 필요한 `OnDestroyHero`는 유지한다.
- 결과의 `OnSpawnedHero`도 유지한다.
- `OnChangedHeroPosition`과 `OnFieldHeroesChanged`만 배치 완료까지 지연한다.
- 배치 중 예외가 발생하면 Commit하지 않는다.
- Commit되지 않은 배치는 최종 필드 변경 이벤트를 발생시키지 않는다.
- 상태가 일부 변경된 뒤 실패하면 롤백하지 않고 FatalStop한다.
- 현재 구현은 단일 합성 작업 범위만 지원한다.
- 배치 안에서 다시 배치를 시작하는 중첩 호출은 지원하지 않으며 개발 오류로 즉시 실패시킨다.

## 24. 초기 전체 검증

검증은 게임 Scene 초기화 시 한 번 실행한다. 현재 저장 레벨이나 해금 상태와 관계없이 등록된 전체 스킬 데이터를 검사한다.

런타임 초기 검증과 커스텀 Inspector 검증은 동일한 공용 Validator 코드를 호출한다. 검증 책임은 다음 네 영역으로 나누고, 상위 Validator는 결과만 취합한다.

- 영웅 성장 규칙
- SkillSet 구조
- Skill/Effect 그래프
- 합성 및 리소스 연결

하나의 거대한 검증 메서드나 Editor 전용 중복 검증 로직은 만들지 않는다.

### Hero와 진행 데이터

- HeroData UID와 SkillSetContainer UID 일치
- 중복 SkillSetContainer UID 금지
- BaseGrade가 D/C/B 중 하나
- BaseGrade에서 세 단계 진화 시 S를 초과하지 않음
- EvolutionLevel 유효 범위
- 저장 Level 유효 범위
- 일반 합성 결과 HeroData의 BaseGrade가 C
- 신화 합성 결과 HeroData의 BaseGrade가 B
- 덱, 일반 합성, 신화 합성에 참조된 HeroData 존재

### 등급 그룹

- BaseGrade부터 연속된 정확히 세 그룹 존재
- 누락, 중복, 도달 불가능한 추가 그룹 금지
- 그룹과 목록 null 금지
- 각 그룹에 하나 이상의 항목 존재
- Level 오름차순 정렬
- 같은 Level 허용
- Level 범위 `0~MaxLevel`

### 기본 공격과 액티브 스킬

- 각 그룹에 Level 0 기본 공격 정확히 하나
- Priority 0
- NoneTrigger
- ActivationChance 1
- Level 0에 다른 항목 금지
- 비기본 액티브 Priority는 0보다 큼
- 동일 그룹의 같은 ActiveSkillData 중복 금지
- 서로 다른 그룹의 같은 ActiveSkillData 재사용 허용
- Execution, Finder, Trigger, AnimationData 필수 참조 검증
- 수치가 NaN 또는 Infinity인지 검증

### 패시브와 강화 데이터

- Passive Target과 Effects 존재
- 여러 PassiveSkillData 항목의 누적 허용
- 패시브 성장 슬롯에서 PassiveSkillData와 지원되는 강화 데이터 허용
- 패시브 성장 슬롯의 ActiveSkillData 직접 등록 금지
- 강화 대상 ActiveSkill이 같은 그룹의 현재 또는 이전 Level에 존재
- 강화 대상 Effect와 EffectContainer가 대상 RuntimeExecution 그래프 안에 존재
- Effect stat key 유효성
- ExtraEffect의 Effect 참조와 순환 참조 검증
- Trigger 감소 대상이 ManaTrigger 또는 HitCountTrigger인지 검증
- 발동 확률과 Trigger 감소 누적 결과 범위 검증
- Execution과 중첩 Effect 그래프의 null, 중복, 순환 참조 검증

### 리소스와 합성 연결

- Hero SpriteAtlas 존재
- EvolutionLevel 0~2 Idle Sprite 존재
- 덱 영웅의 저장 데이터 존재
- 저장 데이터 없는 일반 합성 결과는 합성 불가
- 저장 데이터 없는 신화 결과는 후보와 목록에서 제외
- 모든 합성 재료와 결과의 HeroData, SkillSetContainer, Sprite 존재

## 25. 검증 실행 위치

`GameSceneBootStrapper` 초기화 순서를 다음과 같이 구성한다.

```text
TimeController와 IFatalStopService 준비
-> 기본 Data와 Resource 참조 확인
-> HeroSpawner.RegisterSkillSets()
-> 일반 합성 Repository 초기화
-> 신화 합성 Repository 초기화
-> 전체 콘텐츠 Validator 실행
-> Runtime 서비스 초기화
-> HeroSpawner.Init()과 ObjectPool 초기화
-> Presenter 초기화
-> 이벤트 Bind
-> 첫 스폰과 Stage 진행 허용
```

- 검증 실패 시 Presenter와 입력을 Bind하지 않는다.
- Start 초기화 경계에서 예외를 FatalStop에 전달하고 다시 던진다.
- 런타임 생성에서도 동일한 검증을 반복하지 않는다.
- 런타임에서는 필요한 직접 전제조건만 검사한다.

현재 `HeroSpawner.Init()`에 함께 들어 있는 SkillSet 등록과 풀 초기화 책임은 위 순서에 맞게 분리한다. 이 단계에서는 별도 SkillSet Repository를 추가하지 않고 `HeroSpawner`의 등록 책임만 명시적으로 분리한다.

## 26. Scene 종료 정리

Scene 종료 시 모든 영웅의 SkillController를 명시적으로 Dispose한다.

```text
Scene 종료 상태 설정
-> 신규 사용자 명령 차단
-> HeroController 필드 Snapshot과 HeroSpawner 활성 Snapshot 생성
-> 참조 기준 중복 제거
-> 모든 Hero의 SkillController Dispose
-> 필드 컬렉션 정리
-> Presenter와 서비스 구독 해제
-> TimeController 정리
-> Time.timeScale 1 복구
```

- 한 영웅의 Dispose가 실패해도 다른 영웅 정리를 계속한다.
- Scene 종료 정리 예외는 모아서 출력한다.
- Scene 종료 중에는 FatalStop으로 다시 게임을 중단할 필요가 없다.
- 일반 `ClearHero()`를 Scene 전체 정리에 사용하지 않는다.
- Scene 종료 중 불필요한 필드 변경 이벤트를 발생시키지 않는다.
- 필드 등록 전에 실패한 생성 중 Hero도 HeroSpawner 활성 Snapshot을 통해 정리 대상에 포함한다.

Unity 객체 파괴 순서에 대한 보조 경로:

- `Hero.OnDisable()`에서 같은 멱등 런타임 정리를 호출한다.
- `ProjectileItem.OnDisable()`에서 전략 구독과 EffectLifetime lease를 정리한다.
- `SummonItem.OnDisable()`에서 이동·실행 전략과 EffectLifetime lease를 정리한다.
- 정상 ObjectPool 반환에서는 `OnDespawn()`이 먼저 정리하므로 뒤이은 `OnDisable()`은 no-op이다.

## 27. 커스텀 Inspector 설계

런타임 설계 구현 후 `SkillSetContainer` 전용 Inspector를 만든다.

- UID에 연결된 HeroData와 BaseGrade를 표시한다.
- 도달 가능한 세 등급 그룹만 생성하도록 돕는다.
- 그룹별 Foldout을 제공한다.
- Level, Skill 타입과 에셋 이름을 한 줄에서 확인할 수 있게 한다.
- 같은 Level 항목의 수동 순서를 유지한다.
- 표준 슬롯은 표시 문구나 Level이 아니라 내부 용도 키로 식별한다.
- 사용자가 표준 슬롯에 에셋 참조를 지정하면 같은 용도의 다른 등급 슬롯에도 즉시 같은 참조를 연결한다.
- 패시브의 해금, `+`, `++`는 서로 다른 용도로 취급하며 각 단계 안에서만 참조를 공유한다.
- 패시브 성장 슬롯에는 `PassiveSkillData`와 지원되는 액티브 강화 데이터를 허용하고, `ActiveSkillData` 자체는 허용하지 않는다.
- 기존 데이터에서 같은 용도에 서로 다른 참조가 발견되면 자동 덮어쓰지 않고 충돌 오류를 표시한다.
- 공유 참조를 해제할 때는 같은 용도의 모든 슬롯을 함께 비울지 확인한다.
- 사용자가 추가한 사용자 슬롯은 자동 참조 연결 대상에서 제외한다.
- Inspector가 목록을 자동 정렬하거나 데이터를 자동 삭제하지 않는다.
- 잘못된 정렬은 경고로 표시한다.
- 사용자가 누르는 명시적인 `Sort by Level` 버튼만 제공한다.
- 정렬은 같은 Level 내부 순서를 유지하는 안정 정렬이어야 한다.
- 런타임 초기 검증과 동일한 공용 Validator를 호출하는 전체 검증 버튼과 오류 목록을 제공한다.
- 그룹 재생성처럼 데이터 손실 가능성이 있는 작업은 확인 없이 실행하지 않는다.

## 28. 테스트 항목

### 등급과 레벨

- D/C/B 시작 영웅이 각각 올바른 세 등급 그룹을 선택한다.
- EvolutionLevel 0/1/2에서 CurrentGrade가 정확하다.
- 현재 Level 이하의 항목만 구성한다.
- 잘못된 저장 Level과 EvolutionLevel이 초기 검증에 실패한다.

### 우선순위

- 마나 스킬과 타수 스킬 Trigger가 모두 충족되면 높은 Priority 스킬이 실행된다.
- 같은 Priority는 작성 순서를 유지한다.
- 고Priority 스킬 발동 실패 시 자원을 소비하고 기본 공격을 실행한다.
- 발동 실패 후 다음 Priority 스킬을 실행하지 않는다.

### 강화

- 액티브 `+`, `++`가 같은 런타임 스킬에 누적된다.
- 패시브 해금, `+`, `++`의 서로 다른 PassiveSkillData가 별도 런타임 패시브로 누적된다.
- 패시브 성장 슬롯의 ExtraEffectData가 기본 공격에 확률 효과를 추가한다.
- 이후 단계의 EffectValueEnhanceData가 추가된 효과의 확률을 누적 강화한다.
- 패시브 성장 슬롯에 ActiveSkillData를 직접 등록하면 Inspector 검증에 실패한다.
- 마나와 타수 감소가 Level에 따라 누적된다.
- 잘못된 강화 대상과 Effect stat key가 초기 검증에 실패한다.

### 진화와 합성

- 새 컨트롤러 생성 실패 전에 재료가 제거되지 않는다.
- `Hero.UpgradeEvolution(nextController)` 외의 경로로 진화 상태만 변경할 수 없다.
- 일반 진화 후 기존 컨트롤러가 Dispose되고 새 등급 스킬이 활성화된다.
- 기존 컨트롤러 Dispose나 진화 상태 변경 중 실패하면 준비한 새 컨트롤러가 정리된다.
- 초기 연결과 진화 교체 실패 후 Hero에 Disposed 컨트롤러 참조와 공격 속도 이벤트 구독이 남지 않는다.
- 하나의 패시브 Release가 실패해도 나머지 패시브와 액티브 정리를 계속한다.
- 일반 진화에서 외부 버프와 상태는 유지되고 스킬 자원은 초기화된다.
- 다른 UID 합성 결과가 재료의 버프, 상태, 마나를 계승하지 않는다.
- 결과 영웅은 자신의 저장 Level을 사용한다.
- 저장 데이터 없는 결과는 재료를 소비하지 않는다.
- 필드 변경 집계 이벤트가 성공 Commit 후 한 번만 발생한다.
- 실패한 배치는 완료 이벤트를 발생시키지 않는다.
- 필드 등록 시작 전 스폰 실패 시 타일 점유와 SkillController가 정리되고 Hero가 풀로 반환된다.
- 필드 등록 중 실패한 Hero가 Scene 종료 활성 Snapshot에서 누락되지 않는다.

### 유지 효과와 식별

- 진화·합성 전에 발사된 투사체가 이후에도 완료된다.
- 기존 DOT와 소환물이 RuntimeExecution 정리 이후 필요한 수명만큼 유지된다.
- 풀에서 같은 Hero 객체가 재사용되어도 기존 효과가 새 영웅 위치와 스탯을 참조하지 않는다.
- 대상이 풀로 반환된 뒤 같은 객체가 재사용되어도 기존 유도 투사체와 소환물이 새 대상을 추적하지 않는다.
- 투사체와 소환물의 완료, 만료, 풀 반환, OnDisable 모든 경로에서 타겟 이벤트 구독이 해제된다.
- 비추적 투사체와 소환물은 원래 Target 참조 없이 필요한 대상 종류와 위치만 유지한다.
- 새 합성 결과의 SpawnIndex가 재료의 기존 효과 소유권과 분리된다.
- 같은 런타임 효과의 DOT 중복은 차단된다.
- 진화 후 새 RuntimeEffectInstanceId의 DOT는 기존 DOT와 함께 존재할 수 있다.
- 모든 런타임 Effect 복제본은 0보다 큰 RuntimeEffectInstanceId를 가지며 DotData까지 같은 값이 전달된다.
- 원본 Effect 에셋과 런타임 복제 경로를 거치지 않은 Effect의 ID 0은 DOT 적용 전에 실패한다.

### 오류와 종료

- 생성, 활성화, 패시브, 액티브 실행 예외가 FatalStop을 발생시킨다.
- 필드 변경 이벤트의 패시브 재계산 예외가 FatalStop을 발생시킨다.
- 투사체, 소환물, DOT와 시간제 상태 효과의 비동기 실행 예외가 해당 객체 정리 후 FatalStop을 발생시킨다.
- 중복 FatalStop 호출이 최초 예외와 문맥을 덮어쓰지 않는다.
- 일부 패시브 적용 후 `Activate()`가 실패해도 시작된 패시브와 RuntimeExecution이 정리되고 컨트롤러가 `Disposed` 상태가 된다.
- FatalStop 이후 Pause 해제로 시간이 재개되지 않는다.
- FatalStop 이후 소환, 이동, 진화, 합성 입력이 차단된다.
- 모든 영웅의 SkillController가 Scene 종료 시 Dispose된다.
- 활성 투사체와 소환물의 EffectLifetime lease가 Scene 종료 시 해제된다.
- 일부 정리가 실패해도 나머지 객체 정리를 계속한다.
- Scene 종료 후 Time.timeScale이 1로 복구된다.

## 29. 구현 범위

주요 수정 대상:

- `HeroData`, `Hero`, `HeroStats`, `StatValue`
- `SkillSetContainer`
- `SkillFactory`, `SkillEnhancementApplier`
- `SkillController`, `ActiveSkill`, `PassiveSkill`, Skill interfaces
- `SkillExecutionContext`, `RuntimeExecution`, `EffectBase`
- `DamageContext`, DOT와 시간제 상태 효과 런타임 데이터·Manager
- `ProjectileItem`, `ProjectileSpawner`
- `SummonItem`, `SummonSpawner`, Summon execution
- `HeroSpawner`, `HeroController`, 일반 진화·합성 경로
- `TimeController`, FatalStop 상태와 게임 명령 진입점
- `GameSceneBootStrapper`, 초기 Validator와 Scene 종료 정리
- `SkillSetContainer` 커스텀 Inspector

제외 범위:

- 전용무기 해금과 장착
- 게임 Scene 내 영웅 레벨 상승
- SkillSet 또는 SkillController 풀링
- 기존 SkillSetContainer 자동 마이그레이션
- FatalStop 이후 런타임 상태 복구와 게임 재개

## 30. 최종 확정 사항

- 영웅별 SkillSetContainer 하나와 등급 그룹 세 개를 사용한다.
- `HeroSpawner`가 SkillSet 등록과 `IHeroSkillConfigurator` 구현을 함께 담당한다.
- 현재 등급 그룹의 현재 Level 이하 항목으로 스킬 전체를 새로 생성한다.
- 진화 시 Hero를 제거하고 다시 만들지 않으며 `Hero.UpgradeEvolution(nextController)`에서만 SkillController를 교체한다.
- 다른 UID 합성 결과는 신규 Hero로 생성하고 재료 상태를 계승하지 않는다.
- 이미 생성된 투사체, 지속 효과, 소환물은 불변 소유 정보로 유지한다.
- SkillUID와 공통 `ISkill`은 제거하고 OwnerSpawnIndex와 내부 전용 RuntimeEffectInstanceId를 사용한다. DOT 중복 키에는 전역 유일 RuntimeEffectInstanceId만 사용한다.
- 풀링된 대상이 비활성화되면 기존 유지 효과의 대상 참조를 영구 무효화한다.
- 타겟 추적 전략은 `IDisposable`로 구독 수명을 직접 관리하고 비추적 객체는 장기 Target 참조를 저장하지 않는다.
- `TimeController`가 `IFatalStopService`를 구현하며 모든 치명적 스킬 오류는 FatalStop 후 원래 예외를 다시 던진다.
- 초기 연결과 진화 교체는 실패 시 준비한 새 컨트롤러와 Hero의 연결 상태를 정리하며 상태 변경 이후에는 롤백하지 않는다.
- 필드 이벤트와 투사체·소환물·DOT·시간제 상태 효과 실행 경계도 같은 FatalStop 정책을 사용한다.
- RuntimeEffectInstanceId는 런타임 Effect 복제본에 저장하고 DotData가 그대로 전달받는다.
- Scene 종료 시 HeroController 필드 Snapshot과 HeroSpawner 활성 Snapshot을 합쳐 모든 영웅과 유지 효과의 런타임 참조를 정리한다.
- 런타임 구현 완료 후 새 데이터 구조에 맞는 커스텀 Inspector를 제작한다.
