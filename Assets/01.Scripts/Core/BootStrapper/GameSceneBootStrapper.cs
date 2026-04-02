using UnityEngine;
using Map;
using Heros;
using Heros.UI;
using Enemies;
using Stage;
using Core.Scene;
using Skill;
using Combat;

namespace Core.BootStrapper
{
    public class GameSceneBootStrapper : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private GameSceneManager _sceneManager;
        [SerializeField] private HeroManager _heroManager;
        [SerializeField] private EnemyManager _enemyManager;
        [SerializeField] private StageManager _stage;

        [Header("Map")]
        [SerializeField] private MapBoard _map;

        [Header("HUD")]
        [SerializeField] private HeroSummmonPresenter _heroSummonPresenter;
        [SerializeField] private DamageViewer _damageViewer;

        private SkillContext _skillContext = new SkillContext();
        private BattleManager _battleManager = new BattleManager();
        private HeroSkillFactory _heroSkillFactory = new HeroSkillFactory();

        private void Start()
        {
            Init();
            Wire();
        }

        private void Init()
        {
            _sceneManager.Init(_stage, _enemyManager);

            _map.Init();

            _heroManager.Init(_map, _heroSkillFactory, GameManager.Data);

            int stageUID = GameManager.Payload.StageUID;
            _enemyManager.Init(_map, stageUID);
            _stage.Init(_enemyManager, stageUID);

            _heroSummonPresenter.Init(_heroManager);

            _heroSkillFactory.Init(_skillContext, GameManager.Data);

            _damageViewer.Init();
        }
        private void Wire()
        {
            _skillContext.Register<IFieldEnemyService>(_enemyManager);
            _skillContext.Register<IAttackRegister>(_battleManager);

            _battleManager.OnApplyDamage += _damageViewer.ShowDamageText;
        }
    }
}
