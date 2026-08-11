using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Enemies
{
    public sealed class MimicTimerView : MonoBehaviour
    {
        private static readonly Vector2 TimerSize = new Vector2(24f, 12f);
        private static readonly Vector2 TimerPosition = new Vector2(0f, -6f);
        private static readonly Color NormalColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        private static readonly Color WarningColor = new Color(0.68f, 0.12f, 0.12f, 0.95f);

        private RectTransform _rectTransform;
        private Image _background;
        private TMP_Text _timerText;
        private Transform _inactiveParent;
        private Enemy _enemy;
        private int _lifeCycleVersion;

        public bool IsBound => _enemy != null;

        public static MimicTimerView Create(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var root = new GameObject(
                "MiddleBossTimer",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(MimicTimerView));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);

            var view = root.GetComponent<MimicTimerView>();
            view._inactiveParent = parent;
            view.BuildVisuals();
            root.SetActive(false);
            return view;
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

        private void BuildVisuals()
        {
            _rectTransform = (RectTransform)transform;
            SetRect(_rectTransform, TimerSize, TimerPosition);

            _background = GetComponent<Image>();
            _background.color = NormalColor;
            _background.raycastTarget = false;

            var textObject = new GameObject(
                "TimeText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(transform, false);

            _timerText = textObject.GetComponent<TextMeshProUGUI>();
            _timerText.raycastTarget = false;
            _timerText.alignment = TextAlignmentOptions.Center;
            _timerText.fontSize = 9f;
            _timerText.color = Color.white;
            _timerText.font = TMP_Settings.defaultFontAsset;

            RectTransform textRect = _timerText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
