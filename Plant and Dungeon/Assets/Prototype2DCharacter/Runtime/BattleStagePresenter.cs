using UnityEngine;

namespace PlantAndDungeon.Prototype2D
{
    [DisallowMultipleComponent]
    public sealed class BattleStagePresenter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer farLayer;
        [SerializeField] private SpriteRenderer midLayer;
        [SerializeField] private SpriteRenderer foregroundLayer;

        public void Apply(BattleThemeDefinition theme)
        {
            if (theme == null)
                return;

            if (farLayer != null)
            {
                farLayer.sprite = theme.FarBackground;
                farLayer.color = theme.AmbientTint;
            }
            if (midLayer != null) midLayer.sprite = theme.MidBackground;
            if (foregroundLayer != null) foregroundLayer.sprite = theme.Foreground;
        }
    }
}
