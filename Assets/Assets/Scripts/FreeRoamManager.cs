using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class FreeRoamManager : MonoBehaviour
{
    public static FreeRoamManager Instance { get; private set; }
    
    [Header("Canvas Management")]
    public Canvas freeRoamCanvas;
    public Canvas mainTitleCanvas; // NEW: Reference to main title canvas to hide it
    
    [Header("Free Roam UI")]
    public TMP_Text instructionText;
    public Button backToTitleButton;
    
    [Header("Direct Scene Access Buttons")]
    public Button dungeon1Button;
    public Button dungeon2Button;
    public Button dungeon3Button;
    public Button soapTask1Button;
    public Button soapTask2Button;
    public Button soapTask3Button;
    public Button decomposeTaskButton;
    public Button overworldButton;
    
    // This flag tells other systems we're in free roam mode
    public static bool IsFreeRoamActive { get; private set; } = false;
    
    // Track the original return scene for free roam
    public static string FreeRoamReturnScene { get; private set; } = "TitleScene";
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Set return scene when free roam starts
            FreeRoamReturnScene = "TitleScene";
            
            Debug.Log("[FreeRoamManager] Created persistent FreeRoamManager");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        FindCanvasReferences();
        SetupUI();
        HideFreeRoamMenu();
    }
    
    // NEW: Method to find canvas references
    private void FindCanvasReferences()
    {
        // Auto-find canvases if not assigned or destroyed
        if (freeRoamCanvas == null)
        {
            // Try to find FreeRoamCanvas by name
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.name.Contains("FreeRoam"))
                {
                    freeRoamCanvas = canvas;
                    Debug.Log("[FreeRoamManager] Auto-found FreeRoamCanvas: " + canvas.name);
                    break;
                }
            }
        }
        
        if (mainTitleCanvas == null)
        {
            // Try to find main title canvas (usually the first active canvas)
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.gameObject.activeInHierarchy && canvas != freeRoamCanvas)
                {
                    mainTitleCanvas = canvas;
                    Debug.Log("[FreeRoamManager] Auto-found MainTitleCanvas: " + canvas.name);
                    break;
                }
            }
        }
    }
    
    // NEW: Re-find UI references when showing free roam menu
    private void RefreshUIReferences()
    {
        Debug.Log("[FreeRoamManager] RefreshUIReferences called");
        
        // Re-find canvas references in case they were destroyed/recreated
        FindCanvasReferences();
        
        // If we still don't have a free roam canvas, try more aggressive searching
        if (freeRoamCanvas == null)
        {
            Debug.Log("[FreeRoamManager] Attempting more aggressive canvas search...");
            
            // Method 1: Search by exact name
            GameObject canvasGO = GameObject.Find("FreeRoamCanvas");
            if (canvasGO != null)
            {
                freeRoamCanvas = canvasGO.GetComponent<Canvas>();
                Debug.Log("[FreeRoamManager] Found FreeRoamCanvas by GameObject.Find");
            }
            
            // Method 2: Search all GameObjects in scene
            if (freeRoamCanvas == null)
            {
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Equals("FreeRoamCanvas", System.StringComparison.OrdinalIgnoreCase))
                    {
                        Canvas canvas = obj.GetComponent<Canvas>();
                        if (canvas != null)
                        {
                            freeRoamCanvas = canvas;
                            Debug.Log("[FreeRoamManager] Found FreeRoamCanvas in all objects search: " + obj.name);
                            break;
                        }
                    }
                }
            }
        }
        
        // Re-find UI components in the free roam canvas
        if (freeRoamCanvas != null)
        {
            Debug.Log("[FreeRoamManager] Free roam canvas found, setting up UI components");
            
            // Try to find instruction text
            if (instructionText == null)
            {
                instructionText = freeRoamCanvas.GetComponentInChildren<TMP_Text>();
                if (instructionText != null)
                    Debug.Log("[FreeRoamManager] Found instruction text: " + instructionText.name);
            }
            
            // Try to find back button
            if (backToTitleButton == null)
            {
                Button[] buttons = freeRoamCanvas.GetComponentsInChildren<Button>();
                foreach (Button btn in buttons)
                {
                    if (btn.name.Contains("Back") || btn.name.Contains("Title") || btn.name.Contains("Return"))
                    {
                        backToTitleButton = btn;
                        Debug.Log("[FreeRoamManager] Found back button: " + btn.name);
                        break;
                    }
                }
            }
            
            // Try to find scene buttons
            if (dungeon1Button == null || dungeon2Button == null || dungeon3Button == null)
            {
                Button[] buttons = freeRoamCanvas.GetComponentsInChildren<Button>();
                foreach (Button btn in buttons)
                {
                    if (btn.name.Contains("Dungeon1")) { dungeon1Button = btn; Debug.Log("[FreeRoamManager] Found " + btn.name); }
                    else if (btn.name.Contains("Dungeon2")) { dungeon2Button = btn; Debug.Log("[FreeRoamManager] Found " + btn.name); }
                    else if (btn.name.Contains("Dungeon3")) { dungeon3Button = btn; Debug.Log("[FreeRoamManager] Found " + btn.name); }
                    else if (btn.name.Contains("Soap1")) { soapTask1Button = btn; Debug.Log("[FreeRoamManager] Found " + btn.name); }
                    else if (btn.name.Contains("Soap2")) { soapTask2Button = btn; Debug.Log("[FreeRoamManager] Found " + btn.name); }
                    else if (btn.name.Contains("Soap3")) { soapTask3Button = btn; Debug.Log("[FreeRoamManager] Found " + btn.name); }
                    else if (btn.name.Contains("Decompose")) { decomposeTaskButton = btn; Debug.Log("[FreeRoamManager] Found " + btn.name); }
                    else if (btn.name.Contains("Overworld")) { overworldButton = btn; Debug.Log("[FreeRoamManager] Found " + btn.name); }
                }
            }
            
            // Re-setup button listeners
            SetupUI();
        }
        else
        {
            Debug.LogWarning("[FreeRoamManager] Still no FreeRoamCanvas found after aggressive search");
        }
    }
    
    private void SetupUI()
    {
        // Setup direct access buttons
        if (dungeon1Button != null)
            dungeon1Button.onClick.AddListener(() => LoadScene("DungeonScene"));
            
        if (dungeon2Button != null)
            dungeon2Button.onClick.AddListener(() => LoadScene("DungeonScene2"));
            
        if (dungeon3Button != null)
            dungeon3Button.onClick.AddListener(() => LoadScene("DungeonScene3"));
        
        if (soapTask1Button != null)
            soapTask1Button.onClick.AddListener(() => LoadScene("SoapScene"));
            
        if (soapTask2Button != null)
            soapTask2Button.onClick.AddListener(() => LoadScene("SoapScene2"));
            
        if (soapTask3Button != null)
            soapTask3Button.onClick.AddListener(() => LoadScene("SoapScene3"));
        
        if (decomposeTaskButton != null)
            decomposeTaskButton.onClick.AddListener(() => LoadScene("PortalScene"));
            
        if (overworldButton != null)
            overworldButton.onClick.AddListener(() => LoadScene("overworldScene"));
            
        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(BackToTitle);
    }
    
    public void ShowFreeRoamMenu()
    {
        Debug.Log("[FreeRoamManager] ShowFreeRoamMenu called");
        
        // NEW: Refresh UI references before showing
        RefreshUIReferences();
        
        // Activate free roam mode
        IsFreeRoamActive = true;
        
        // Start free roam session
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.StartFreeRoamMode();
        }
        
        // FIXED: Hide main title canvas first
        if (mainTitleCanvas != null)
        {
            mainTitleCanvas.gameObject.SetActive(false);
            Debug.Log("[FreeRoamManager] Main title canvas hidden: " + mainTitleCanvas.name);
        }
        else
        {
            Debug.LogWarning("[FreeRoamManager] Main title canvas not found!");
        }
        
        // FIXED: Show free roam canvas
        if (freeRoamCanvas != null)
        {
            freeRoamCanvas.gameObject.SetActive(true);
            Debug.Log("[FreeRoamManager] Free roam canvas shown: " + freeRoamCanvas.name);
            
            // Update instruction text
            if (instructionText != null)
            {
                instructionText.text = "Mode Libre - Accès direct à tous les contenus\nAucune progression n'est sauvegardée";
            }
            
            Debug.Log("[FreeRoamManager] Free roam mode activated - all restrictions bypassed");
        }
        else
        {
            Debug.LogError("[FreeRoamManager] Free roam canvas not found! Creating simple fallback...");
            CreateSimpleFallbackUI();
        }
    }
    
    // NEW: Simplified fallback UI creation that's less likely to cause errors
    private void CreateSimpleFallbackUI()
    {
        Debug.Log("[FreeRoamManager] Creating simple fallback Free Roam UI");
        
        try
        {
            // Find or create a canvas to work with
            Canvas targetCanvas = mainTitleCanvas;
            if (targetCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                if (canvases.Length > 0)
                {
                    targetCanvas = canvases[0];
                }
            }
            
            if (targetCanvas == null)
            {
                Debug.LogError("[FreeRoamManager] No canvas found to create fallback UI");
                return;
            }
            
            // Create a simple panel overlay
            GameObject panelGO = new GameObject("FreeRoamFallbackPanel");
            panelGO.transform.SetParent(targetCanvas.transform, false);
            
            RectTransform panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            // Add title text
            GameObject textGO = new GameObject("FreeRoamText");
            textGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.7f);
            textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TMP_Text text = textGO.AddComponent<TMP_Text>();
            text.text = "Mode Libre\n\nLe système de mode libre n'est pas encore configuré.\nVeuillez configurer le FreeRoamCanvas dans Unity.\n\nCliquez pour retourner au menu principal.";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            // Add click to return functionality
            Button panelButton = panelGO.AddComponent<Button>();
            panelButton.targetGraphic = panelImage;
            panelButton.onClick.AddListener(() => {
                Destroy(panelGO);
                HideFreeRoamMenu();
            });
            
            // Store reference
            freeRoamCanvas = targetCanvas;
            instructionText = text;
            
            Debug.Log("[FreeRoamManager] Simple fallback UI created successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FreeRoamManager] Error creating fallback UI: {e.Message}");
        }
    }
    
    public void HideFreeRoamMenu()
    {
        Debug.Log("[FreeRoamManager] HideFreeRoamMenu called");
        
        IsFreeRoamActive = false;
        
        // Hide free roam canvas
        if (freeRoamCanvas != null)
        {
            freeRoamCanvas.gameObject.SetActive(false);
            Debug.Log("[FreeRoamManager] Free roam canvas hidden");
        }
        
        // Show main title canvas
        if (mainTitleCanvas != null)
        {
            mainTitleCanvas.gameObject.SetActive(true);
            Debug.Log("[FreeRoamManager] Main title canvas shown");
        }
    }
    
    private void BackToTitle()
    {
        Debug.Log("[FreeRoamManager] Returning to title screen");
        
        // Deactivate free roam mode
        IsFreeRoamActive = false;
        
        // Reset session
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.StartFreeRoamMode();
        }
        
        // Hide free roam canvas and show main title canvas
        HideFreeRoamMenu();
    }
    
    private void LoadScene(string sceneName)
    {
        Debug.Log($"[FreeRoamManager] Loading scene: {sceneName}");
        
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.TransitionToScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    
    // Static method to check if any dungeon should be unlocked in free roam
    public static bool ShouldBypassRestrictions()
    {
        return IsFreeRoamActive;
    }
    
    // NEW: Method to check if we should return to title instead of overworld
    public static string GetReturnScene()
    {
        return IsFreeRoamActive ? FreeRoamReturnScene : "overworldScene";
    }
    
    // NEW: Handle task completion in free roam mode
    public static void OnTaskCompleted()
    {
        if (IsFreeRoamActive)
        {
            Debug.Log("[FreeRoamManager] Task completed in free roam mode - returning to TitleScene");
            
            // Directly transition back to title scene
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.TransitionToScene(FreeRoamReturnScene);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(FreeRoamReturnScene);
            }
        }
    }
}
