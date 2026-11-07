using UnityEngine;

namespace Horror.Settings
{
    /// <summary>
    /// Placeholder for the sound settings until FMOD is added.
    ///
    /// Right now the four volumes are saved, and Master also controls Unity's own audio
    /// (AudioListener.volume) so you can hear it working. When FMOD is in the project:
    ///   1. In FMOD Studio, make buses (mixer groups) called Music, SFX and NPC.
    ///   2. Delete the AudioListener line below and uncomment the FMOD block.
    ///   3. Make sure the bus paths match the names you used in FMOD Studio.
    /// </summary>
    public static class AudioVolumes
    {
        public const string MasterBusPath = "bus:/";
        public const string MusicBusPath = "bus:/Music";
        public const string SfxBusPath = "bus:/SFX";
        public const string NpcBusPath = "bus:/NPC";

        public static void Apply(SettingsData s)
        {
            AudioListener.volume = s.masterVolume;

            // --- FMOD (uncomment once the FMOD for Unity package is installed) ---
            // SetBusVolume(MasterBusPath, s.masterVolume);
            // SetBusVolume(MusicBusPath, s.musicVolume);
            // SetBusVolume(SfxBusPath, s.sfxVolume);
            // SetBusVolume(NpcBusPath, s.npcVolume);
        }

        // private static void SetBusVolume(string path, float volume)
        // {
        //     FMOD.Studio.Bus bus = FMODUnity.RuntimeManager.GetBus(path);
        //     if (bus.isValid()) bus.setVolume(volume);
        // }
    }
}
