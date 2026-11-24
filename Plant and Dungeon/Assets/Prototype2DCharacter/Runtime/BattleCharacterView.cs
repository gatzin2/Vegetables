using UnityEngine;

namespace PlantAndDungeon.Prototype2D
{
    /// <summary>
    /// Presentation-only bridge for a Slay-the-Spire-style battle character.
    /// Combat rules send commands; this view owns no health, damage, or turn state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleCharacterView : MonoBehaviour
    {
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DieHash = Animator.StringToHash("Die");
        private static readonly int ResetHash = Animator.StringToHash("Reset");

        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool facesRightByDefault = true;

        public bool IsDead { get; private set; }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            visualRoot = transform.Find("VisualRoot");
        }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (visualRoot == null)
                visualRoot = transform.Find("VisualRoot");
        }

        public void SetFacing(bool faceRight)
        {
            if (visualRoot == null)
                return;

            Vector3 scale = visualRoot.localScale;
            float sign = faceRight == facesRightByDefault ? 1f : -1f;
            scale.x = Mathf.Abs(scale.x) * sign;
            visualRoot.localScale = scale;
        }

        public void Play(BattleAnimationCommand command)
        {
            if (animator == null)
                return;
            if (IsDead && command != BattleAnimationCommand.Idle)
                return;

            switch (command)
            {
                case BattleAnimationCommand.Idle:
                    IsDead = false;
                    animator.SetTrigger(ResetHash);
                    break;
                case BattleAnimationCommand.Attack:
                    animator.SetTrigger(AttackHash);
                    break;
                case BattleAnimationCommand.Hit:
                    animator.SetTrigger(HitHash);
                    break;
                case BattleAnimationCommand.Death:
                    IsDead = true;
                    animator.SetTrigger(DieHash);
                    break;
            }
        }

        public void PlayState(string stateName)
        {
            if (animator == null || IsDead) return;
            int state = Animator.StringToHash(stateName);
            if (!animator.HasState(0, state))
            {
                Debug.LogWarning($"Animator state not found: {stateName}", this);
                return;
            }
            animator.CrossFadeInFixedTime(state, 0.05f, 0);
        }
    }
}
