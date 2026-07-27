using System;
using System.Collections.Generic;

namespace NutriMind.App.Routing
{
    /// <summary>
    /// Mutable route stack for one application scene (Main or QuizPortal).
    /// </summary>
    public sealed class AppRouteStack
    {
        private readonly List<AppRouteEntry> _entries = new List<AppRouteEntry>(8);

        public int Count => _entries.Count;

        public bool IsEmpty => _entries.Count == 0;

        public AppRouteEntry Current
        {
            get
            {
                if (_entries.Count == 0)
                {
                    throw new InvalidOperationException("Route stack is empty.");
                }

                return _entries[_entries.Count - 1];
            }
        }

        public bool TryGetCurrent(out AppRouteEntry entry)
        {
            if (_entries.Count == 0)
            {
                entry = default;
                return false;
            }

            entry = _entries[_entries.Count - 1];
            return true;
        }

        public IReadOnlyList<AppRouteEntry> Snapshot()
        {
            return _entries.ToArray();
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public void Reset(AppRouteEntry root)
        {
            _entries.Clear();
            _entries.Add(root);
        }

        public void Push(AppRouteEntry entry)
        {
            _entries.Add(entry);
        }

        public void Replace(AppRouteEntry entry)
        {
            if (_entries.Count == 0)
            {
                _entries.Add(entry);
                return;
            }

            _entries[_entries.Count - 1] = entry;
        }

        public bool TryPop(out AppRouteEntry removed)
        {
            if (_entries.Count == 0)
            {
                removed = default;
                return false;
            }

            int index = _entries.Count - 1;
            removed = _entries[index];
            _entries.RemoveAt(index);
            return true;
        }
    }
}
