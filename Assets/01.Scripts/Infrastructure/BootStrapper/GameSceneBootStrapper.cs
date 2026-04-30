using UnityEngine;
using Map;
using Heros;
using Heros.UI;
using Enemies;
using Stage;
using Core.Scene;
using Skill;
using Combat;
using UnityEngine.UI;
using System;
using UnityEngine.U2D;

namespace Core.BootStrapper
{
    public class GameSceneBootStrapper : MonoBehaviour
    {
        [Header("Hero")]
        [SerializeField] private HeroManager _heroManager;
        [SerializeField] private HeroSpawner _heroSpawner;
        private HeroSkillFactory _heroSkillFactory = new HeroSkillFactory();

        [SerializeField] private HeroSelecter _heroSelecter;

        [SerializeField] private HeroSummonViewer _heroSummonViewer;
        private HeroSummmonPresenter _heroSummonPresenter = new HeroSummmonPresenter();

        private MergeRepository _mergeRepository = new MergeRepository();
        private HeroOverlapProcessor _heroOverlapProcessor = new HeroOverlapProcessor();

        [Header("Enemy")]
        [SerializeField] private EnemySpawner _enemySpawner;
        private EnemySpriteRepository _enemySpriteRepository = new EnemySpriteRepository();
        private EnemyTracker _enemyTracker = new EnemyTracker();

        [Header("Skill")]
        [SerializeField] private BuffManager _buff;
        [SerializeField] private ProjectileSpawner _projectileSpawner;
        [SerializeField] private SummonSpawner _summonSpawner;
        [SerializeField] private VFXSpawner _vfxSpawner;
        private SkillContext _skillContext = new SkillContext();
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
        [SerializeField] private MapBoard _map;

        [Header("Time")]
        [SerializeField] private TimeViewer _timeViewer;
        private TimeController _timeController = new TimeController();
        private TimePresenter _timePresenter = new TimePresenter();


        private void Start()
        {
            Init();
            Bind();
        }

        private void Init()
        {
            var PlayerConfig = GameManager.Data.PlayerConfig;

            var deck = PlayerConfig.HeroDecks[0];

            _timePresenter.Init(_timeController, _timeViewer);
            _sceneManager.Init(_stage);
            _map.Init();
            _heroManager.Init(_heroOverlapProcessor, _map, deck, _heroSpawner);
            _heroSpawner.Init(_heroSkillFactory, GameManager.Data);

            _enemySpawner.Init(_map, _enemySpriteRepository, GameManager.Data);

            var vfxAtlas = GameManager.Data.GetSpriteAtlas("HitEffect");

            _vfxRepository.Init(vfxAtlas);
            _projectileRepository.Init(GameManager.Data.GetProjectileAtlas());

            int stageUID = GameManager.Payload.StageUID;
            SpriteAtlas enemyAtlas = GameManager.Data.GetEnemyAtlas(stageUID);
            _enemySpriteRepository.Init(enemyAtlas);

            var stageConfig = GameManager.Data.StageConfig;
            var stage = GameManager.Data.GetStageData(stageUID);
            _stage.Init(_enemySpawner, _enemyTracker, stage, stageConfig);

            var economy = GameManager.Data.GameEconomy;
            _economy.Init(economy.StartMoney);

            _heroSummonPresenter.Init(_heroSpawner, _heroSummonViewer, _economy, economy);

            _stageInfoPresenter.Init(_stage, _stageInfoViewer);

            _heroSkillFactory.Init(_skillContext, GameManager.Data);

            _damageViewer.Init();

            _vfxSpawner.Init(_vfxRepository);

            _battleManager.Init(_vfxSpawner);

            _rewardSystem.Init(_economy);

            var data = GameManager.Data;

            _projectileSpawner.Init(data, _projectileRepository);
            _summonSpawner.Init(_projectileRepository, data);
            _buff.Init(data);
            _mergeRepository.Init(GameManager.Data.MergeData);
            _heroOverlapProcessor.Init(_mergeRepository);
        }
        private void Bind()
        {
            _skillContext.Register<IFieldEnemyService>(_enemyTracker);
            _skillContext.Register<IAttackRegister>(_battleManager);
            _skillContext.Register<IProjectileProvider>(_projectileSpawner);
            _skillContext.Register<ISummonProvider>(_summonSpawner);
            _skillContext.Register<IBuffRegister>(_buff);

            _battleManager.OnApplyDamage += _damageViewer.ShowDamageText;

            _heroSelecter.OnPointDownTile += _heroManager.OnPointDown;
            _heroSelecter.OnPointUpTile += _heroManager.OnPointUp;

            _stage.OnChangeCurrentWave += _stageInfoPresenter.UpdateWave;
            _stage.OnChangeRemainTime += _stageInfoPresenter.UpdateWaveTime;

            _enemySpawner.OnSpawnEnemy += _enemyTracker.AddFieldEnemy;

            _economy.OnChangeMoney += _economyViewer.UpdateMoneyText;

            _enemyTracker.OnEnemyDeath += _rewardSystem.OccurRewards;
        }
    }
}
