using UnityEngine;

namespace PlantAndDungeon.Prototype2D
{
    /// <summary>Temporary keyboard harness. Replace this with battle-system commands.</summary>
    [DisallowMultipleComponent]
    public sealed class BattlePrototypeInput : MonoBehaviour
    {
        [SerializeField] private BattleCharacterView character;

        private void Awake()
        {
            if (character == null)
                character = GetComponent<BattleCharacterView>();
        }

        private void Update()
        {
            if (character == null)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) character.Play(BattleAnimationCommand.Idle);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Space)) character.Play(BattleAnimationCommand.Attack);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.H)) character.Play(BattleAnimationCommand.Hit);
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.K)) character.Play(BattleAnimationCommand.Death);
        }
    }
}
