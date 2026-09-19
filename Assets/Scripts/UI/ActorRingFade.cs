using Test1.Combat.Core;
using UnityEngine;

namespace Test1.Combat.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class ActorRingFade : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [SerializeField]
        private Health target;

        [SerializeField]
        private Renderer targetRenderer;

        [SerializeField]
        private Material visibleMaterial;

        [SerializeField]
        private Material fadeMaterial;

        [SerializeField]
        [Min(0.01f)]
        private float fadeDuration = 0.35f;

        private MaterialPropertyBlock propertyBlock;
        private Color visibleColor;
        private int colorPropertyId;
        private float currentAlpha = 1f;
        private bool isFading;
        private bool hasStarted;

        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            target ??= GetComponentInParent<Health>();
            visibleMaterial ??= targetRenderer.sharedMaterial;
            fadeMaterial ??= visibleMaterial;
            colorPropertyId = fadeMaterial.HasProperty(BaseColorId)
                ? BaseColorId
                : ColorId;
            visibleColor = fadeMaterial.GetColor(colorPropertyId);
            propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            target.Died += OnDied;
            target.Revived += OnRevived;
            if (hasStarted)
            {
                SynchronizeVisibility();
            }
            else
            {
                ShowVisible();
            }
        }

        private void OnDisable()
        {
            target.Died -= OnDied;
            target.Revived -= OnRevived;
            isFading = false;
        }

        private void Start()
        {
            hasStarted = true;
            SynchronizeVisibility();
        }

        private void Update()
        {
            if (isFading == false)
            {
                return;
            }

            SetAlpha(Mathf.MoveTowards(
                currentAlpha,
                0f,
                Time.deltaTime / fadeDuration));
            if (currentAlpha <= 0f)
            {
                isFading = false;
            }
        }

        public void Configure(
            Health health,
            Renderer ringRenderer,
            Material normalMaterial,
            Material fadingMaterial,
            float duration)
        {
            target = health;
            targetRenderer = ringRenderer;
            visibleMaterial = normalMaterial;
            fadeMaterial = fadingMaterial;
            fadeDuration = Mathf.Max(0.01f, duration);
        }

        private void OnDied(Health health)
        {
            targetRenderer.sharedMaterial = fadeMaterial;
            SetAlpha(1f);
            isFading = true;
        }

        private void OnRevived(Health health)
        {
            ShowVisible();
        }

        private void ShowVisible()
        {
            isFading = false;
            currentAlpha = 1f;
            targetRenderer.sharedMaterial = visibleMaterial;
            propertyBlock.Clear();
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ShowHidden()
        {
            isFading = false;
            targetRenderer.sharedMaterial = fadeMaterial;
            SetAlpha(0f);
        }

        private void SynchronizeVisibility()
        {
            if (target.IsAlive)
            {
                ShowVisible();
            }
            else
            {
                ShowHidden();
            }
        }

        private void SetAlpha(float value)
        {
            currentAlpha = Mathf.Clamp01(value);
            Color color = visibleColor;
            color.a *= currentAlpha;
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
