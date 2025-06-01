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
                // No need to set dungeonId anymore
                Debug.Log("[DungeonEntrance] Enter button pressed on: " + gameObject.name + " (scene=" + dungeonSceneName + ")");
                DungeonManager.EnterDungeon(this);
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
        if (GameManager.Instance != null)
        {
            // Only allow entry if this dungeon is unlocked (optional: keep if you want sequential unlock)
            if (dungeonSceneName == "DungeonScene")
                unlocked = true;
            else if (dungeonSceneName == "DungeonScene2")
                unlocked = GameManager.Instance.IsDungeonCompleted("dungeon1");
            else if (dungeonSceneName == "DungeonScene3")
                unlocked = GameManager.Instance.IsDungeonCompleted("dungeon2");
            // Add more if you have more dungeons/scenes
        }

        if (promptText != null)
        {
            Color c = promptText.color;
            c.a = 1f;
            promptText.color = c;
            promptText.text = unlocked ? frenchPromptMessage : "Ce donjon est verrouillé pour l'instant.";
            promptText.gameObject.SetActive(true);
            StartCoroutine(AnimateTextIn());
        }
        if (enterButton != null)
        {
            enterButton.gameObject.SetActive(true);
            enterButton.interactable = unlocked;
        }
    }
    
    private IEnumerator AnimateTextIn()
    {
        if (promptText == null) yield break;
        
        // Starting with transparent text
        Color startColor = promptText.color;
        Color transparentColor = new Color(startColor.r, startColor.g, startColor.b, 0);
        promptText.color = transparentColor;
        
        // Fade in text
        float time = 0;
        float duration = 0.5f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            
            // Easing function for smoother animation
            t = 1 - Mathf.Pow(1 - t, 3); // Ease out cubic
            
            promptText.color = Color.Lerp(transparentColor, startColor, t);
            yield return null;
        }
        
        // Ensure final color is set
        promptText.color = startColor;
    }

    private void HidePrompt()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
            StopAllCoroutines();
        }
        if (enterButton != null)
        {
            enterButton.gameObject.SetActive(false);
            enterButton.interactable = false;
        }
    }

    private void UpdatePlayerNearby()
    {
        // Always recalculate playerNearby based on current distance
        if (playerTransform != null)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            playerNearby = distance <= detectionRadius;
        }
    }

    public void TryEnter()
    {
        if (playerNearby)
            EnterDungeon();
    }

    private void EnterDungeon()
    {
        Debug.Log("[DungeonEntrance] EnterDungeon called.");

        // Save player position before entering
        if (GameManager.Instance != null)
        {
            GameManager.Instance.lastPlayerPosition = playerTransform.position;
        }
        Debug.Log("[DungeonEntrance] Loading scene: " + dungeonSceneName);
        SceneManager.LoadScene(dungeonSceneName);
    }
    
    // Helper method to visualize the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
