using Skill.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Enemies;
using WhatMerge.Heros;

namespace Skill
{
    public interface IFinder
    {
        float Range { get; }
        bool TryGetTargets(Vector3 pivot, out IReadOnlyList<ICombatant> targets);
    }

    public class SelfTargetFinder : IFinder
    {
        private readonly ICombatant _owner;
        private readonly IReadOnlyList<ICombatant> _ownerTarget;

        public float Range => 0;

        public SelfTargetFinder(ICombatant owner)
        {
            _owner = owner;
            _ownerTarget = new[] { owner };
        }

        public bool TryGetTargets(Vector3 pivot, out IReadOnlyList<ICombatant> targets)
        {
            if (!_owner.IsActive)
            {
                targets = Array.Empty<ICombatant>();
                return false;
            }

            targets = _ownerTarget;
            return true;
        }
    }

    public class NearHeroFinder : IFinder
    {
        private readonly int _range;
        private readonly Hero _owner;
        private readonly IFieldHeroService _fieldHeroService;

        public float Range => _range;

        public NearHeroFinder(IFieldHeroService fieldHeroService, Hero owner, int range)
        {
            _fieldHeroService = fieldHeroService;
            _owner = owner;
            _range = range;
        }

        public bool TryGetTargets(Vector3 pivot, out IReadOnlyList<ICombatant> targets)
        {
            if (_owner.OccupiedTile == null)
            {
                targets = Array.Empty<ICombatant>();
                return false;
            }

            IReadOnlyList<Hero> heroes = _fieldHeroService.GetNearHeros(
                _owner.OccupiedTile,
                (HeroSearchType)_range);

            return FinderResult.TryCopyActiveTargets(heroes, out targets);
        }
    }

    public class AllHeroFinder : IFinder
    {
        private readonly IFieldHeroService _fieldHeroService;

        public float Range => 0;

        public AllHeroFinder(IFieldHeroService fieldHeroService)
        {
            _fieldHeroService = fieldHeroService;
        }

        public bool TryGetTargets(Vector3 pivot, out IReadOnlyList<ICombatant> targets)
        {
            return FinderResult.TryCopyActiveTargets(
                _fieldHeroService.GetAllFieldHero,
                out targets);
        }
    }

    public class NearEnemyFinder : IFinder
    {
        private readonly float _radius;

        public float Range => _radius;

        public NearEnemyFinder(float radius)
        {
            _radius = radius;
        }

        public bool TryGetTargets(Vector3 pivot, out IReadOnlyList<ICombatant> targets)
        {
            IReadOnlyList<Enemy> enemies = SearchUtility.GetNearEnemies(pivot, _radius);
            return FinderResult.TryCopyActiveTargets(enemies, out targets);
        }
    }

    public class AllEnemyFinder : IFinder
    {
        private readonly IFieldEnemyService _fieldEnemyService;

        public float Range => 0;

        public AllEnemyFinder(IFieldEnemyService fieldEnemyService)
        {
            _fieldEnemyService = fieldEnemyService;
        }

        public bool TryGetTargets(Vector3 pivot, out IReadOnlyList<ICombatant> targets)
        {
            return FinderResult.TryCopyActiveTargets(
                _fieldEnemyService.GetAllFieldEnemy,
                out targets);
        }
    }

    internal static class FinderResult
    {
        public static bool TryCopyActiveTargets<T>(
            IReadOnlyList<T> candidates,
            out IReadOnlyList<ICombatant> targets)
            where T : class, ICombatant
        {
            if (candidates == null || candidates.Count == 0)
            {
                targets = Array.Empty<ICombatant>();
                return false;
            }

            List<ICombatant> activeTargets = new List<ICombatant>(candidates.Count);

            for (int i = 0; i < candidates.Count; i++)
            {
                T candidate = candidates[i];
                if (candidate != null && candidate.IsActive)
                {
                    activeTargets.Add(candidate);
                }
            }

            targets = activeTargets;
            return activeTargets.Count > 0;
        }
    }
}
