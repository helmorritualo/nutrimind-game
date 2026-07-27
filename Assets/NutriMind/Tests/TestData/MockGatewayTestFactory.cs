using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Utilities;

namespace NutriMind.Tests.TestData
{
    /// <summary>
    /// Shared factory for fast MockStudentGateway instances in tests.
    /// </summary>
    public static class MockGatewayTestFactory
    {
        public const string ValidLrn = MockServerState.ValidMockLrn;
        public const string ValidPin = MockServerState.ValidMockPin;

        public static NutriMindRuntimeOptions CreateFastOptions(
            MockApiScenario scenario = MockApiScenario.HappyPath,
            bool startOffline = false,
            int minimumLatencyMs = 0,
            int maximumLatencyMs = 0)
        {
            var options = NutriMindRuntimeOptions.CreateDefaults();
            options.Mode = NutriMindRuntimeMode.Mock;
            options.MockScenario = scenario;
            options.StartOffline = startOffline;
            options.MinimumMockLatencyMilliseconds = minimumLatencyMs;
            options.MaximumMockLatencyMilliseconds = maximumLatencyMs;
            options.LogGatewayOperations = false;
            options.Clamp();
            return options;
        }

        public static MockStudentGateway CreateGateway(
            MockApiScenario scenario = MockApiScenario.HappyPath,
            bool startOffline = false,
            IConnectivityService connectivity = null,
            IAuthTokenStore tokenStore = null,
            IMockFixtureSource fixtures = null,
            int minimumLatencyMs = 0,
            int maximumLatencyMs = 0,
            MockServerState state = null)
        {
            NutriMindRuntimeOptions options = CreateFastOptions(
                scenario,
                startOffline,
                minimumLatencyMs,
                maximumLatencyMs);

            return new MockStudentGateway(
                options,
                connectivity ?? new MockConnectivityService(!startOffline),
                tokenStore ?? new InMemoryMockAuthTokenStore(),
                fixtures,
                clock: new FixedMockClock(),
                ids: new DeterministicMockIdGenerator(),
                state: state);
        }
    }
}
