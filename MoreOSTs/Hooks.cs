using EntityStates.Missions.BrotherEncounter;
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
        
        public void Apply()
        {
            On.RoR2.AudioManager.VolumeConVar.SetString += On_VolumeConVar_SetString;
            TeleporterInteraction.onTeleporterBeginChargingGlobal += TeleporterInteraction_SongChange;
            TeleporterInteraction.onTeleporterChargedGlobal += TeleporterInteraction_SongChange;
            PauseManager.onPauseStartGlobal += PauseManager_PauseStart;
            PauseManager.onPauseEndGlobal += PauseManager_PauseEnd;
            SceneManager.sceneLoaded += SceneManager_SongChange;

            On.EntityStates.Missions.BrotherEncounter.BrotherEncounterBaseState.OnEnter +=
                On_BrotherEncounterBaseState_OnEnter;

            //TODO: Mithrix - Test
            //TODO: Voidling
            // On.EntityStates.VoidRaidCrab.SpawnState.OnEnter
            // On.EntityStates.VoidRaidCrab.SpawnState.DeathState
            //TODO: False son
        }

        public void Remove()
        {
            On.RoR2.AudioManager.VolumeConVar.SetString -= On_VolumeConVar_SetString;
            TeleporterInteraction.onTeleporterBeginChargingGlobal -= TeleporterInteraction_SongChange;
            TeleporterInteraction.onTeleporterChargedGlobal -= TeleporterInteraction_SongChange;
            PauseManager.onPauseStartGlobal -= PauseManager_PauseStart;
            PauseManager.onPauseEndGlobal -= PauseManager_PauseEnd;
            SceneManager.sceneLoaded -= SceneManager_SongChange;
            
            On.EntityStates.Missions.BrotherEncounter.BrotherEncounterBaseState.OnEnter -=
                On_BrotherEncounterBaseState_OnEnter;
        }

        private void On_VolumeConVar_SetString(On.RoR2.AudioManager.VolumeConVar.orig_SetString orig, BaseConVar self, string newvalue)
        {
            orig(self, newvalue);
            
            if(self == AudioManager.cvVolumeParentMsx)
                Plugin.UpdateVolume();

            AkSoundEngine.SetRTPCValue(AudioManager.cvVolumeMsx.rtpcName, 0);
        }

        private void TeleporterInteraction_SongChange(TeleporterInteraction _)
        {
            Plugin.TeleporterStateChanged();
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
            Plugin.SceneChanged();
        }

        private void On_BrotherEncounterBaseState_OnEnter(On.EntityStates.Missions.BrotherEncounter.BrotherEncounterBaseState.orig_OnEnter orig, BrotherEncounterBaseState self)
        {
            orig(self);
            
            Plugin.MithrixStateChanged(self);
        }
    }
}