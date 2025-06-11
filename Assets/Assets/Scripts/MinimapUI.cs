using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinimapUI : MonoBehaviour
{
    [Header("UI Components")]
    // REMOVED: toggleButton - no longer needed
    [SerializeField] private GameObject minimapPanel;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private Button enlargeButton;
    [SerializeField] private GameObject enlargedMinimapPanel;
    [SerializeField] private Button closeButton; // NEW: Direct reference to close button
    [SerializeField] public RawImage enlargedMinimapDisplay; // FIXED: Made public so MinimapManager can access it

    [Header("Settings")]
    [SerializeField] private bool startExpanded = true;
    [SerializeField] private float updateInterval = 1f;

    private MinimapManager minimapManager;
    private bool isExpanded = true;
    private bool isEnlarged = false;

    private void Start()
    {
        minimapManager = FindFirstObjectByType<MinimapManager>();

        // FIXED: Setup buttons with proper visibility
        if (enlargeButton != null)
        {
            enlargeButton.onClick.AddListener(ToggleEnlargedMinimap);
            // FIXED: Ensure button is visible and interactable
            EnsureButtonVisibility(enlargeButton);
        }

        // FIXED: Setup close button directly
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseEnlargedMinimap);
            // FIXED: Ensure button is visible and interactable
            EnsureButtonVisibility(closeButton);
        }

        // FIXED: Find enlarged minimap display if not assigned
        if (enlargedMinimapDisplay == null && enlargedMinimapPanel != null)
        {
            enlargedMinimapDisplay = enlargedMinimapPanel.GetComponentInChildren<RawImage>();
        }

        isExpanded = startExpanded;
        UpdateMinimapVisibility();

        // Start updating objective text
        InvokeRepeating(nameof(UpdateObjectiveText), 0f, updateInterval);
        
        // FIXED: Setup enlarged minimap display
        SetupEnlargedMinimapDisplay();
    }

    // NEW: Helper method to ensure button visibility
    private void EnsureButtonVisibility(Button button)
    {
        if (button == null) return;
        
        // Ensure button is active and interactable
        button.gameObject.SetActive(true);
        button.interactable = true;
        
        // Ensure proper Canvas Group settings
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // Ensure button is in front
        button.transform.SetAsLastSibling();
        
        Debug.Log($"[MinimapUI] Ensured button {button.name} visibility and interactability");
    }

    // FIXED: Method to properly setup enlarged minimap display with border support
    private void SetupEnlargedMinimapDisplay()
    {
        if (enlargedMinimapDisplay != null)
        {
            // Find the main minimap display to share its texture
            RawImage mainDisplay = minimapPanel?.GetComponentInChildren<RawImage>();
            if (mainDisplay != null && mainDisplay.texture != null)
            {
                enlargedMinimapDisplay.texture = mainDisplay.texture;
                enlargedMinimapDisplay.color = Color.white;
                enlargedMinimapDisplay.raycastTarget = false;
                Debug.Log("[MinimapUI] Enlarged minimap display configured with shared texture");
                
                // FIXED: Trigger border creation for enlarged minimap
                MinimapManager minimapManager = FindFirstObjectByType<MinimapManager>();
                if (minimapManager != null)
                {
                    // Call the border creation method after enlarged display is set up
                    StartCoroutine(TriggerEnlargedBorderCreation(minimapManager));
                }
            }
            else
            {
                // If main display isn't ready yet, try again later
                Invoke(nameof(SetupEnlargedMinimapDisplay), 1f);
            }
        }
    }

    // FIXED: Helper coroutine to ensure border is created for enlarged minimap
    private System.Collections.IEnumerator TriggerEnlargedBorderCreation(MinimapManager minimapManager)
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure everything is set up
        
        // Access the private method through reflection
        var method = minimapManager.GetType().GetMethod("AddBorderToRawImage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null && enlargedMinimapDisplay != null)
        {
            method.Invoke(minimapManager, new object[] { enlargedMinimapDisplay, "EnlargedMinimapBorder" });
            Debug.Log("[MinimapUI] Triggered enlarged minimap border creation");
        }
    }

    // REMOVED: ToggleMinimap method - no longer needed

    private void ToggleEnlargedMinimap()
    {
        isEnlarged = !isEnlarged;

        if (enlargedMinimapPanel != null)
        {
            enlargedMinimapPanel.SetActive(isEnlarged);
            
            // FIXED: Ensure close button is visible when enlarged panel opens
            if (isEnlarged && closeButton != null)
            {
                StartCoroutine(EnsureCloseButtonVisibilityDelayed());
            }
        }

        // Hide small minimap when enlarged is open
        if (minimapPanel != null)
        {
            minimapPanel.SetActive(!isEnlarged && isExpanded);
            
            // FIXED: Ensure enlarge button is visible when small panel is shown
            if (!isEnlarged && enlargeButton != null)
            {
                EnsureButtonVisibility(enlargeButton);
            }
        }

        Debug.Log($"[MinimapUI] Enlarged minimap: {isEnlarged}");
    }

    // NEW: Coroutine to ensure close button visibility after panel opens
    private System.Collections.IEnumerator EnsureCloseButtonVisibilityDelayed()
    {
        yield return null; // Wait one frame for UI to update
        
        if (closeButton != null)
        {
            EnsureButtonVisibility(closeButton);
        }
    }

    // NEW: Simple close method
    private void CloseEnlargedMinimap()
    {
        isEnlarged = false;

        if (enlargedMinimapPanel != null)
        {
            enlargedMinimapPanel.SetActive(false);
        }

        if (minimapPanel != null)
        {
            minimapPanel.SetActive(isExpanded);
        }

        Debug.Log("[MinimapUI] Closed enlarged minimap");
    }

    private void UpdateMinimapVisibility()
    {
        if (minimapPanel != null && !isEnlarged)
        {
            minimapPanel.SetActive(isExpanded);
        }

        // REMOVED: toggle button text update - no longer needed
    }

    private void UpdateObjectiveText()
    {
        if (objectiveText != null && minimapManager != null)
        {
            string description = minimapManager.GetObjectiveDescription();
            objectiveText.text = description;

            // Color code based on objective type
            var objective = minimapManager.GetCurrentObjective();
            switch (objective)
            {
                case MinimapManager.MinimapObjective.Dungeon1:
                case MinimapManager.MinimapObjective.Dungeon2:
                case MinimapManager.MinimapObjective.Dungeon3:
                    objectiveText.color = Color.cyan;
                    break;
                case MinimapManager.MinimapObjective.SoapTask1:
                case MinimapManager.MinimapObjective.SoapTask2:
                case MinimapManager.MinimapObjective.SoapTask3:
                    objectiveText.color = Color.yellow;
                    break;
                case MinimapManager.MinimapObjective.Portal:
                    objectiveText.color = Color.magenta;
                    break;
                case MinimapManager.MinimapObjective.Completed:
                    objectiveText.color = Color.green;
                    break;
                default:
                    objectiveText.color = Color.white;
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}
