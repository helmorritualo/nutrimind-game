using System;
using UnityEngine;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Local device settings used by the Settings panel.
    /// Persists to PlayerPrefs only — no server sync in this milestone slice.
    /// </summary>
    public sealed class AppLocalSettings
    {
        public const string PrefsPrefix = "NutriMind.Settings.";

        public float MasterVolume = 0.80f;
        public float MusicVolume = 0.70f;
        public float AmbientVolume = 0.55f;
        public float SfxVolume = 0.80f;
        public float VoiceVolume = 0.85f;
        public float Brightness = 0.75f;
        public float InputSensitivity = 0.50f;
        public int GraphicsQualityIndex = 2;
        public int TextSizeIndex = 2;

        /// <summary>
        /// Only English ships in this milestone. Other languages are shown in the
        /// dropdown as "Coming Soon" but are not selectable — always forced to 0.
        /// </summary>
        public int LanguageIndex = 0;
        public bool ReduceMotion = true;
        public bool HighContrast;
        public bool TutorialCompleted = true;

        public static AppLocalSettings CreateDefaults()
        {
            return new AppLocalSettings();
        }

        public static AppLocalSettings Load()
        {
            var settings = CreateDefaults();
            settings.MasterVolume = PlayerPrefs.GetFloat(PrefsPrefix + "MasterVolume", settings.MasterVolume);
            settings.MusicVolume = PlayerPrefs.GetFloat(PrefsPrefix + "MusicVolume", settings.MusicVolume);
            settings.AmbientVolume = PlayerPrefs.GetFloat(PrefsPrefix + "AmbientVolume", settings.AmbientVolume);
            settings.SfxVolume = PlayerPrefs.GetFloat(PrefsPrefix + "SfxVolume", settings.SfxVolume);
            settings.VoiceVolume = PlayerPrefs.GetFloat(PrefsPrefix + "VoiceVolume", settings.VoiceVolume);
            settings.Brightness = PlayerPrefs.GetFloat(PrefsPrefix + "Brightness", settings.Brightness);
            settings.InputSensitivity = PlayerPrefs.GetFloat(PrefsPrefix + "InputSensitivity", settings.InputSensitivity);
            settings.GraphicsQualityIndex = PlayerPrefs.GetInt(PrefsPrefix + "GraphicsQualityIndex", settings.GraphicsQualityIndex);
            settings.TextSizeIndex = PlayerPrefs.GetInt(PrefsPrefix + "TextSizeIndex", settings.TextSizeIndex);
            // Forced to English (0) regardless of any stored value — other languages
            // are not selectable until localization content actually ships.
            settings.LanguageIndex = 0;
            settings.ReduceMotion = PlayerPrefs.GetInt(PrefsPrefix + "ReduceMotion", settings.ReduceMotion ? 1 : 0) == 1;
            settings.HighContrast = PlayerPrefs.GetInt(PrefsPrefix + "HighContrast", settings.HighContrast ? 1 : 0) == 1;
            settings.TutorialCompleted = PlayerPrefs.GetInt(PrefsPrefix + "TutorialCompleted", settings.TutorialCompleted ? 1 : 0) == 1;
            return settings;
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(PrefsPrefix + "MasterVolume", MasterVolume);
            PlayerPrefs.SetFloat(PrefsPrefix + "MusicVolume", MusicVolume);
            PlayerPrefs.SetFloat(PrefsPrefix + "AmbientVolume", AmbientVolume);
            PlayerPrefs.SetFloat(PrefsPrefix + "SfxVolume", SfxVolume);
            PlayerPrefs.SetFloat(PrefsPrefix + "VoiceVolume", VoiceVolume);
            PlayerPrefs.SetFloat(PrefsPrefix + "Brightness", Brightness);
            PlayerPrefs.SetFloat(PrefsPrefix + "InputSensitivity", InputSensitivity);
            PlayerPrefs.SetInt(PrefsPrefix + "GraphicsQualityIndex", GraphicsQualityIndex);
            PlayerPrefs.SetInt(PrefsPrefix + "TextSizeIndex", TextSizeIndex);
            // LanguageIndex is intentionally not persisted while only English ships.
            PlayerPrefs.SetInt(PrefsPrefix + "ReduceMotion", ReduceMotion ? 1 : 0);
            PlayerPrefs.SetInt(PrefsPrefix + "HighContrast", HighContrast ? 1 : 0);
            PlayerPrefs.SetInt(PrefsPrefix + "TutorialCompleted", TutorialCompleted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ApplyRuntimeEffects()
        {
            AudioListener.volume = Mathf.Clamp01(MasterVolume);

            int qualityCount = QualitySettings.names.Length;
            if (qualityCount > 0)
            {
                int clamped = Mathf.Clamp(GraphicsQualityIndex, 0, qualityCount - 1);
                if (QualitySettings.GetQualityLevel() != clamped)
                {
                    QualitySettings.SetQualityLevel(clamped, true);
                }
            }

#if UNITY_IOS || UNITY_ANDROID
            try
            {
                Screen.brightness = Mathf.Clamp01(Brightness);
            }
            catch (Exception)
            {
                // Brightness may be unavailable on some devices / editor hosts.
            }
#endif
        }

        public AppLocalSettings Clone()
        {
            return new AppLocalSettings
            {
                MasterVolume = MasterVolume,
                MusicVolume = MusicVolume,
                AmbientVolume = AmbientVolume,
                SfxVolume = SfxVolume,
                VoiceVolume = VoiceVolume,
                Brightness = Brightness,
                InputSensitivity = InputSensitivity,
                GraphicsQualityIndex = GraphicsQualityIndex,
                TextSizeIndex = TextSizeIndex,
                LanguageIndex = LanguageIndex,
                ReduceMotion = ReduceMotion,
                HighContrast = HighContrast,
                TutorialCompleted = TutorialCompleted
            };
        }
    }
}
