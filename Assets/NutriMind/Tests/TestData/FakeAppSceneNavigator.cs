using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.App.Routing;

namespace NutriMind.Tests.TestData
{
    /// <summary>
    /// Records scene load requests without touching Unity SceneManager.
    /// </summary>
    public sealed class FakeAppSceneNavigator : IAppSceneNavigator
    {
        private readonly List<AppSceneId> _loads = new List<AppSceneId>();

        public AppSceneId CurrentScene { get; private set; } = AppSceneId.Bootstrap;

        public IReadOnlyList<AppSceneId> Loads => _loads;

        public Task LoadAsync(AppSceneId sceneId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _loads.Add(sceneId);
            CurrentScene = sceneId;
            return Task.CompletedTask;
        }

        public void ClearHistory()
        {
            _loads.Clear();
        }
    }
}
