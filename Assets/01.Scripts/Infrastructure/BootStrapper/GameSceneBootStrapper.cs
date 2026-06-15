using UnityEngine;
using Map;
using Heros;
using Heros.UI;
using Stage;
using Core.Scene;
using Skill;
using Combat;
using UnityEngine.U2D;
using Skill.Summon;
using Skill.Projectile;
using WhatMerge.Enemies;
using WhatMerge.Combat;

namespace Core.BootStrapper
{
    public class GameSceneBootStrapper : MonoBehaviour
    {
        [Header("Hero")]
        [SerializeField] private HeroRangeViewer _heroRangeViewer;
        [SerializeField] private HeroBagViewer _bagViewer;
        [SerializeField] private HeroSpawner _heroSpawner;
        [SerializeField] private HeroSummonViewer _heroSummonViewer;
        [SerializeField] private HeroClickInteractPresenter _heroClickInteractViewer;

        private HeroController _heroController = new HeroController();
        private MergeRepository _mergeRepository = new MergeRepository();
        private HeroOverlapProcessor _heroOverlapProcessor = new HeroOverlapProcessor();
        private HeroSummmonPresenter _heroSummonPresenter = new HeroSummmonPresenter();
        private HeroBag _heroBag = new HeroBag();
        private HeroBagPresenter _bagPresenter = new HeroBagPresenter();

        [Header("Map")]
        [SerializeField] private MapBoard _map;
        [SerializeField] private TileClicker _heroClicker;
        [SerializeField] private TileMarkerPresenter _tileMarkerPresenter;

        [Header("Enemy")]
        [SerializeField] private EnemySpawner _enemySpawner;
        private EnemySpriteRepository _enemySpriteRepository = new EnemySpriteRepository();
        private FieldEnemyService _fieldEnemyService = new FieldEnemyService();

        [Header("Skill")]
        [SerializeField] private BuffManager _buff;
        [SerializeField] private ProjectileSpawner _projectileSpawner;
        [SerializeField] private SummonSpawner _summonSpawner;
        [SerializeField] private VFXSpawner _vfxSpawner;
        private SkillServiceLocate _skillContext = new SkillServiceLocate();
        private VFXSpriteRepository _vfxRepository = new VFXSpriteRepository();
        private VFXSpriteRepository _projectileRepository = new VFXSpriteRepository();

        [Header("Battle")]
        [SerializeField] private DamageTextSpawner _damageViewer;
        [SerializeField] private EconomyViewer _economyViewer;
        private BattleManager _battleManager = new BattleManager();
        private RewardSystem _rewardSystem = new RewardSystem();

        [Header("Stage")]
        private StagePresenter _stageInfoPresenter = new StagePresenter();
        [SerializeField] private StageManager _stage;
        [SerializeField] private StageViewer _stageInfoViewer;

        [Header("World")]
        [SerializeField] private GameSceneManager _sceneManager;
        private GameEconomySystem _economy = new GameEconomySystem();
       

        [Header("Time")]
        [SerializeField] private TimeViewer _timeViewer;
        private TimeController _timeController = new TimeController();
        private TimePresenter _timePresenter = new TimePresenter();

        [Header("Mock")]
        private SkillFactory _skillFactory = new SkillFactory();

        [Header("Facade")]
        private SkillCommonContext _skillExecutionService;

        private void Start()
        {
            Init();
            Bind();
        }

        private void Init()
        {
            var PlayerConfig = GameManager.Data.PlayerConfig;

            var economyConfig = GameManager.Data.GameEconomy;

            var data = GameManager.Data;

            var resource = GameManager.Resource;

            var deck = PlayerConfig.HeroDecks[0];

            var playerData = GameManager.NetworkData.GetPlayerData();

            _timePresenter.Init(_timeController, _timeViewer);
            _sceneManager.Init(_stage);
            _map.Init();

            //TODO
            #region Test
            _skillExecutionService = new SkillCommonContext(_projectileSpawner, _battleManager, _heroController, _fieldEnemyService);
            _skillFactory.Init(_skillExecutionService);
            _heroSpawner.factory = _skillFactory;

            #endregion


            #region Hero Init

            _heroController.Init(_heroSpawner, _heroOverlapProcessor, _map, _tileMarkerPresenter, _economy);
            _heroSpawner.Init(_map, resource, data, playerData.GetSelectHeroDeck());
            _mergeRepository.Init(GameManager.Data.MergeData);
            _heroOverlapProcessor.Init(_mergeRepository);
            _heroSummonPresenter.Init(_heroSpawner, _heroSummonViewer, _economy, economyConfig);

            _heroClickInteractViewer.Init(_heroBag, _heroController);

            _bagPresenter.Init(_heroBag, _bagViewer, resource);

            _heroBag.Init(3, _heroSpawner);

            #endregion

            _enemySpawner.Init(_map, _enemySpriteRepository, GameManager.Data);

            var vfxAtlas = GameManager.Resource.GetAtlas("HitEffect");

            _vfxRepository.Init(vfxAtlas);
            _projectileRepository.Init(GameManager.Resource.GetAtlas("Projectile"));

            int stageUID = GameManager.Payload.StageUID;
            SpriteAtlas enemyAtlas = GameManager.Resource.GetAtlas($"Stage{stageUID}");
            _enemySpriteRepository.Init(enemyAtlas);

            var stageConfig = GameManager.Data.StageConfig;
            var stage = GameManager.Data.GetStageData(stageUID);
            _stage.Init(_enemySpawner, _fieldEnemyService, stage, stageConfig);

            _economy.Init(economyConfig.StartMoney);

            _stageInfoPresenter.Init(_stage, _stageInfoViewer);

            _damageViewer.Init();

            _vfxSpawner.Init(_vfxRepository);

            _battleManager.Init(_vfxSpawner, _summonSpawner, _buff);

            _rewardSystem.Init(_economy);

            _projectileSpawner.Init(_projectileRepository, _battleManager);
            _summonSpawner.Init(_projectileRepository, _battleManager);
            _buff.Init(data);
        }
        private void Bind()
        {
            _skillContext.Register<IFieldEnemyService>(_fieldEnemyService);
            _skillContext.Register<ICombatService>(_battleManager);
            _skillContext.Register<IProjectileProvider>(_projectileSpawner);
            _skillContext.Register<ISummonProvider>(_summonSpawner);
            _skillContext.Register<IBuffRegister>(_buff);

            _heroSpawner.OnSpawndRanHero += _heroController.AddFieldHero;

            _battleManager.OnApplyDamage += _damageViewer.ShowDamageText;

            _heroClicker.OnPointDownTile += _heroController.PointDownTile;
            _heroClicker.OnPointDownTile += (tile) => { _heroClickInteractViewer.HideInteractUI(); };

            _heroClicker.OnPointUpTile += _heroController.PointUpTile;
            _heroClicker.OnDragTile += _heroController.DragTile;
            _heroClicker.OnPointDownTile += _=> _heroRangeViewer.HideHeroRange();

            _heroController.OnSelectHero += _heroClickInteractViewer.ShowInteractUI;
            _heroController.OnSelectHero += _heroRangeViewer.ShowHeroRange;

            _stage.OnChangeCurrentWave += _stageInfoPresenter.UpdateWave;
            _stage.OnChangeRemainTime += _stageInfoPresenter.UpdateWaveTime;

            _enemySpawner.OnSpawnEnemy += _fieldEnemyService.AddFieldEnemy;

            _economy.OnChangeMoney += _economyViewer.UpdateMoneyText;

            _fieldEnemyService.OnEnemyDeath += _rewardSystem.OccurRewards;
        }
    }
}
