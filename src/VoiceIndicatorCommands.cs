using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ToasterVoiceChatUI;

// Handles /commands for voice indicator settings
public static class VoiceIndicatorCommands
{
    private const string HELP_COLOR = "#FFA500"; // Orange
    private const string VALUE_COLOR = "#00FF00"; // Green
    private const string ERROR_COLOR = "#FF0000"; // Red
    
    // Patch UIChat.Client_SendClientChatMessage to intercept commands before sending to server
    [HarmonyPatch(typeof(UIChat), "Client_SendClientChatMessage")]
    public static class ClientSendClientChatMessagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(string message, bool useTeamChat)
        {
            if (string.IsNullOrEmpty(message)) return true;
            
            // Check if it's a voice indicator command
            if (message.StartsWith("/voice", StringComparison.OrdinalIgnoreCase))
            {
                ProcessVoiceCommand(message);
                return false; // Don't send to server
            }
            
            return true; // Let other messages through
        }
    }
    
    private static void ProcessVoiceCommand(string message)
    {
        try
        {
            string[] parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                ShowHelp();
                return;
            }
            
            string subCommand = parts[1].ToLower();
            
            switch (subCommand)
            {
                case "mode":
                    HandleModeCommand(parts);
                    break;
                case "height":
                    HandleHeightCommand(parts);
                    break;
                case "size":
                    HandleSizeCommand(parts);
                    break;
                case "help":
                default:
                    ShowHelp();
                    break;
            }
        }
        catch (Exception e)
        {
            ShowMessage($"<size=75%><color={ERROR_COLOR}>Error processing command: {e.Message}</color></size>");
            Plugin.LogError($"Error processing voice command: {e}");
        }
    }
    
    private static void HandleModeCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            // Toggle mode if no argument provided
            VoiceChatSettings.ToggleIndicatorMode();
            string currentMode = GetModeString(VoiceChatSettings.IndicatorMode);
            ShowMessage($"<size=75%>Voice indicator mode: <color={VALUE_COLOR}>{currentMode}</color></size>");
            return;
        }
        
        string modeArg = parts[2].ToLower();
        VoiceIndicatorMode newMode;
        
        switch (modeArg)
        {
            case "off":
            case "none":
            case "0":
                newMode = VoiceIndicatorMode.None;
                break;
            case "text":
            case "1":
                newMode = VoiceIndicatorMode.Text;
                break;
            case "image":
            case "icon":
            case "2":
                newMode = VoiceIndicatorMode.Image;
                break;
            default:
                ShowMessage($"<size=75%><color={ERROR_COLOR}>Invalid mode. Use: off, text, or image</color></size>");
                return;
        }
        
        VoiceChatSettings.IndicatorMode = newMode;
        RefreshIndicators();
        ShowMessage($"<size=75%>Voice indicator mode set to: <color={VALUE_COLOR}>{GetModeString(newMode)}</color></size>");
    }
    
    private static void HandleHeightCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            ShowMessage($"<size=75%>Current height: <color={VALUE_COLOR}>{VoiceChatSettings.IndicatorHeight:F1}</color> (range: 1.0 - 10.0)</size>");
            return;
        }
        
        string heightArg = parts[2];
        
        // Handle relative adjustments
        if (heightArg.StartsWith("+") || heightArg.StartsWith("-"))
        {
            if (float.TryParse(heightArg, out float delta))
            {
                float newHeight = Mathf.Clamp(VoiceChatSettings.IndicatorHeight + delta, 1.0f, 10.0f);
                VoiceChatSettings.IndicatorHeight = newHeight;
                RefreshIndicators();
                ShowMessage($"<size=75%>Indicator height: <color={VALUE_COLOR}>{newHeight:F1}</color></size>");
            }
            else
            {
                ShowMessage($"<size=75%><color={ERROR_COLOR}>Invalid height value</color></size>");
            }
            return;
        }
        
        // Handle absolute value
        if (float.TryParse(heightArg, out float height))
        {
            height = Mathf.Clamp(height, 1.0f, 10.0f);
            VoiceChatSettings.IndicatorHeight = height;
            RefreshIndicators();
            ShowMessage($"<size=75%>Indicator height set to: <color={VALUE_COLOR}>{height:F1}</color></size>");
        }
        else
        {
            ShowMessage($"<size=75%><color={ERROR_COLOR}>Invalid height value. Use a number between 1.0 and 10.0</color></size>");
        }
    }
    
    private static void HandleSizeCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            ShowMessage($"<size=75%>Current size: <color={VALUE_COLOR}>{VoiceChatSettings.IndicatorSize:F1}</color> 0.5 - 5.0</size>");
            return;
        }
        
        string sizeArg = parts[2];
        
        // Handle relative adjustments
        if (sizeArg.StartsWith("+") || sizeArg.StartsWith("-"))
        {
            if (float.TryParse(sizeArg, out float delta))
            {
                float newSize = Mathf.Clamp(VoiceChatSettings.IndicatorSize + delta, 0.5f, 5.0f);
                VoiceChatSettings.IndicatorSize = newSize;
                RefreshIndicators();
                ShowMessage($"<size=75%>Indicator size: <color={VALUE_COLOR}>{newSize:F1}</color></size>");
            }
            else
            {
                ShowMessage($"<size=75%><color={ERROR_COLOR}>Invalid size value</color></size>");
            }
            return;
        }
        
        // Handle absolute value
        if (float.TryParse(sizeArg, out float size))
        {
            size = Mathf.Clamp(size, 0.5f, 5.0f);
            VoiceChatSettings.IndicatorSize = size;
            RefreshIndicators();
            ShowMessage($"<size=75%>Indicator size set to: <color={VALUE_COLOR}>{size:F1}</color></size>");
        }
        else
        {
            ShowMessage($"<size=75%><color={ERROR_COLOR}>Invalid size value. Use a number between 0.5 and 5.0</color></size>");
        }
    }
    
    private static void ShowHelp()
    {
        var lines = new[]
        {
            $"<size=75%><color={HELP_COLOR}>=== Voice Indicator Commands ===</color></size>",
            $"<size=75%><color={HELP_COLOR}>/voice mode [off|text|image]</color> - Set display mode</size>",
            $"<size=75%><color={HELP_COLOR}>/voice height [value]</color> - Set height (1.0-10.0</size>",
            $"<size=75%><color={HELP_COLOR}>/voice size [value]</color> - Set size 0.5-5.0</size>",
            $"<size=75%><color={HELP_COLOR}>/voice help</color> - Show this help</size>",
            $"<size=75%><Current: Mode=<color={VALUE_COLOR}>{GetModeString(VoiceChatSettings.IndicatorMode)}</color>, " +
            $"Height=<color={VALUE_COLOR}>{VoiceChatSettings.IndicatorHeight:F1}</color>, " +
            $"Size=<color={VALUE_COLOR}>{VoiceChatSettings.IndicatorSize:F1}</color></size>"
        };
        
        foreach (var line in lines)
        {
            ShowMessage(line);
        }
    }
    
    private static string GetModeString(VoiceIndicatorMode mode)
    {
        return mode switch
        {
            VoiceIndicatorMode.None => "Off",
            VoiceIndicatorMode.Text => "Text",
            VoiceIndicatorMode.Image => "Image",
            _ => "Unknown"
        };
    }
    
    private static void RefreshIndicators()
    {
        if (PlayerActivityIndicator.Instance != null)
        {
            PlayerActivityIndicator.Instance.RefreshVoiceIndicatorState();
        }
    }
    
    private static void ShowMessage(string message)
    {
        try
        {
            var chat = UIChat.Instance;
            if (chat != null)
            {
                chat.AddChatMessage(message);
            }
        }
        catch
        {
            // Chat might not be available
        }
    }
}
