using RoR2;
using RoR2.ConVar;
using UnityEngine.SceneManagement;

namespace MoreOSTs
{
    public class Hooks
    {
        public MoreOSTs Plugin;

        public Hooks(MoreOSTs plugin)
        {
            Plugin = plugin;
        }
        
        public Hooks Apply()
        {
            On.RoR2.AudioManager.VolumeConVar.SetString += On_VolumeConVar_SetString;
            TeleporterInteraction.onTeleporterBeginChargingGlobal += TeleporterInteraction_SongChange;
            TeleporterInteraction.onTeleporterChargedGlobal += TeleporterInteraction_SongChange;
            PauseManager.onPauseStartGlobal += PauseManager_PauseStart;
            PauseManager.onPauseEndGlobal += PauseManager_PauseEnd;
            SceneManager.sceneLoaded += SceneManager_SongChange;
            
            //TODO: Mithrix
            //On.EntityStates.Missions.BrotherEncounter.Phase1.OnEnter
            //On.EntityStates.Missions.BrotherEncounter.EncounterFinished.OnEnter
            //TODO: Voidling
            // On.EntityStates.VoidRaidCrab.SpawnState.OnEnter
            // On.EntityStates.VoidRaidCrab.SpawnState.DeathState
            //TODO: False son
            
            return this;
        }

        public void Remove()
        {
            On.RoR2.AudioManager.VolumeConVar.SetString -= On_VolumeConVar_SetString;
            TeleporterInteraction.onTeleporterBeginChargingGlobal -= TeleporterInteraction_SongChange;
            TeleporterInteraction.onTeleporterChargedGlobal -= TeleporterInteraction_SongChange;
            PauseManager.onPauseStartGlobal -= PauseManager_PauseStart;
            PauseManager.onPauseEndGlobal -= PauseManager_PauseEnd;
            SceneManager.sceneLoaded -= SceneManager_SongChange;
        }

        private void On_VolumeConVar_SetString(On.RoR2.AudioManager.VolumeConVar.orig_SetString orig, BaseConVar self, string newvalue)
        {
            //TODO: Check if this works
            //TODO: Volume controls using in-game music volume setting
            orig(self, newvalue);
            
            if(self == AudioManager.cvVolumeMsx)
                Plugin.UpdateVolume();

            AkSoundEngine.SetRTPCValue(AudioManager.cvVolumeMsx.rtpcName, 0);
        }

        private void TeleporterInteraction_SongChange(TeleporterInteraction _)
        {
            Plugin.SelectSong();
        }

        private void PauseManager_PauseStart()
        {
            Plugin.Pause();
        }

        private void PauseManager_PauseEnd()
        {
            Plugin.Resume();
        }

        private void SceneManager_SongChange(Scene arg0, LoadSceneMode arg1)
        {
            Plugin.SelectSong();
        }
    }
}