using System;
using UnityEngine;

namespace NutriMind.Gameplay.Runtime
{
    public sealed class StoryFragmentCollectible : MonoBehaviour
    {
        [SerializeField] private string _collectibleId;
        [SerializeField] private Collider _triggerCollider;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private float _bobAmplitude = 0.15f;
        [SerializeField] private float _bobSpeed = 2f;

        private bool _revealed;
        private bool _collected;
        private Vector3 _baseLocalPosition;
        private MissionPrototypeController _missionController;

        public string CollectibleId => _collectibleId;
        public bool IsRevealed => _revealed;
        public bool IsCollected => _collected;
        public GameObject VisualRoot => _visualRoot;
        public Collider TriggerCollider => _triggerCollider;

        public event Action<StoryFragmentCollectible> Collected;

        private void Awake()
        {
            if (_visualRoot == null)
            {
                Transform visual = transform.Find("FragmentVisual");
                _visualRoot = visual != null ? visual.gameObject : null;
            }

            if (_visualRoot != null)
            {
                _baseLocalPosition = _visualRoot.transform.localPosition;
            }

            SetRevealed(false);
        }

        public void Initialize(MissionPrototypeController missionController)
        {
            _missionController = missionController;
        }

        public void SetRevealed(bool revealed)
        {
            if (_collected)
            {
                revealed = false;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            _revealed = revealed;

            if (_visualRoot != null)
            {
                _visualRoot.SetActive(_revealed);
            }

            if (_triggerCollider != null)
            {
                _triggerCollider.enabled = _revealed;
            }
        }

        private void Update()
        {
            if (!_revealed || _collected || _visualRoot == null)
            {
                return;
            }

            float offset = Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude;
            _visualRoot.transform.localPosition = _baseLocalPosition + Vector3.up * offset;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_revealed || _collected)
            {
                return;
            }

            if (!other.CompareTag("Player"))
            {
                return;
            }

            TryCollect();
        }

        public bool TryCollect()
        {
            if (_collected || !_revealed)
            {
                return false;
            }

            _collected = true;
            _revealed = false;
            if (_triggerCollider != null)
            {
                _triggerCollider.enabled = false;
            }

            if (_visualRoot != null)
            {
                _visualRoot.SetActive(false);
            }

            Collected?.Invoke(this);
            _missionController?.HandleFragmentCollected(_collectibleId);
            return true;
        }
    }
}
