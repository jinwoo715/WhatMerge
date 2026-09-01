# 신화 합성 설계

## 1. 목적

필드에 신화 합성 재료가 모두 존재하면 합성 가능한 후보 버튼을 표시하고, 버튼을 누르면 재료 영웅들을 소비하여 신화 영웅을 소환한다.

- 일반 2영웅 합성과 별개의 조합 데이터를 사용한다.
- 합성 후보 버튼은 전체 합쳐 최대 3개만 표시한다.
- 실제 조합 데이터의 영웅 UID와 수량은 데이터 작성자가 관리한다.
- 코드에 신화 조합을 하드코딩하지 않는다.

## 2. 확정된 등급 규칙

`EvolutionLevel`은 실제 등급이 아니라 진화 횟수이며 모든 영웅이 `0~2`를 사용한다.

| 영웅 종류 | EvolutionLevel 0 | EvolutionLevel 1 | EvolutionLevel 2 |
| --- | --- | --- | --- |
| 일반 영웅 | D | C | B |
| 일반 합성 영웅 | C | B | A |
| 신화 영웅 | B | A | S |

신화 합성에서는 다음 규칙만 먼저 적용한다.

- 모든 재료 영웅의 `EvolutionLevel`이 동일해야 한다.
- 결과 신화 영웅은 재료의 `EvolutionLevel`을 그대로 계승한다.
- 등급별 스킬 재구성은 [HeroGradeSkillSetDesign.md](HeroGradeSkillSetDesign.md)의 확정 설계를 따른다.
- 내부 값은 계속 `EvolutionLevel 0/1/2`를 사용하고 UI에는 등급과 관계없이 `1단계/2단계/3단계`로 표시한다.
- 결과 신화 영웅은 자신의 `BaseGrade`, 계승한 `EvolutionLevel`, 저장 `Level`을 기준으로 현재 등급의 스킬 구성을 생성한다.

## 3. 데이터 구조

신화 합성은 재료 수가 가변적이고 같은 UID를 여러 마리 요구할 수 있으므로 일반 `MergeData`와 분리한다.

```text
MythicMergeData
- ResultHeroUID
- Materials
  - HeroUID
  - Count
```

별도의 조합 UID와 `DisplayPriority`는 두지 않는다.

- 신화 영웅 하나당 합성 조합은 하나만 존재한다.
- `ResultHeroUID`가 조합을 유일하게 식별한다.
- 실제 합성 후보는 `ResultHeroUID + EvolutionLevel`로 식별한다.
- 조합 표시 순서는 `MythicMergeData.json`에 작성된 순서를 사용한다.
- 전체 재료 수량 합계는 2~4다.
- 동일 UID를 여러 마리 요구할 때는 재료 항목을 중복 작성하지 않고 `Count`를 사용한다.
- `ResultHeroUID`를 같은 조합의 재료로 사용할 수 없다.

## 4. 데이터 로딩

기존 일반 합성 데이터와 같은 흐름으로 로드한다.

```text
Addressables: MythicMergeData
-> DataManager.MythicMergeData
-> MythicMergeRepository.Init(...)
```

`MythicMergeData.json`은 Addressables Data 그룹에 `MythicMergeData` 주소로 등록한다.

## 5. 데이터 검증

`MythicMergeRepository` 초기화 시 다음 항목을 검증한다.

- `ResultHeroUID` 중복 금지
- 재료 목록 null 또는 빈 목록 금지
- 전체 재료 수량 합계가 2~4
- 각 재료의 `Count > 0`
- 한 조합 안에서 동일한 `HeroUID` 항목 중복 금지
- `ResultHeroUID`와 같은 재료 UID 금지
- 재료와 결과 UID에 해당하는 `HeroData` 존재

결과 영웅이 실제로 소환 가능한지도 게임 시작 전에 검증한다.

- 결과와 모든 재료 영웅의 SpriteAtlas 존재
- 결과와 모든 재료 영웅의 EvolutionLevel 0~2 Idle Sprite 존재
- 결과와 모든 재료 영웅의 `SkillSetContainer` 등록

신화 결과 영웅의 `BaseGrade`는 B여야 하며 게임 시작 검증에서 확인한다.

## 6. 합성 후보 계산

`MythicMergeController`가 후보를 계산한다.

1. 필드 영웅을 `EvolutionLevel`별로 분류한다.
2. 각 단계에서 `HeroUID`별 보유 수량을 계산한다.
3. 조합의 모든 재료 수량을 만족하면 후보를 생성한다.
4. 후보에는 `ResultHeroUID`와 `EvolutionLevel`만 저장한다.
5. 결과 영웅의 `HeroSaveData`가 없는 조합은 후보에서 제외한다.
6. 데이터 작성 순서를 먼저 적용한다.
7. 같은 조합에서는 `EvolutionLevel` 오름차순으로 정렬한다.
8. 앞에서 최대 3개만 반환한다.

실제 `Hero` 인스턴스는 후보에 저장하지 않는다. 버튼 클릭 시 현재 필드를 다시 조회하여 오래된 참조가 사용되지 않게 한다.

## 7. 재료 선택 규칙

버튼 클릭 시 현재 필드 상태를 다시 검사한다.

- 선택한 후보와 동일한 `EvolutionLevel`의 영웅만 사용한다.
- 같은 `HeroUID + EvolutionLevel` 영웅이 요구 수량보다 많으면 `SpawnIndex`가 낮은 순서로 소비한다.
- 선택된 모든 재료 중 `SpawnIndex`가 가장 낮은 영웅의 타일을 결과 소환 위치로 사용한다.
- 재검증에 실패하면 어떤 영웅도 소비하지 않고 후보 UI만 다시 계산한다.

## 8. 실행 인터페이스

필드 영웅과 타일 점유 상태를 소유한 `HeroController`가 `IHeroMergeExecutor`를 구현한다.

```csharp
bool TryMergeHeroes(
    IReadOnlyList<Hero> materials,
    int resultHeroUID,
    int evolutionLevel);
```

- 모든 재료가 아직 필드에 등록되어 있는지 최종 확인한다.
- 재료 목록에 같은 `Hero` 인스턴스가 중복되지 않았는지 확인한다.
- 결과 소환 타일은 재료를 반환하기 전에 저장한다.
- 성공 여부만 반환하며 별도의 요청 객체는 만들지 않는다.
- 기존 일반 합성도 이 메서드를 사용한다.

## 9. 영웅 제거와 이벤트 순서

재료는 일괄 합성 범위 안에서 한 마리씩 정상적인 생명주기를 거쳐 제거한다.

```text
재료를 필드 Dictionary와 타일에서 제거
-> OnDestroyHero
-> HeroSpawner.ReturnHero
-> 스킬 및 패시브 정리
-> 다음 재료 제거
```

`OnDestroyHero`와 `OnSpawnedHero`는 버프 및 패시브 시스템이 사용하므로 생략하거나 마지막에 합쳐서 발생시키지 않는다.

합성 도중 중간 상태가 외부에 노출되지 않도록 아래 집계 이벤트만 억제한다.

- `OnChangedHeroPosition`
- `OnFieldHeroesChanged`

모든 재료 제거와 결과 소환이 완료된 후 각각 한 번만 발생시킨다.

## 10. OnFieldHeroesChanged 규칙

신화 합성 후보는 위치가 아니라 필드 영웅 구성에 따라 달라지므로 `OnChangedHeroPosition`과 별도의 이벤트를 사용한다.

`OnFieldHeroesChanged` 발생 조건:

- 영웅 단일 소환 완료
- 영웅 판매 또는 가방 이동 완료
- 일반 진화 완료
- 일반 합성 완료
- 신화 합성 완료

단순 위치 이동이나 자리 교환에는 발생시키지 않는다.

일반 진화에서는 새 등급의 SkillController 준비, 재료 제거, 기존 컨트롤러 정리, 진화 상태와 기본 스탯 갱신, 새 컨트롤러 활성화가 모두 성공한 뒤 한 번 발생시킨다.

## 11. 합성 실행 순서

```text
버튼 클릭
-> ResultHeroUID + EvolutionLevel로 조합 조회
-> 현재 필드 기준으로 조합 가능 여부 재검증
-> SpawnIndex 오름차순으로 중복 없는 재료 Hero 선택
-> 결과 소환 타일 저장
-> 재료별 OnDestroyHero 및 풀 반환
-> 동일 EvolutionLevel로 결과 영웅 소환
-> OnSpawnedHero
-> OnChangedHeroPosition 1회
-> OnFieldHeroesChanged 1회
```

합성 성공 시 Presenter는 별도로 후보를 다시 계산하지 않고 최종 `OnFieldHeroesChanged` 이벤트로만 갱신한다. 재검증 실패 시에는 이벤트가 발생하지 않으므로 Presenter가 즉시 후보를 다시 계산한다.

## 12. 결과 생성 실패 방지

재료 제거 후 결과 생성에 실패하면 재료만 사라질 수 있으므로 결과 영웅의 정적 구성을 게임 시작 시 검증한다.

`HeroSpawner`는 풀에서 꺼낸 Hero를 활성 목록에 기록한다. 결과 Hero의 SkillController 생성·활성화 또는 필드 등록 시작 전 타일 점유에서 예외가 발생하면 점유를 해제하고 컨트롤러를 정리한 뒤 Hero를 풀에 반환하고 FatalStop한 후 원래 예외를 전달한다. 필드 등록이 시작된 뒤 실패하면 부분 등록 가능성이 있으므로 롤백하지 않고 FatalStop하며, Scene 종료 시 HeroSpawner 활성 Snapshot을 통해 누락 없이 정리한다.

합성 실행 중 복잡한 롤백 시스템은 추가하지 않는다. HeroData, SpriteAtlas, SkillSet 등록 오류를 시작 단계에서 실패시키는 방식으로 처리한다.

## 13. UI 구조

버튼 수가 최대 3개로 고정되어 있으므로 버튼 프리팹을 런타임에 생성하지 않는다.

- Scene에 `MythicMergeButtonSlot` 3개를 미리 배치한다.
- `MythicMergeViewer`가 슬롯 배열을 직렬화하여 관리한다.
- 사용하지 않는 슬롯은 비활성화한다.
- `Awake()`에서 모든 슬롯을 먼저 비활성화하여 초기 화면 노출을 방지한다.
- 클릭 리스너는 초기화 시 한 번만 등록한다.
- 슬롯 해제 시 후보 값, Sprite, 텍스트를 초기화한다.

각 슬롯 표시 정보:

- 결과 신화 영웅 이미지
- `1단계 / 2단계 / 3단계`
- 재료 목록은 표시하지 않음

영웅 이미지는 기존 HeroBag UI와 같은 방식으로 Presenter가 조회한다.

```text
HeroData.SpriteKey
-> SpriteAtlas
-> "{SpriteKey}_{EvolutionLevel + 1}_Idle"
-> MythicMergeViewer
```

## 14. 클래스 책임

### MythicMergeRepository

- 조합 데이터 보관
- 데이터 작성 순서 유지
- 초기 데이터 검증
- `ResultHeroUID`로 조합 조회

### MythicMergeController

- 필드 영웅으로부터 합성 후보 계산
- 버튼 클릭 시 조합 재검증
- 실제 소비할 Hero 인스턴스 선택
- `IHeroMergeExecutor` 호출

### MythicMergePresenter

- 초기화 직후 후보 UI 갱신
- `OnFieldHeroesChanged` 구독
- Viewer 클릭 이벤트를 Controller에 전달
- HeroData와 SpriteAtlas를 이용한 버튼 이미지 구성
- 합성 판정 로직은 포함하지 않음

### MythicMergeViewer

- 버튼 슬롯 최대 3개 관리
- 후보 표시 및 미사용 슬롯 숨김
- 클릭 이벤트 전달
- 필드 조회나 합성 판정은 하지 않음

### MythicMergeButtonSlot

- Button, Image, 레벨 텍스트 보관
- 현재 `ResultHeroUID + EvolutionLevel` 보관
- 클릭 이벤트 발생

### HeroController / IHeroMergeExecutor

- 필드 Dictionary와 타일 점유 상태 검증
- 재료 제거와 생명주기 이벤트 처리
- 결과 영웅 소환
- 합성 완료 이벤트 발생

## 15. 선택 상태 정리

신화 합성 재료로 선택 중인 영웅이 소비될 수 있으므로 오래된 Hero 참조를 제거한다.

- 소비 대상이 `HeroController._clickedHero`이면 드래그 상태를 초기화한다.
- 타일 마커를 숨긴다.
- `OnDestroyHero` 발생 시 영웅 상호작용 UI를 숨긴다.
- `OnDestroyHero` 발생 시 공격 범위 UI를 숨긴다.

## 16. Scene 및 부트스트랩 연결

- Scene에 버튼 슬롯을 총 3개 배치한다.
- `GameSceneBootStrapper`에 Viewer 참조를 연결한다.
- DataManager, Repository, Controller, Presenter 순서로 초기화한다.
- HeroSpawner의 결과 소환 이벤트는 기존과 같이 `HeroController.AddFieldHero`로 연결한다.

## 17. 테스트 항목

- 재료가 부족하면 후보가 생성되지 않는다.
- 모든 재료가 있어도 EvolutionLevel이 다르면 후보가 생성되지 않는다.
- 동일 UID 여러 마리 요구 조합의 Count를 정확히 검사한다.
- 같은 조합이 여러 EvolutionLevel에서 가능하면 단계별 후보를 생성한다.
- 데이터 순서와 EvolutionLevel 오름차순을 유지한다.
- 후보가 4개 이상이어도 버튼은 3개만 표시한다.
- 버튼 표시 후 필드가 바뀌면 클릭 재검증에서 실패한다.
- 저장 데이터가 없는 결과 영웅은 빠른 후보와 패널 목록에서 제외된다.
- 실행 직전에 결과 영웅의 저장 데이터를 다시 검사한다.
- 전체 재료 수량이 4개를 초과하면 초기화에 실패한다.
- 결과 영웅과 같은 UID가 재료에 있으면 초기화에 실패한다.
- 같은 재료가 많으면 SpawnIndex가 낮은 영웅부터 소비한다.
- 가장 낮은 SpawnIndex 영웅의 타일에 결과가 생성된다.
- 재료마다 OnDestroyHero가 한 번씩 발생한다.
- 결과 영웅의 OnSpawnedHero가 한 번 발생한다.
- 집계 이벤트는 합성 완료 후 한 번씩만 발생한다.
- 선택된 영웅이 소비되면 상호작용 UI와 범위 UI가 사라진다.
- 잘못된 MythicMergeData는 초기화 단계에서 실패한다.

## 18. 신화 조합 목록 패널 목적

빠른 합성 버튼과 별도로 전체 해금 신화 영웅의 조합과 재료 상태를 확인하고 직접 합성할 수 있는 패널을 제공한다.

- 기존 최대 3개 후보 버튼은 빠른 합성 용도로 유지한다.
- 패널은 저장 데이터가 있는 결과 신화 영웅만 표시한다.
- 잠긴 신화 영웅은 비활성 카드로 표시하지 않고 목록에서 숨긴다.
- 해금 상태는 로비에서만 변경되므로 게임 Scene 초기화 시 목록을 한 번 구성한다.
- 패널과 빠른 후보는 동일한 `MythicMergeController.TryMerge()`를 사용한다.

## 19. 패널 목록과 진행률

하단 목록은 `ScrollRect + GridLayoutGroup`으로 구성한다.

- 목록 아이템은 프리팹을 직렬화 참조하여 초기화 시 한 번 생성한다.
- `Resources.Load`를 사용하지 않는다.
- 목록은 `MythicMergeData.json` 순서를 유지한다.
- 목록 이미지는 항상 EvolutionLevel 0 Idle Sprite를 사용한다.
- 선택한 아이템은 선택 테두리로 표시한다.
- 어느 단계도 완성되지 않았다면 세 단계 중 최고 진행률을 백분율로 표시한다.
- 어느 단계든 완성됐다면 백분율 대신 합성 가능 아이콘을 표시한다.
- 진행률은 단계별 `보유 슬롯 수 / 전체 재료 슬롯 수 * 100`이며 소수점은 버린다.
- 서로 다른 EvolutionLevel의 재료를 합산하지 않는다.
- 진행률에 따라 목록 순서를 변경하지 않는다.

권장 단계는 다음 순서로 결정한다.

1. 완성된 단계가 있으면 가장 낮은 완성 단계
2. 완성된 단계가 없으면 진행률이 가장 높은 단계
3. 진행률이 같으면 낮은 단계

## 20. 패널 상세 영역

패널 상단은 선택한 결과 영웅과 단계의 상세 상태를 표시한다.

- 결과 영웅 이름
- 결과 영웅의 `HeroSaveData.Level`
- 선택한 EvolutionLevel로 계산한 결과 영웅의 `CurrentGrade`
- 선택한 EvolutionLevel의 결과 영웅 이미지
- `1단계 / 2단계 / 3단계` 선택 버튼
- 재료 슬롯 최대 4개
- 소환 버튼

단계 버튼은 항상 선택할 수 있다.

- UI 문구 `1단계 / 2단계 / 3단계`는 내부 EvolutionLevel `0 / 1 / 2`에 대응한다.
- 선택한 단계는 별도 표시한다.
- 해당 단계가 합성 가능하면 단계 버튼에 체크 아이콘을 표시한다.
- 단계 버튼에는 진행률을 표시하지 않는다.
- 등급 시스템과 무관하게 단계 문구를 유지한다. 현재 등급은 별도 영웅 정보로 표시한다.

재료 슬롯은 Scene에 4개를 고정 배치한다.

- JSON의 Materials 작성 순서를 유지한다.
- 같은 UID의 Count는 해당 위치에서 연속된 슬롯으로 펼친다.
- 보유 슬롯은 원래 색상, 체크 아이콘, `보유`를 표시한다.
- 미보유 슬롯은 어둡게 표시하고 체크 없이 `미보유`를 표시한다.
- 조합에서 사용하지 않는 나머지 슬롯은 비활성화한다.
- 현재 보유 수량 숫자는 표시하지 않는다.

## 21. 패널 선택과 갱신

- 최초에는 표시 가능한 JSON 첫 조합을 선택한다.
- 최초 단계는 해당 조합의 권장 단계를 사용한다.
- 다른 조합을 선택하면 그 조합의 권장 단계를 선택한다.
- 패널을 다시 열 때는 마지막 결과 영웅과 단계를 유지한다.
- 필드 상태가 바뀌어도 현재 선택은 유지하고 표시 상태만 갱신한다.
- 패널이 열려 있을 때만 전체 카드 진행률과 상세 영역을 갱신한다.
- 패널이 닫혀 있으면 다음에 열 때 한 번 계산한다.
- 단순 위치 이동에는 갱신하지 않고 `OnFieldHeroesChanged`만 사용한다.

소환 버튼은 선택한 단계의 모든 재료가 있을 때만 활성화한다.

- 확인 팝업 없이 즉시 합성을 요청한다.
- 클릭 시 현재 필드와 결과 영웅 해금 상태를 다시 검사한다.
- 성공 시 `OnFieldHeroesChanged`로만 갱신한다.
- 실패 시 Presenter가 즉시 다시 조회한다.
- 성공 또는 실패 후에도 패널과 선택 상태를 유지한다.

## 22. 패널 열기, 닫기 및 시간 제어

`MythicMergePanelViewer`는 항상 활성화된 UI 부모에 배치하고 실제 패널 Root만 숨긴다.

- Unity Button 리스너는 Viewer가 등록한다.
- Viewer는 `OnOpenRequested`, `OnCloseRequested` 이벤트만 발생시킨다.
- Presenter가 데이터 조회, 표시 상태, 일시정지를 관리한다.
- 표시 가능한 조합이 없으면 열기 버튼을 비활성화한다.
- 패널을 열면 게임 시간을 정지한다.
- 전체 화면 Raycast 차단으로 뒤쪽 UI, 영웅, 타일 입력을 막는다.
- 우측 상단 닫기 버튼과 Esc로만 닫는다.
- 패널 바깥 클릭으로는 닫지 않는다.
- 닫을 때 이전 게임 속도를 복구한다.
- UI 입력과 애니메이션은 UnscaledTime을 사용한다.

기존 `ITimeService`를 사용하고 `TimeController`를 보강한다.

- 기본 게임 속도는 1이다.
- 정지 중 SpeedUp을 호출해도 Time.timeScale은 0을 유지한다.
- TimeViewer에는 설정된 속도 1~3만 전달한다.
- `MythicMergePanelPresenter.Dispose()`는 열린 패널의 일시정지를 해제한다.
- `GameSceneBootStrapper.OnDestroy()`에서 Dispose를 호출한다.

## 23. 패널 클래스 책임

### MythicMergeRecipeSummary

- ResultHeroUID
- 최고 진행률
- 합성 가능 여부
- 권장 EvolutionLevel
- 단계별 합성 가능 여부

### MythicMergeRecipeDetail

- ResultHeroUID
- 선택 EvolutionLevel
- 재료 슬롯별 HeroUID와 IsOwned
- 진행률과 전체 합성 가능 여부

### MythicMergePanelPresenter

- 선택 ResultHeroUID와 EvolutionLevel 보관
- 해금된 목록의 이름, 레벨, Sprite 구성
- Viewer 이벤트 처리
- 열린 상태의 필드 변경 갱신
- 일시정지 요청과 종료 정리

### MythicMergePanelViewer

- 열기/닫기 버튼과 패널 Root 관리
- 목록 아이템 초기 생성과 상태 출력
- 결과 정보, 단계 버튼, 재료 슬롯, 소환 버튼 출력
- 도메인 판정 없이 사용자 입력 이벤트 전달

### MythicMergeListItem

- 결과 영웅 이미지
- 진행률 또는 합성 가능 아이콘
- 선택 테두리
- ResultHeroUID 클릭 이벤트

### MythicMergeEvolutionButton

- 단계 문구
- 선택 표시
- 단계 합성 가능 아이콘
- EvolutionLevel 클릭 이벤트

### MythicMergeMaterialSlot

- 재료 영웅 이미지
- 보유 체크 아이콘
- 보유/미보유 텍스트

## 24. 패널 테스트 항목

- 저장 데이터가 없는 결과 영웅은 목록에 나타나지 않는다.
- 표시 가능한 조합이 없으면 열기 버튼이 비활성화된다.
- 같은 UID Count를 슬롯 단위로 정확히 펼친다.
- 단계별 진행률이 섞이지 않고 최고 진행률을 표시한다.
- 완성 조합은 백분율 대신 아이콘을 표시한다.
- 완성 단계와 최고 진행률 단계 선택 규칙이 동작한다.
- 단계 버튼은 재료가 부족해도 선택할 수 있다.
- 선택 단계에 따라 결과와 재료 Sprite가 변경된다.
- 합성 성공 후 패널과 선택 상태가 유지된다.
- 패널을 열면 게임이 정지하고 닫으면 기존 속도로 복구된다.
- 정지 중 SpeedUp으로 게임이 재개되지 않는다.
- Scene 종료 시 Time.timeScale이 0으로 남지 않는다.

## 25. 구현 반영 범위와 준비 항목

현재 코드와 Scene에는 다음 항목이 반영되어 있다.

- `MythicMergeData.json` Addressables 로드와 Repository 검증
- 빠른 합성 후보 최대 3개 계산 및 기존 Scene 슬롯 연결
- 패널 목록, 단계 선택, 재료 슬롯, 직접 소환, 진행률 계산
- 결과 영웅 저장 데이터 유무에 따른 빠른 후보와 패널 목록 필터링
- 합성 직전 재검증, SpawnIndex 기준 소비와 소환 위치 결정
- 패널 열기/닫기 시 게임 정지와 기존 속도 복구
- `MythicMergePanel.prefab`, `MythicMergeListItem.prefab` 생성 및 `SampleScene` 연결
- `Resources.Load` 없이 Scene과 직렬화된 프리팹 참조 사용

실제 콘텐츠 적용을 위해 데이터/리소스 작성자가 준비할 항목은 다음과 같다.

- `MythicMergeData.json`의 실제 조합 데이터. 현재 파일은 빈 배열이므로 데이터 입력 전에는 빠른 후보와 패널 목록이 표시되지 않는다.
- 표시할 결과 신화 영웅의 `HeroSaveData`. 저장 데이터가 없으면 해당 조합은 숨겨지고 소환할 수 없다.
- 결과와 모든 재료 영웅의 HeroData, SkillSetContainer, SpriteAtlas
- 각 영웅의 `"{SpriteKey}_1_Idle"`부터 `"{SpriteKey}_3_Idle"`까지의 Sprite
- 한글 글리프를 포함한 TMP FontAsset. 현재 기본 `LiberationSans SDF`는 한글을 지원하지 않으므로 패널과 목록 프리팹의 TMP 텍스트에 실제 한글 폰트를 지정해야 한다.
- `MergeableIcon`, `OwnedIcon`에 사용할 실제 체크 Sprite와 최종 버튼/패널 아트

데이터와 최종 UI 리소스가 준비되기 전에도 C# 컴파일과 직렬화 참조 검증은 가능하지만, 실제 조합 합성과 최종 화면 표시는 콘텐츠 입력 후 Play Mode에서 확인한다.

## 26. 등급별 스킬 재구성 연계

상세 규칙과 오류 정책은 [HeroGradeSkillSetDesign.md](HeroGradeSkillSetDesign.md)를 기준으로 한다.

- `CurrentGrade = BaseGrade + EvolutionLevel`로 계산한다.
- 신화 합성 결과는 다른 UID의 신규 Hero이므로 재료의 스킬과 런타임 상태를 계승하지 않는다.
- 결과 Hero는 자신의 저장 `Level`과 현재 등급에 맞는 SkillController를 생성한 뒤 필드에 등록한다.
- 동일 UID 일반 진화는 새 등급 SkillController를 먼저 준비하고 기존 Hero의 컨트롤러를 교체한다.
- 신화 조합과 후보 버튼의 단계 표기는 항상 `1단계/2단계/3단계`를 유지한다.

예를 들어 40레벨 D 영웅이 C로 진화하면 기존 D등급 스킬 구성을 제거하고 C등급 표의 Lv1~Lv40 구성으로 다시 생성한다.
