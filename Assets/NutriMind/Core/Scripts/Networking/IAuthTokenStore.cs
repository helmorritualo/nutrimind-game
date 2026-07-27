using System.Threading;
using System.Threading.Tasks;

namespace NutriMind.Core.Networking
{
    public interface IAuthTokenStore
    {
        bool HasToken { get; }
        Task<string> ReadAsync(CancellationToken cancellationToken = default);
        Task WriteAsync(string token, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// In-process mock token store. Never persists to SQLite or PlayerPrefs.
    /// </summary>
    public sealed class InMemoryMockAuthTokenStore : IAuthTokenStore
    {
        private string _token;

        public bool HasToken => !string.IsNullOrEmpty(_token);

        public Task<string> ReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_token);
        }

        public Task WriteAsync(string token, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(token))
            {
                _token = null;
            }
            else
            {
                _token = token.Trim();
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _token = null;
            return Task.CompletedTask;
        }
    }
}
