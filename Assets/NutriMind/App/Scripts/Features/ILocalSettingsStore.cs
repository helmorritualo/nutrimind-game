using NutriMind.App.UI;
using NutriMind.Core.Data;

namespace NutriMind.App.Features
{
    /// <summary>
    /// Device-local settings store. Uses PlayerPrefs via <see cref="AppLocalSettings"/>.
    /// </summary>
    public interface ILocalSettingsStore
    {
        AppLocalSettings Load();

        AppResult Save(AppLocalSettings settings);

        AppResult RestoreDefaults();
    }

    public sealed class PlayerPrefsLocalSettingsStore : ILocalSettingsStore
    {
        public AppLocalSettings Load() => AppLocalSettings.Load();

        public AppResult Save(AppLocalSettings settings)
        {
            if (settings == null)
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "Settings are required.");
            }

            settings.Save();
            settings.ApplyRuntimeEffects();
            return AppResult.Success();
        }

        public AppResult RestoreDefaults()
        {
            AppLocalSettings defaults = AppLocalSettings.CreateDefaults();
            defaults.Save();
            defaults.ApplyRuntimeEffects();
            return AppResult.Success();
        }
    }
}
