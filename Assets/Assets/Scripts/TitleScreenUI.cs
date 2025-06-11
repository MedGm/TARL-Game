using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class TitleScreenUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button startButton;
    [SerializeField] private string overworldSceneName = "overworldScene"; // Set this to your overworld map scene

    // NEW: Free Roam and QR Scanner buttons
    [Header("New Features")]
    [SerializeField] private Button freeRoamButton;
    [SerializeField] private Button qrScanButton;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject qrScanPanel;
    
    [Header("Version")]
    [SerializeField] private TMP_Text versionText; // Shows game version and student info after QR scan
    
    // QR Code functionality
    private bool hasValidQRData = false;
    private QRCodeData currentQRData = null;

    // NEW: Complete implementation
    private void Start()
    {
        // Title styling
        if (titleText != null)
        {
            titleText.text = "Number-Game";
            titleText.fontSize = 180; // Huge for 1920x1440
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.74f, 0.18f); // Yellow-orange (#FFBD2E)
            titleText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            titleText.outlineWidth = 0.35f;
            titleText.outlineColor = new Color(0.36f, 0.22f, 0.09f); // Brown border (#5C3917)
            // Optionally add a shadow in the editor for more depth
        }

        // NEW: Disable Start button by default until QR code is scanned
        if (startButton != null)
        {
            startButton.interactable = false;
            ColorBlock colors = startButton.colors;
            colors.disabledColor = Color.gray;
            startButton.colors = colors;
        }

        SetupButtons();
        ShowMainMenu();
        
        // Initialize in free roam mode by default
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.StartFreeRoamMode();
        }
        
        // Show version and requirement info
        if (versionText != null)
        {
            versionText.text = "Version 1.0.0 - Scanner QR requis";
        }

        // Always reset or initialize progression on title screen
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentProgression = GameManager.ProgressionStage.Overworld;
        }
        
        // NEW: Check if returning from free roam task
        if (FreeRoamManager.IsFreeRoamActive && FreeRoamManager.Instance != null)
        {
            Debug.Log("[TitleScreenUI] Returning from free roam task - showing free roam menu");
            
            // NEW: Ensure FreeRoamManager has correct canvas reference
            if (FreeRoamManager.Instance.freeRoamCanvas == null)
            {
                // Find and assign the free roam canvas
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    if (canvas.name.Contains("FreeRoam"))
                    {
                        FreeRoamManager.Instance.freeRoamCanvas = canvas;
                        Debug.Log("[TitleScreenUI] Reassigned FreeRoamCanvas to FreeRoamManager");
                        break;
                    }
                }
            }
            
            // NEW: Ensure FreeRoamManager has correct main canvas reference
            if (FreeRoamManager.Instance.mainTitleCanvas == null)
            {
                FreeRoamManager.Instance.mainTitleCanvas = GetComponentInParent<Canvas>();
                Debug.Log("[TitleScreenUI] Reassigned MainTitleCanvas to FreeRoamManager");
            }
            
            // Delay to ensure everything is initialized
            StartCoroutine(ShowFreeRoamAfterDelay(0.5f));
        }
    }
    
    private System.Collections.IEnumerator ShowFreeRoamAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (FreeRoamManager.Instance != null)
        {
            FreeRoamManager.Instance.ShowFreeRoamMenu();
        }
    }

    private void SetupButtons()
    {
        // Start button setup: use your playbutton PNG sprite, no TMP text or styling needed
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);

            // Remove TMP_Text child if present (since the sprite already has "Start" text)
            TMP_Text btnText = startButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                Destroy(btnText.gameObject);

            // Optionally, set the button image to your playbutton PNG in the editor
            // No further styling needed
        }
        
        // NEW: Free Roam button
        if (freeRoamButton != null)
            freeRoamButton.onClick.AddListener(StartFreeRoam);
        
        // NEW: QR Scan button  
        if (qrScanButton != null)
            qrScanButton.onClick.AddListener(ShowQRScanner);
    }

    // Called by QR scanner when successful
    public void OnQRCodeScanned(string qrData)
    {
        Debug.Log($"[TitleScreenUI] QR Code scanned: {qrData}");
        
        // Parse QR data
        currentQRData = QRCodeData.FromJson(qrData);
        
        if (currentQRData != null && currentQRData.IsValid())
        {
            ProcessValidQRData();
        }
        else
        {
            ProcessInvalidQRData();
        }
    }

    // FIXED: New method to accept QRCodeData directly without re-parsing
    public void OnQRCodeScannedWithData(QRCodeData qrData)
    {
        Debug.Log($"[TitleScreenUI] QR Code data received directly: {qrData}");
        
        currentQRData = qrData;
        
        if (currentQRData != null && currentQRData.IsValid())
        {
            ProcessValidQRData();
        }
        else
        {
            ProcessInvalidQRData();
        }
    }

    private void ProcessValidQRData()
    {
        hasValidQRData = true;
        
        // Enable Start button
        if (startButton != null)
        {
            startButton.interactable = true;
        }
        
        // ADDED: More detailed debugging
        Debug.Log($"[TitleScreenUI] QRCodeData processed: studentId='{currentQRData.studentId}', studentName='{currentQRData.studentName}', classId='{currentQRData.classId}', linkedSchoolId='{currentQRData.linkedSchoolId}'");
        
        // Update version text to show student info
        if (versionText != null)
        {
            string displayName = !string.IsNullOrEmpty(currentQRData.studentName) ? 
                currentQRData.studentName : currentQRData.studentId;
            string displayClass = !string.IsNullOrEmpty(currentQRData.classId) ? 
                currentQRData.classId : "N/A";
            string displaySchool = !string.IsNullOrEmpty(currentQRData.linkedSchoolId) ?
                currentQRData.linkedSchoolId : "N/A";
                
            Debug.Log($"[TitleScreenUI] Display values: Name='{displayName}', Class='{displayClass}', School='{displaySchool}'");
                
            versionText.text = $"Élève: {displayName}\nClasse: {displayClass}\nÉcole: {displaySchool}";
        }
        
        Debug.Log($"[TitleScreenUI] Valid QR code for student: {currentQRData.studentName} ({currentQRData.studentId})");
        ShowMainMenu();
    }

    private void ProcessInvalidQRData()
    {
        Debug.LogError("[TitleScreenUI] Invalid QR code data");
        hasValidQRData = false;
        if (startButton != null)
        {
            startButton.interactable = false;
        }
        if (versionText != null)
        {
            versionText.text = "QR Code invalide - Réessayez";
        }
    }

    private void OnStartClicked()
    {
        // Only allow start if QR code has been scanned
        if (!hasValidQRData || currentQRData == null)
        {
            Debug.LogWarning("[TitleScreenUI] Cannot start game - no valid QR data scanned");
            ShowQRScanner();
            return;
        }

        Debug.Log("[TitleScreenUI] Starting test mode with QR data");
        Debug.Log($"[TitleScreenUI] Student: {currentQRData.studentName}, Test: {currentQRData.testId}");
        
        // Reset all progress for fresh start
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetAllProgress();
        }
        
        // Start in TEST mode using QR data
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.StartTestMode(currentQRData.studentId, currentQRData.testId);
        }
        
        // FIXED: Use correct scene name that exists in build settings
        string targetScene = "overworldScene"; // FIXED: Use the correct scene name
        
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.TransitionToScene(targetScene);
        }
        else
        {
            // Fallback: try different possible scene names
            string[] possibleScenes = { "overworldScene", "overworld", "Overworld", "OverworldScene" };
            
            foreach (string sceneName in possibleScenes)
            {
                try
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                    Debug.Log($"[TitleScreenUI] Successfully loaded scene: {sceneName}");
                    break;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[TitleScreenUI] Failed to load scene '{sceneName}': {e.Message}");
                }
            }
        }
    }
    
    private void StartFreeRoam()
    {
        Debug.Log("[TitleScreenUI] Starting free roam mode");
        
        // FIXED: Better error handling and canvas assignment
        if (FreeRoamManager.Instance != null)
        {
            // Try to help FreeRoamManager find the canvases
            if (FreeRoamManager.Instance.mainTitleCanvas == null)
            {
                // Find the main canvas more reliably
                Canvas mainCanvas = GetComponentInParent<Canvas>();
                if (mainCanvas == null)
                {
                    // Search for active canvas in scene
                    Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    foreach (Canvas canvas in canvases)
                    {
                        if (canvas.gameObject.activeInHierarchy && !canvas.name.Contains("FreeRoam"))
                        {
                            mainCanvas = canvas;
                            break;
                        }
                    }
                }
                FreeRoamManager.Instance.mainTitleCanvas = mainCanvas;
                Debug.Log("[TitleScreenUI] Assigned main canvas: " + (mainCanvas != null ? mainCanvas.name : "null"));
            }
            
            // Try to find FreeRoamCanvas
            if (FreeRoamManager.Instance.freeRoamCanvas == null)
            {
                GameObject freeRoamGO = GameObject.Find("FreeRoamCanvas");
                if (freeRoamGO != null)
                {
                    Canvas freeRoamCanvas = freeRoamGO.GetComponent<Canvas>();
                    if (freeRoamCanvas != null)
                    {
                        FreeRoamManager.Instance.freeRoamCanvas = freeRoamCanvas;
                        Debug.Log("[TitleScreenUI] Found and assigned FreeRoamCanvas");
                    }
                }
                else
                {
                    Debug.LogWarning("[TitleScreenUI] FreeRoamCanvas GameObject not found in scene! Please create it or FreeRoamManager will use fallback UI.");
                }
            }
            
            FreeRoamManager.Instance.ShowFreeRoamMenu();
        }
        else
        {
            Debug.LogError("[TitleScreenUI] FreeRoamManager not found! Make sure FreeRoamManager exists in the scene.");
        }
    }
    
    private void ShowQRScanner()
    {
        Debug.Log("[TitleScreenUI] Opening QR Scanner");
        ShowPanel(qrScanPanel);
    }
    
    private void ShowMainMenu()
    {
        ShowPanel(mainMenuPanel);
    }
    
    private void ShowPanel(GameObject panel)
    {
        // Hide all panels
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
            
        if (qrScanPanel != null)
            qrScanPanel.SetActive(false);
        
        // Show requested panel
        if (panel != null)
            panel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        ShowMainMenu();
    }
}
