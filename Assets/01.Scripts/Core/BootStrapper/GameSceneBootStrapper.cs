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
        [Header("Managers")]
        [SerializeField] private GameSceneManager _sceneManager;
        [SerializeField] private HeroManager _heroManager;
        [SerializeField] private StageManager _stage;
        private GameEconomyManager _economy = new GameEconomyManager();

        [Header("Controller")]
        private TimeController _timeController = new TimeController();

        [Header("Ref")]
        EnemySpriteRepository _enemySpriteRepository = new EnemySpriteRepository();

        [Header("World")]
        [SerializeField] private MapBoard _map;
        [SerializeField] private TileSelecter _tileSelecter;
        [SerializeField] private EnemySpawner _enemySpawner;
        private EnemyTracker _enemyTracker = new EnemyTracker();

        [Header("Presenter")]
        private StageInfoPresenter _stageInfoPresenter = new StageInfoPresenter();
        private HeroSummmonPresenter _heroSummonPresenter = new HeroSummmonPresenter();
        private TimePresenter _timePresenter = new TimePresenter();

        [Header("Viewer")]
        [SerializeField] private StageInfoViewer _stageInfoViewer;
        [SerializeField] private HeroSummonViewer _heroSummonViewer;
        [SerializeField] private TimeViewer _timeViewer;
        [SerializeField] private DamageViewer _damageViewer;


        private SkillContext _skillContext = new SkillContext();
        private BattleManager _battleManager = new BattleManager();
        private HeroSkillFactory _heroSkillFactory = new HeroSkillFactory();

        private void Start()
        {
            Init();
            Bind();
        }

        private void Init()
        {
            _timePresenter.Init(_timeController, _timeViewer);
            _sceneManager.Init(_stage);
            _map.Init();
            _heroManager.Init(_map, _heroSkillFactory, GameManager.Data);
            _enemySpawner.Init(_map, _enemySpriteRepository, GameManager.Data);

            int stageUID = GameManager.Payload.StageUID;
            SpriteAtlas enemyAtlas = GameManager.Data.GetEnemyAtlas(stageUID);
            _enemySpriteRepository.Init(enemyAtlas);

            var stageConfig = GameManager.Data.StageConfig;
            var stage = GameManager.Data.GetStageData(stageUID);
            _stage.Init(_enemySpawner, _enemyTracker, stage, stageConfig);

            var economy = GameManager.Data.GameEconomy;
            _economy.Init(economy);
            _heroSummonPresenter.Init(_heroManager, _heroSummonViewer, _economy, economy);

            _stageInfoPresenter.Init(_stage, _stageInfoViewer);

            _heroSkillFactory.Init(_skillContext, GameManager.Data);

            _damageViewer.Init();
        }
        private void Bind()
        {
            _skillContext.Register<IFieldEnemyService>(_enemyTracker);
            _skillContext.Register<IAttackRegister>(_battleManager);

            _battleManager.OnApplyDamage += _damageViewer.ShowDamageText;

            _tileSelecter.OnPointDownTile += _heroManager.OnPointDownHero;
            _tileSelecter.OnPointUpTile += _heroManager.OnPointUpHero;

            _stage.OnChangeCurrentWave += _stageInfoPresenter.UpdateWave;
            _stage.OnChangeRemainTime += _stageInfoPresenter.UpdateWaveTime;

            _enemySpawner.OnSpawnEnemy += _enemyTracker.AddFieldEnemy;
        }
    }
}
