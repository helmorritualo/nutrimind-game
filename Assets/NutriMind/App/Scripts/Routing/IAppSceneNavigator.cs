using System.Threading;
using System.Threading.Tasks;

namespace NutriMind.App.Routing
{
    public interface IAppSceneNavigator
    {
        AppSceneId CurrentScene { get; }

        Task LoadAsync(AppSceneId sceneId, CancellationToken cancellationToken = default);
    }
}
