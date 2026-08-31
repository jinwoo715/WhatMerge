using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Enemies
{
    public sealed class EnemyHealthBarView : MonoBehaviour
    {
        [SerializeField] private Image _healthFill;

        private RectTransform _rectTransform;
        private Enemy _enemy;
        private int _lifeCycleVersion;

        private RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = transform as RectTransform;

                return _rectTransform
                    ?? throw new System.InvalidOperationException("Enemy health bar view requires a RectTransform.");
            }
        }

        public void Bind(Enemy enemy)
        {
            if (enemy == null)
                throw new System.ArgumentNullException(nameof(enemy));

            if (_enemy != null)
                throw new System.InvalidOperationException("The health bar is already bound to an enemy.");
            if (_healthFill == null)
                throw new System.InvalidOperationException("Health fill image is not assigned.");
            if (_healthFill.sprite == null)
                throw new System.InvalidOperationException("Health fill image requires a sprite for fillAmount.");
            if (_healthFill.type != Image.Type.Filled)
                throw new System.InvalidOperationException("Health fill image type must be Filled.");

            _enemy = enemy;
            _lifeCycleVersion = enemy.LifeCycleVersion;
            _enemy.OnHealthChanged += SetHealth;

            _healthFill.color = GetFillColor(enemy.Type);
            SetHealth(enemy.CurrentHP, enemy.MaxHP);
            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            if (_enemy != null)
                _enemy.OnHealthChanged -= SetHealth;

            _enemy = null;
            _lifeCycleVersion = 0;
            gameObject.SetActive(false);
        }

        public bool Matches(Enemy enemy)
        {
            return ReferenceEquals(_enemy, enemy)
                && _lifeCycleVersion == enemy.LifeCycleVersion;
        }

        public void SetPosition(Vector2 anchoredPosition)
        {
            RectTransform.anchoredPosition = anchoredPosition;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        private void SetHealth(int currentHealth, int maxHealth)
        {
            _healthFill.fillAmount = maxHealth > 0 ? Mathf.Clamp01(currentHealth / (float)maxHealth) : 0f;
        }

        private static Color GetFillColor(EnemyType enemyType)
        {
            return enemyType switch
            {
                EnemyType.Mimic => new Color(0.95f, 0.56f, 0.12f, 1f),
                EnemyType.Boss => new Color(0.82f, 0.16f, 0.16f, 1f),
                _ => new Color(0.22f, 0.78f, 0.32f, 1f)
            };
        }
    }
}
