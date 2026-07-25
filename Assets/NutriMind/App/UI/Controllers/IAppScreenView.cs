using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Minimal lifetime contract for a plain C# application-screen presentation controller.
    /// An app screen view binds to an already-instantiated, content-only UXML root and must
    /// not require its own <c>UIDocument</c>. Implementations must unregister every UI
    /// callback and event from <see cref="System.IDisposable.Dispose"/>.
    /// <para>
    /// AppShell static-preview hosting may retain a screen view through this interface.
    /// Existing <c>&lt;Panel&gt;Controller</c> MonoBehaviours may later remain as standalone
    /// preview adapters that bind the same view to a direct <c>UIDocument</c>.
    /// </para>
    /// This is a presentation lifetime contract, not a production routing contract.
    /// </summary>
    public interface IAppScreenView : System.IDisposable
    {
        VisualElement Root { get; }

        bool IsBound { get; }
    }
}
