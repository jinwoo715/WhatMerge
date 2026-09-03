# Enemy Skill 설계

## 1. 목적

`EnemyType.Special` 적이 피해와 투사체 없이 다음 스킬을 사용하도록 구성한다.

- 자신 또는 아군 Enemy 버프
- Hero 디버프
- Hero 버프 해제
- Enemy 디버프 해제
- 특정 Enemy 소환
- Enemy 합체
- 스킬 VFX 출력

Hero는 HP가 없으며 Enemy는 Hero에게 피해를 주지 않는다. 기존 Hero 스킬 런타임은 `Hero owner`, 기본 공격, 마나를 전제로 하므로 Enemy 스킬 런타임과 직접 공유하지 않는다.

## 2. 데이터 저장 방식

Enemy 기본 데이터와 Enemy 스킬 데이터는 다음과 같이 분리한다.

| 데이터 | 저장 방식 | 역할 |
| --- | --- | --- |
| `EnemyData` | Excel/JSON | Enemy 기본 능력치와 `SkillSetUID` 관리 |
| `EnemySkillCatalog` | ScriptableObject | 전체 Enemy 스킬 세트 등록 및 UID 조회 |
| `EnemySkillSetContainer` | ScriptableObject | 하나의 `SkillSetUID`에 포함되는 스킬 목록 |
| `EnemySkillData` | ScriptableObject | Trigger, 실행 정책, Action 조립 |
| Trigger/Target/Effect | ScriptableObject | 재사용 가능한 스킬 컴포넌트 |

컴포넌트형 스킬은 타입과 개수가 스킬마다 다르고 VFX 같은 Unity 에셋을 참조하므로 SO를 사용한다. 밸런스 수치도 현재는 SO만 원본으로 사용한다.

```text
EnemyData(Excel)
└─ SkillSetUID
   └─ EnemySkillCatalog(SO)
      └─ EnemySkillSetContainer(SO)
         └─ EnemySkillData(SO)
```

- Special Enemy는 `SkillSetUID > 0`이어야 한다.
- Special이 아닌 Enemy는 `SkillSetUID == 0`이어야 한다.
- `EnemySkillSetContainer.UID`는 Catalog 안에서 유일해야 한다.
- SO에는 설정값만 저장하고 쿨다운, 발동 횟수 같은 런타임 상태를 저장하지 않는다.

## 3. 컴포넌트 구조

```text
EnemySkillData
├─ Trigger 1개
├─ ExecutionPolicy
└─ Action 1개 이상
   ├─ Target 0~1개
   └─ Effect 1개 이상
```

`Condition`은 초기 설계와 구현에서 제외한다. 여러 Trigger의 AND/OR 조합도 사용하지 않으며 스킬 하나는 Trigger 하나만 가진다.

Target과 Effect를 평면 목록으로 분리하지 않고 `Action`으로 묶는다. Action 내부의 Effect는 목록 순서대로 실행한다. 대상이 필요 없는 소환 또는 시전자 기준 VFX Action은 Target을 비워둘 수 있다.

## 4. 실행 정책

```csharp
public sealed class EnemySkillExecutionPolicy
{
    public int Priority;
    public float Cooldown;
    public int MaxActivationCount;
}
```

- `Priority`: 값이 높은 스킬부터 실행한다. 값이 같으면 SkillSet 등록 순서를 따른다.
- `Cooldown`: 동일 생명주기에서 같은 스킬의 재발동 제한 시간이다.
- `MaxActivationCount`: 생명주기 동안 가능한 최대 발동 횟수다. 0은 무제한이다.
- Death 및 Enemy Proximity Trigger는 현재 `MaxActivationCount == 1`이어야 한다.

## 5. Trigger

| 타입 | 필드 | 데이터 구현 | 런타임 구현 |
| --- | --- | --- | --- |
| `EnemyTimeTriggerData` | `InitialDelay`, `Interval` | 완료 | 미구현 |
| `EnemyHitCountTriggerData` | `RequiredHitCount` | 완료 | 미구현 |
| `EnemyHpRatioTriggerData` | `ThresholdRatio` | 완료 | 미구현 |
| `EnemyDeathTriggerData` | 없음 | 완료 | 완료 |
| `EnemyProximityTriggerData` | `TargetEnemyUID`, `DetectionDistance` | 완료 | 완료 |

`EnemyProximityTriggerData.DetectionDistance`의 기본값은 `0.1f`다. 판정은 Collider가 아니라 두 Enemy의 월드 좌표 중심점을 사용한다.

```csharp
Vector2 delta = target.Position - owner.Position;
bool detected = delta.sqrMagnitude <= 0.1f * 0.1f;
```

Physics2D, Rigidbody2D, Trigger Collider는 합체 판정에 사용하지 않는다.

## 6. Target

| 타입 | 대상 | 주요 필드 |
| --- | --- | --- |
| `SelfEnemyTargetData` | 시전자 Enemy | 없음 |
| `NearAllyEnemyTargetData` | 주변 아군 Enemy | `Radius`, `IncludeSelf`, `AllowedTypes` |
| `AllAllyEnemyTargetData` | 전체 아군 Enemy | `IncludeSelf`, `AllowedTypes` |
| `NearHeroFromEnemyTargetData` | 주변 Hero | `Radius` |
| `AllHeroFromEnemyTargetData` | 전체 Hero | 없음 |
| `TriggeredEnemyTargetData` | Trigger가 감지한 Enemy | 없음 |

`TriggeredEnemyTargetData`는 현재 `EnemyProximityTriggerData`에서만 사용할 수 있다.

## 7. Effect

모든 Effect는 공통으로 `Chance`와 선택적인 `VFXData`를 가진다.

| 타입 | 주요 데이터 | 데이터 구현 | 런타임 구현 |
| --- | --- | --- | --- |
| `EnemyBuffEffectData` | 지속시간, Enemy 스탯 증가량 | 완료 | 미구현 |
| `HeroDebuffEffectData` | 지속시간, Hero 스탯 감소량 | 완료 | 미구현 |
| `DispelHeroBuffEffectData` | 최대 해제 수, 해제 정책 | 완료 | 미구현 |
| `CleanseEnemyDebuffEffectData` | 최대 해제 수, 해제 정책 | 완료 | 미구현 |
| `SpawnEnemyEffectData` | Enemy UID, 수량, 위치 정책 | 완료 | 완료 |
| `MergeEnemyEffectData` | 결과 Enemy UID | 완료 | 완료 |
| `EnemySkillVFXEffectData` | VFXData | 완료 | 완료 |

현재 Spawn 런타임은 `SpawnInterval == 0`인 즉시 소환만 지원한다. `AroundOwner`는 Enemy의 경로 위치를 보존할 수 없어 런타임에서 지원하지 않는다.

소환 위치 정책:

- `PathStart`: 경로 시작점
- `Owner`: 시전자의 경로 위치
- `Target`: 대상 Enemy의 경로 위치
- `RelativeToOwnerPath`: 시전자의 경로 위치에서 진행 방향 기준 상대 거리
- `AroundOwner`: 데이터만 존재하며 현재 런타임 미지원

`RelativeToOwnerPath.PathDistanceOffset`은 양수가 앞, 음수가 뒤다. 오프셋이 현재 선분을 벗어나면 인접 경로 선분으로 이동하고, 순환 경로의 시작과 끝은 전체 경로 길이로 순환 보정한다.

`MergeEnemyEffectData.Chance`는 반드시 1이어야 한다. 접촉 후 확률 실패로 다시 진입하지 못하는 상태를 방지하기 위함이다.

## 8. 경로 위치

`EnemyPathPosition`은 다음 값을 가진다.

```text
SegmentStartIndex
DistanceFromSegmentStart
```

`MoveController`는 현재 경로 위치를 노출하며 임의 경로 위치에서 초기화할 수 있다. Enemy는 비활성화 전에 `LastActivePathPosition`을 저장하므로 Death Trigger가 실행되는 시점에도 사망 위치를 사용할 수 있다.

`EnemySpawner.SpawnEnemy(int enemyUID, EnemyPathPosition pathPosition)`은 지정한 경로 위치에서 Enemy를 생성하고 해당 위치부터 이동을 이어간다.

## 9. 런타임 구조

`EnemySkillSystem`은 Enemy마다 Rigidbody2D나 감지 컴포넌트를 추가하지 않고 중앙에서 스킬 상태를 관리한다.

- Field Spawn 이벤트에서 `SkillSetUID`에 대응하는 런타임 Controller를 생성한다.
- Field Death 이벤트에서 Death Trigger를 동기 실행한다.
- `LateUpdate`에서 모든 Enemy 이동이 끝난 후 Enemy Proximity Trigger를 검사한다.
- 실행 직전에 `IsActive`와 `LifeCycleVersion`을 다시 확인한다.
- 동일 프레임에 하나의 Enemy가 두 합체에 사용되지 않도록 예약한다.
- 더 높은 Priority의 스킬을 먼저 처리한다.
- 스킬 생성 또는 실행 예외는 `IFatalStopService`로 전달해 추가 상태 변경을 중단한다.

현재 Proximity 판정은 해당 프레임의 위치만 비교한다. 한 프레임 사이에 `0.1f` 범위를 완전히 통과하는 현상이 확인되면 이전 위치와 현재 위치의 선분 최소 거리 검사를 추가한다.

## 10. 사망 시 X/Y 소환

부모 Enemy의 스킬은 다음과 같이 조립한다.

```text
EnemySkillData
├─ Trigger: EnemyDeathTriggerData
├─ ExecutionPolicy
│  └─ MaxActivationCount: 1
└─ Action
   ├─ Target: 없음
   └─ Effects
      ├─ SpawnEnemyEffectData
      │  ├─ EnemyUID: X
      │  ├─ Count: 1
      │  ├─ SpawnInterval: 0
      │  ├─ SpawnPositionType: RelativeToOwnerPath
      │  └─ PathDistanceOffset: 양수
      └─ SpawnEnemyEffectData
         ├─ EnemyUID: Y
         ├─ Count: 1
         ├─ SpawnInterval: 0
         ├─ SpawnPositionType: RelativeToOwnerPath
         └─ PathDistanceOffset: 음수
```

사망한 부모는 필드 목록에서 먼저 제거되지만 Enemy 수 변경 알림은 X/Y 소환 완료 후 한 번만 전달한다. 따라서 중간의 빈 필드 상태가 Stage 로직에 노출되지 않는다.

## 11. Y와 X의 합체

Y는 필드에 존재하는 같은 UID의 모든 X 중 중심점 거리가 가장 가까운 X와 합체한다. 부모가 함께 소환한 X로 제한하지 않는다.

```text
Y EnemyData
└─ SkillSetUID
   └─ EnemySkillData
      ├─ Trigger: EnemyProximityTriggerData
      │  ├─ TargetEnemyUID: X
      │  └─ DetectionDistance: 0.1
      ├─ ExecutionPolicy
      │  └─ MaxActivationCount: 1
      └─ Action
         ├─ Target: TriggeredEnemyTargetData
         └─ Effect: MergeEnemyEffectData
            ├─ ResultEnemyUID: Z
            └─ Chance: 1
```

합체 처리 순서:

1. `FieldEnemyService.GetEnemiesByUID(X_UID)`로 X 후보만 조회한다.
2. Y와 중심점 거리가 `0.1f` 이하인 X 중 가장 가까운 대상을 선택한다.
3. X와 Y를 해당 프레임의 합체 대상으로 예약한다.
4. 모든 Y의 검사가 끝난 후 예약된 합체 명령을 실행한다.
5. Z를 Y의 경로 위치에 먼저 생성한다.
6. X와 Y를 사망이 아닌 Despawn으로 제거한다.
7. X/Y 제거와 Z 추가가 끝난 뒤 Enemy 수 변경 알림을 한 번만 전달한다.

X와 Y는 사망 처리되지 않으므로 사망 보상, 사망 스킬, 처치 수 증가를 발생시키지 않는다.

## 12. 필드 Enemy 관리

`FieldEnemyService`는 다음 인덱스를 함께 관리한다.

```csharp
Dictionary<int, List<Enemy>> _activeEnemiesByUID;
```

```csharp
IReadOnlyList<Enemy> GetEnemiesByUID(int enemyUID);
IDisposable DeferEnemyCountNotifications();
```

UID 인덱스는 Proximity Trigger가 전체 Enemy를 매 프레임 필터링하지 않도록 한다. Enemy 수 알림 지연 Scope는 사망 소환과 합체 도중의 중간 개체 수가 Stage 실패 또는 빈 필드 판정에 사용되지 않도록 한다.

## 13. 검증 규칙

`EnemySkillValidator`는 기존 검증에 다음 항목을 추가한다.

- Proximity 대상 Enemy UID가 양수이며 EnemyData에 존재하는지 확인
- Proximity 감지 거리가 유한한 양수인지 확인
- Proximity Trigger의 최대 발동 횟수가 1인지 확인
- Triggered Enemy Target이 Proximity Trigger와 함께 사용되는지 확인
- Merge 결과 Enemy UID가 양수이며 EnemyData에 존재하는지 확인
- Merge Effect가 Triggered Enemy Target을 사용하는지 확인
- Merge Effect의 Chance가 1인지 확인
- 상대 경로 소환 오프셋이 유한한 값인지 확인

런타임 초기화 시 현재 지원하지 않는 Target, Effect, 지연 소환, `AroundOwner` 사용도 검사한다.

## 14. Scene 연결

`GameSceneBootStrapper`의 `Enemy Skill Catalog` 필드에 Catalog SO를 할당해야 Enemy 스킬 런타임이 활성화된다. Catalog가 할당되지 않으면 기존 전투는 유지되지만 Enemy 스킬은 실행되지 않는다.

구체적인 스킬 SO 에셋을 만들 때는 X/Y/Z의 실제 Enemy UID와 부모 기준 앞뒤 소환 거리를 입력한다.

## 15. 후속 범위

- Time, HitCount, HP Ratio Trigger 런타임
- Enemy Buff와 Hero Debuff 런타임
- Hero Buff 해제와 Enemy Debuff 정화 런타임
- 지연 Enemy 소환
- `AroundOwner` 위치를 경로상 위치로 변환하는 정책
- 고속 이동을 위한 연속 거리 판정
- 커스텀 Inspector와 실제 X/Y/Z 스킬 SO 에셋

Condition과 복합 Trigger는 계속 제외한다.
