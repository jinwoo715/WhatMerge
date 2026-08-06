# EnemyData 설계

## 1. 게임 전제

- 장르는 랜덤 디펜스입니다.
- 적은 목적지를 향해 이동하며 영웅을 공격하지 않습니다.
- `Normal`, `MiddleBoss`, `Boss` 모두 처치 시 게임 진행용 재화를 즉시 지급할 수 있습니다.
- `Boss`는 추후 로비 재화, 영구 강화 재료 또는 아이템을 추가로 지급할 수 있습니다.
- 보스는 개별 스킬을 가질 수 있지만 별도의 `BossData`는 만들지 않습니다.

## 2. 데이터 관리 방식

`EnemyData.csv`를 원본으로 사용하고 변환된 `EnemyData.json`을 런타임에서 읽습니다.

```text
EnemyData.csv
  -> DataTransformer
  -> EnemyData.json
  -> Addressable TextAsset
  -> DataManager
```

적 데이터는 행 단위로 비교하고 일괄 수정할 일이 많으므로 ScriptableObject보다 CSV가 적합합니다. Unity 객체 참조가 필요한 스킬은 ScriptableObject로 관리하고 `EnemyData`에서는 UID로 연결합니다.

## 3. 확정 스키마

```csv
UID,Name,Description,SpriteKey,EnemyType,MaxHP,Armor,MoveSpeed,Attribute,SkillSetUID,RewardGroupUID
```

| 필드 | 형식 | 설명 |
| --- | --- | --- |
| `UID` | `int` | 적 고유 식별자 |
| `Name` | `string` | UI에 표시할 한글 이름 |
| `Description` | `string` | UI에 표시할 설명 |
| `SpriteKey` | `string` | 스프라이트 이름의 고정 접두사 |
| `EnemyType` | `EnemyType` | `Normal`, `MiddleBoss`, `Boss` |
| `MaxHP` | `float` | 기본 최대 체력 |
| `Armor` | `float` | 기본 방어력 |
| `MoveSpeed` | `float` | 기본 이동속도 |
| `Attribute` | `ElementType` | 기본 속성 |
| `SkillSetUID` | `int` | `SkillSetContainer` UID. 스킬이 없으면 `0` |
| `RewardGroupUID` | `int` | `EnemyRewardData`의 보상 그룹 UID |

적이 영웅을 공격하지 않으므로 `AttackPower`, 관통력, 치명타 등의 공격 필드는 포함하지 않습니다.

## 4. 제거 및 변경된 필드

| 기존 필드 | 변경 내용 |
| --- | --- |
| `HP` | `MaxHP`로 변경 |
| `Amour` | `Armor`로 변경 |
| `Nomal` | `Normal`로 변경 |
| `Coin` | 제거하고 `RewardGroupUID`로 대체 |
| `SkillUID` | 제거하고 `SkillSetUID`로 대체 |
| `IsBoss` | 제거하고 `EnemyType`으로 통일 |

## 5. 이름과 스프라이트 규칙

`Name`과 `SpriteKey`의 책임을 분리합니다.

```text
Name      = 슬라임 킹
SpriteKey = SlimeKing
```

스프라이트 파일명은 다음 규칙을 사용합니다.

```text
{SpriteKey}_{Action}_{Frame}

Slime_Move_00
Slime_Move_01
SlimeKing_Move_00
SlimeKing_Skill01_00
```

`Name`이 변경되어도 스프라이트 연결에는 영향을 주지 않습니다. 행동별 애니메이션은 `SpriteKey`와 스킬의 `AnimationData.MotionName`을 조합해 조회하는 방향을 사용합니다.

현재 `EnemySpriteRepository`는 첫 번째 `_` 앞의 `SpriteKey`만 기준으로 스프라이트를 묶습니다. 보스 스킬 애니메이션을 연결할 때는 `(SpriteKey, Action)` 단위로 조회하도록 확장해야 합니다.

## 6. 스킬 연결

`EnemyData`는 스킬 ScriptableObject를 직접 참조하지 않고 `SkillSetUID`를 보유합니다.

```text
EnemyData.SkillSetUID
  -> SkillSetContainer.UID
  -> ActiveSkillData / PassiveSkillData
```

- 스킬이 없는 적은 `SkillSetUID = 0`을 사용합니다.
- 보스별 `SkillSetContainer`를 만들면 여러 액티브 스킬과 패시브 스킬을 함께 구성할 수 있습니다.
- 스킬, 페이즈, 소환 패턴만으로 보스를 표현할 수 있는 동안에는 별도 `BossData`를 만들지 않습니다.
- 전용 AI나 맵 기믹이 필요해질 때만 `BossPatternData` 같은 별도 설정을 검토합니다.

현재 영웅 스킬 런타임은 소유자 타입을 `Hero`로 고정하고 있으므로, 보스가 같은 ScriptableObject 스킬을 실행하려면 공용 스킬 소유자와 진영 기준 타겟 탐색으로 일반화해야 합니다.

## 7. 보상 연결

`EnemyData`에는 보상 수량을 직접 넣지 않고 `RewardGroupUID`만 둡니다.

```csv
UID,RewardGroupUID,RewardType,RewardUID,Amount,DropChance
```

현재 데이터는 기존 `Coin = 10` 동작을 다음과 같이 옮겼습니다.

```csv
UID,RewardGroupUID,RewardType,RewardUID,Amount,DropChance
1,1,BattleCurrency,1,10,1
```

- `Normal`, `MiddleBoss`, `Boss`의 `BattleCurrency`는 처치 즉시 `GameEconomySystem`에 지급합니다.
- 보스의 영구 재화와 아이템은 종류가 확정된 뒤 같은 `RewardGroupUID`에 추가합니다.
- 영구 보상은 전투 재화와 다른 저장소로 전달해야 합니다.

## 8. CSV 변환 규칙

`DataTransformer`는 다음 규칙으로 동작합니다.

- 클래스의 public 필드와 CSV 헤더명을 기준으로 매칭합니다.
- CSV 열 순서는 변환 결과에 영향을 주지 않습니다.
- 누락된 헤더, 알 수 없는 헤더, 중복 헤더를 오류로 처리합니다.
- 따옴표로 감싼 쉼표와 줄바꿈을 지원합니다.
- `""`를 CSV 내부 따옴표로 처리합니다.
- 숫자는 문화권에 영향받지 않는 형식으로 변환합니다.

예를 들어 쉼표가 포함된 설명은 다음처럼 작성할 수 있습니다.

```csv
1,슬라임,"느리지만, 꾸준히 이동한다",Slime,Normal,5,0,1,None,0,1
```

Unity 메뉴의 `Tools/Parse/CSV To Json`을 실행하면 다음 파일을 갱신합니다.

- `EnemyData.json`
- `EnemyRewardData.json`

JSON은 생성 결과물이므로 직접 수정하지 않습니다.

## 9. 현재 데이터 상태

- 기존 영문 적 이름은 `SpriteKey`로 보존했습니다.
- `Name`은 한글 표시명으로 변경했습니다.
- 실제 `Stage1` 아틀라스에 스프라이트가 존재하는 12종만 유지했습니다.
- 스프라이트와 스테이지 참조가 없는 기존 `MergeKeeper(1001)` 행은 제거했습니다.
- 기존 `IsBoss = true`인 적은 `Boss`로 변경했습니다.
- 기존 `IsBoss = false`인 적은 `Normal`로 변경했습니다.
- 현재 중간 보스로 확정된 UID가 없어 `MiddleBoss` 행은 추가하지 않았습니다.
- 기존 CSV의 체력 값을 원본으로 사용했습니다.
- 현재 모든 적의 `SkillSetUID`는 기존 데이터와 동일하게 `0`입니다.
- 현재 모든 적은 기존과 동일하게 전투 재화 10을 확정적으로 지급합니다.

## 10. 다음 작업

1. 실제 중간 보스 UID와 능력치 입력
2. 보스별 `SkillSetContainer` 제작 및 `SkillSetUID` 입력
3. 적 스킬 실행을 위한 공용 스킬 소유자 구조 설계
4. 행동별 적 스프라이트 조회 구조 확장
5. 보스 영구 재화 또는 아이템 확정 후 저장 경로 연결
