using System.Collections.Generic;
using HarmonyLib;

namespace ToasterVoiceChatUI;

public static class VoiceChatPatch
{
    public static List<Player> talkingPlayers = new List<Player>();
    // TODO remove null players from this list when they leave
    
    [HarmonyPatch(typeof(PlayerBodyV2Controller), "Event_OnPlayerVoiceStarted")]
    public static class PlayerBodyV2ControllerEventOnPlayerVoiceStarted
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerBodyV2Controller __instance, Dictionary<string, object> message)
        {
            try
            {
                Player player = (Player) message["player"];
                if (player == null) return;
                
                if (!talkingPlayers.Contains(player))
                {
                    talkingPlayers.Add(player);
                    VoiceChatUI.UpdateTalkingUI();
                }
                
                // Notify PlayerTypingDetector for overhead indicator
                ulong steamId = RosterSnapshot.GetSteamId(player);
                if (steamId != 0)
                {
                    PlayerTypingDetector.SetPlayerTalking(steamId, true);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error in voice started event: " + ex.Message);
            }
            
            // StartedTalking(player.OwnerClientId);
        }
    }
    
    [HarmonyPatch(typeof(PlayerBodyV2Controller), "Event_OnPlayerVoiceStopped")]
    public static class PlayerBodyV2ControllerEventOnPlayerVoiceStopped
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerBodyV2Controller __instance, Dictionary<string, object> message)
        {
            try
            {
                Player player = (Player) message["player"];
                if (player == null) return;

                if (talkingPlayers.Contains(player))
                {
                    talkingPlayers.Remove(player);
                    VoiceChatUI.UpdateTalkingUI();
                }
                
                // Notify PlayerTypingDetector for overhead indicator
                ulong steamId = RosterSnapshot.GetSteamId(player);
                if (steamId != 0)
                {
                    PlayerTypingDetector.SetPlayerTalking(steamId, false);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error in voice stopped event: " + ex.Message);
            }
        }
    }
}