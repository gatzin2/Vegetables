using UnityEngine;

namespace PlantAndDungeon.Prototype2D
{
    /// <summary>
    /// Authored regional presentation data. Exploration passes a theme id into the battle loader;
    /// the battle scene then swaps layered background sprites without changing combat logic.
    /// </summary>
    [CreateAssetMenu(menuName = "Plant and Dungeon/Battle Theme", fileName = "BattleTheme")]
    public sealed class BattleThemeDefinition : ScriptableObject
    {
        [SerializeField] private string themeId = "wasteland-frontier-town";
        [SerializeField] private Sprite farBackground;
        [SerializeField] private Sprite midBackground;
        [SerializeField] private Sprite foreground;
        [SerializeField] private Color ambientTint = Color.white;

        public string ThemeId => themeId;
        public Sprite FarBackground => farBackground;
        public Sprite MidBackground => midBackground;
        public Sprite Foreground => foreground;
        public Color AmbientTint => ambientTint;
    }
}
