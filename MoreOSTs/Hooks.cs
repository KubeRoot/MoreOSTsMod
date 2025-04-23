using EntityStates;
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

            On.EntityStates.VoidRaidCrab.SpawnState.OnEnter += On_VoidRaidCrab_SpawnState_Enter;
            On.EntityStates.VoidRaidCrab.DeathState.OnEnter += On_VoidRaidCrab_DeathState_Enter;

            On.EntityStates.FalseSonBoss.HeartSpawnState.OnEnter += On_FalseSonBoss_HeartSpawnState_Enter;
            On.EntityStates.FalseSonBoss.SkyJumpDeathState.OnEnter += On_FalseSonBoss_SkyJumpDeathState_Enter;
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
            
            On.EntityStates.VoidRaidCrab.SpawnState.OnEnter -= On_VoidRaidCrab_SpawnState_Enter;
            On.EntityStates.VoidRaidCrab.DeathState.OnEnter -= On_VoidRaidCrab_DeathState_Enter;

            On.EntityStates.FalseSonBoss.HeartSpawnState.OnEnter -= On_FalseSonBoss_HeartSpawnState_Enter;
            On.EntityStates.FalseSonBoss.SkyJumpDeathState.OnEnter -= On_FalseSonBoss_SkyJumpDeathState_Enter;
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

        private void On_VoidRaidCrab_SpawnState_Enter(On.EntityStates.VoidRaidCrab.SpawnState.orig_OnEnter orig, EntityStates.VoidRaidCrab.SpawnState self)
        {
            orig(self);
            
            Plugin.VoidlingStateChanged(self);
        }

        private void On_VoidRaidCrab_DeathState_Enter(On.EntityStates.VoidRaidCrab.DeathState.orig_OnEnter orig, EntityStates.VoidRaidCrab.DeathState self)
        {
            orig(self);
            
            Plugin.VoidlingStateChanged(self);
        }

        private void On_FalseSonBoss_HeartSpawnState_Enter(On.EntityStates.FalseSonBoss.HeartSpawnState.orig_OnEnter orig, EntityStates.FalseSonBoss.HeartSpawnState self)
        {
            orig(self);
            
            Plugin.FalseSonStateChanged(self);
        }

        private void On_FalseSonBoss_SkyJumpDeathState_Enter(On.EntityStates.FalseSonBoss.SkyJumpDeathState.orig_OnEnter orig, EntityStates.FalseSonBoss.SkyJumpDeathState self)
        {
            orig(self);
            
            Plugin.FalseSonStateChanged(self);
        }
    }
}