using System;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Stage;

namespace WhatMerge.Enemies
{
    public sealed class EnemyHealthBarManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int _prewarmCount = 20;
        [SerializeField] private Camera _worldCamera;

        private readonly Dictionary<Enemy, EnemyHealthBarView> _activeViews = new();
        private readonly Stack<EnemyHealthBarView> _viewPool = new();
        private readonly List<Enemy> _staleEnemies = new();

        private RectTransform _root;
        private Canvas _canvas;
        private IEnemySpawnService _enemySpawnService;
        private IMidBossChallengeInfo _midBossChallengeInfo;
        private bool _initialized;

        public void Init(
            IEnemySpawnService enemySpawnService,
            IMidBossChallengeInfo midBossChallengeInfo)
        {
            if (_initialized)
                throw new InvalidOperationException("The enemy health bar manager is already initialized.");

            _enemySpawnService = enemySpawnService
                ?? throw new ArgumentNullException(nameof(enemySpawnService));
            _midBossChallengeInfo = midBossChallengeInfo
                ?? throw new ArgumentNullException(nameof(midBossChallengeInfo));

            _root = transform as RectTransform
                ?? throw new InvalidOperationException("The health bar manager must be placed under a Canvas.");
            _canvas = GetComponentInParent<Canvas>()
                ?? throw new InvalidOperationException("The health bar manager requires a Canvas.");
            _worldCamera = _worldCamera != null ? _worldCamera : Camera.main;

            if (_worldCamera == null)
                throw new InvalidOperationException("A world camera is required for enemy health bars.");

            for (int i = 0; i < _prewarmCount; i++)
                _viewPool.Push(CreateView());

            _enemySpawnService.OnSpawnEnemy += HandleEnemySpawn;
            _enemySpawnService.OnDeathEnemy += HandleEnemyRemoved;
            _enemySpawnService.OnDespawnEnemy += HandleEnemyRemoved;
            _midBossChallengeInfo.OnMidBossTimeChanged += HandleMidBossTimeChanged;
            _midBossChallengeInfo.OnMidBossChallengeEnded += HandleMidBossChallengeEnded;
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized)
                return;

            Camera canvasCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;

            _staleEnemies.Clear();

            foreach (KeyValuePair<Enemy, EnemyHealthBarView> pair in _activeViews)
            {
                Enemy enemy = pair.Key;
                EnemyHealthBarView view = pair.Value;

                if (!enemy.IsActive || !view.Matches(enemy))
                {
                    _staleEnemies.Add(enemy);
                    continue;
                }

                Vector3 screenPosition = _worldCamera.WorldToScreenPoint(enemy.HealthBarPosition);
                bool isVisible = screenPosition.z > 0f
                    && screenPosition.x >= 0f
                    && screenPosition.x <= Screen.width
                    && screenPosition.y >= 0f
                    && screenPosition.y <= Screen.height;

                view.SetVisible(isVisible);

                if (!isVisible)
                    continue;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _root,
                    screenPosition,
                    canvasCamera,
                    out Vector2 localPosition))
                {
                    view.SetPosition(localPosition);
                }
            }

            for (int i = 0; i < _staleEnemies.Count; i++)
                Release(_staleEnemies[i]);
        }

        private void HandleEnemySpawn(Enemy enemy)
        {
            if (_activeViews.ContainsKey(enemy))
                throw new InvalidOperationException("An enemy already has a health bar.");

            EnemyHealthBarView view = _viewPool.Count > 0
                ? _viewPool.Pop()
                : CreateView();
            view.Bind(enemy);
            _activeViews.Add(enemy, view);
        }

        private void HandleEnemyRemoved(Enemy enemy)
        {
            Release(enemy);
        }

        private void HandleMidBossTimeChanged(Enemy enemy, float remainTime, float totalTime)
        {
            if (!_activeViews.TryGetValue(enemy, out EnemyHealthBarView view))
                throw new InvalidOperationException("The active middle boss has no health bar.");

            view.SetTimer(remainTime, totalTime);
        }

        private void HandleMidBossChallengeEnded(Enemy enemy)
        {
            if (_activeViews.TryGetValue(enemy, out EnemyHealthBarView view))
                view.HideTimer();
        }

        private void Release(Enemy enemy)
        {
            if (!_activeViews.TryGetValue(enemy, out EnemyHealthBarView view))
                return;

            _activeViews.Remove(enemy);
            view.Unbind();
            _viewPool.Push(view);
        }

        private EnemyHealthBarView CreateView()
        {
            return EnemyHealthBarView.Create(_root);
        }

        private void OnDestroy()
        {
            if (!_initialized)
                return;

            _enemySpawnService.OnSpawnEnemy -= HandleEnemySpawn;
            _enemySpawnService.OnDeathEnemy -= HandleEnemyRemoved;
            _enemySpawnService.OnDespawnEnemy -= HandleEnemyRemoved;
            _midBossChallengeInfo.OnMidBossTimeChanged -= HandleMidBossTimeChanged;
            _midBossChallengeInfo.OnMidBossChallengeEnded -= HandleMidBossChallengeEnded;

            _staleEnemies.Clear();
            foreach (Enemy enemy in new List<Enemy>(_activeViews.Keys))
                Release(enemy);
        }
    }
}
