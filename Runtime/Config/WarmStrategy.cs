namespace KidzDev.Unity.Audio
{
    /// <summary>Which library clips <see cref="IAudioService.InitializeAsync"/> preloads before reporting ready.</summary>
    public enum WarmStrategy
    {
        /// <summary>Preload nothing — every clip loads on first play.</summary>
        None,

        /// <summary>Preload every entry tagged <see cref="SoundCategory.SFX"/>, ignoring <see cref="AudioServiceSettings.WarmCategories"/>.</summary>
        AllSfx,

        /// <summary>Preload only the categories listed in <see cref="AudioServiceSettings.WarmCategories"/> — unlike <see cref="AllSfx"/> this can include BGM, ambience, and the rest.</summary>
        ByCategory
    }
}
