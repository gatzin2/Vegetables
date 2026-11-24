using UnityEngine;

namespace PlantAndDungeon.Prototype2D
{
    public enum BattlePreviewTarget { Player, Enemy, Both }

    [DisallowMultipleComponent]
    public sealed class BattleAnimationTestBench : MonoBehaviour
    {
        [SerializeField] private BattleCharacterView playerPrefab;
        [SerializeField] private BattleCharacterView enemyPrefab;
        [SerializeField] private Transform playerAnchor;
        [SerializeField] private Transform enemyAnchor;
        [SerializeField] private BattleStagePresenter stage;
        [SerializeField] private BattleThemeDefinition theme;
        [SerializeField] private BattlePreviewTarget previewTarget;
        [SerializeField] private BattleAnimationCommand previewAction = BattleAnimationCommand.Attack;
        [SerializeField] private string customStateName;
        [SerializeField, HideInInspector] private BattleCharacterView playerInstance;
        [SerializeField, HideInInspector] private BattleCharacterView enemyInstance;

        public BattleCharacterView PlayerPrefab => playerPrefab;
        public BattleCharacterView EnemyPrefab => enemyPrefab;
        public Transform PlayerAnchor => playerAnchor;
        public Transform EnemyAnchor => enemyAnchor;
        public BattleCharacterView PlayerInstance => playerInstance;
        public BattleCharacterView EnemyInstance => enemyInstance;

        private void Start()
        {
            if (playerInstance == null) playerInstance = Spawn(playerPrefab, playerAnchor, true);
            if (enemyInstance == null) enemyInstance = Spawn(enemyPrefab, enemyAnchor, false);
            ApplyTheme();
        }

        private static BattleCharacterView Spawn(BattleCharacterView prefab, Transform anchor, bool right)
        {
            if (prefab == null || anchor == null) return null;
            BattleCharacterView view = Instantiate(prefab, anchor);
            view.transform.localPosition = Vector3.zero;
            view.transform.localRotation = Quaternion.identity;
            view.SetFacing(right);
            return view;
        }

        public void ApplyTheme() { if (stage != null) stage.Apply(theme); }

        public void PlayPreview()
        {
            if (!Application.isPlaying) return;
            if (previewTarget != BattlePreviewTarget.Enemy) Play(playerInstance);
            if (previewTarget != BattlePreviewTarget.Player) Play(enemyInstance);
        }

        private void Play(BattleCharacterView view)
        {
            if (view == null) return;
            if (string.IsNullOrWhiteSpace(customStateName)) view.Play(previewAction);
            else view.PlayState(customStateName);
        }

        public void ResetPreview()
        {
            if (!Application.isPlaying) return;
            if (playerInstance != null) playerInstance.Play(BattleAnimationCommand.Idle);
            if (enemyInstance != null) enemyInstance.Play(BattleAnimationCommand.Idle);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ResetPreview();
            if (Input.GetKeyDown(KeyCode.Space)) PlayPreview();
        }
    }
}
