using Entity;
using Map;
using Skill;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.U2D;

namespace Heros
{
    public interface IHeroSpawnService
    {
        void SpawnRandomHero();
    }

    public struct HeroSkillBundle
    {
        public int BaseSkill;
        public int FirstSkill;
        public int SecondSkill;
        public int SpecialSkill;

        public HeroSkillBundle(int baseSkill, int first, int second, int special)
        {
            BaseSkill = baseSkill;
            FirstSkill = first;
            SecondSkill = second;
            SpecialSkill = special;
        }
    }

    public class HeroManager : MonoBehaviour, IHeroSpawnService
    {
        public int SpawnHeroUid;
        
        [SerializeField] private Hero _heroPrefab;

        private Dictionary<Tile, Hero> _fieldHeros = new Dictionary<Tile, Hero>();
        private Hero _clickedHero = null;

        IHeroMapService _heroMapService;
        ISkillCreater _skillCreater;
        ISpriteAtlasRepository _spriteAtlasRepository;

        public void Init(IHeroMapService heroMapService, ISkillCreater skillCreater, ISpriteAtlasRepository spriteAtlasRepository)
        {
            _heroMapService = heroMapService;
            _skillCreater = skillCreater;
            _spriteAtlasRepository = spriteAtlasRepository;
        }
        public void SpawnRandomHero()
        {
            if (_heroMapService.TryGetNextHeroTile(out Tile tile))
            {
                var hero = GetHero();

                SkillContext ownerContext = new SkillContext();
                ownerContext.Register<Transform>(hero.transform);
                ownerContext.Register<IHeroInfoProvider>(hero);
                ownerContext.Register<ISpriteChanger>(hero.SpriteChanger);
                ownerContext.Register<IAttackStatProvider>(hero);

                HeroData data = GameManager.Data.GetHeroData(SpawnHeroUid);
                ATKData atkData = GameManager.Data.GetATKData(data.ATKUID);
                SpriteAtlas heroAtlas = _spriteAtlasRepository.GetHeroSpriteAtlas(data.UID);

                hero.SetData(data, atkData, heroAtlas);
                hero.SetTile(tile, _heroMapService.GetTileWorldPosition(tile));

                HeroSkillBundle skillBundle = new HeroSkillBundle(data.BaseAttack, data.FirstSkill, data.SecondSkill, data.SpecialSkill);
                List<ISkill> skills = _skillCreater.CreateActiveSkill(skillBundle, ownerContext);

                hero.SetSkill(skills);

                _fieldHeros.Add(tile, hero);
            }
        }
        private Hero GetHero()
        {
            Hero hero = Instantiate(_heroPrefab);
            hero.SpawnInit();
            hero.OnOccupiedTile += _heroMapService.OccupyHeroTile;
            hero.OnFreeTile += _heroMapService.FreeHeroTile;
            return hero;
        }

        public void OnPointUpHero(Tile tile)
        {
            if (_clickedHero == null) return;
            
            if(_fieldHeros.TryGetValue(tile, out var hero))
            {
                if (hero != _clickedHero) return;                
            }

            _clickedHero.SetTile(tile, _heroMapService.GetTileWorldPosition(tile));
        }
        public void OnPointDownHero(Tile tile)
        {
            if (_fieldHeros.TryGetValue(tile, out var hero))
            {
                _clickedHero = hero;
            }
        }
    }
}
