using System;
using NutriMind.Core.Data;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Shared live mock scenario/connectivity state. Startup options seed this once;
    /// developer controls mutate this instance without recreating the gateway.
    /// </summary>
    public interface IMockRuntimeState
    {
        MockApiScenario Scenario { get; }

        ConnectivityState Connectivity { get; }

        event Action Changed;

        void SetScenario(MockApiScenario scenario);

        void SetConnectivity(ConnectivityState connectivity);

        /// <summary>
        /// Clears in-memory mock mutation/idempotency state only. Does not touch SQLite.
        /// </summary>
        void ResetServerState();
    }

    public sealed class MockRuntimeState : IMockRuntimeState
    {
        private readonly object _gate = new object();
        private readonly IConnectivityService _connectivity;
        private readonly MockServerState _serverState;
        private MockApiScenario _scenario;

        public MockRuntimeState(
            MockApiScenario initialScenario,
            IConnectivityService connectivity,
            MockServerState serverState)
        {
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
            _serverState = serverState ?? throw new ArgumentNullException(nameof(serverState));
            _scenario = Enum.IsDefined(typeof(MockApiScenario), initialScenario)
                ? initialScenario
                : MockApiScenario.HappyPath;

            _connectivity.StateChanged += OnConnectivityChanged;
        }

        public MockApiScenario Scenario
        {
            get
            {
                lock (_gate)
                {
                    return _scenario;
                }
            }
        }

        public ConnectivityState Connectivity => _connectivity.State;

        public MockServerState ServerState => _serverState;

        public event Action Changed;

        public void SetScenario(MockApiScenario scenario)
        {
            if (!Enum.IsDefined(typeof(MockApiScenario), scenario))
            {
                scenario = MockApiScenario.HappyPath;
            }

            bool changed;
            lock (_gate)
            {
                changed = _scenario != scenario;
                _scenario = scenario;
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        public void SetConnectivity(ConnectivityState connectivity)
        {
            _connectivity.SetState(connectivity);
        }

        public void ResetServerState()
        {
            _serverState.ResetMutations();
            Changed?.Invoke();
        }

        private void OnConnectivityChanged(ConnectivityState _)
        {
            Changed?.Invoke();
        }
    }
}
