using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class AreaEntryTrigger : MonoBehaviour
    {
        [SerializeField] private string _areaId;

        public string AreaId => _areaId;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            MissionPrototypeController controller = FindFirstObjectByType<MissionPrototypeController>();
            controller?.HandleAreaEntry(_areaId);
        }
    }
}
