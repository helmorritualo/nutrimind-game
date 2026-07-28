using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    /// <summary>
    /// Marks a newly created placeholder that still needs manual Scene-view placement.
    /// Validation fails while any of these markers remain unconfirmed.
    /// </summary>
    public sealed class MissionPlacementRequired : MonoBehaviour
    {
        [SerializeField] [TextArea] private string _instruction;
        [SerializeField] private bool _confirmed;

        public string Instruction => _instruction;
        public bool IsConfirmed => _confirmed;

        public void Configure(string instruction)
        {
            _instruction = instruction;
            _confirmed = false;
        }

        public void ConfirmPlaced()
        {
            _confirmed = true;
        }
    }
}
