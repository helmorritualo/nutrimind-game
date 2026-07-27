using System;
using UnityEngine;

namespace NutriMind.Core.Data
{
    [Serializable]
    public sealed class NutriMindRuntimeOptions
    {
        public const int DefaultMinimumMockLatencyMilliseconds = 180;
        public const int DefaultMaximumMockLatencyMilliseconds = 520;

        [SerializeField] private NutriMindRuntimeMode _mode = NutriMindRuntimeMode.Mock;
        [SerializeField] private MockApiScenario _mockScenario = MockApiScenario.HappyPath;
        [SerializeField] private int _minimumMockLatencyMilliseconds = DefaultMinimumMockLatencyMilliseconds;
        [SerializeField] private int _maximumMockLatencyMilliseconds = DefaultMaximumMockLatencyMilliseconds;
        [SerializeField] private bool _resetMockDatabaseOnStart;
        [SerializeField] private bool _startOffline;
        [SerializeField] private bool _logGatewayOperations = true;
        [SerializeField] private bool _logDatabaseOperations;

        public NutriMindRuntimeMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public MockApiScenario MockScenario
        {
            get => _mockScenario;
            set => _mockScenario = value;
        }

        public int MinimumMockLatencyMilliseconds
        {
            get => _minimumMockLatencyMilliseconds;
            set => _minimumMockLatencyMilliseconds = value;
        }

        public int MaximumMockLatencyMilliseconds
        {
            get => _maximumMockLatencyMilliseconds;
            set => _maximumMockLatencyMilliseconds = value;
        }

        public bool ResetMockDatabaseOnStart
        {
            get => _resetMockDatabaseOnStart;
            set => _resetMockDatabaseOnStart = value;
        }

        public bool StartOffline
        {
            get => _startOffline;
            set => _startOffline = value;
        }

        public bool LogGatewayOperations
        {
            get => _logGatewayOperations;
            set => _logGatewayOperations = value;
        }

        public bool LogDatabaseOperations
        {
            get => _logDatabaseOperations;
            set => _logDatabaseOperations = value;
        }

        public static NutriMindRuntimeOptions CreateDefaults()
        {
            var options = new NutriMindRuntimeOptions();
            options.Clamp();
            return options;
        }

        public NutriMindRuntimeOptions Clone()
        {
            var clone = new NutriMindRuntimeOptions
            {
                _mode = _mode,
                _mockScenario = _mockScenario,
                _minimumMockLatencyMilliseconds = _minimumMockLatencyMilliseconds,
                _maximumMockLatencyMilliseconds = _maximumMockLatencyMilliseconds,
                _resetMockDatabaseOnStart = _resetMockDatabaseOnStart,
                _startOffline = _startOffline,
                _logGatewayOperations = _logGatewayOperations,
                _logDatabaseOperations = _logDatabaseOperations
            };
            clone.Clamp();
            return clone;
        }

        public void Clamp()
        {
            if (!Enum.IsDefined(typeof(NutriMindRuntimeMode), _mode))
            {
                _mode = NutriMindRuntimeMode.Mock;
            }

            if (!Enum.IsDefined(typeof(MockApiScenario), _mockScenario))
            {
                _mockScenario = MockApiScenario.HappyPath;
            }

            _minimumMockLatencyMilliseconds = Mathf.Clamp(_minimumMockLatencyMilliseconds, 0, 30_000);
            _maximumMockLatencyMilliseconds = Mathf.Clamp(_maximumMockLatencyMilliseconds, 0, 60_000);
            if (_maximumMockLatencyMilliseconds < _minimumMockLatencyMilliseconds)
            {
                _maximumMockLatencyMilliseconds = _minimumMockLatencyMilliseconds;
            }
        }
    }
}
