# Trigger 요구량 감소 설계

## 1. 목적

마나 스킬과 타수 스킬의 Trigger 요구량을 다음 두 방식으로 감소시킨다.

- `Ratio`: 기본 요구량을 비율로 감소
- `Fixed`: 비율 계산 이후 고정 수치만큼 추가 감소

비율 감소와 고정 감소는 하나의 강화 데이터 타입으로 관리하고, 동일한 대상 스킬에 여러 강화가 적용되면 유형별로 누적한다.

## 2. 데이터 구조

`TriggerRequirementReductionData`는 다음 필드로 구성한다.

```csharp
public enum TriggerRequirementReductionType
{
    Ratio,
    Fixed
}

public sealed class TriggerRequirementReductionData : SkillBaseData
{
    public ActiveSkillData TargetSkill;
    public TriggerRequirementReductionType ReductionType;

    [FormerlySerializedAs("ReductionRatio")]
    [Min(0f)]
    public float ReductionValue;
}
```

필드 의미:

- `TargetSkill`: 요구량을 감소시킬 액티브 스킬
- `ReductionType`: 비율 감소와 고정 감소 중 적용 방식
- `ReductionValue`: 선택한 방식에 사용할 감소값

감소 방식마다 별도의 필드를 두지 않는다. 하나의 데이터는 한 가지 방식만 담당하므로 enum과 단일 값으로 잘못된 조합을 줄인다.

## 3. 최종 요구량 계산

비율과 고정 감소는 별도로 누적하고 다음 순서로 계산한다.

```text
최종 요구량 = 기본 요구량 * (1 - 누적 비율 감소) - 누적 고정 감소
```

최종 요구량의 최솟값은 `1`이다.

예시:

| 기본 요구량 | 누적 비율 | 누적 고정값 | 최종 요구량 |
| ---: | ---: | ---: | ---: |
| 마나 100 | 20% | 10 | 70 |
| 타수 10 | 20% | 1 | 7 |
| 마나 20 | 100% | 5 | 1 |

비율과 고정 감소를 런타임에서 각각 저장하므로 강화 데이터의 적용 순서가 달라도 결과는 같다.

### 3.1 ManaTrigger

- 기본 요구량과 고정 감소값은 `float`를 사용한다.
- 소수 고정 감소를 허용한다.
- 계산 결과에 `Mathf.Max(1f, value)`를 적용한다.

### 3.2 HitCountTrigger

- 기본 요구 타수는 `int`다.
- 고정 감소값은 정수만 허용한다.
- 비율 계산에서 소수가 발생하면 올림하여 필요한 실제 타수를 결정한다.
- 계산 결과의 최솟값은 `1`이다.

## 4. 런타임 적용

`ManaTrigger`와 `HitCountTrigger`는 `ITriggerRequirementModifier`를 구현한다.

```csharp
public interface ITriggerRequirementModifier
{
    void AddRequirementReductionRatio(float ratio);
    void AddRequirementReductionFixed(float value);
}
```

`SkillEnhancementApplier`는 `ReductionType`에 따라 대응하는 메서드를 호출한다.

```text
Ratio -> AddRequirementReductionRatio(ReductionValue)
Fixed -> AddRequirementReductionFixed(ReductionValue)
```

지원하지 않는 enum 값이나 `ITriggerRequirementModifier`를 구현하지 않은 Trigger를 대상으로 하면 예외를 던진다.

## 5. 데이터 검증

게임 씬 초기 검증에서 다음 항목을 오류로 처리한다.

- `TargetSkill`이 없거나 현재 레벨까지 해금되지 않음
- 대상 Trigger가 `ManaTriggerData` 또는 `HitCountTriggerData`가 아님
- `ReductionType`이 정의되지 않은 enum 값임
- `ReductionValue`가 `NaN`, 무한대 또는 `0 이하`임
- `Ratio` 값이 `1`을 초과함
- 같은 스킬의 누적 비율이 `1`을 초과함
- `HitCountTriggerData`에 소수 고정 감소값을 사용함
- 고정 감소 누적값이 `float` 범위를 초과함

누적 비율이 100%를 초과하는 구성은 런타임 Clamp에 의존하지 않고 데이터 오류로 중단한다.

고정 감소는 기본 요구량 이상으로 설정할 수 있다. 이 경우 최종 요구량은 최소 제한에 따라 `1`이 된다.

## 6. SkillSetContainer 등록 규칙

일반 액티브 강화 슬롯과 패시브 성장 슬롯에는 `Ratio` 또는 `Fixed` 데이터를 등록할 수 있다.

등록 순서:

1. `Skill/SkillEnhancer/TriggerRequirementReduction` 메뉴에서 에셋을 만든다.
2. `TargetSkill`에 강화할 `ActiveSkillData`를 지정한다.
3. `ReductionType`을 선택한다.
4. `ReductionValue`를 입력한다.
5. 대상 액티브 스킬이 같은 레벨 또는 이전 레벨에 해금된 등급 그룹에 등록한다.

레벨 100/150의 전용 Trigger 감소 슬롯은 기존 기획을 유지한다.

- `ReductionType`: `Ratio`
- `ReductionValue`: `0.2`
- 마나 감소 슬롯은 `ManaTriggerData`만 대상
- 타수 감소 슬롯은 `HitCountTriggerData`만 대상

전용 슬롯에는 `Fixed` 데이터를 등록할 수 없다. 고정 감소는 일반 강화 슬롯을 사용한다.

## 7. 기존 에셋 호환

기존 에셋의 `ReductionRatio`는 `FormerlySerializedAs`를 통해 `ReductionValue`로 이전된다.

- enum의 기본값 `0`은 `Ratio`다.
- 기존 에셋에는 `ReductionType`이 없으므로 모두 기존과 동일한 비율 감소로 해석된다.
- Unity가 에셋을 다시 저장하기 전에는 YAML에 `ReductionRatio`가 남아 있을 수 있지만 로드 시에는 `ReductionValue`로 복원된다.

따라서 기존 20% 감소 에셋을 일괄 수정할 필요는 없다.

## 8. 구현 파일

| 역할 | 파일 |
| --- | --- |
| 감소 데이터와 enum | `TriggerRequirementReductionData.cs` |
| 런타임 변경 인터페이스 | `ITrigger.cs` |
| 마나·타수 계산 | `Triggers.cs` |
| 강화 데이터 적용 | `SkillEnhancementApplier.cs` |
| 초기 데이터 검증 | `SkillSetContainer.cs` |
| 전용 슬롯 검증 | `SkillSetContainerEditor.cs` |

## 9. 검증 결과

- `Assembly-CSharp-Editor.csproj` 빌드 성공
- 컴파일 오류 0개
- 기존 에셋 필드명 직접 참조가 코드에 남아 있지 않음
- Unity Play Mode 동작 검증은 별도로 필요

