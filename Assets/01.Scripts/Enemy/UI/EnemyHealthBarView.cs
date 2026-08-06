using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhatMerge.Enemies
{
    public sealed class EnemyHealthBarView : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Image _healthFill;
        private GameObject _timerRoot;
        private Image _timerBackground;
        private TMP_Text _timerText;
        private Enemy _enemy;
        private int _lifeCycleVersion;

        public static EnemyHealthBarView Create(Transform parent)
        {
            var root = new GameObject("EnemyHealthBar", typeof(RectTransform), typeof(EnemyHealthBarView));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);

            var view = root.GetComponent<EnemyHealthBarView>();
            view.BuildVisuals();
            root.SetActive(false);
            return view;
        }

        public void Bind(Enemy enemy)
        {
            if (enemy == null)
                throw new System.ArgumentNullException(nameof(enemy));

            if (_enemy != null)
                throw new System.InvalidOperationException("The health bar is already bound to an enemy.");

            _enemy = enemy;
            _lifeCycleVersion = enemy.LifeCycleVersion;
            _enemy.OnHealthChanged += SetHealth;

            _healthFill.color = GetFillColor(enemy.Type);
            SetHealth(enemy.CurrentHP, enemy.MaxHP);
            HideTimer();
            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            if (_enemy != null)
                _enemy.OnHealthChanged -= SetHealth;

            _enemy = null;
            _lifeCycleVersion = 0;
            HideTimer();
            gameObject.SetActive(false);
        }

        public bool Matches(Enemy enemy)
        {
            return ReferenceEquals(_enemy, enemy)
                && _lifeCycleVersion == enemy.LifeCycleVersion;
        }

        public void SetPosition(Vector2 anchoredPosition)
        {
            _rectTransform.anchoredPosition = anchoredPosition;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        public void SetTimer(float remainTime, float totalTime)
        {
            _timerRoot.SetActive(true);
            _timerText.text = Mathf.CeilToInt(Mathf.Max(0f, remainTime)).ToString();

            float ratio = totalTime > 0f ? remainTime / totalTime : 0f;
            _timerBackground.color = ratio <= 0.2f
                ? new Color(0.68f, 0.12f, 0.12f, 0.95f)
                : new Color(0.08f, 0.08f, 0.08f, 0.9f);
        }

        public void HideTimer()
        {
            if (_timerRoot != null)
                _timerRoot.SetActive(false);
        }

        private void SetHealth(int currentHealth, int maxHealth)
        {
            _healthFill.fillAmount = maxHealth > 0
                ? Mathf.Clamp01(currentHealth / (float)maxHealth)
                : 0f;
        }

        private void BuildVisuals()
        {
            _rectTransform = (RectTransform)transform;
            SetRect(_rectTransform, new Vector2(64f, 22f), Vector2.zero);

            Image background = CreateImage(
                "HealthBackground",
                transform,
                new Color(0.05f, 0.05f, 0.05f, 0.92f));
            SetRect(background.rectTransform, new Vector2(60f, 8f), new Vector2(0f, 5f));

            _healthFill = CreateImage(
                "HealthFill",
                background.transform,
                new Color(0.22f, 0.78f, 0.32f, 1f));
            RectTransform fillRect = _healthFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);
            _healthFill.type = Image.Type.Filled;
            _healthFill.fillMethod = Image.FillMethod.Horizontal;
            _healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            _timerBackground = CreateImage(
                "MidBossTimer",
                transform,
                new Color(0.08f, 0.08f, 0.08f, 0.9f));
            _timerRoot = _timerBackground.gameObject;
            SetRect(_timerBackground.rectTransform, new Vector2(24f, 12f), new Vector2(0f, -6f));

            GameObject textObject = new GameObject(
                "TimeText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(_timerRoot.transform, false);

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

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static Color GetFillColor(EnemyType enemyType)
        {
            return enemyType switch
            {
                EnemyType.MiddleBoss => new Color(0.95f, 0.56f, 0.12f, 1f),
                EnemyType.Boss => new Color(0.82f, 0.16f, 0.16f, 1f),
                _ => new Color(0.22f, 0.78f, 0.32f, 1f)
            };
        }
    }
}
