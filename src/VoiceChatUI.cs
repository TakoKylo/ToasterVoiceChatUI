using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

namespace ToasterVoiceChatUI;

public static class VoiceChatUI
{
    private static VisualElement _voiceChatUIContainer; // Container for Flexbox
    public static VisualElement _voiceChatUI;
    private static Label _talkingUsersLabel;
    private static bool isSetup = false;
    
    static readonly FieldInfo _uiHudField = typeof(UIHUDController)
        .GetField("uiHud",
            BindingFlags.Instance | BindingFlags.NonPublic);
    
    static readonly FieldInfo _uiHudContainerField = typeof(UIHUD)
        .GetField("container",
            BindingFlags.Instance | BindingFlags.NonPublic);

    private static void Setup(UIHUDController uihudController)
    {
        UIHUD uiHud = (UIHUD) _uiHudField.GetValue(uihudController);
        VisualElement uiHudContainer = (VisualElement) _uiHudContainerField.GetValue(uiHud);
            
        // TUtils.LogDebug($"Patch: UIHUDController.Start called.");
        // if (!Plugin.configTeamIndicatorEnabled.Value) return;
        CreateVoiceChatUI(uiHud, uiHudContainer);
        
        // Initialize PlayerActivityIndicator for overhead indicators
        InitializePlayerActivityIndicator();
        
        EventManager em = EventManager.Instance;
        // em.AddEventListener("Event_Client_OnServerConfiguration", new Action<Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object>>(
        //     (evt) =>
        //     {
        //         TUtils.LogDebug($"Event_Client_OnServerConfiguration");
        //         _teamColorBar.style.display = DisplayStyle.Flex;
        //     }));
        em.AddEventListener("Event_OnClientDisconnected", new Action<Dictionary<string, object>>(
            (evt) =>
            {
                // TUtils.LogDebug($"Event_OnClientDisconnected");
                _voiceChatUIContainer.style.display = DisplayStyle.None;
            }));
    }
    
    [HarmonyPatch(typeof(UIHUDController), "Event_OnPlayerBodySpawned")]
    public static class UiHudControllerEventOnPlayerBodySpawnedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(UIHUDController __instance, Dictionary<string, object> message)
        {
            if (isSetup) return;

            Setup(__instance);
            isSetup = true;
        }
    }
    
    private static void InitializePlayerActivityIndicator()
    {
        try
        {
            // Create a persistent GameObject for the PlayerActivityIndicator
            GameObject indicatorObj = new GameObject("ToasterVoiceActivityIndicator");
            GameObject.DontDestroyOnLoad(indicatorObj);
            indicatorObj.AddComponent<PlayerActivityIndicator>();
        }
        catch (System.Exception e)
        {
            Plugin.LogError($"Failed to initialize PlayerActivityIndicator: {e.Message}");
        }
    }

    private static void CreateVoiceChatUI(UIHUD hud, VisualElement rootVisualElement)
    {
        if (rootVisualElement == null)
        {
            Plugin.LogError("Root VisualElement not found!");
            return;
        }
        
        // VisualElement root = rootVisualElement.parent.parent;
        VisualElement root = rootVisualElement.parent;
        // Create a CONTAINER VisualElement for Flexbox layout
        _voiceChatUIContainer = new VisualElement();
        _voiceChatUIContainer.name = "VoiceChatUIContainer";

        // Flexbox styles for the CONTAINER
        root.Add(_voiceChatUIContainer);
        _voiceChatUIContainer.style.position = Position.Absolute;
        _voiceChatUIContainer.style.display = DisplayStyle.None; // Start hidden
        // _voiceChatUIContainer.style.position = Position.Absolute;
        _voiceChatUIContainer.style.bottom = 0; // Anchor to the bottom
        _voiceChatUIContainer.style.right = 0;
        // _voiceChatUIContainer.style.width = Length.Percent(100f);
        // Plugin.Log.LogInfo($"container width: {_voiceChatUIContainer.resolvedStyle.width}");
        
        // _voiceChatUIContainer.style.left = 0;
        _voiceChatUIContainer.style.backgroundColor = new StyleColor(new Color(91f/255f, 91/255f, 91f/255f, 0.3f));
        _voiceChatUIContainer.style.borderBottomLeftRadius = new StyleLength(new Length(8, LengthUnit.Pixel));       
        _voiceChatUIContainer.style.borderBottomRightRadius = new StyleLength(new Length(8, LengthUnit.Pixel));   
        _voiceChatUIContainer.style.borderTopLeftRadius = new StyleLength(new Length(8, LengthUnit.Pixel));   
        _voiceChatUIContainer.style.borderTopRightRadius = new StyleLength(new Length(8, LengthUnit.Pixel));   
        _voiceChatUIContainer.style.paddingBottom = new StyleLength(new Length(10, LengthUnit.Pixel));
        _voiceChatUIContainer.style.paddingTop = new StyleLength(new Length(10, LengthUnit.Pixel));
        _voiceChatUIContainer.style.paddingLeft = new StyleLength(new Length(10, LengthUnit.Pixel));
        _voiceChatUIContainer.style.paddingRight = new StyleLength(new Length(10, LengthUnit.Pixel));
        _voiceChatUIContainer.style.marginBottom = new StyleLength(new Length(50, LengthUnit.Pixel));
        _voiceChatUIContainer.style.marginRight = new StyleLength(new Length(50, LengthUnit.Pixel));
        
        VisualElement _voiceChatUI = new VisualElement();
        _voiceChatUI.style.display = DisplayStyle.Flex;
        _voiceChatUI.style.position = Position.Relative;
        _voiceChatUI.style.flexDirection = FlexDirection.Column;
        _voiceChatUI.style.alignItems = Align.Center;
        // _voiceChatUI.style.maxWidth = new StyleLength(new Length(300, LengthUnit.Pixel));
        // _voiceChatUI.style.minWidth = new StyleLength(new Length(300, LengthUnit.Pixel));
        // _voiceChatUI.style.minHeight = new StyleLength(new Length(300, LengthUnit.Pixel));
        _voiceChatUIContainer.Add(_voiceChatUI);
        
        // Create a new Label (UI Toolkit's equivalent of TextMeshProUGUI)
        Label _voiceTitleLabel = new Label("Voice Chat"); // Initial text
        _voiceTitleLabel.text = "Voice Chat";
        _voiceTitleLabel.style.fontSize = 16;
        _voiceTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _voiceTitleLabel.name = "Voice Title Label"; // Useful for styling in USS
        _voiceTitleLabel.style.color = Color.white;
        // _voiceTitleLabel.style.backgroundColor = new StyleColor(new Color(255f/255f, 91/255f, 91f/255f, 0.3f));
        _voiceChatUI.Add(_voiceTitleLabel);
 

        // Style the Label (you can do this in C# or with a USS file)
        _talkingUsersLabel = new Label("Nobody is talking yet");
        _talkingUsersLabel.text = "Nobody is talking yet";
        _talkingUsersLabel.style.fontSize = 12;
        // _talkingUsersLabel.style.marginTop = new StyleLength(new Length(10, LengthUnit.Pixel));
        _talkingUsersLabel.style.color = Color.white;
        // _talkingUsersLabel.style.backgroundColor = new StyleColor(new Color(91f/255f, 255/255f, 91f/255f, 0.3f));
        // _talkingUsersLabel.style.position = Position.Absolute; //To allow for location customization
        _talkingUsersLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        // Set position in bottom-right corner
        // _pluginListLabel.style.right = 20; // Right position
        // _pluginListLabel.style.bottom = 80; // Bottom position
        // _talkingUsersLabel.style.width = 300;
        // _talkingUsersLabel.style.height = 300;
        _voiceChatUI.Add(_talkingUsersLabel);
        

        // // Center horizontally using Flexbox
        // _teamColorBarContainer.style.display = DisplayStyle.Flex;
        // _teamColorBarContainer.style.justifyContent = Justify.Center; //Center horizontally
        // _teamColorBarContainer.style.alignItems = Align.FlexEnd; // Align to the Bottom of the screen
        //
        // // TEAM COLOR BAR
        // // Create a new VisualElement for the team color bar
        // _teamColorBar = new VisualElement();
        // _teamColorBar.name = "TeamColorBar"; // Useful for styling in USS
        //
        // // float barWidth = 600; // Set the desired width of the bar
        // // Style the team color bar
        // _teamColorBar.style.position = Position.Absolute;
        // // Calculate the left and right margin based on the resolution
        // // float margin = (root.resolvedStyle.width - barWidth) / 2;
        // // // // Plugin.Log.LogInfo($"Screen width: {Screen.width}");
        // // //
        // // // // // Ensure the margin is not negative
        // // margin = Mathf.Max(0, margin);
        // // // Plugin.Log.LogInfo($"Computed margin: {margin}");
        // // //
        // // _teamColorBar.style.left = margin;
        // // _teamColorBar.style.right = margin;
        // _teamColorBar.style.bottom = 0; // Align to the bottom
        // _teamColorBar.style.height = 5; // Height of the bar
        // _teamColorBar.style.backgroundColor = _teamColorSpectator;
        //
        // _teamColorBar.style.minWidth = Length.Percent(100f);
        // _teamColorBar.style.maxWidth = Length.Percent(100f);
        // _teamColorBar.style.display = DisplayStyle.None;
        // // _teamColorBar.style.width = barWidth;
        // // _teamColorBar.style.unityTextAlign = TextAnchor.MiddleCenter; //This does not work because there is no content, there is just a shape
        //
        // // _teamColorBarContainer.Add(_teamColorBar);
        //
        // root.Add(_teamColorBar);
    }
    
    public static void UpdateTalkingUI()
    {
        // PlayerManager playerManager = NetworkBehaviourSingleton<PlayerManager>.instance;
        // Player localPlayer = playerManager.GetLocalPlayer();
        //
        // UIChat chat = NetworkBehaviourSingleton<UIChat>.Instance;
        // string output = "Talking players: ";
        // foreach (Player talkingPlayer in VoiceChatPatch.talkingPlayers)
        // {
        //     
        //     // If the player doesn't exist somehow?
        //     if (talkingPlayer == null)
        //     {
        //         VoiceChatPatch.talkingPlayers.Remove(talkingPlayer);
        //         continue;
        //     }
        //
        //     float distanceBetweenPlayer = 0;
        //     
        //     // If player has body, distance between bodies
        //     if (talkingPlayer.PlayerBody != null && localPlayer.PlayerBody != null)
        //     {
        //         distanceBetweenPlayer = Vector3.Distance(talkingPlayer.PlayerBody.Rigidbody.transform.position, localPlayer.PlayerBody.Rigidbody.transform.position);
        //     }
        //     
        //     // If player does not have body, distance between spectatorcamera and talking body
        //     if (talkingPlayer.PlayerBody == null)
        //     {
        //         // We can't even hear this guy
        //     }
        //
        //     if (talkingPlayer.PlayerBody != null && localPlayer.SpectatorCamera != null)
        //     {
        //         distanceBetweenPlayer = Vector3.Distance(talkingPlayer.PlayerBody.Rigidbody.transform.position, localPlayer.SpectatorCamera.CameraComponent.transform.position);
        //     }
        //
        //     output += $"{talkingPlayer.Username.Value.ToString()} ({Math.Round(distanceBetweenPlayer, 1)}u) ";
        // }
        //
        // chat.AddChatMessage(output);

        if (_talkingUsersLabel == null)
        {
            Plugin.LogError($"_talkingUsersLabel is null");
            return;
        }

        if (_voiceChatUIContainer == null)
        {
            Plugin.LogError($"_voiceChatUIContainer is null");
            return;
        }
        
        _talkingUsersLabel.text = GetTalkingUsersListAsString();
        // Plugin.Log($"Talking users as string {_talkingUsersLabel.text}");
        if (VoiceChatPatch.talkingPlayers.Count == 0)
        {
            // TODO uncomment this later after testing
            _voiceChatUIContainer.style.display = DisplayStyle.None;
        }
        else
        {
            _voiceChatUIContainer.style.display = DisplayStyle.Flex;
        }
        
        // mark that we are updating the UI at this time
        lastUpdateTime = Time.realtimeSinceStartup;
    }

    public static string GetTalkingUsersListAsString()
    {
        PlayerManager playerManager = PlayerManager.Instance;
        Player localPlayer = playerManager.GetLocalPlayer();
        UIChat chat = UIChat.Instance;
        string output = "";
        for (int i = 0; i < VoiceChatPatch.talkingPlayers.Count; i++)
        {
            Player talkingPlayer = VoiceChatPatch.talkingPlayers[i];
            // If the player doesn't exist somehow?
            if (talkingPlayer == null)
            {
                VoiceChatPatch.talkingPlayers.Remove(talkingPlayer);
                continue;
            }

            float distanceBetweenPlayer = 0;
            
            // If player has body, distance between bodies
            if (talkingPlayer.PlayerBody != null && localPlayer.PlayerBody != null)
            {
                distanceBetweenPlayer = Vector3.Distance(talkingPlayer.PlayerBody.Rigidbody.transform.position, localPlayer.PlayerBody.Rigidbody.transform.position);
            }
            // If player does not have body, distance between spectatorcamera and talking body
            if (talkingPlayer.PlayerBody == null)
            {
                // We can't even hear this guy
            }

            if (talkingPlayer.PlayerBody != null && localPlayer.SpectatorCamera != null)
            {
                distanceBetweenPlayer = Vector3.Distance(talkingPlayer.PlayerBody.Rigidbody.transform.position, localPlayer.SpectatorCamera.CameraComponent.transform.position);
            }

            output += chat.WrapInTeamColor(talkingPlayer.Team.Value, $"#{talkingPlayer.Number.Value.ToString()} {talkingPlayer.Username.Value.ToString()} - {Math.Round(distanceBetweenPlayer, 1)}m");
            if (i < VoiceChatPatch.talkingPlayers.Count - 1) output += "\n";
        }

        return output;
    }
    
    readonly static float uiUpdateRate = 0.2f;
    static float lastUpdateTime = 0;
    [HarmonyPatch(typeof(SynchronizedObjectManager), "Update")]
    public static class SynchronizedObjectManagerUpdate
    {
        [HarmonyPostfix]
        public static void Postfix(SynchronizedObjectManager __instance)
        {
            if (VoiceChatPatch.talkingPlayers.Count == 0) return;
            if(Time.realtimeSinceStartup - lastUpdateTime > uiUpdateRate) {
                UpdateTalkingUI();
            }
        }
    }
}