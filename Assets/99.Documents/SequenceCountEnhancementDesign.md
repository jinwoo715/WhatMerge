# SequenceCount 강화 설계

## 1. 목적

`SequenceHitExecutionData.SequenceCount`를 사용하는 연속 타격 스킬에 타격 횟수 증가 강화를 적용한다.

예시:

```text
기본 SequenceCount 3
+ 강화 AddCount 1
++ 강화 AddCount 2
최종 SequenceCount 6
```

`SequenceHitExecutionData`는 `EffectBase`가 아니므로 기존 `EffectValueEnhanceData`를 확장해서 사용하지 않는다. 전용 강화 데이터가 런타임 `SequenceExecution`의 횟수를 직접 누적하는 방식으로 구현한다.

## 2. 현재 구조와 제약

현재 생성 순서는 다음과 같다.

1. `RuntimeExecution`이 원본 `ExecutionData`와 Effect를 런타임 복제한다.
2. `SkillFactory`가 `ActiveSkill`과 `SequenceExecution`을 생성한다.
3. `SequenceExecution` 생성자가 복제된 `SequenceHitExecutionData.SequenceCount`를 `_sequenceCount`에 저장한다.
4. 모든 스킬 생성이 끝난 후 강화 데이터가 적용된다.

현재 `_sequenceCount`는 `readonly`이고 변경 메서드가 없으므로 생성 이후 직접 강화할 수 없다.

복제된 `SequenceHitExecutionData`만 나중에 수정해도 이미 생성된 `SequenceExecution`에는 반영되지 않는다. 따라서 런타임 실행 객체에 명시적인 변경 API를 추가한다.

## 3. 설계 결정

다음 구조를 사용한다.

```text
SequenceCountEnhanceData
        |
        v
SkillEnhancementApplier
        |
        v
ISequenceCountModifier.AddSequenceCount()
        |
        v
SequenceExecution._sequenceCount
```

선택 이유:

- `EffectValueEnhanceData`의 Effect 전용 책임을 유지한다.
- `SequenceHitExecutionData`를 `EffectBase`로 잘못 분류하지 않는다.
- `SkillFactory`의 전체 생성 순서를 재구성하지 않아도 된다.
- ActivationChance와 Trigger 감소처럼 생성된 런타임 객체를 강화하는 기존 방식과 일치한다.
- `int` 값인 타격 횟수를 문자열 Stat Key와 `float` 변환 없이 처리한다.

## 4. 강화 데이터

```csharp
[CreateAssetMenu(
    fileName = "SequenceCountEnhance",
    menuName = "Skill/SkillEnhancer/SequenceCountEnhance",
    order = 0)]
public sealed class SequenceCountEnhanceData : SkillBaseData
{
    [Header("대상 Skill")]
    public ActiveSkillData TargetSkill;

    [Header("추가 타격 횟수")]
    [Min(1)]
    public int AddCount = 1;
}
```

필드 의미:

- `TargetSkill`: 강화할 연속 타격 액티브 스킬
- `AddCount`: 해당 성장 단계에서 추가할 타격 횟수

`AddCount`는 최종값이 아닌 증가분이다. 같은 대상의 강화 데이터가 여러 개 활성화되면 모두 더한다.

감소나 값 교체는 지원하지 않는다. 현재 요구사항은 타격 횟수 증가이므로 `AddCount`는 반드시 `1 이상`이어야 한다.

## 5. 런타임 변경 인터페이스

```csharp
public interface ISequenceCountModifier
{
    void AddSequenceCount(int count);
}
```

`ISequenceCountModifier`는 별도 파일을 만들지 않고 `ExecutionBase.cs`의 `IExecute` 옆에 둔다.

`SequenceExecution`이 이 인터페이스를 구현한다.

```csharp
public class SequenceExecution : ExecutionBase, ISequenceCountModifier
{
    private int _sequenceCount;

    public void AddSequenceCount(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        _sequenceCount = checked(_sequenceCount + count);
    }
}
```

변경 사항:

- `_sequenceCount`의 `readonly`를 제거한다.
- 생성자에서는 기존과 동일하게 기본 `SequenceCount`를 복사한다.
- 이후 강화는 런타임 필드에만 누적한다.
- `checked` 연산으로 정수 오버플로를 허용하지 않는다.

원본 `SequenceHitExecutionData`와 `RuntimeExecutionData`의 복제본은 강화 과정에서 수정하지 않는다. 실제 실행에 필요한 최종값은 `SequenceExecution`이 소유한다.

## 6. 강화 적용

`SkillFactory`는 활성화된 `SequenceCountEnhanceData`를 별도 Queue에 수집한다.

```csharp
Queue<SequenceCountEnhanceData> sequenceCountEnhancers = new();
```

모든 `ActiveSkill` 생성이 끝난 후, `SkillSet`을 반환하기 전에 적용한다.

```csharp
SkillEnhancementApplier.ApplySequenceCountEnhance(
    runtimeActiveSkills,
    sequenceCountEnhancers);
```

개념적인 적용 코드는 다음과 같다.

```csharp
ActiveSkill activeSkill = GetRuntimeActiveSkill(
    activeSkills,
    enhancer.TargetSkill,
    enhancer);

if (activeSkill.Execution is not ISequenceCountModifier modifier)
    throw new InvalidOperationException(...);

modifier.AddSequenceCount(enhancer.AddCount);
```

이 시점에는 아직 생성된 `SkillSet`이 외부에 반환되지 않았으므로 실제 스킬 실행과 강화 적용이 동시에 발생하지 않는다.

## 7. 초기 데이터 검증

`SkillSetContainer` 초기 검증에서 다음 항목을 오류로 처리한다.

- `TargetSkill`이 없음
- 대상 액티브 스킬이 같은 레벨 또는 이전 레벨에 해금되지 않음
- `TargetSkill.Execution`이 `SequenceHitExecutionData`가 아님
- 기본 `SequenceHitExecutionData.SequenceCount`가 `1 미만`
- `AddCount`가 `1 미만`
- 기본값과 활성화된 모든 `AddCount`의 합이 `int` 범위를 초과함

잘못된 구성은 Clamp하거나 무시하지 않고 게임 진행 전 예외로 중단한다. 런타임 적용부에서도 같은 핵심 조건을 다시 확인하여 초기 검증 경로를 거치지 않은 호출을 차단한다.

`SequenceHitExecutionData.SequenceCount`에는 Inspector 입력 보조를 위해 `[Min(1)]`과 기본값 `1`을 지정한다. `[Min]`은 에디터 입력 보조일 뿐이므로 초기 검증과 런타임 검증을 대체하지 않는다.

누적 검증은 `Dictionary<ActiveSkillData, long>`으로 계산한다. 검증 과정에서 `int` 오버플로가 먼저 발생하지 않도록 기본값과 모든 `AddCount`를 `long`으로 더한 뒤 `int.MaxValue`와 비교한다.

## 8. Custom Inspector 연동

`SkillSetContainerEditor`에서 `SequenceCountEnhanceData`를 지원되는 강화 데이터에 추가한다.

- 액티브 강화 슬롯에 등록 가능
- 패시브 성장 슬롯에도 기존 강화 데이터 규칙에 따라 등록 가능
- `TargetSkill`을 이용한 해금 순서와 슬롯 대상 일치 검증 적용
- 대상 스킬의 Execution이 `SequenceHitExecutionData`가 아니면 오류 표시

필드가 `TargetSkill`과 `AddCount` 두 개뿐이므로 별도의 Custom Editor는 만들지 않는다.

## 9. 실행 시간 규칙

타격 횟수가 증가해도 전체 애니메이션 시간은 현재와 동일하게 유지한다.

Coroutine 실행 도중 값이 변경되어도 현재 실행이 영향을 받지 않도록 실행 시작 시 최종 횟수를 지역 변수에 저장한다.

```csharp
int sequenceCount = _sequenceCount;
float perTimeScale = animationTimeScale / sequenceCount;

for (int i = 0; i < sequenceCount; i++)
```

```text
타격당 AnimationTimeScale = 전체 AnimationTimeScale / 최종 SequenceCount
```

따라서 타수가 증가하면 각 타격 간격이 짧아진다.

- 전체 스킬 실행 시간은 증가하지 않는다.
- Charge 대기는 기존처럼 첫 타격 전에 한 번만 적용된다.
- 등록된 전체 Effect가 매 타격마다 적용된다.
- 실행 VFX도 매 타격마다 호출된다.

타격 증가로 Effect와 VFX 호출 횟수가 함께 증가하므로 지나치게 큰 값은 성능에 영향을 줄 수 있다. 현재 설계에서는 임의의 하드 코딩 상한을 추가하지 않는다. 기획 상한이 필요해지면 공통 설정 데이터에서 관리하고 초기 검증과 런타임 검증이 같은 값을 사용해야 한다.

## 10. 오류 및 정리

강화 적용 중 오류가 발생하면 `SkillFactory.CreateSkill()`은 현재 실패 처리 경로를 사용한다.

- 생성한 `ActiveSkill`을 Dispose
- 연결된 `RuntimeExecution`을 Dispose
- 예외를 다시 전달하여 게임 진행 중단

별도의 MonoBehaviour, Coroutine, Object Pool 또는 Scene 오브젝트는 추가하지 않는다.

## 11. 확장 기준

현재는 `SequenceCount` 한 항목만 필요하므로 전용 데이터와 인터페이스가 적절하다.

향후 서로 다른 `ExecutionData`의 여러 필드를 강화해야 하는 실제 요구가 추가되면 다음 구조로 일반화할 수 있다.

- `ExecutionValueEnhanceData`
- `IExecutionValueModifier`
- 강화 가능한 Execution Stat 정의
- Stat 선택용 Custom Editor

현재 단계에서 이 일반화까지 도입하면 문자열 키, 타입별 값 변환, Inspector 선택 UI와 검증 코드가 추가되므로 과도하다.

## 12. 구현 파일

구현 변경 대상:

| 역할 | 파일 |
| --- | --- |
| 강화 데이터 추가 | `SequenceCountEnhanceData.cs` |
| 기본값과 Inspector 최소값 | `SequenceHitExecutionData.cs` |
| 런타임 변경 인터페이스 | `ExecutionBase.cs`의 `IExecute` 옆 |
| 누적 처리 | `SequenceExecution.cs` |
| 강화 수집과 적용 호출 | `SkillFactory.cs` |
| 런타임 강화 적용 | `SkillEnhancementApplier.cs` |
| 초기 데이터 검증 | `SkillSetContainer.cs` |
| 슬롯 지원과 즉시 오류 표시 | `SkillSetContainerEditor.cs` |

## 13. 구현 및 검증 상태

- 설계한 런타임 직접 누적 방식 구현 완료
- 실행 시작 시 `SequenceCount` 스냅샷 적용 완료
- 기본값 `1`과 `[Min(1)]` 적용 완료
- 초기 누적 검증에 `long` 적용 완료
- `SkillFactory`, `SkillEnhancementApplier`, `SkillSetContainerEditor` 연결 완료
- Editor 어셈블리 빌드 오류 0개
- 기존 컴파일 경고 4개는 이번 변경과 무관
- Unity Play Mode 동작 검증은 별도로 필요
