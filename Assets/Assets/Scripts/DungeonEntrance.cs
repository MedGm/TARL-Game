using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DungeonEntrance : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign your TMP_Text component here in the inspector")]
    [SerializeField] private TMP_Text promptText;
    
    public Button enterButton; // Assign in Inspector

    [Header("Settings")]
    public string frenchPromptMessage = "Appuyez sur ENTRÉE pour entrer dans le donjon";
    public string dungeonSceneName = "DungeonScene"; // <-- set this per entrance in Inspector (DungeonScene, DungeonScene2, DungeonScene3)
    public float detectionRadius = 2f;

    [Header("Text Styling")]
    [SerializeField] private Color textColor = new Color(1f, 0.9f, 0.2f); // Golden yellow
    [SerializeField] private Color outlineColor = new Color(0.5f, 0.1f, 0f); // Dark red/brown
    [SerializeField] private float outlineThickness = 0.2f;

    // Minimap icon (assign a sprite in the inspector, set to Minimap layer)
    public GameObject minimapIcon;

    private bool playerNearby = false;
    private Transform playerTransform;

    // --- NEW: DungeonEntrance registration ---
    private void OnEnable()
    {
        DungeonManager.RegisterEntrance(this);
    }
    private void OnDisable()
    {
        DungeonManager.UnregisterEntrance(this);
    }
    
    private void Awake()
    {
        // Try to find the text component if not assigned
        if (promptText == null)
        {
            // First check if it's a child of this GameObject
            promptText = GetComponentInChildren<TMP_Text>(true);
            
            // If still not found, try to find it in the scene
            if (promptText == null)
            {
                // Try to find any TextMeshPro text in the scene - using non-obsolete method
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
                    if (texts.Length > 0)
                    {
                        promptText = texts[0];
                        Debug.Log("Found TMP_Text automatically in canvas: " + canvas.name);
                        break;
                    }
                }
            }
        }
    }
    
    private void Start()
    {
        // Initial setup - hide text
        if (promptText != null)
        {
            // Apply styling
            promptText.color = new Color(textColor.r, textColor.g, textColor.b, 1f); // Ensure alpha is 1
            promptText.text = frenchPromptMessage;

            // Only set outline properties if the component supports it
            try {
                promptText.outlineWidth = outlineThickness;
                promptText.outlineColor = outlineColor;
            }
            catch {
                Debug.LogWarning("This TextMeshPro component doesn't support outline properties.");
            }

            // Hide initially
            promptText.gameObject.SetActive(false);
            Debug.Log("Text component configured successfully: " + promptText.gameObject.name);
        }
        else
        {
            Debug.LogWarning("No TMP_Text found! The prompt will not be displayed. Please create a UI Canvas with a TextMeshPro Text element.");
        }
        
        // Hide enter button initially and always assign the listener
        if (enterButton != null)
        {
            enterButton.gameObject.SetActive(false);
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() => {
                Debug.Log("[DungeonEntrance] Enter button pressed on: " + gameObject.name + " (scene=" + dungeonSceneName + ")");
                // FIXED: Call EnterDungeon directly instead of going through DungeonManager
                EnterDungeon();
            });
        }

        // Find player by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("No GameObject with 'Player' tag found!");
        }

        if (minimapIcon != null)
            minimapIcon.transform.SetParent(transform, false);

        // ADDED: Ensure SceneTransition exists
        EnsureSceneTransitionExists();
    }

    private void EnsureSceneTransitionExists()
    {
        if (SceneTransition.Instance == null)
        {
            Debug.LogWarning("[DungeonEntrance] SceneTransition not found, creating basic transition");
            
            // Create a simple fallback transition system
            GameObject transitionGO = new GameObject("SceneTransition_Fallback");
            DontDestroyOnLoad(transitionGO);
            
            // You could add a simplified transition script here if needed
            // For now, we'll rely on the null check in EnterDungeon()
        }
    }

    private void Update()
    {
        // Use distance-based detection
        if (playerTransform != null)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            bool inRange = distance <= detectionRadius;
            
            // Only update if state changed
            if (inRange != playerNearby)
            {
                playerNearby = inRange;
                if (playerNearby)
                {
                    ShowPrompt();
                }
                else
                {
                    HidePrompt();
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            ShowPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            HidePrompt();
        }
    }

    private void ShowPrompt()
    {
        bool unlocked = true;
        bool completed = false;
        string promptMessage = frenchPromptMessage;
        
        // NEW: Check if we're in free roam mode - if so, bypass all restrictions
        if (FreeRoamManager.ShouldBypassRestrictions())
        {
            unlocked = true;
            completed = false;
            promptMessage = frenchPromptMessage + " (Mode Libre)";
            Debug.Log("[DungeonEntrance] Free roam mode - all dungeons unlocked");
        }
        else if (GameManager.Instance != null)
        {
            // Normal game mode - check progression
            string dungeonId = GetDungeonIdFromSceneName(dungeonSceneName);
            completed = GameManager.Instance.IsDungeonCompleted(dungeonId);
            
            if (completed)
            {
                unlocked = false;
                promptMessage = "Vous avez complété ce donjon !";
            }
            else
            {
                // Check if dungeon is unlocked based on progression
                if (dungeonSceneName == "DungeonScene")
                {
                    unlocked = true; // First dungeon always unlocked
                }
                else if (dungeonSceneName == "DungeonScene2")
                {
                    unlocked = GameManager.Instance.IsDungeonCompleted("dungeon1");
                }
                else if (dungeonSceneName == "DungeonScene3")
                {
                    unlocked = GameManager.Instance.IsDungeonCompleted("dungeon2");
                }
                
                if (!unlocked)
                {
                    promptMessage = "Ce donjon est verrouillé pour l'instant.";
                }
            }
        }
        else
        {
            // Fallback logic
            unlocked = (dungeonSceneName == "DungeonScene");
            if (!unlocked)
            {
                promptMessage = "Ce donjon est verrouillé pour l'instant.";
            }
        }

        if (promptText != null)
        {
            Color c = promptText.color;
            c.a = 1f;
            
            // Change color based on status
            if (completed)
            {
                c = Color.green; // Green for completed
            }
            else if (!unlocked)
            {
                c = Color.red; // Red for locked
            }
            else
            {
                c = textColor; // Normal color for available
            }
            
            promptText.color = c;
            promptText.text = promptMessage;
            promptText.gameObject.SetActive(true);
            StartCoroutine(AnimateTextIn());
        }
        
        if (enterButton != null)
        {
            enterButton.gameObject.SetActive(true);
            enterButton.interactable = unlocked && !completed; // Can only enter if unlocked and not completed
        }
    }
    
    // NEW: Helper method to convert scene name to dungeon ID
    private string GetDungeonIdFromSceneName(string sceneName)
    {
        switch (sceneName)
        {
            case "DungeonScene":
                return "dungeon1";
            case "DungeonScene2":
                return "dungeon2";
            case "DungeonScene3":
                return "dungeon3";
            default:
                return "unknown";
        }
    }

    private void EnterDungeon()
    {
        // NEW: In free roam mode, always allow entry
        if (FreeRoamManager.ShouldBypassRestrictions())
        {
            Debug.Log($"[DungeonEntrance] Free roam mode - entering {dungeonSceneName} without restrictions");
        }
        else if (GameManager.Instance != null)
        {
            // Normal mode - check restrictions
            string dungeonId = GetDungeonIdFromSceneName(dungeonSceneName);
            if (GameManager.Instance.IsDungeonCompleted(dungeonId))
            {
                Debug.Log($"[DungeonEntrance] Cannot enter {dungeonSceneName} - already completed!");
                return;
            }
            
            // Save player position and current dungeon before entering
            GameManager.Instance.lastPlayerPosition = playerTransform.position;
            GameManager.Instance.currentDungeonId = dungeonId;
        }
        
        // FIXED: Call SceneTransition directly instead of going through DungeonManager
        if (SceneTransition.Instance != null)
        {
            Debug.Log($"[DungeonEntrance] Using SceneTransition to load: {dungeonSceneName}");
            SceneTransition.Instance.PokemonStyleTransition(dungeonSceneName); // Use dramatic transition for dungeon entry
        }
        else
        {
            Debug.LogWarning($"[DungeonEntrance] SceneTransition.Instance is null! Loading {dungeonSceneName} directly");
            SceneManager.LoadScene(dungeonSceneName);
        }
    }
    
    // Helper method to visualize the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private void HidePrompt()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
        
        if (enterButton != null)
        {
            enterButton.gameObject.SetActive(false);
            enterButton.interactable = false;
        }
    }

    private IEnumerator AnimateTextIn()
    {
        if (promptText == null) yield break;
        
        // Simple fade-in animation
        Color originalColor = promptText.color;
        Color transparentColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        
        promptText.color = transparentColor;
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, originalColor.a, elapsed / duration);
            promptText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        promptText.color = originalColor;
    }
}
