using System;
using System.IO;
using UnityEngine;

namespace ToasterVoiceChatUI;

// Configuration settings for voice chat indicators with persistent storage
[Serializable]
public class VoiceChatConfig
{
    // Voice indicator display mode
    public VoiceIndicatorMode IndicatorMode = VoiceIndicatorMode.Image;
    
    // Height of the indicator above player's head (in units)
    public float IndicatorHeight = 3.0f;
    
    // Size multiplier for the indicator
    public float IndicatorSize = 1.0f;
}

// Static accessor for voice chat settings with auto-save
public static class VoiceChatSettings
{
    private static VoiceChatConfig _config;
    private static string _configPath;
    
    static VoiceChatSettings()
    {
        LoadConfig();
    }
    
    public static VoiceIndicatorMode IndicatorMode
    {
        get => _config.IndicatorMode;
        set
        {
            _config.IndicatorMode = value;
            SaveConfig();
        }
    }
    
    public static float IndicatorHeight
    {
        get => _config.IndicatorHeight;
        set
        {
            _config.IndicatorHeight = value;
            SaveConfig();
        }
    }
    
    public static float IndicatorSize
    {
        get => _config.IndicatorSize;
        set
        {
            _config.IndicatorSize = value;
            SaveConfig();
        }
    }
    
    private static void LoadConfig()
    {
        try
        {
            string modDirectory = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
            _configPath = Path.Combine(modDirectory, "ToasterVoiceChatUI_config.json");
            
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                _config = JsonUtility.FromJson<VoiceChatConfig>(json);
                Plugin.Log("Config loaded from " + _configPath);
            }
            else
            {
                _config = new VoiceChatConfig();
                SaveConfig();
                Plugin.Log("Created new config at " + _configPath);
            }
        }
        catch (Exception ex)
        {
            Plugin.LogError("Failed to load config: " + ex.Message);
            _config = new VoiceChatConfig();
        }
    }
    
    private static void SaveConfig()
    {
        try
        {
            string json = JsonUtility.ToJson(_config, true);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Plugin.LogError("Failed to save config: " + ex.Message);
        }
    }
    
    // Cycle through indicator modes: None -> Text -> Image -> None
    public static void ToggleIndicatorMode()
    {
        IndicatorMode = IndicatorMode switch
        {
            VoiceIndicatorMode.None => VoiceIndicatorMode.Text,
            VoiceIndicatorMode.Text => VoiceIndicatorMode.Image,
            VoiceIndicatorMode.Image => VoiceIndicatorMode.None,
            _ => VoiceIndicatorMode.Text
        };
        
        // Refresh all indicators
        if (PlayerActivityIndicator.Instance != null)
        {
            PlayerActivityIndicator.Instance.RefreshVoiceIndicatorState();
        }
    }
}
