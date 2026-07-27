using System;

namespace NutriMind.Core.Networking
{
    public enum ConnectivityState
    {
        Online = 0,
        Offline = 1
    }

    public interface IConnectivityService
    {
        ConnectivityState State { get; }
        bool IsOnline { get; }
        event Action<ConnectivityState> StateChanged;
        void SetState(ConnectivityState state);
        void SetOnline(bool online);
    }

    public sealed class MockConnectivityService : IConnectivityService
    {
        private ConnectivityState _state;

        public MockConnectivityService(bool startOnline = true)
        {
            _state = startOnline ? ConnectivityState.Online : ConnectivityState.Offline;
        }

        public ConnectivityState State => _state;
        public bool IsOnline => _state == ConnectivityState.Online;

        public event Action<ConnectivityState> StateChanged;

        public void SetState(ConnectivityState state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            StateChanged?.Invoke(_state);
        }

        public void SetOnline(bool online)
        {
            SetState(online ? ConnectivityState.Online : ConnectivityState.Offline);
        }
    }
}

