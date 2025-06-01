using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueCanvas : MonoBehaviour
{
    public TMP_Text dialogueText;
    public Button continueButton;
    public CanvasGroup canvasGroup;

    // Add references to other canvases that should be hidden during dialogue
    public List<Canvas> canvasesToHide = new List<Canvas>();
    public string playerCanvasTag = "PlayerUI"; // Optional: Find by tag

    // To store the original active state of canvases
    private Dictionary<Canvas, bool> originalCanvasStates = new Dictionary<Canvas, bool>();

    private string[] dialogueLines;
    private int currentLine = 0;
    private System.Action onDialogueComplete;

    private void Awake()
    {
        // Make sure the canvas group exists
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        // If still null, add one
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        // Make sure we start hidden
        Hide();
    }

    public void ShowDialogue(string[] lines, System.Action onComplete = null)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogError("[DialogueCanvas] Attempted to show dialogue with no lines");
            onDialogueComplete?.Invoke();
            return;
        }

        gameObject.SetActive(true);
        
        // Store the dialogue lines and callback
        dialogueLines = lines;
        currentLine = 0;
        onDialogueComplete = onComplete;
        
        // Initialize the canvas group
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        
        // Set up first line
        if (dialogueText != null)
        {
            dialogueText.text = dialogueLines[currentLine];
        }
        
        // Set up continue button
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(NextLine);
            continueButton.interactable = true;
        }
        
        Debug.Log($"[DialogueCanvas] Started dialogue with {lines.Length} lines. First line: {lines[0]}");

        // Hide other canvases (like player UI) before showing dialogue
        HideOtherCanvases();
    }

    private void ShowCurrentLine()
    {
        if (dialogueText != null && dialogueLines != null && currentLine < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLine];
        }
    }

    public void SetDialogueLine(string line)
    {
        if (dialogueText != null)
            dialogueText.text = line;
    }

    public void NextLine()
    {
        currentLine++;
        
        Debug.Log("[DialogueCanvas] Moving to line " + currentLine);
        
        if (dialogueLines != null && currentLine < dialogueLines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            Debug.Log("[DialogueCanvas] End of dialogue reached, hiding canvas");
            
            // FIXED: Don't immediately hide/disable - just hide visually first
            HideVisual();
            
            // Complete dialogue with delay but don't disable GameObject yet
            StartCoroutine(CompleteAfterDelay(0.5f));
        }
    }

    // NEW: Visual-only hide that keeps GameObject active
    private void HideVisual()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        Debug.Log("[DialogueCanvas] Canvas visually hidden but still active");
    }

    // NEW: Method to hide other canvases
    private void HideOtherCanvases()
    {
        // Save and hide specified canvases
        foreach (Canvas canvas in canvasesToHide)
        {
            if (canvas != null && canvas != GetComponentInParent<Canvas>())
            {
                originalCanvasStates[canvas] = canvas.gameObject.activeSelf;
                canvas.gameObject.SetActive(false);
            }
        }
        
        // Find and hide player canvas by tag if not already in the list
        if (!string.IsNullOrEmpty(playerCanvasTag))
        {
            GameObject[] playerUIObjects = GameObject.FindGameObjectsWithTag(playerCanvasTag);
            foreach (GameObject obj in playerUIObjects)
            {
                Canvas canvas = obj.GetComponent<Canvas>();
                if (canvas != null && !canvasesToHide.Contains(canvas) && 
                    canvas != GetComponentInParent<Canvas>())
                {
                    originalCanvasStates[canvas] = canvas.gameObject.activeSelf;
                    canvas.gameObject.SetActive(false);
                }
            }
        }
    }

    // Wait a moment before completing the dialogue
    private IEnumerator CompleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Restore other canvases to their original state
        RestoreOtherCanvases();
        
        if (onDialogueComplete != null)
        {
            Debug.Log("[DialogueCanvas] Invoking completion callback");
            onDialogueComplete();
        }
        
        // Only NOW fully disable the GameObject
        gameObject.SetActive(false);
        Debug.Log("[DialogueCanvas] GameObject disabled after callback completion");
    }
    
    // NEW: Method to restore other canvases
    private void RestoreOtherCanvases()
    {
        foreach (var kvp in originalCanvasStates)
        {
            if (kvp.Key != null)
            {
                kvp.Key.gameObject.SetActive(kvp.Value);
            }
        }
        originalCanvasStates.Clear();
    }

    // UPDATED: Hide now just calls HideVisual
    public void Hide()
    {
        HideVisual();
        Debug.Log("[DialogueCanvas] Hide called (visually hidden only)");
    }
}
