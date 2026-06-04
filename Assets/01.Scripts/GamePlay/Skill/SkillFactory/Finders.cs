using Enemies;
using Entity;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public interface ITarget
    {
        bool HasTargetInRange(Vector3 pivot);
        IReadOnlyList<Creature> GetTargets(Vector3 pivot);
    }
    public class SelfTargetFinder : ITarget
    {
        private Creature _owner;

        public SelfTargetFinder(Creature owner)
        {
            _owner = owner;
        }

        public IReadOnlyList<Creature> GetTargets(Vector3 pivot)
        {
            return new List<Creature> { _owner };
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            if (_owner.IsActive)
                return true;

            return false;
        }
    }
    public class NearHeroFinder : ITarget
    {
        private float _range;
        private Hero _owner;
        private IFieldHeroService _fieldHero;
        public NearHeroFinder(IFieldHeroService fieldHero, Hero owner, float range)
        {
            _fieldHero = fieldHero;
            _owner = owner;
            _range = range;
        }
        public IReadOnlyList<Creature> GetTargets(Vector3 pivot)
        {
            var heros = _fieldHero.GetNearHeros(_owner.OccupiedTile, (int)_range);

            List<Creature> results = new List<Creature>();

            foreach (var hero in heros)
            {
                results.Add(hero);
            }

            return results;
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            return _fieldHero.GetNearHeros(_owner.OccupiedTile, (int)_range).Count > 0;
        }
    }
    public class AllHeroFinder : ITarget
    {
        private IFieldHeroService _fieldHeroService;
        public AllHeroFinder(IFieldHeroService fieldHeroService)
        {
            _fieldHeroService = fieldHeroService;
        }
        public IReadOnlyList<Creature> GetTargets(Vector3 pivot)
        {
            return _fieldHeroService.GetAllFieldHero;
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            return true;
        }
    }
    public class NearEnemyFinder : ITarget
    {
        private Hero _owner;
        private float _range;
        public NearEnemyFinder(Hero owner, float range)
        {
            _owner = owner;
            _range = range;
        }

        public IReadOnlyList<Creature> GetTargets(Vector3 pivot)
        {
            var enemies = SearchUtility.GetNearAll2DTargets<Enemy>(_owner.Position, _range, LayerMask.GetMask("Enemy"));

            List<Creature> results = new List<Creature>();

            foreach (var enemy in enemies)
            {
                results.Add(enemy);
            }

            return results;
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            return SearchUtility.IsExistEnemyInRange(_owner.Position, _range);
        }
    }
    public class AllEnemyFinder : ITarget
    {
        private IFieldEnemyService _fieldEnemyService;

        public AllEnemyFinder(IFieldEnemyService fieldEnemyService)
        {
            _fieldEnemyService = fieldEnemyService;
        }

        public IReadOnlyList<Creature> GetTargets(Vector3 pivot)
        {
            return _fieldEnemyService.GetAllFieldEnemy;
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            return _fieldEnemyService.GetAllFieldEnemy.Count > 0;
        }
    }
}