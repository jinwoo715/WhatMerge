using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Enemies;
using WhatMerge.Heros;

namespace Skill
{
    public interface ITarget
    {
        float Range { get; }
        bool HasTargetInRange(Vector3 pivot);
        IReadOnlyList<ICombatant> GetTargets(Vector3 pivot);
    }
    public class SelfTargetFinder : ITarget
    {
        private ICombatant _owner;
        public float Range => 0;

        public SelfTargetFinder(ICombatant owner)
        {
            _owner = owner;
        }


        public IReadOnlyList<ICombatant> GetTargets(Vector3 pivot)
        {
            return new List<ICombatant> { _owner };
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
        private int _range;
        private Hero _owner;
        private IFieldHeroService _fieldHero;
        public float Range => _range;

        public NearHeroFinder(IFieldHeroService fieldHero, Hero owner, int range)
        {
            _fieldHero = fieldHero;
            _owner = owner;
            _range = range;
        }

        public IReadOnlyList<ICombatant> GetTargets(Vector3 pivot)
        {
            var heros = _fieldHero.GetNearHeros(_owner.OccupiedTile, _range);

            List<ICombatant> results = new List<ICombatant>();

            foreach (var hero in heros)
            {
                results.Add(hero);
            }

            return results;
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            return _fieldHero.GetNearHeros(_owner.OccupiedTile, _range).Count > 0;
        }
    }
    public class AllHeroFinder : ITarget
    {
        public float Range => 0;
        private IFieldHeroService _fieldHeroService;
        public AllHeroFinder(IFieldHeroService fieldHeroService)
        {
            _fieldHeroService = fieldHeroService;
        }
        public IReadOnlyList<ICombatant> GetTargets(Vector3 pivot)
        {
            return _fieldHeroService.GetAllFieldHero;
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            return true;
        }
    }

    public abstract class NearEnemyFinder : ITarget
    {
        public Transform _owner;
        public float _radius;
        protected Enemy _latestEnemy;
        public float Range => _radius;

        public abstract IReadOnlyList<ICombatant> GetTargets(Vector3 pivot);
        public bool HasTargetInRange(Vector3 pivot)
        {
            if(_latestEnemy == null)
            {
                return SearchUtility.IsExistEnemyInRange(_owner.position, _radius);
            }
            else
            {
                if (IsTargetInRadius())
                {
                    return true;
                }
                else
                {
                    return SearchUtility.IsExistEnemyInRange(_owner.position, _radius);
                }
            }
        }
        public bool IsTargetInRadius()
        {
            if (_latestEnemy == null)
                SetLatestByNearestEnemy();

            return Vector2.Distance(_owner.position, _latestEnemy.Position) <= _radius;
        }
        public void SetLatestByNearestEnemy()
        {
            _latestEnemy = SearchUtility.GetNearestEnemy(_owner.position, _radius);
        }
    }

    public class SingleEnemyFinder : NearEnemyFinder
    {
        public SingleEnemyFinder(Transform owner, float radius)
        {
            _owner = owner;
            _radius = radius;
        }
        public override IReadOnlyList<ICombatant> GetTargets(Vector3 pivot)
        {
            List<ICombatant> results = new List<ICombatant>();

            if (_latestEnemy != null)
            {
                SetLatestByNearestEnemy();
                results.Add(_latestEnemy);
            }
            else
            {
                if (IsTargetInRadius())
                {
                    results.Add(_latestEnemy);
                }
                else
                {
                    SetLatestByNearestEnemy();
                    results.Add(_latestEnemy);
                }
            }
            return results;
        }
    }

    public class ConeEnemyFinder : NearEnemyFinder
    {
        public float _angle;
        public ConeEnemyFinder(Transform owner, float radius, float angle)
        {
            _owner = owner;
            _radius = radius;
            _angle = angle;
        }

        public override IReadOnlyList<ICombatant> GetTargets(Vector3 pivot)
        {
            List<ICombatant> results = new List<ICombatant>();

            Enemy pivotEnemy;

            if (_latestEnemy != null)
            {
                SetLatestByNearestEnemy();
                pivotEnemy = _latestEnemy;
            }
            else
            {
                if (IsTargetInRadius())
                {
                    pivotEnemy = _latestEnemy;
                }
                else
                {
                    SetLatestByNearestEnemy();
                    pivotEnemy = _latestEnemy;
                }
            }

            Vector3 dir = (pivotEnemy.Position - pivot).normalized;
            var enemies = SearchUtility.GetConeEnemies(pivot, dir, _radius, _angle);

            foreach (var enemy in enemies)
            {
                results.Add(enemy);
            }

            return results;
        }
    }

    public class AllEnemyFinder : ITarget
    {
        private IFieldEnemyService _fieldEnemyService;

        public float Range => 0;

        public AllEnemyFinder(IFieldEnemyService fieldEnemyService)
        {
            _fieldEnemyService = fieldEnemyService;
        }

        public IReadOnlyList<ICombatant> GetTargets(Vector3 pivot)
        {
            return _fieldEnemyService.GetAllFieldEnemy;
        }

        public bool HasTargetInRange(Vector3 pivot)
        {
            return _fieldEnemyService.GetAllFieldEnemy.Count > 0;
        }
    }
}
