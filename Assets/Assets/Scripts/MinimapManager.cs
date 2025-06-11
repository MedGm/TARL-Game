using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class MinimapManager : MonoBehaviour
{
    [Header("Minimap Components")]
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RawImage minimapDisplay;
    [SerializeField] private Transform minimapParent;
    [SerializeField] private Canvas minimapCanvas;

    [Header("Player Tracking")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject playerIconPrefab;
    [SerializeField] private float mapScale = 1f;
    [SerializeField] private Vector2 mapCenter = Vector2.zero;

    [Header("Objective Icons")]
    [SerializeField] private GameObject dungeonIconPrefab;
    [SerializeField] private GameObject portalIconPrefab;
    [SerializeField] private GameObject completedDungeonIconPrefab;
    [SerializeField] private Color glowColor = Color.yellow;
    [SerializeField] private float glowSpeed = 2f;
    [SerializeField] private float glowIntensity = 1.5f;

    // NEW: Icon scaling settings
    [Header("Icon Scaling")]
    [SerializeField] private float playerIconScale = 1.5f; // Scale for player icon
    [SerializeField] private float dungeonIconScale = 2.0f; // Scale for dungeon icons (reduced from 3f)
    [SerializeField] private float portalIconScale = 2.5f; // Scale for portal icon
    [SerializeField] private float completedIconScale = 1.8f; // Scale for completed dungeons

    // NEW: Simple themed background settings
    [Header("Minimap Theme")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.12f, 0.06f, 1f); // Dark forest green
    [SerializeField] private Color borderColor = new Color(0.4f, 0.6f, 0.3f, 1f); // Lighter green border
    [SerializeField] private float borderWidth = 4f;

    [Header("World Positions")]
    [SerializeField] private Vector3 dungeon1WorldPos = new Vector3(-10, 0, 0);
    [SerializeField] private Vector3 dungeon2WorldPos = new Vector3(10, 0, 0);
    [SerializeField] private Vector3 dungeon3WorldPos = new Vector3(0, 10, 0);
    [SerializeField] private Vector3 portalWorldPos = new Vector3(0, -10, 0);

    // Runtime references
    private GameObject playerIcon;
    private Dictionary<string, GameObject> objectiveIcons = new Dictionary<string, GameObject>();
    private Dictionary<string, Vector3> objectivePositions = new Dictionary<string, Vector3>();
    private string currentObjective = "";
    private Coroutine glowCoroutine;

    // Minimap state
    public enum MinimapObjective
    {
        Dungeon1,
        SoapTask1,
        Dungeon2,
        SoapTask2,
        Dungeon3,
        SoapTask3,
        Portal,
        Completed
    }

    private MinimapObjective currentMinimapObjective = MinimapObjective.Dungeon1;

    private void Awake()
    {
        // Initialize objective positions dictionary
        objectivePositions["dungeon1"] = dungeon1WorldPos;
        objectivePositions["dungeon2"] = dungeon2WorldPos;
        objectivePositions["dungeon3"] = dungeon3WorldPos;
        objectivePositions["portal"] = portalWorldPos;

        SetupMinimap();
    }

    private void Start()
    {
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForSeconds(0.5f);

        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        CreatePlayerIcon();
        CreateObjectiveIcons();
        UpdateObjectives();

        Debug.Log("[MinimapManager] Initialized successfully");
    }

    private void SetupMinimap()
    {
        // Simple minimap camera setup - only Minimap layer
        if (minimapCamera != null)
        {
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 15f;
            minimapCamera.transform.rotation = Quaternion.identity;
            minimapCamera.depth = -10;
            
            int minimapLayer = LayerMask.NameToLayer("Minimap");
            if (minimapLayer == -1)
            {
                Debug.LogError("[MinimapManager] Minimap layer does not exist! Please create it in Project Settings > Tags and Layers");
                return;
            }
            
            // Only show Minimap layer - no Grass
            minimapCamera.cullingMask = 1 << minimapLayer;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = backgroundColor;
        }
        
        // Create render texture and setup displays
        if (minimapDisplay != null && minimapCamera != null)
        {
            RenderTexture renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            renderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm;
            renderTexture.Create();
            
            minimapCamera.targetTexture = renderTexture;
            minimapDisplay.texture = renderTexture;
            minimapDisplay.color = Color.white;
            minimapDisplay.raycastTarget = false;
            
            // Setup enlarged minimap display
            GameObject enlargedPanel = GameObject.Find("EnlargedMinimapPanel");
            if (enlargedPanel != null)
            {
                RawImage enlargedDisplay = enlargedPanel.GetComponentInChildren<RawImage>();
                if (enlargedDisplay != null)
                {
                    enlargedDisplay.texture = renderTexture;
                    enlargedDisplay.color = Color.white;
                    enlargedDisplay.raycastTarget = false;
                }
            }
            
            // Add borders to both displays - MOVED after setup
            AddMinimapBorders();
            
            Debug.Log("[MinimapManager] Simple themed minimap created");
        }
    }

    // FIXED: Improved border addition method
    private void AddMinimapBorders()
    {
        // Add border to main minimap
        if (minimapDisplay != null)
        {
            AddBorderToRawImage(minimapDisplay, "MinimapBorder");
            Debug.Log("[MinimapManager] Added border to main minimap");
        }
        
        // FIXED: Better way to find and add border to enlarged minimap
        StartCoroutine(AddEnlargedMinimapBorderDelayed());
    }

    // FIXED: Add enlarged minimap border with delay to ensure it's properly set up
    private System.Collections.IEnumerator AddEnlargedMinimapBorderDelayed()
    {
        // Wait a frame to ensure all UI is initialized
        yield return null;
        
        // Try multiple ways to find the enlarged minimap
        RawImage enlargedDisplay = null;
        
        // Method 1: Find by GameObject name
        GameObject enlargedPanel = GameObject.Find("EnlargedMinimapPanel");
        if (enlargedPanel != null)
        {
            enlargedDisplay = enlargedPanel.GetComponentInChildren<RawImage>();
            Debug.Log("[MinimapManager] Found enlarged panel by name");
        }
        
        // Method 2: Find through MinimapUI component
        if (enlargedDisplay == null)
        {
            MinimapUI minimapUI = FindFirstObjectByType<MinimapUI>();
            if (minimapUI != null)
            {
                // Access the enlarged minimap display through reflection or public field
                var enlargedField = minimapUI.GetType().GetField("enlargedMinimapDisplay", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (enlargedField != null)
                {
                    enlargedDisplay = enlargedField.GetValue(minimapUI) as RawImage;
                    Debug.Log("[MinimapManager] Found enlarged display through MinimapUI");
                }
            }
        }
        
        // Method 3: Find all RawImages and identify the enlarged one
        if (enlargedDisplay == null)
        {
            RawImage[] allRawImages = FindObjectsByType<RawImage>(FindObjectsSortMode.None);
            foreach (RawImage rawImg in allRawImages)
            {
                if (rawImg != minimapDisplay && rawImg.name.ToLower().Contains("minimap"))
                {
                    enlargedDisplay = rawImg;
                    Debug.Log("[MinimapManager] Found enlarged display by name search");
                    break;
                }
            }
        }
        
        // Add border if found
        if (enlargedDisplay != null)
        {
            AddBorderToRawImage(enlargedDisplay, "EnlargedMinimapBorder");
            Debug.Log("[MinimapManager] Successfully added border to enlarged minimap");
        }
        else
        {
            Debug.LogWarning("[MinimapManager] Could not find enlarged minimap display to add border");
        }
    }

    // FIXED: Improved border creation method that preserves button visibility
    private void AddBorderToRawImage(RawImage rawImage, string borderName)
    {
        if (rawImage == null)
        {
            Debug.LogWarning($"[MinimapManager] Cannot add border {borderName}: RawImage is null");
            return;
        }
        
        // Check if border already exists
        Transform existingBorder = rawImage.transform.parent.Find(borderName);
        if (existingBorder != null)
        {
            Debug.Log($"[MinimapManager] Border {borderName} already exists, skipping");
            return;
        }
        
        // FIXED: Store references to all buttons before creating border
        Transform parent = rawImage.transform.parent;
        List<Transform> buttonTransforms = new List<Transform>();
        
        // Find all button components in the parent
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.GetComponent<Button>() != null)
            {
                buttonTransforms.Add(child);
            }
        }
        
        // Create border as a child of the RawImage's parent
        GameObject borderObj = new GameObject(borderName);
        borderObj.transform.SetParent(parent, false);
        
        // FIXED: Position border at the beginning (index 0) so it's behind everything
        borderObj.transform.SetSiblingIndex(0);
        
        // Add Image component for the border
        UnityEngine.UI.Image borderImage = borderObj.AddComponent<UnityEngine.UI.Image>();
        borderImage.color = borderColor;
        borderImage.raycastTarget = false; // Important: don't block clicks
        
        // Set RectTransform to be slightly larger than the RawImage
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        RectTransform rawImageRect = rawImage.GetComponent<RectTransform>();
        
        // Copy anchors and position
        borderRect.anchorMin = rawImageRect.anchorMin;
        borderRect.anchorMax = rawImageRect.anchorMax;
        borderRect.anchoredPosition = rawImageRect.anchoredPosition;
        borderRect.sizeDelta = rawImageRect.sizeDelta + Vector2.one * borderWidth * 2f;
        
        // FIXED: Ensure RawImage and all buttons are in front of the border
        rawImage.transform.SetAsLastSibling();
        
        // FIXED: Move all buttons to the front to ensure they're visible and clickable
        foreach (Transform buttonTransform in buttonTransforms)
        {
            buttonTransform.SetAsLastSibling();
            
            // FIXED: Ensure button has proper Canvas Group or sorting
            CanvasGroup canvasGroup = buttonTransform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = buttonTransform.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            Debug.Log($"[MinimapManager] Ensured button {buttonTransform.name} is visible and interactable");
        }
        
        Debug.Log($"[MinimapManager] Successfully created border {borderName} for {rawImage.name} with {buttonTransforms.Count} buttons preserved");
    }

    private void CreateObjectiveIcons()
    {
        // Create dungeon icons
        CreateObjectiveIcon("dungeon1", dungeonIconPrefab, dungeon1WorldPos);
        CreateObjectiveIcon("dungeon2", dungeonIconPrefab, dungeon2WorldPos);
        CreateObjectiveIcon("dungeon3", dungeonIconPrefab, dungeon3WorldPos);

        // Create portal icon
        CreateObjectiveIcon("portal", portalIconPrefab, portalWorldPos);

        Debug.Log("[MinimapManager] Objective icons created");
    }

    private void CreatePlayerIcon()
    {
        if (playerIconPrefab != null && minimapParent != null)
        {
            playerIcon = Instantiate(playerIconPrefab, minimapParent);
            playerIcon.name = "PlayerIcon";
            playerIcon.transform.localScale = Vector3.one * playerIconScale;
            
            if (playerTransform != null)
            {
                Vector3 worldPos = playerTransform.position;
                playerIcon.transform.position = new Vector3(worldPos.x, worldPos.y, 1f);
            }
            
            // Apply bright green color for player
            Renderer[] renderers = playerIcon.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    Material newMaterial = new Material(Shader.Find("Sprites/Default"));
                    newMaterial.color = new Color(1f, 1f, 1f, 1f); // White color for player icon
                    renderer.material = newMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
            
            int minimapLayer = LayerMask.NameToLayer("Minimap");
            if (minimapLayer != -1)
            {
                SetLayerRecursively(playerIcon, minimapLayer);
                Debug.Log("[MinimapManager] Player icon created");
            }
        }
    }

    private void CreateObjectiveIcon(string objectiveId, GameObject prefab, Vector3 worldPos)
    {
        if (prefab != null && minimapParent != null)
        {
            GameObject icon = Instantiate(prefab, minimapParent);
            icon.name = $"{objectiveId}Icon";
            
            Vector3 iconPos = new Vector3(worldPos.x * mapScale, worldPos.y * mapScale, 1f);
            icon.transform.position = iconPos;
            icon.transform.localScale = Vector3.one * GetIconScale(objectiveId);
            
            // Apply themed colors for better visibility
            Renderer[] renderers = icon.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    Material newMaterial = new Material(Shader.Find("Sprites/Default"));
                    newMaterial.color = GetThemedObjectiveColor(objectiveId);
                    renderer.material = newMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
            
            int minimapLayer = LayerMask.NameToLayer("Minimap");
            if (minimapLayer != -1)
            {
                SetLayerRecursively(icon, minimapLayer);
            }
            
            objectiveIcons[objectiveId] = icon;
            icon.SetActive(false);
            
            Debug.Log($"[MinimapManager] Created {objectiveId} icon with themed colors");
        }
    }

    // NEW: Get themed colors that stand out against dark background
    private Color GetThemedObjectiveColor(string objectiveId)
    {
        switch (objectiveId)
        {
            case "dungeon1":
                return new Color(1f, 0.3f, 0.3f, 1f); // Bright red
            case "dungeon2":
                return new Color(0.3f, 0.6f, 1f, 1f); // Bright blue
            case "dungeon3":
                return new Color(1f, 0.3f, 1f, 1f); // Bright magenta
            case "portal":
                return new Color(0.3f, 1f, 1f, 1f); // Bright cyan
            default:
                return new Color(1f, 1f, 0.3f, 1f); // Bright yellow
        }
    }

    // NEW: Helper method to get appropriate scale for different icon types
    private float GetIconScale(string objectiveId)
    {
        switch (objectiveId)
        {
            case "dungeon1":
            case "dungeon2":
            case "dungeon3":
                return dungeonIconScale;
            case "portal":
                return portalIconScale;
            default:
                return dungeonIconScale;
        }
    }

    // NEW: Helper method to get appropriate colors for different objectives (used in glow effect)
    private Color GetObjectiveColor(string objectiveId)
    {
        switch (objectiveId)
        {
            case "dungeon1":
                return new Color(1f, 0.3f, 0.3f, 1f); // Bright red
            case "dungeon2":
                return new Color(0.3f, 0.6f, 1f, 1f); // Bright blue
            case "dungeon3":
                return new Color(1f, 0.3f, 1f, 1f); // Bright magenta
            case "portal":
                return new Color(0.3f, 1f, 1f, 1f); // Bright cyan
            default:
                return new Color(1f, 1f, 0.3f, 1f); // Bright yellow
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        // FIXED: Validate layer is in valid range [0...31]
        if (newLayer < 0 || newLayer > 31)
        {
            Debug.LogError($"[MinimapManager] Invalid layer {newLayer}. Must be in range [0...31]");
            return;
        }

        // FIXED: Check if layer exists
        if (newLayer != 0 && LayerMask.LayerToName(newLayer) == "")
        {
            Debug.LogError($"[MinimapManager] Layer {newLayer} does not exist. Please create 'Minimap' layer in Project Settings");
            return;
        }

        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void Update()
    {
        UpdatePlayerIcon();
        CheckForObjectiveChanges();
    }

    private void UpdatePlayerIcon()
    {
        if (playerIcon != null && playerTransform != null)
        {
            // FIXED: Update player icon position with proper Z depth
            Vector3 worldPos = playerTransform.position;
            Vector3 minimapPos = new Vector3(worldPos.x * mapScale, worldPos.y * mapScale, 1f); // Z=1 in front of background
            playerIcon.transform.position = minimapPos;
            
            // FIXED: Update minimap camera to follow player (2D top-down style)
            if (minimapCamera != null)
            {
                Vector3 cameraPos = new Vector3(worldPos.x * mapScale, worldPos.y * mapScale, -20f); // Z=-20 to look at Z=0 to Z=1 objects
                minimapCamera.transform.position = cameraPos;
            }
        }
    }

    private Vector3 WorldToMinimapPosition(Vector3 worldPos)
    {
        // FIXED: Use mapScale properly
        return new Vector3(worldPos.x * mapScale, worldPos.y * mapScale, -1f);
    }

    private void CheckForObjectiveChanges()
    {
        if (GameManager.Instance == null) return;

        MinimapObjective newObjective = DetermineCurrentObjective();

        if (newObjective != currentMinimapObjective)
        {
            currentMinimapObjective = newObjective;
            UpdateObjectives();
            Debug.Log($"[MinimapManager] Objective changed to: {currentMinimapObjective}");
        }
    }

    private MinimapObjective DetermineCurrentObjective()
    {
        var gm = GameManager.Instance;

        // Check completion status
        bool dungeon1Complete = gm.IsDungeonCompleted("dungeon1");
        bool dungeon2Complete = gm.IsDungeonCompleted("dungeon2");
        bool dungeon3Complete = gm.IsDungeonCompleted("dungeon3");

        // Check soap task progress
        bool hasPlayedSoap1 = gm.soapTask1Count > 0;
        bool hasPlayedSoap2 = gm.soapTask2Count > 0;
        bool hasPlayedSoap3 = gm.soapTask3Count > 0;

        // Determine next objective based on progress
        if (!dungeon1Complete)
        {
            return MinimapObjective.Dungeon1;
        }
        else if (dungeon1Complete && !hasPlayedSoap1)
        {
            return MinimapObjective.SoapTask1; // Show bubble spawning area
        }
        else if (hasPlayedSoap1 && !dungeon2Complete)
        {
            return MinimapObjective.Dungeon2;
        }
        else if (dungeon2Complete && !hasPlayedSoap2)
        {
            return MinimapObjective.SoapTask2;
        }
        else if (hasPlayedSoap2 && !dungeon3Complete)
        {
            return MinimapObjective.Dungeon3;
        }
        else if (dungeon3Complete && !hasPlayedSoap3)
        {
            return MinimapObjective.SoapTask3;
        }
        else if (hasPlayedSoap3 && gm.keysCollected >= 3)
        {
            return MinimapObjective.Portal;
        }
        else
        {
            return MinimapObjective.Completed;
        }
    }

    private void UpdateObjectives()
    {
        // Hide all objective icons first
        foreach (var icon in objectiveIcons.Values)
        {
            if (icon != null)
                icon.SetActive(false);
        }

        // Stop any existing glow effect
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
            glowCoroutine = null;
        }

        // Show appropriate icons based on progress
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Show completed dungeons with different icon
        if (gm.IsDungeonCompleted("dungeon1"))
            ShowCompletedDungeon("dungeon1");
        if (gm.IsDungeonCompleted("dungeon2"))
            ShowCompletedDungeon("dungeon2");
        if (gm.IsDungeonCompleted("dungeon3"))
            ShowCompletedDungeon("dungeon3");

        // Show and highlight current objective
        switch (currentMinimapObjective)
        {
            case MinimapObjective.Dungeon1:
                ShowAndHighlightObjective("dungeon1");
                break;

            case MinimapObjective.SoapTask1:
            case MinimapObjective.SoapTask2:
            case MinimapObjective.SoapTask3:
                // For soap tasks, highlight the overworld area (player needs to wait for bubble)
                ShowSoapTaskIndicator();
                break;

            case MinimapObjective.Dungeon2:
                ShowAndHighlightObjective("dungeon2");
                break;

            case MinimapObjective.Dungeon3:
                ShowAndHighlightObjective("dungeon3");
                break;

            case MinimapObjective.Portal:
                ShowAndHighlightObjective("portal");
                break;

            case MinimapObjective.Completed:
                ShowCompletionState();
                break;
        }
    }

    private IEnumerator GlowEffect(GameObject target)
    {
        if (target == null) yield break;

        var renderers = target.GetComponentsInChildren<Renderer>();
        Color originalColor = GetObjectiveColor(target.name.Replace("Icon", "").Replace("Completed", ""));

        while (target != null && target.activeInHierarchy)
        {
            float glow = Mathf.Sin(Time.time * glowSpeed) * 0.5f + 0.5f;
            Color glowedColor = Color.Lerp(originalColor, glowColor, glow * glowIntensity);

            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = glowedColor;
                }
            }

            yield return null;
        }
    }

    private void ShowCompletedDungeon(string dungeonId)
    {
        if (objectiveIcons.ContainsKey(dungeonId))
        {
            GameObject icon = objectiveIcons[dungeonId];
            if (icon != null)
            {
                if (completedDungeonIconPrefab != null)
                {
                    Vector3 pos = icon.transform.position;
                    Destroy(icon);

                    GameObject completedIcon = Instantiate(completedDungeonIconPrefab, minimapParent);
                    completedIcon.transform.position = pos;
                    completedIcon.name = $"{dungeonId}CompletedIcon";
                    completedIcon.transform.localScale = Vector3.one * completedIconScale;

                    // Apply bright green for completed
                    Renderer[] renderers = completedIcon.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                        {
                            Material newMaterial = new Material(Shader.Find("Sprites/Default"));
                            newMaterial.color = new Color(0.3f, 1f, 0.3f, 1f); // Bright green
                            renderer.material = newMaterial;
                            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            renderer.receiveShadows = false;
                        }
                    }

                    SetLayerRecursively(completedIcon, LayerMask.NameToLayer("Minimap"));
                    objectiveIcons[dungeonId] = completedIcon;
                }

                objectiveIcons[dungeonId].SetActive(true);
            }
        }
    }

    private void ShowAndHighlightObjective(string objectiveId)
    {
        if (objectiveIcons.ContainsKey(objectiveId))
        {
            GameObject icon = objectiveIcons[objectiveId];
            if (icon != null)
            {
                icon.SetActive(true);

                // Start glowing effect
                glowCoroutine = StartCoroutine(GlowEffect(icon));

                currentObjective = objectiveId;
            }
        }
    }

    private void ShowSoapTaskIndicator()
    {
        // For soap tasks, we can show a special indicator or text
        // Since the player needs to wait for a bubble to spawn, we show a general "wait" indicator
        if (minimapCanvas != null)
        {
            StartCoroutine(ShowSoapTaskMessage());
        }
    }

    private IEnumerator ShowSoapTaskMessage()
    {
        // Create temporary text indicator
        GameObject textObj = new GameObject("SoapTaskIndicator");
        textObj.transform.SetParent(minimapCanvas.transform, false);

        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "Attendez la bulle de savon...";
        text.fontSize = 14;
        text.color = glowColor;
        text.alignment = TMPro.TextAlignmentOptions.Center;

        var rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.1f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.1f);
        rectTransform.sizeDelta = new Vector2(200, 30);

        // Animate the text
        float timer = 0f;
        while (timer < 3f)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Sin(timer * glowSpeed) * 0.5f + 0.5f;
            text.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
            yield return null;
        }

        Destroy(textObj);
    }

    private void ShowCompletionState()
    {
        // Show all completed objectives
        foreach (var icon in objectiveIcons.Values)
        {
            if (icon != null)
            {
                icon.SetActive(true);
                var renderers = icon.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.material.color = Color.green;
                }
            }
        }
    }

    // Public methods for external access
    public void ForceUpdateObjectives()
    {
        UpdateObjectives();
    }

    public MinimapObjective GetCurrentObjective()
    {
        return currentMinimapObjective;
    }

    public string GetObjectiveDescription()
    {
        switch (currentMinimapObjective)
        {
            case MinimapObjective.Dungeon1:
                return "Allez au premier donjon";
            case MinimapObjective.SoapTask1:
                return "Attendez la première bulle de savon";
            case MinimapObjective.Dungeon2:
                return "Allez au deuxième donjon";
            case MinimapObjective.SoapTask2:
                return "Attendez la deuxième bulle de savon";
            case MinimapObjective.Dungeon3:
                return "Allez au troisième donjon";
            case MinimapObjective.SoapTask3:
                return "Attendez la troisième bulle de savon";
            case MinimapObjective.Portal:
                return "Allez au portail final";
            case MinimapObjective.Completed:
                return "Jeu terminé !";
            default:
                return "";
        }
    }

    private void OnDestroy()
    {
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
        }
    }
}
