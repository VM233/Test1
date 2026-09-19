using System;
using Test1.Combat.Core;
using Test1.Combat.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Test1.Combat.Editor
{
    internal static class CombatDemoUiFactory
    {
        private static readonly Color BackgroundColor =
            new(0.02f, 0.025f, 0.035f, 0.9f);

        internal static GameObject CreateObject(
            string name,
            Transform parent,
            params Type[] componentTypes)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            for (int index = 0; index < componentTypes.Length; index++)
            {
                Type componentType = componentTypes[index];
                if (gameObject.GetComponent(componentType) == null)
                {
                    gameObject.AddComponent(componentType);
                }
            }

            return gameObject;
        }

        internal static void CreatePlayerHealthBar(
            Transform parent,
            Health target)
        {
            GameObject barObject = CreateBar(
                "Player Health",
                parent,
                new Vector2(520f, 52f),
                new Color(0.12f, 0.65f, 0.95f),
                "玩家",
                27,
                out RectTransform fill,
                out Text label);
            RectTransform barRect = barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(32f, -32f);

            HealthBarView view = barObject.AddComponent<HealthBarView>();
            view.Configure(target, fill, label, "玩家");
        }

        internal static void CreateExitButton(Transform parent)
        {
            GameObject buttonObject = CreateObject(
                "Exit Button",
                parent,
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(ExitGameButton));
            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.one;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.pivot = Vector2.one;
            buttonRect.anchoredPosition = new Vector2(-32f, -32f);
            buttonRect.sizeDelta = new Vector2(180f, 64f);

            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.52f, 0.11f, 0.1f, 0.94f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.85f, 0.82f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            CreateLabel(
                buttonObject.transform,
                "退出",
                new Vector2(170f, 58f),
                28);
        }

        internal static void CreateMonsterHealthBar(
            Transform actorRoot,
            Health target)
        {
            GameObject canvasObject = CreateObject(
                "Monster Health Bar",
                actorRoot,
                typeof(Canvas),
                typeof(CanvasGroup));
            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();
            canvasRect.localPosition = new Vector3(0f, 1.55f, 0f);
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.007f;
            canvasRect.sizeDelta = new Vector2(280f, 42f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30;

            CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            GameObject barObject = CreateBar(
                "Bar",
                canvasObject.transform,
                canvasRect.sizeDelta,
                new Color(0.88f, 0.16f, 0.12f),
                "野猪",
                21,
                out RectTransform fill,
                out Text label);
            RectTransform barRect = barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = Vector2.zero;

            HealthBarView view = barObject.AddComponent<HealthBarView>();
            view.Configure(target, fill, label, "野猪");

            TransientHealthBarVisibility visibility =
                canvasObject.AddComponent<TransientHealthBarVisibility>();
            visibility.Configure(
                target,
                canvas,
                canvasGroup,
                1.8f,
                0.25f);
            canvasObject.AddComponent<CameraFacingBillboard>();
            canvas.enabled = false;
        }

        private static GameObject CreateBar(
            string name,
            Transform parent,
            Vector2 size,
            Color fillColor,
            string labelText,
            int fontSize,
            out RectTransform fill,
            out Text label)
        {
            GameObject barObject = CreateObject(
                name,
                parent,
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform barRect = barObject.GetComponent<RectTransform>();
            barRect.sizeDelta = size;
            Image background = barObject.GetComponent<Image>();
            background.color = BackgroundColor;
            background.raycastTarget = false;

            GameObject fillObject = CreateObject(
                "Fill",
                barObject.transform,
                typeof(CanvasRenderer),
                typeof(Image));
            fill = fillObject.GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(4f, 4f);
            fill.offsetMax = new Vector2(-4f, -4f);
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            label = CreateLabel(
                barObject.transform,
                labelText,
                size - new Vector2(10f, 4f),
                fontSize);
            return barObject;
        }

        private static Text CreateLabel(
            Transform parent,
            string content,
            Vector2 size,
            int fontSize)
        {
            GameObject labelObject = CreateObject(
                "Label",
                parent,
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(SystemFontText),
                typeof(Outline));
            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = size;

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            label.text = content;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            Outline outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
            return label;
        }
    }
}
