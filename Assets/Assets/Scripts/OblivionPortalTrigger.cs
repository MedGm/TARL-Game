using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Add this for the new Input System
using System.Collections;
public class OblivionPortalTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text promptText;

    [Header("UI Canvas")]
    [SerializeField] private Canvas portalCanvas; // NEW: Add this field and assign in inspector

    [Header("Settings")]
    [SerializeField] private string portalSceneName = "PortalScene";
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private string promptMessage = "Appuyez sur ESPACE pour entrer dans le portail final";
    [SerializeField] private string lockedMessage = "Collectez les 3 clés et terminez les 3 quêtes de savon pour ouvrir le portail !";

    public Button enterButton;

    [Header("Wizard Dialogue")]
    public DialogueCanvas wizardDialogueCanvas;
    public BubbleWizardNPC wizardNPC;
    public Canvas playerUICanvas; // Assign the player canvas in inspector
    public Canvas minimapCanvas; // NEW: Assign the minimap canvas in inspector
    private bool wizardNearby = false;

    private bool playerNearby = false;
    private Transform playerTransform;

    private bool dialogueActive = false; // Track if dialogue is running
    private int wizardDialogueIndex = 0;
    private string[] wizardLines = new string[]
    {
        "Ah, voyageur... Tu es arrivé jusqu'ici, mais le portail est scellé par un sort puissant.",
        "Dans le livre magique, j'ai mélangé l'ordre d'un nombre. Peux-tu le déchiffrer en le décomposant ?",
        "Observe bien le livre, trouve le nombre en vert, et recompose-le pour ouvrir le portail !"
    };

    // Minimap icon (assign a sprite in the inspector, set to Minimap layer)
    public GameObject minimapIcon;

    private void Start()
    {
        // Find player by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // FIXED: Ensure portal canvas starts disabled
        if (portalCanvas != null)
            portalCanvas.gameObject.SetActive(false);

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        // Hide enter button initially and always assign the listener
        if (enterButton != null)
        {
            enterButton.gameObject.SetActive(false);
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() => {
                Debug.Log("[OblivionPortalTrigger] Enter button clicked. playerNearby=" + playerNearby);
                if (playerNearby)
                    AttemptPortalEntry();
            });
        }

        // ENHANCED: Add both player UI canvas and minimap canvas to the dialogue canvas's list of canvases to hide
        if (wizardDialogueCanvas != null)
        {
            if (playerUICanvas != null && !wizardDialogueCanvas.canvasesToHide.Contains(playerUICanvas))
            {
                wizardDialogueCanvas.canvasesToHide.Add(playerUICanvas);
            }
            
            // NEW: Add minimap canvas to be hidden during dialogue
            if (minimapCanvas != null && !wizardDialogueCanvas.canvasesToHide.Contains(minimapCanvas))
            {
                wizardDialogueCanvas.canvasesToHide.Add(minimapCanvas);
            }
        }

        if (minimapIcon != null)
            minimapIcon.transform.SetParent(transform, false);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = distance <= detectionRadius;

        if (inRange != playerNearby)
        {
            playerNearby = inRange;
            if (playerNearby)
            {
                UpdatePrompt();
                ShowPrompt();
            }
            else
            {
                HidePrompt();
            }
        }

        // --- FIX: Use new Input System for Enter key ---
        // Only allow Enter key to trigger portal entry if dialogue is NOT active
        if (playerNearby && !dialogueActive && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            AttemptPortalEntry();
        }
        // If dialogue is active, allow Enter key to advance dialogue
        else if (dialogueActive && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            // Dialogue advancement is now handled by DialogueCanvas
        }
    }

    public void TryPortalEntry()
    {
        if (playerNearby)
            AttemptPortalEntry();
    }

    // Add this method so BubbleWizardNPC can call it
    public void SetWizardNearby(bool nearby)
    {
        wizardNearby = nearby;
    }

    private void UpdatePrompt()
    {
        if (promptText == null) return;

        if (GameManager.Instance == null)
        {
            promptText.text = promptMessage;
            return;
        }

        if (GameManager.Instance.keysCollected < 3 || !GameManager.Instance.HasCompletedAllSoapTaskTypes())
        {
            promptText.text = lockedMessage;
        }
        else
        {
            promptText.text = promptMessage;
        }
    }

    private void ShowPrompt()
    {
        // FIXED: Enable portal canvas first
        if (portalCanvas != null)
            portalCanvas.gameObject.SetActive(true);
            
        if (promptText != null)
            promptText.gameObject.SetActive(true);
        if (enterButton != null)
        {
            enterButton.gameObject.SetActive(true);
            enterButton.interactable = true;
        }
    }

    private void HidePrompt()
    {
        // FIXED: Disable portal canvas when hiding prompt
        if (portalCanvas != null)
            portalCanvas.gameObject.SetActive(false);
            
        if (promptText != null)
            promptText.gameObject.SetActive(false);
        if (enterButton != null)
        {
            enterButton.gameObject.SetActive(false);
            enterButton.interactable = false;
        }
    }

    private void AttemptPortalEntry()
    {
        Debug.Log("[OblivionPortalTrigger] AttemptPortalEntry called. playerNearby=" + playerNearby);
        
        // Check basic conditions
        if (!playerNearby) return;
        if (GameManager.Instance == null) return;
        if (dialogueActive) return; // Never proceed if dialogue is already active

        // Check game progression requirements
        bool requirementsMet = GameManager.Instance.keysCollected >= 3 && 
                              GameManager.Instance.HasCompletedAllSoapTaskTypes();
                              
        if (!requirementsMet)
        {
            // Requirements not met, show error message
            if (promptText != null) 
                promptText.text = lockedMessage;
            return;
        }

        // CRITICAL FIX: Check for wizard dialogue path FIRST and handle separately
        if (wizardNearby && wizardDialogueCanvas != null)
        {
            Debug.Log("[OblivionPortalTrigger] Starting wizard dialogue sequence");
            
            // Clear prompts
            if (promptText != null) promptText.gameObject.SetActive(false);
            if (enterButton != null) enterButton.gameObject.SetActive(false);
            
            // Activate dialogue - this sets dialogueActive to true
            dialogueActive = true;
            
            // Show the dialogue canvas and dialogue
            wizardDialogueCanvas.ShowDialogue(wizardLines, () => {
                // This callback only runs AFTER the dialogue is complete
                Debug.Log("[OblivionPortalTrigger] Dialogue completed, loading portal scene after delay");
                StartCoroutine(LoadSceneAfterDelay(portalSceneName, 0.5f));
            });
        }
        else
        {
            // No wizard nearby, go directly to portal scene
            Debug.Log("[OblivionPortalTrigger] No wizard dialogue, loading portal scene directly");
            
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.PokemonStyleTransition(portalSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(portalSceneName);
            }
        }
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        dialogueActive = false; // Reset dialogue flag
        yield return new WaitForSeconds(delay);
        
        // Use Pokemon-style transition for the final portal (dramatic effect)
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.PokemonStyleTransition(sceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, new Vector3(2*detectionRadius, detectionRadius, detectionRadius));
    }
}
