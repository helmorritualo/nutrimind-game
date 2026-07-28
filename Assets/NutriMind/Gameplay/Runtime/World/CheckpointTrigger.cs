using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private string _checkpointId;
        [SerializeField] private Transform _respawnPoint;

        public string CheckpointId => _checkpointId;
        public Transform RespawnPoint => _respawnPoint != null ? _respawnPoint : transform;

        private bool _activated;
        private MissionPrototypeController _missionController;

        public void Initialize(MissionPrototypeController missionController)
        {
            _missionController = missionController;
        }

        public void SetActivated(bool activated)
        {
            _activated = activated;
            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                trigger.enabled = activated;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_activated || !other.CompareTag("Player"))
            {
                return;
            }

            _missionController?.HandleCheckpointReached(_checkpointId, RespawnPoint);
        }
    }
}
