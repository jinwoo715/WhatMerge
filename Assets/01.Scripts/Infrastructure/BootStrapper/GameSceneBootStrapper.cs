using UnityEngine;
using WhatMerge.Map;
using WhatMerge.Stage;
using Core.Scene;
using Skill;
using UnityEngine.U2D;
using System.Collections.Generic;
using System;
using WhatMerge.Enemies;
using WhatMerge.Combat;
using WhatMerge.Combat.Effects;
using WhatMerge.Heros;
using WhatMerge.Projectiles;
using WhatMerge.Summons;

namespace Core.BootStrapper
{
    public class GameSceneBootStrapper : MonoBehaviour
    {
        [Header("Hero")]
        [SerializeField] private HeroRangeIndicator _heroRangeViewer;
        [SerializeField] private HeroBagViewer _bagViewer;
        [SerializeField] private HeroSpawner _heroSpawner;
        [SerializeField] private HeroSummonViewer _heroSummonViewer;
        [SerializeField] private HeroClickInteractPresenter _heroClickInteractViewer;
        [SerializeField] private MythicMergeViewer _mythicMergeViewer;
        [SerializeField] private MythicMergePanelViewer _mythicMergePanelViewer;

        private HeroController _heroController = new HeroController();
        private NomalMergeRepository _mergeRepository = new NomalMergeRepository();
        private HeroOverlapProcessor _heroOverlapProcessor = new HeroOverlapProcessor();
        private MythicMergeRepository _mythicMergeRepository = new MythicMergeRepository();
        private MythicMergeController _mythicMergeController = new MythicMergeController();
        private MythicMergePresenter _mythicMergePresenter = new MythicMergePresenter();
        private MythicMergePanelPresenter _mythicMergePanelPresenter = new MythicMergePanelPresenter();
        private HeroSummmonPresenter _heroSummonPresenter = new HeroSummmonPresenter();
        private HeroBag _heroBag = new HeroBag();
        private HeroBagPresenter _bagPresenter = new HeroBagPresenter();

        [Header("Map")]
        [SerializeField] private MapBoard _map;
        [SerializeField] private TileClicker _heroClicker;
        [SerializeField] private TileIndicator _tileMarkerPresenter;

        [Header("Enemy")]
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private EnemyHealthBarManager _enemyHealthBarManager;
        private EnemySpriteRepository _enemySpriteRepository = new EnemySpriteRepository();
        private FieldEnemyService _fieldEnemyService = new FieldEnemyService();

        [Header("Skill")]
        [SerializeField] private BuffManager _buff;
        [SerializeField] private ProjectileSpawner _projectileSpawner;
        [SerializeField] private SummonSpawner _summonSpawner;
        [SerializeField] private VFXSpawner _vfxSpawner;
        private VFXSpriteRepository _vfxRepository = new VFXSpriteRepository();
        private VFXSpriteRepository _projectileRepository = new VFXSpriteRepository();

        [Header("Battle")]
        [SerializeField] private DamageTextSpawner _damageViewer;
        [SerializeField] private EconomyViewer _economyViewer;
        [SerializeField] private EffectProcessor _effectProcessor;
        private CombatService _combatService = new CombatService();
        private DamageCalculator _damageCalculator = new DamageCalculator();
        private RewardSystem _rewardSystem = new RewardSystem();

        [Header("Stage")]
        [SerializeField] private StageManager _stage;
        [SerializeField] private StageViewer _stageInfoViewer;
        [SerializeField] private MidBossInfoPopup _midBossPopup;
        private StagePresenter _stageInfoPresenter = new StagePresenter();

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
        private SkillRuntimeContext _skillRuntimeContext;


        [Header("Effect")]
        [SerializeField] private DotEffectManager _dotEffectManager;
        [SerializeField] private TimeEffectManager _timeEffectManager;
        private DamageApplier _damageApplier = new DamageApplier();

        private void Start()
        {
            try
            {
                Init();
                Bind();
            }
            catch (Exception exception)
            {
                _timeController.FatalStop(exception, "Game scene initialization failed.");
                throw;
            }
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
            _buff.Init(_heroController);

            //TODO
            #region Test
            _skillRuntimeContext = new SkillRuntimeContext(
                _combatService,
                _heroController,
                _fieldEnemyService,
                _vfxSpawner,
                _timeController);
            _skillFactory.Init(_skillRuntimeContext);
            _heroSpawner.factory = _skillFactory;

            #endregion


            #region Hero Init

            _heroController.Init(
                _heroSpawner,
                _heroSpawner,
                _heroOverlapProcessor,
                _map,
                _tileMarkerPresenter,
                _economy,
                _timeController);
            _heroSpawner.Init(
                _map,
                resource,
                data,
                deck,
                data.HeroProgression.MaxLevel,
                _timeController);
            _mergeRepository.Init(GameManager.Data.MergeData);
            _heroOverlapProcessor.Init(_mergeRepository);

            _mythicMergeRepository.Init(GameManager.Data.MythicMergeData, data);
            ValidateHeroDefinitions(deck, data);

            _mythicMergeController.Init(_mythicMergeRepository, _heroController, _heroController, data);
            _mythicMergePresenter.Init(_heroController, _mythicMergeController, _mythicMergeViewer, data, resource);
            _mythicMergePanelPresenter.Init(
                _heroController,
                _mythicMergeController,
                _mythicMergePanelViewer,
                data,
                resource,
                _timeController);
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

            _economy.Init(economyConfig.StartMoney);

            _stageInfoPresenter.Init(
                _stage,
                _stage,
                _stageInfoViewer,
                _midBossPopup,
                data,
                _enemySpriteRepository);
            _enemyHealthBarManager.Init(_enemySpawner, _stage);
            _stage.Init(_enemySpawner, _fieldEnemyService, _economy, 3);

            _damageViewer.Init();

            _vfxSpawner.Init(_vfxRepository);


            List<IEffectHandler> effectHandlers = new List<IEffectHandler>
            {
                new DamageEffectHandler(_damageCalculator, _damageApplier),
                new SummonSpawnEffectHandler(_summonSpawner),
                new ProjectileSpawnEffectHandler(_projectileSpawner),
                new GoldEffectHandler(_economy),
                new ManaRestoreEffectHandler(),
            };

            List<IDurationEffectHandler> durationEffectHandlers = new List<IDurationEffectHandler>
            {
                new DotDurationEffectHandler(_dotEffectManager),
                new SlowDurationEffectHandler(_timeEffectManager),
                new StunDurationEffectHandler(_timeEffectManager),
                new ElementDurationEffectHandler(_timeEffectManager),
                new ArmorReductionDurationEffectHandler(_timeEffectManager),
                new DamageTransferDurationEffectHandler(_timeEffectManager, _damageApplier),
                new BuffDurationEffectHandler(_buff),
            };

            var durationEffectApplier = new DurationEffectApplier(durationEffectHandlers);

            _dotEffectManager.Init(_damageApplier, _damageCalculator, _timeController);
            _timeEffectManager.Init(_timeController);
            _effectProcessor.Init(
                _damageCalculator,
                _vfxSpawner,
                effectHandlers,
                durationEffectApplier,
                _timeEffectManager,
                _damageApplier);
            _combatService.Init(_effectProcessor);

            _rewardSystem.Init(_economy, data);

            _projectileSpawner.Init(_projectileRepository, _combatService, _timeController);
            _summonSpawner.Init(_projectileRepository, _combatService, _timeController);
        }

        private void ValidateHeroDefinitions(HeroDeck deck, DataManager data)
        {
            if (deck?.Heros == null)
                throw new InvalidOperationException("Selected hero deck is null.");

            HashSet<int> referencedHeroUIDs = new HashSet<int>();
            for (int i = 0; i < deck.Heros.Length; i++)
            {
                int heroUID = deck.Heros[i];
                referencedHeroUIDs.Add(heroUID);

                if (!data.TryGetHeroSaveData(heroUID, out _))
                    throw new InvalidOperationException($"Deck hero UID {heroUID} has no save data.");
            }

            for (int i = 0; i < data.MergeData.Count; i++)
            {
                MergeData merge = data.MergeData[i]
                    ?? throw new InvalidOperationException($"Normal merge data at index {i} is null.");
                referencedHeroUIDs.Add(merge.First);
                referencedHeroUIDs.Add(merge.Second);
                referencedHeroUIDs.Add(merge.Result);

                HeroData resultData = data.GetHeroData(merge.Result)
                    ?? throw new InvalidOperationException(
                        $"Normal merge result UID {merge.Result} has no HeroData.");
                if (resultData.BaseGrade != HeroGrade.C)
                {
                    throw new InvalidOperationException(
                        $"Normal merge result UID {merge.Result} must start at C grade.");
                }
            }

            for (int i = 0; i < _mythicMergeRepository.Recipes.Count; i++)
            {
                MythicMergeData recipe = _mythicMergeRepository.Recipes[i];
                referencedHeroUIDs.Add(recipe.ResultHeroUID);

                HeroData resultData = data.GetHeroData(recipe.ResultHeroUID)
                    ?? throw new InvalidOperationException(
                        $"Mythic result UID {recipe.ResultHeroUID} has no HeroData.");
                if (resultData.BaseGrade != HeroGrade.B)
                {
                    throw new InvalidOperationException(
                        $"Mythic result UID {recipe.ResultHeroUID} must start at B grade.");
                }

                for (int materialIndex = 0; materialIndex < recipe.Materials.Count; materialIndex++)
                    referencedHeroUIDs.Add(recipe.Materials[materialIndex].HeroUID);
            }

            foreach (int heroUID in referencedHeroUIDs)
                _heroSpawner.ValidateHeroDefinition(heroUID);
        }

        private void OnDestroy()
        {
            TrySceneCleanup(_mythicMergePanelPresenter.Dispose);
            TrySceneCleanup(() => _heroController.CleanupSceneHeroes(_heroSpawner.ActiveHeroes));
            TrySceneCleanup(_heroController.Dispose);
            TrySceneCleanup(_timeController.Dispose);
        }

        private static void TrySceneCleanup(Action cleanup)
        {
            try
            {
                cleanup?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void Bind()
        {
            _heroSpawner.OnSpawndRanHero += _heroController.AddFieldHero;

            _damageApplier.OnApplyDamage += _damageViewer.ShowDamageText;

            _heroClicker.OnPointDownTile += _heroController.PointDownTile;
            _heroClicker.OnPointDownTile += (tile) => { _heroClickInteractViewer.HideInteractUI(); };

            _heroClicker.OnPointUpTile += _heroController.PointUpTile;
            _heroClicker.OnDragTile += _heroController.DragTile;
            _heroClicker.OnPointDownTile += _=> _heroRangeViewer.HideHeroRange();

            _heroBag.OnInputHero += (_, _) => _heroRangeViewer.HideHeroRange();

            _heroController.OnSelectHero += _heroClickInteractViewer.ShowInteractUI;
            _heroController.OnSelectHero += _heroRangeViewer.ShowHeroRange;
            _heroController.OnSellHeroEvent += _=> _heroClickInteractViewer.HideInteractUI();
            _heroController.OnSellHeroEvent += _=> _heroRangeViewer.HideHeroRange();
            _heroController.OnDestroyHero += _heroClickInteractViewer.HideIfSelected;
            _heroController.OnDestroyHero += _heroRangeViewer.HideIfSelected;

            _enemySpawner.OnSpawnEnemy += _fieldEnemyService.AddFieldEnemy;
            _enemySpawner.OnDeathEnemy += _fieldEnemyService.DeathEnemy;
            _enemySpawner.OnDespawnEnemy += _fieldEnemyService.RemoveFieldEnemy;

            _economy.OnChangeMoney += _economyViewer.UpdateMoneyText;

            _fieldEnemyService.OnEnemyDeath += _rewardSystem.OccurRewards;
        }
    }
}
