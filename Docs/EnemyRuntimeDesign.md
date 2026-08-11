# Enemy 런타임 설계

## 1. 범위

이 문서는 `EnemyData`가 로드된 이후 적이 생성되고, 피해를 받고, 속성 효과를 적용받고, 필드에서 제거되는 런타임 구조를 정리합니다.

- `Enemy`: 개별 적의 상태와 생명주기
- `EnemySpawner`: 생성 요청, 오브젝트 풀, 처치 및 강제 제거 전달
- `FieldEnemyService`: 활성 적 목록과 보스 상태
- `StatusContainer`: 적에게 적용된 임시 속성
- `AttributeDamageRule`: 공격 속성과 대상 속성의 상성 판정

데이터 스키마는 `EnemyDataDesign.md`를 기준으로 합니다.

## 2. Enemy 책임

`Enemy`는 다음 상태를 보유합니다.

| 구분 | 내용 |
| --- | --- |
| 데이터 | UID, EnemyType, KillGold, RewardGroupUID |
| 기본 스탯 | MaxHP, Armor, MoveSpeed |
| 기본 속성 | `EnemyData.Attribute`에서 가져온 `BaseAttribute` |
| 임시 속성 | `StatusContainer`에서 관리 |
| 런타임 상태 | CurrentHP, IsActive, LifeCycleVersion |

스킬 상성에 사용하는 기본 속성과 임시 속성은 서로 덮어쓰지 않습니다.

## 3. 초기화와 검증

초기화는 두 단계로 구분합니다.

1. `Initialize(IPathProvider)`
   - 풀 객체 생성 시 한 번 호출합니다.
   - 이동 컨트롤러와 방향 변경 이벤트를 연결합니다.
2. `Init(EnemyData, List<Sprite>)`
   - 풀에서 꺼낼 때마다 호출합니다.
   - 데이터, 스탯, 기본 속성, 스프라이트와 생명주기를 초기화합니다.

`Init`은 다음 데이터를 검증합니다.

- UID가 0보다 큰지
- Name과 SpriteKey가 비어 있지 않은지
- EnemyType과 Attribute가 정의된 단일 Enum 값인지
- MaxHP와 MoveSpeed가 0보다 큰 유한값인지
- Armor가 0 이상의 유한값인지
- KillGold와 RewardGroupUID가 0 이상인지
- 이동 스프라이트가 3개 이상이고 null을 포함하지 않는지

검증에 실패한 적은 활성화하지 않으며 `EnemySpawner`가 다시 풀로 반환합니다.

## 4. 생명주기

```text
Pool.GetItem
  -> Enemy.Init
  -> IsActive = true
  -> 전투
  -> Death 또는 강제 Despawn
  -> Deactivate
  -> Pool.ReturnItem
```

`Deactivate()`는 다음 작업을 한 번에 처리합니다.

- `IsActive = false`
- 임시 속성 제거
- 호환용 Element 상태 제거
- 이동 중지
- `OnActiveOff` 발생

`IsActive`를 가드로 사용하므로 한 생명주기에서 `OnActiveOff`는 한 번만 발생합니다. `Death()` 이후 풀의 `OnDespawn()`이 다시 호출되어도 중복 발생하지 않습니다.

`OnActiveOff`를 받은 시간제 효과 관리자는 해당 적의 둔화, 기절, 방어력 감소와 임시 속성을 즉시 해제합니다. 따라서 같은 적 객체가 같은 프레임에 재사용되어도 이전 생명주기의 효과가 넘어가지 않습니다.

## 5. 기본 속성과 임시 속성

### 기본 속성

- `EnemyData.Attribute`를 `Enemy.BaseAttribute` 필드에 저장합니다.
- 풀 생명주기 동안 변경하지 않습니다.
- 현재 CSV 데이터는 모두 `None`입니다.

### 임시 속성

- `ElementEffect`가 `TimeEffectManager`를 통해 `StatusContainer`에 적용합니다.
- 속성별 최대 5스택을 유지합니다.
- 각 효과의 수명이 끝나면 해당 스택 하나를 제거합니다.
- 적이 비활성화되면 모든 임시 속성을 제거합니다.
- 피해 상성 계산에서는 스택 수를 사용하지 않고 속성의 존재 여부만 한 번 반영합니다.

## 6. 속성 상성

현재 코드의 `Ice`는 수 속성을 의미합니다.

### 5원소

```text
화 < 수 < 뢰 < 지 < 풍 < 화
```

공격 속성 기준으로 표현하면 다음과 같습니다.

| 공격 속성 | 유리한 대상 |
| --- | --- |
| 수 (`Ice`) | 화 (`Fire`) |
| 뢰 (`Electric`) | 수 (`Ice`) |
| 지 (`Earth`) | 뢰 (`Electric`) |
| 풍 (`Wind`) | 지 (`Earth`) |
| 화 (`Fire`) | 풍 (`Wind`) |

표의 반대 방향으로 공격하면 불리 판정입니다. 서로 인접하지 않은 5원소와 동일 속성은 중립입니다.

### 빛과 어둠

- 빛으로 어둠을 공격하면 유리합니다.
- 어둠으로 빛을 공격하면 유리합니다.
- 빛과 어둠으로 5원소를 공격하면 유리합니다.
- 5원소로 빛 또는 어둠을 공격하면 불리합니다.
- 빛 대 빛, 어둠 대 어둠은 중립입니다.

### 복합 속성 판정

대상의 기본 속성과 현재 존재하는 임시 속성을 각각 한 번 판정합니다.

- 유리만 존재: `Advantage`
- 불리만 존재: `Disadvantage`
- 유리와 불리가 함께 존재: 서로 상쇄하여 `Neutral`
- 유리와 불리가 모두 없음: `Neutral`

같은 임시 속성이 여러 스택이어도 판정 횟수는 증가하지 않습니다.

## 7. 피해 계산

```text
DamageEffect.Attribute
  -> DamageEffectHandler
  -> DamageCalculator
  -> AttributeDamageRule
  -> 최종 피해
```

`AttributeDamageRule`은 공격 속성, 대상의 기본 속성, 임시 속성을 받아 `Advantage`, `Disadvantage`, `Neutral` 중 하나를 반환합니다.

현재 배율은 아직 확정되지 않았습니다.

| 판정 | 현재 배율 |
| --- | --- |
| Advantage | 1.0 |
| Disadvantage | 1.0 |
| Neutral | 1.0 |

배율이 확정되면 `AttributeDamageRule` 생성자에 증가 배율과 감소 배율을 전달합니다. 상성 판정 코드와 피해 적용 경로는 변경하지 않습니다.

## 8. EnemySpawner

`EnemySpawner`는 적의 필드 상태를 직접 보관하지 않습니다.

| 이벤트 | 의미 |
| --- | --- |
| `OnSpawnEnemy` | 초기화가 완료된 적이 생성됨 |
| `OnDeathEnemy` | 적이 처치됨 |
| `OnDespawnEnemy` | 처치 이외의 이유로 적이 강제 제거됨 |
| `OnEndWaveSpawn` | 하나의 `EnemySpawnData` 생성 요청이 모두 완료됨 |

처치 순서는 다음과 같습니다.

```text
Enemy.Death
  -> EnemySpawner.OnDeathEnemy
  -> FieldEnemyService.DeathEnemy
  -> 보상 및 보스 처치 이벤트
  -> Pool.ReturnItem
```

강제 제거는 `OnDespawnEnemy`만 발생하고 처치 및 보상 이벤트를 발생시키지 않습니다.

`EnemySpawner`는 동시에 실행되는 여러 생성 요청을 각각 추적합니다. `CancelWaveSpawn()`은 진행 중인 생성 코루틴을 모두 중단하며, 취소된 요청에는 `OnEndWaveSpawn`을 발생시키지 않습니다.

## 9. FieldEnemyService

`FieldEnemyService`는 다음 상태를 관리합니다.

- 활성 적 목록과 개수
- 현재 활성 보스 목록과 생존 수
- 일반 처치, 중간 보스 처치, 보스 처치 구분
- 필드가 비었는지 여부

여러 `Boss`를 동시에 등록하며 `AliveBossCount`로 생존 수를 판단합니다. `Mimic`은 일반 활성 적 목록과 전체 적 수에는 포함되지만 보스 생존 수에는 포함되지 않습니다.

전체 활성 적 스탯 변경은 의미를 명확하게 구분합니다.

- `AddFixedValueToAllEnemies`: 고정값 증감
- `AddMultiplierToAllEnemies`: 배율값 증감

필드 목록에 등록되어 있고 현재 활성 상태인 적만 변경합니다.

## 10. 보상 연결

처치된 적만 `FieldEnemyService.OnEnemyDeath`를 통해 `RewardSystem`으로 전달합니다.

- `KillGold`: `EnemyData`에서 읽어 처치 즉시 지급합니다.
- `PermanentCurrency`: 저장소 연결 전입니다.
- `Item`: 인벤토리 또는 저장소 연결 전입니다.
- `RewardGroupUID = 0`: 추가 보상 판정을 생략합니다.
- 강제 Despawn: 보상을 지급하지 않습니다.

## 11. 남은 작업

1. 실제 EnemyData와 DamageEffect에 속성 입력
2. 유리·불리 피해 배율 확정
3. SkillSetUID를 적 스킬 런타임과 연결
4. 보스 영구 재화와 아이템 저장 경로 연결
5. 속성 판정과 생명주기 자동화 테스트 추가
