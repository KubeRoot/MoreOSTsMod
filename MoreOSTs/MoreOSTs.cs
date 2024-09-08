using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Reflection;
using System.Globalization;
using System.Security.Permissions;
using BepInEx.Configuration;
using BepInEx.Logging;
using NAudio.Extras;
using RoR2;
using Path = System.IO.Path;


//Including this allows the mod to access private fields/methods on game classes
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete


namespace MoreOSTs
{
    // The MoreOSTs plugin (previously OriginalSoundTrack) - For replacing the in game music with Risk of Rain 1 music (or your own).
    // You will need access to your own risk of rain 1 sound files (or any others you want to use).
    // Edit the settings.xml to specify how the music plays in game.
    // Also don't forget to mute the in game music in the in game settings (this plugin doesn't take away RoR2 music).

    [BepInPlugin("com.mrcountermax.moreostsmod", "MoreOSTsMod", "2.0.0")]
    [BepInDependency("com.rune580.riskofoptions")]
    public class MoreOSTs : BaseUnityPlugin {

        //private float globalMusicVolume = 0.5f; // default global music volume.
        // private ConfigEntry<float> globalMusicVolume;

        internal ManualLogSource logger => Logger;

        private Hooks hooks;
        private SongManager songManager;
        private SongPlayer songPlayer;
        public void OnEnable()
        {
            hooks = new Hooks(this).Apply();
        }

        public void OnDisable()
        {
            hooks?.Remove();
            songPlayer?.StopImmediate();
        }

        public void Awake() {
            // var pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pluginPath = "Z:/home/kuberoot/.config/r2modmanPlus-local/RiskOfRain2/profiles/Dev/BepInEx/dev_music/"; //TODO: Testing code
            
            // globalMusicVolume = Config.Bind(new ConfigDefinition("General", "Volume"), 40f, new ConfigDescription("The volume of the More OSTs Mod music. KEEP THE GAME'S MUSIC VOLUME AT 0!!!", new AcceptableValueRange<float>(0, 100)));
            // ModSettingsManager.AddOption(new StepSliderOption(globalMusicVolume, new StepSliderConfig{
            //     min = 0f,
            //     max = 100f,
            //     increment = 1f,
            //     formatString = "{0:0}%"
            // }));
            //
            // globalMusicVolume.SettingChanged += (_, __) => UpdateVolume();


            var modIconTexture = new Texture2D(0, 0);

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MoreOSTs.ror2_more_osts_mod_clean.png"))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                byte[] data = memoryStream.ToArray();
                modIconTexture.LoadImage(data);
            }

            Sprite modIconSprite = Sprite.Create(modIconTexture, new Rect(0, 0, modIconTexture.width, modIconTexture.height), new Vector2(0, 0));
            ModSettingsManager.SetModIcon(modIconSprite);

            songManager = new SongManager(this);          
            songManager.Load(Path.Combine(pluginPath, "settings.xml"), Path.Combine(pluginPath, "music"));

            songPlayer = new SongPlayer(this);
            songPlayer.SongEnding += SelectSong;
            
            UpdateVolume();
        }

        public void Update()
        {
            songPlayer.Update();
        }

        public bool IsTeleporterActive => (TeleporterInteraction.instance?.isCharging ?? false)
                                          || (TeleporterInteraction.instance?.isIdleToCharging ?? false);

        public void UpdateVolume() {
            songPlayer.Volume = float.Parse(AudioManager.cvVolumeMsx.GetString() ?? "100") / 100f; //TODO: Test if this is the correct value
        }

        public void SelectSong()
        {
            var boss = IsTeleporterActive;
            var scene = SceneManager.GetActiveScene().name;

            // Try to get a new song to play, if one isn't found, try to find any song
            var song = songManager.PickSong(scene, boss, songPlayer.CurrentSong?.Name)
                       ?? songManager.PickSong(scene, boss)
                       ?? songManager.PickSong(null, boss);

            if (!song.HasValue)
            {
                Logger.LogError("Failed to find a valid song");
                return;
            }
            
            songPlayer.Play(song.Value);
        }

        public void Pause() => songPlayer.Pause();
        public void Resume() => songPlayer.Resume();
    }
}
