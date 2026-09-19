using Test1.Combat.Core;
using UnityEngine;

namespace Test1.Combat.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TransientHealthBarVisibility : MonoBehaviour
    {
        [SerializeField]
        private Health target;

        [SerializeField]
        private Canvas targetCanvas;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        [Min(0f)]
        private float visibleDuration = 1.8f;

        [SerializeField]
        [Min(0.01f)]
        private float fadeDuration = 0.25f;

        private double fadeStartsAt;
        private bool isVisible;
        private bool isFading;

        private void Awake()
        {
            if (target == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(TransientHealthBarVisibility)} on '{name}' " +
                    $"requires a {nameof(Health)} target.");
            }

            targetCanvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            HideImmediately();
        }

        private void OnEnable()
        {
            target.ValueChanged += OnHealthValueChanged;
            target.Died += OnTargetDied;
            HideImmediately();
        }

        private void OnDisable()
        {
            if (target != null)
            {
                target.ValueChanged -= OnHealthValueChanged;
                target.Died -= OnTargetDied;
            }
        }

        private void Update()
        {
            if (isVisible == false)
            {
                return;
            }

            if (isFading == false)
            {
                if (Time.unscaledTimeAsDouble < fadeStartsAt)
                {
                    return;
                }

                isFading = true;
            }

            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha,
                0f,
                Time.unscaledDeltaTime / fadeDuration);
            if (canvasGroup.alpha <= 0f)
            {
                HideImmediately();
            }
        }

        public void Configure(
            Health health,
            Canvas canvas,
            CanvasGroup group,
            float duration,
            float fadeOutDuration)
        {
            target = health;
            targetCanvas = canvas;
            canvasGroup = group;
            visibleDuration = Mathf.Max(0f, duration);
            fadeDuration = Mathf.Max(0.01f, fadeOutDuration);
        }

        private void OnHealthValueChanged(
            Health health,
            float previousHealth,
            float currentHealth)
        {
            if (currentHealth >= previousHealth)
            {
                return;
            }

            targetCanvas.enabled = true;
            canvasGroup.alpha = 1f;
            fadeStartsAt = Time.unscaledTimeAsDouble + visibleDuration;
            isVisible = true;
            isFading = false;
        }

        private void OnTargetDied(Health health)
        {
            if (isVisible)
            {
                isFading = true;
            }
        }

        private void HideImmediately()
        {
            isVisible = false;
            isFading = false;
            canvasGroup.alpha = 0f;
            targetCanvas.enabled = false;
        }
    }
}
