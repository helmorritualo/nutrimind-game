using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class AreaEntryTrigger : MonoBehaviour
    {
        [SerializeField] private string _areaId;

        public string AreaId => _areaId;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            MissionPrototypeController controller = FindFirstObjectByType<MissionPrototypeController>();
            controller?.HandleAreaEntry(_areaId);
        }

        private void OnTriggerStay(Collider other)
        {
            // Catch cases where the player was already overlapping when Area 1 completed,
            // or when CharacterController first enables after spawn.
            if (!IsPlayer(other))
            {
                return;
            }

            MissionPrototypeController controller = FindFirstObjectByType<MissionPrototypeController>();
            controller?.HandleAreaEntry(_areaId);
        }

        private static bool IsPlayer(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
                return true;
            }

            Transform root = other.transform.root;
            return root != null && root.CompareTag("Player");
        }
    }
}
