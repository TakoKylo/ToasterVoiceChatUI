using System.Collections.Generic;
using UnityEngine;

namespace ToasterVoiceChatUI;

// Tracks which players are currently talking and manages their activity state
public static class PlayerTypingDetector
{
    public class PlayerActivity
    {
        public bool IsTalking { get; set; }
        public float LastActivityTime { get; set; }
    }
    
    private static Dictionary<ulong, PlayerActivity> _playerActivities = new Dictionary<ulong, PlayerActivity>();
    
    // Mark a player as talking
    public static void SetPlayerTalking(ulong steamId, bool isTalking)
    {
        if (!_playerActivities.ContainsKey(steamId))
        {
            _playerActivities[steamId] = new PlayerActivity();
        }
        
        _playerActivities[steamId].IsTalking = isTalking;
        _playerActivities[steamId].LastActivityTime = Time.time;
    }
    
    // Get all active player activities
    public static Dictionary<ulong, PlayerActivity> GetActiveActivities()
    {
        // Clean up old activities
        var toRemove = new List<ulong>();
        foreach (var kvp in _playerActivities)
        {
            if (!kvp.Value.IsTalking && Time.time - kvp.Value.LastActivityTime > 5f)
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var id in toRemove)
        {
            _playerActivities.Remove(id);
        }
        
        return _playerActivities;
    }
    
    // Clear all activities
    public static void Clear()
    {
        _playerActivities.Clear();
    }
}

// Helper class to get Steam ID from roster
public static class RosterSnapshot
{
    public static ulong GetSteamId(Player player)
    {
        if (player == null) return 0;
        
        try
        {
            // Get the Steam ID from the player's network object owner
            return player.OwnerClientId;
        }
        catch (System.Exception e)
        {
            Plugin.LogError($"Error getting Steam ID: {e.Message}");
            return 0;
        }
    }
}
