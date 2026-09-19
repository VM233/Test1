using UnityEngine;
using UnityEngine.UI;

namespace Test1.Combat.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class SystemFontText : MonoBehaviour
    {
        private static readonly string[] PreferredFonts =
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "SimHei",
            "PingFang SC",
            "Noto Sans CJK SC",
            "Arial Unicode MS"
        };

        private static Font sharedFont;

        [SerializeField]
        [Min(1)]
        private int dynamicFontSize = 32;

        private void Awake()
        {
            if (sharedFont == null)
            {
                sharedFont = Font.CreateDynamicFontFromOSFont(
                    PreferredFonts,
                    dynamicFontSize);
            }

            if (sharedFont != null)
            {
                GetComponent<Text>().font = sharedFont;
            }
        }
    }
}
