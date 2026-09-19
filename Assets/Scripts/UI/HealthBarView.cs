using Test1.Combat.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Test1.Combat.UI
{
    [DisallowMultipleComponent]
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField]
        private Health target;

        [SerializeField]
        private RectTransform fill;

        [SerializeField]
        private Text label;

        [SerializeField]
        private string displayName;

        private void OnEnable()
        {
            target.Changed += OnHealthChanged;
            Refresh();
        }

        private void OnDisable()
        {
            target.Changed -= OnHealthChanged;
        }

        public void Configure(
            Health health,
            RectTransform fillRect,
            Text labelText,
            string actorName)
        {
            if (Application.isPlaying &&
                isActiveAndEnabled &&
                target != null)
            {
                target.Changed -= OnHealthChanged;
            }

            target = health;
            fill = fillRect;
            label = labelText;
            displayName = actorName;

            if (Application.isPlaying && isActiveAndEnabled)
            {
                target.Changed += OnHealthChanged;
            }

            Refresh();
        }

        private void OnHealthChanged(Health health)
        {
            Refresh();
        }

        private void Refresh()
        {
            Vector2 anchorMax = fill.anchorMax;
            anchorMax.x = target.Normalized;
            fill.anchorMax = anchorMax;
            label.text = $"{displayName}  " +
                         $"{Mathf.CeilToInt(target.CurrentHealth)} / " +
                         $"{Mathf.CeilToInt(target.MaxHealth)}";
        }
    }
}
