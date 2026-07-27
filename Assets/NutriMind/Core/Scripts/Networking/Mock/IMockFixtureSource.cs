using System;
using NutriMind.Core.Data;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Loads mock-only JSON fixtures. Only mock infrastructure may call Resources.Load.
    /// </summary>
    public interface IMockFixtureSource
    {
        AppResult<string> LoadText(string fixtureName);

        AppResult<T> LoadJson<T>(string fixtureName) where T : class;
    }
}
