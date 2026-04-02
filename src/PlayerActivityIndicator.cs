// PlayerActivityIndicator.cs - Animated "is talking" text above players
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using TMPro;

namespace ToasterVoiceChatUI
{
    public enum VoiceIndicatorMode
    {
        None,
        Text,
        Image
    }
    
    public class PlayerActivityIndicator : MonoBehaviour
    {
        private static PlayerActivityIndicator _instance;
        private Dictionary<ulong, IndicatorData> _activeIndicators = new Dictionary<ulong, IndicatorData>();
        private float _updateInterval = 0.2f;
        private float _lastUpdateTime = 0f;
        private Player[] _cachedPlayers;
        private float _lastPlayerCacheTime = 0f;
        
        // Static texture cache for image mode
        private static Texture2D _voiceIndicatorTexture = null;
        private static bool _textureLoadAttempted = false;
        
        // Cache for voice indicator setting to avoid reflection every frame
        private bool _cachedVoiceIndicatorEnabled = true;
        private float _lastIndicatorCheckTime = 0f;
        private const float INDICATOR_CHECK_INTERVAL = 0.5f; // Check every 0.5 seconds instead of every frame
        
        // Animation states for "is talking..."
        private readonly string[] _animationFrames = new string[]
        {
            "is talking",
            "is talking.",
            "is talking..",
            "is talking..."
        };
        private float _animationSpeed = 0.5f; // Change frame every 0.5 seconds
        
        private class IndicatorData
        {
            public TextMeshPro textMesh;
            public GameObject imageObject;
            public SpriteRenderer spriteRenderer;
            public string playerName;
            public float animationTimer;
            public int currentFrame;
            public VoiceIndicatorMode mode;
        }
        
        public static PlayerActivityIndicator Instance => _instance;
        
        void Awake()
        {
            _instance = this;
            LoadVoiceIndicatorTexture();
        }
        
        private static void LoadVoiceIndicatorTexture()
        {
            if (_textureLoadAttempted) return;
            _textureLoadAttempted = true;
            
            try
            {
                // Try to load from mod directory
                string modDirectory = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
                string imagePath = Path.Combine(modDirectory, "kndzyi49pe691.png");
                
                if (File.Exists(imagePath))
                {
                    byte[] imageData = File.ReadAllBytes(imagePath);
                    _voiceIndicatorTexture = new Texture2D(2, 2);
                    if (!_voiceIndicatorTexture.LoadImage(imageData))
                    {
                        Plugin.LogError("Failed to load voice indicator image");
                        _voiceIndicatorTexture = null;
                    }
                }
                else
                {
                    Plugin.LogError("Voice indicator image not found at " + imagePath);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error loading voice indicator texture: " + ex.Message);
            }
        }
        
        void OnDestroy()
        {
            CleanupAllIndicators();
            _instance = null;
        }
        
        void Update()
        {
            try
            {
                float currentTime = Time.time;
                if (currentTime - _lastUpdateTime >= _updateInterval)
                {
                    UpdateIndicators();
                    _lastUpdateTime = currentTime;
                }
            }
            catch (System.OverflowException) { /* Suppress Unity networking buffer overflow errors */ }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerActivity] Error in Update: {ex.Message}");
            }
        }
        
        private void UpdateIndicators()
        {
            try
            {
                // Check if voice indicator is enabled in config (cached to avoid reflection spam)
                float currentTime = Time.time;
                if (currentTime - _lastIndicatorCheckTime >= INDICATOR_CHECK_INTERVAL)
                {
                    _cachedVoiceIndicatorEnabled = IsVoiceIndicatorEnabled();
                    _lastIndicatorCheckTime = currentTime;
                }
                
                if (!_cachedVoiceIndicatorEnabled)
                {
                    // Clean up all indicators if disabled
                    if (_activeIndicators.Count > 0)
                    {
                        CleanupAllIndicators();
                    }
                    return;
                }
                
                float deltaTime = currentTime - _lastUpdateTime;
                
                // Cache players to avoid expensive FindObjectsByType calls
                if (_cachedPlayers == null || currentTime - _lastPlayerCacheTime > 2.0f)
                {
                    _cachedPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
                    _lastPlayerCacheTime = currentTime;
                }
                
                var activities = PlayerTypingDetector.GetActiveActivities();
                var currentActiveSteamIds = new HashSet<ulong>();
                
                foreach (var kvp in activities)
                {
                    ulong steamId = kvp.Key;
                    var activity = kvp.Value;
                    
                    // Only show if player is talking
                    if (!activity.IsTalking)
                        continue;
                    
                    currentActiveSteamIds.Add(steamId);
                    
                    // Find the player GameObject
                    Player player = FindPlayerBySteamId(steamId);
                    if (player == null)
                    {
                        if (_activeIndicators.ContainsKey(steamId))
                        {
                            DestroyIndicator(steamId);
                        }
                        continue;
                    }
                    
                    // Create or update indicator
                    if (!_activeIndicators.ContainsKey(steamId))
                    {
                        CreateIndicator(steamId, player);
                    }
                    else
                    {  
                        UpdateIndicatorAnimation(steamId, player, deltaTime);
                    }
                }
                
                // Remove indicators for players no longer active
                var toRemove = _activeIndicators.Keys.Where(id => !currentActiveSteamIds.Contains(id)).ToList();
                foreach (var steamId in toRemove)
                {
                    DestroyIndicator(steamId);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error updating indicators: " + ex.Message);
            }
        }
        
        private Player FindPlayerBySteamId(ulong steamId)
        {
            try
            {
                if (_cachedPlayers != null)
                {
                    foreach (var p in _cachedPlayers)
                    {
                        if (p != null && RosterSnapshot.GetSteamId(p) == steamId)
                            return p;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error finding player: " + ex.Message);
            }
            return null;
        }
        
        private void CreateIndicator(ulong steamId, Player player)
        {
            try
            {
                Transform attachPoint = player.PlayerBody != null ? player.PlayerBody.transform : player.transform;
                string playerName = player.Username?.Value.ToString() ?? "Player";
                
                float indicatorHeight = VoiceChatSettings.IndicatorHeight;
                float indicatorSize = VoiceChatSettings.IndicatorSize;
                VoiceIndicatorMode mode = GetIndicatorMode();
                
                // Skip creating indicator if mode is None
                if (mode == VoiceIndicatorMode.None)
                {
                    return;
                }
                else if (mode == VoiceIndicatorMode.Image)
                {
                    CreateImageIndicator(steamId, player, attachPoint, playerName, indicatorHeight, indicatorSize);
                }
                else
                {
                    CreateTextIndicator(steamId, player, attachPoint, playerName, indicatorHeight, indicatorSize);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error creating indicator: " + ex.Message);
            }
        }
        
        private void CreateTextIndicator(ulong steamId, Player player, Transform attachPoint, string playerName, float height, float size)
        {
            // Create animated "is talking..." text
            GameObject textObj = new GameObject($"VoiceIndicator_{steamId}");
            textObj.tag = "ToasterVoiceIndicator";
            textObj.name = $"[ToasterVoice] Indicator - {playerName}";
            textObj.transform.SetParent(attachPoint, false);
            textObj.transform.localPosition = new Vector3(0, height, 0);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;
            
            TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
            
            // Get team color
            Color teamColor = GetTeamColor(player);
            
            // Configure text appearance
            textMesh.text = $"{playerName} {_animationFrames[0]}";
            textMesh.fontSize = 0.8f * size;
            textMesh.color = teamColor;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.fontStyle = FontStyles.Bold;
            
            var rectTransform = textObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(10, 2);
            }
            
            // Make it face the camera
            textObj.AddComponent<BillboardToCamera>();
            
            // Store in dictionary
            _activeIndicators[steamId] = new IndicatorData
            {
                textMesh = textMesh,
                imageObject = null,
                spriteRenderer = null,
                playerName = playerName,
                animationTimer = 0f,
                currentFrame = 0,
                mode = VoiceIndicatorMode.Text
            };
        }
        
        private void CreateImageIndicator(ulong steamId, Player player, Transform attachPoint, string playerName, float height, float size)
        {
            if (_voiceIndicatorTexture == null)
            {
                Plugin.LogError("Voice indicator texture not loaded, falling back to text mode");
                CreateTextIndicator(steamId, player, attachPoint, playerName, height, size);
                return;
            }
            
            // Create image object
            GameObject imageObj = new GameObject($"VoiceIndicator_{steamId}");
            imageObj.name = $"[ToasterVoice] Indicator - {playerName}";
            imageObj.transform.SetParent(attachPoint, false);
            imageObj.transform.localPosition = new Vector3(0, height, 0);
            imageObj.transform.localRotation = Quaternion.identity;
            imageObj.transform.localScale = Vector3.one * size;
            
            // Create sprite from texture
            Sprite sprite = Sprite.Create(
                _voiceIndicatorTexture,
                new Rect(0, 0, _voiceIndicatorTexture.width, _voiceIndicatorTexture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            
            // Add sprite renderer
            SpriteRenderer spriteRenderer = imageObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = GetTeamColor(player);
            
            // Make it face the camera
            imageObj.AddComponent<BillboardToCamera>();
            
            // Store in dictionary
            _activeIndicators[steamId] = new IndicatorData
            {
                textMesh = null,
                imageObject = imageObj,
                spriteRenderer = spriteRenderer,
                playerName = playerName,
                animationTimer = 0f,
                currentFrame = 0,
                mode = VoiceIndicatorMode.Image
            };
        }
        
        private void UpdateIndicatorAnimation(ulong steamId, Player player, float deltaTime)
        {
            try
            {
                if (!_activeIndicators.TryGetValue(steamId, out IndicatorData data))
                {
                    _activeIndicators.Remove(steamId);
                    return;
                }
                
                if (data.mode == VoiceIndicatorMode.Text)
                {
                    if (data.textMesh == null)
                    {
                        _activeIndicators.Remove(steamId);
                        return;
                    }
                    
                    // Update animation timer
                    data.animationTimer += deltaTime;
                    
                    // Change frame when timer exceeds animation speed
                    if (data.animationTimer >= _animationSpeed)
                    {
                        data.animationTimer = 0f;
                        data.currentFrame = (data.currentFrame + 1) % _animationFrames.Length;
                        
                        // Update text with new frame
                        data.textMesh.text = $"{data.playerName} {_animationFrames[data.currentFrame]}";
                    }
                    
                    // Update team color
                    data.textMesh.color = GetTeamColor(player);
                    
                    // Make sure it's parented correctly
                    Transform attachPoint = player.PlayerBody != null ? player.PlayerBody.transform : player.transform;
                    if (data.textMesh.transform.parent != attachPoint)
                    {
                        float indicatorHeight = VoiceChatSettings.IndicatorHeight;
                        data.textMesh.transform.SetParent(attachPoint, false);
                        data.textMesh.transform.localPosition = new Vector3(0, indicatorHeight, 0);
                        data.textMesh.transform.localRotation = Quaternion.identity;
                        data.textMesh.transform.localScale = Vector3.one;
                    }
                }
                else // Image mode
                {
                    if (data.spriteRenderer == null)
                    {
                        _activeIndicators.Remove(steamId);
                        return;
                    }
                    
                    // Update team color for image
                    data.spriteRenderer.color = GetTeamColor(player);
                    
                    // Make sure it's parented correctly
                    Transform attachPoint = player.PlayerBody != null ? player.PlayerBody.transform : player.transform;
                    if (data.imageObject.transform.parent != attachPoint)
                    {
                        float indicatorHeight = VoiceChatSettings.IndicatorHeight;
                        data.imageObject.transform.SetParent(attachPoint, false);
                        data.imageObject.transform.localPosition = new Vector3(0, indicatorHeight, 0);
                        data.imageObject.transform.localRotation = Quaternion.identity;
                        data.imageObject.transform.localScale = Vector3.one * VoiceChatSettings.IndicatorSize;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error updating indicator animation: " + ex.Message);
            }
        }
        
        private void DestroyIndicator(ulong steamId)
        {
            if (_activeIndicators.TryGetValue(steamId, out IndicatorData data))
            {
                if (data.textMesh != null && data.textMesh.gameObject != null)
                    Destroy(data.textMesh.gameObject);
                if (data.imageObject != null)
                    Destroy(data.imageObject);
                _activeIndicators.Remove(steamId);
            }
        }
        
        private void CleanupAllIndicators()
        {
            foreach (var kvp in _activeIndicators)
            {
                if (kvp.Value.textMesh != null && kvp.Value.textMesh.gameObject != null)
                    Destroy(kvp.Value.textMesh.gameObject);
                if (kvp.Value.imageObject != null)
                    Destroy(kvp.Value.imageObject);
            }
            _activeIndicators.Clear();
        }
        
        private Color GetTeamColor(Player player)
        {
            try
            {
                if (player == null || player.Team == null)
                    return Color.white; // Default white for spectators/unknown
                
                PlayerTeam team = player.Team.Value;
                
                switch (team)
                {
                    //case PlayerTeam.Red:
                    //    return new Color(1f, 0.2f, 0.2f); // Bright red
                    //case PlayerTeam.Blue:
                    //    return new Color(0.2f, 0.5f, 1f); // Bright blue
                    //case PlayerTeam.Spectator:
                    //    return new Color(0.7f, 0.7f, 0.7f); // Light grey for spectators
                    default:
                        return Color.white; // Default white
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error getting team color: " + ex.Message);
                return Color.white;
            }
        }
        
        private bool IsVoiceIndicatorEnabled()
        {
            // Always enabled for this mod
            return true;
        }
        
        private VoiceIndicatorMode GetIndicatorMode()
        {
            var mode = VoiceChatSettings.IndicatorMode;
            
            // If image mode is selected but texture isn't loaded, fall back to text
            if (mode == VoiceIndicatorMode.Image && _voiceIndicatorTexture == null)
            {
                return VoiceIndicatorMode.Text;
            }
            
            return mode;
        }
        
        // Public method to force immediate cache refresh (called from toggle)
        public void RefreshVoiceIndicatorState()
        {
            _cachedVoiceIndicatorEnabled = IsVoiceIndicatorEnabled();
            _lastIndicatorCheckTime = Time.time;
            
            if (!_cachedVoiceIndicatorEnabled)
            {
                CleanupAllIndicators();
            }
            else
            {
                // Recreate all indicators with new mode
                CleanupAllIndicators();
            }
        }
        

    }
    
    // Helper component to make the indicator always face the camera
    public class BillboardToCamera : MonoBehaviour
    {
        private Camera _localPlayerCamera;
        private float _lastCameraCheck = 0f;
        
        void LateUpdate()
        {
            // Refresh camera reference periodically in case it changes
            if (_localPlayerCamera == null || Time.time - _lastCameraCheck > 1f)
            {
                _localPlayerCamera = GetLocalPlayerCamera();
                _lastCameraCheck = Time.time;
            }
            
            if (_localPlayerCamera != null)
            {
                // Face the camera - force this every frame to override any interference
                transform.rotation = _localPlayerCamera.transform.rotation;
            }
        }

        private Camera GetLocalPlayerCamera()
        {
            try
            {
                var players = UnityEngine.Object.FindObjectsByType<Player>(UnityEngine.FindObjectsSortMode.None);
                if (players != null)
                {
                    foreach (var player in players)
                    {
                        if (player != null && player.IsLocalPlayer)
                        {
                            var playerCamera = player.PlayerCamera;
                            if (playerCamera != null && playerCamera.CameraComponent != null)
                            {
                                return playerCamera.CameraComponent;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error finding camera: " + ex.Message);
            }
            
            return Camera.main;
        }
    }
}
