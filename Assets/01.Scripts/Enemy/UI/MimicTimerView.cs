using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Enemies
{
    public sealed class MimicTimerView : MonoBehaviour
    {
        private static readonly Vector2 TimerPosition = new Vector2(0f, -6f);
        private static readonly Color NormalColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        private static readonly Color WarningColor = new Color(0.68f, 0.12f, 0.12f, 0.95f);

        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _timerText;

        private RectTransform _rectTransform;
        private Transform _inactiveParent;
        private Enemy _enemy;
        private int _lifeCycleVersion;

        public bool IsBound => _enemy != null;

        public void Initialize(Transform inactiveParent)
        {
            if (inactiveParent == null)
                throw new ArgumentNullException(nameof(inactiveParent));
            if (_background == null)
                throw new InvalidOperationException("Mimic timer background image is not assigned.");
            if (_timerText == null)
                throw new InvalidOperationException("Mimic timer text is not assigned.");

            _inactiveParent = inactiveParent;
            _rectTransform = transform as RectTransform
                ?? throw new InvalidOperationException("Mimic timer view requires a RectTransform.");
            _rectTransform.anchoredPosition = TimerPosition;
            gameObject.SetActive(false);
        }

        public void Bind(Enemy enemy, Transform healthBar)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));
            if (healthBar == null)
                throw new ArgumentNullException(nameof(healthBar));
            if (IsBound)
                throw new InvalidOperationException("The mimic timer is already bound.");

            _enemy = enemy;
            _lifeCycleVersion = enemy.LifeCycleVersion;
            transform.SetParent(healthBar, false);
            _rectTransform.anchoredPosition = TimerPosition;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            gameObject.SetActive(false);
            transform.SetParent(_inactiveParent, false);
            _rectTransform.anchoredPosition = TimerPosition;
            _enemy = null;
            _lifeCycleVersion = 0;
        }

        public bool Matches(Enemy enemy)
        {
            return enemy != null
                && ReferenceEquals(_enemy, enemy)
                && _lifeCycleVersion == enemy.LifeCycleVersion;
        }

        public void SetTime(float remainTime, float totalTime)
        {
            if (!IsBound)
                throw new InvalidOperationException("The mimic timer is not bound.");

            _timerText.text = Mathf.CeilToInt(Mathf.Max(0f, remainTime)).ToString();

            float ratio = totalTime > 0f ? remainTime / totalTime : 0f;
            _background.color = ratio <= 0.2f ? WarningColor : NormalColor;
        }
    }
}
