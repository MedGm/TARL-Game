using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SoapBubbleTaskManager : MonoBehaviour
{
    [Header("References")]
    public Button[] bubbleButtons; // Assign 7 buttons in order: Ones (0) to Millions (6)
    public TMP_Text[] bubbleTexts; // Assign 7 TMP_Texts for each bubble's label
    public TMP_Text promptText;
    public TMP_Text timerText;

    [Header("Settings")]
    public string[] placeValueNames = {
        "Ones", "Tens", "Hundreds", "Thousands", "TenThousands", "HundredThousands", "Millions"
    };
    public float promptDuration = 5f; // Increased prompt time
    public float selectionTime = 10f;
    public string overworldSceneName = "overworldScene"; // Set your main scene name

    // Progress tracking
    public int totalTasks = 3; // Number of times this task must be played (matches number of keys)
    private int completedTasks = 0;
    private int successCount = 0;
    private int failCount = 0;

    // Internal state
    private int currentTargetIndex = -1;
    private float timer = 0f;
    private bool waitingForSelection = false;

    // Save results for each round
    private List<bool> taskResults = new List<bool>();

    private int displayedNumber = 0;

    private void Start()
    {
        // --- Progressive difficulty based on soapTasksPlayed ---
        int progression = 1;
        if (GameManager.Instance != null)
            progression = Mathf.Clamp(GameManager.Instance.soapTasksPlayed + 1, 1, 3);

        // Set up progression
        switch (progression)
        {
            case 1:
                totalTasks = 1;
                selectionTime = 12f;
                placeValueNames = new string[] { "Ones", "Tens", "Hundreds" };
                break;
            case 2:
                totalTasks = 2;
                selectionTime = 10f;
                placeValueNames = new string[] { "Ones", "Tens", "Hundreds", "Thousands", "TenThousands" };
                break;
            case 3:
            default:
                totalTasks = 3;
                selectionTime = 8f;
                placeValueNames = new string[] { "Ones", "Tens", "Hundreds", "Thousands", "TenThousands", "HundredThousands", "Millions" };
                break;
        }

        // For this task, always use the "Prompt with a Number, Pop the Place Value" style

        // Generate a random number with as many digits as placeValueNames
        int digitCount = placeValueNames.Length;
        int min = (int)Mathf.Pow(10, digitCount - 1);
        int max = (int)Mathf.Pow(10, digitCount) - 1;

        // --- Enhanced: Ensure the number has at least one digit that is unique ---
        string numStr;
        int[] digitCounts;
        int uniqueDigitIdx = -1;
        int attempts = 0;
        do
        {
            displayedNumber = Random.Range(min, max + 1);
            numStr = displayedNumber.ToString();
            digitCounts = new int[10];
            foreach (char c in numStr)
                digitCounts[c - '0']++;
            // Find a digit that occurs only once
            uniqueDigitIdx = -1;
            for (int i = 0; i < numStr.Length; i++)
            {
                if (digitCounts[numStr[i] - '0'] == 1)
                {
                    uniqueDigitIdx = i;
                    break;
                }
            }
            attempts++;
            // If all digits are repeated, try again (limit attempts to avoid infinite loop)
        } while (uniqueDigitIdx == -1 && attempts < 100);

        // Set up bubble labels and play animation on each bubble
        for (int i = 0; i < bubbleTexts.Length; i++)
        {
            if (i < placeValueNames.Length)
            {
                bubbleButtons[i].gameObject.SetActive(true);
                if (bubbleTexts[i] != null)
                    bubbleTexts[i].text = placeValueNames[i];
            }
            else
            {
                bubbleButtons[i].gameObject.SetActive(false);
            }

            // --- Ensure the bubble button has an Image and Animator ---
            if (bubbleButtons[i] != null)
            {
                // 1. Get the Image component (the visual part of the button)
                Image img = bubbleButtons[i].GetComponent<Image>();
                Animator anim = bubbleButtons[i].GetComponent<Animator>();

                // 2. If the Animator is missing, log a warning
                if (anim == null)
                {
                    Debug.LogWarning($"[SoapBubbleTaskManager] Bubble button {i} ({bubbleButtons[i].name}) is missing an Animator component.");
                }
                else
                {
                    // 3. If the Animator Controller is missing, log a warning
                    if (anim.runtimeAnimatorController == null)
                    {
                        Debug.LogWarning($"[SoapBubbleTaskManager] Bubble button {i} ({bubbleButtons[i].name}) is missing an Animator Controller.");
                    }
                    // 4. Optionally, force the animation to play (if you have a state name)
                    // anim.Play("BubbleIdle"); // Replace with your animation state name if needed
                }

                // 5. If the Image is missing, log a warning
                if (img == null)
                {
                    Debug.LogWarning($"[SoapBubbleTaskManager] Bubble button {i} ({bubbleButtons[i].name}) is missing an Image component.");
                }
            }
        }

        // --- Animator troubleshooting: force idle animation on start ---
        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            if (i < digitCount)
            {
                Animator anim = bubbleButtons[i].GetComponent<Animator>();
                if (anim != null)
                    anim.Play("idle", 0, 0f);
            }
        }

        // Remove all listeners and disable all buttons initially
        foreach (var btn in bubbleButtons)
        {
            btn.onClick.RemoveAllListeners();
            btn.interactable = false;
        }

        if (promptText != null)
            promptText.text = "";

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        completedTasks = 0;
        successCount = 0;
        failCount = 0;
        taskResults.Clear();

        // Register that the SoapScene was played (only once per scene entry)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSoapTaskPlayed();
        }

        StartCoroutine(StartNextTask());
    }

    private IEnumerator StartNextTask()
    {
        // Disable all buttons
        foreach (var btn in bubbleButtons)
            btn.interactable = false;

        // Use only as many bubbles as the number of digits in displayedNumber
        string numStr = displayedNumber.ToString();
        int digitCount = numStr.Length;

        // --- Enhanced: Pick a digit that may be repeated, and require all matching units to be popped ---
        // Pick a random digit index
        int digitIdx = Random.Range(0, digitCount);
        char digit = numStr[digitIdx];

        // Find all indices where this digit occurs - FIXED FOR CORRECT PLACE VALUES
        List<int> matchingIndices = new List<int>();
        for (int i = 0; i < numStr.Length; i++)
        {
            if (numStr[i] == digit)
            {
                // Convert to correct place value index (FIXED)
                // For example, in "537", i=2 (7) corresponds to place value index 0 (Ones)
                int placeValueIndex = numStr.Length - 1 - i;
                matchingIndices.Add(placeValueIndex);
            }
        }

        // Debug clearer information about what's expected
        Debug.Log($"[SoapBubbleTaskManager] Number: {displayedNumber}, looking for digit {digit} which appears at positions: {string.Join(", ", matchingIndices)}");
        Debug.Log($"[SoapBubbleTaskManager] Need to pop these place values: {string.Join(", ", matchingIndices.Select(idx => placeValueNames[idx]))}");

        // Prompt: "Dans 4,582, pop la bulle correspondant à la position du chiffre 8."
        if (promptText != null)
        {
            if (matchingIndices.Count == 1)
            {
                promptText.text = $"Dans <b>{displayedNumber}</b>, pop la bulle correspondant à la position du chiffre <b>{digit}</b>.";
            }
            else
            {
                // If the digit appears multiple times, clarify the instruction
                promptText.text = $"Dans <b>{displayedNumber}</b>, pop <b>toutes</b> les bulles correspondant à la position du chiffre <b>{digit}</b>.";
            }
            promptText.gameObject.SetActive(true);
        }

        // Wait for prompt duration
        yield return new WaitForSeconds(promptDuration);

        if (promptText != null)
            promptText.text = "";

        // FIXED: Convert string indices to place value indices
        HashSet<int> correctIndices = new HashSet<int>(matchingIndices);
        HashSet<int> poppedIndices = new HashSet<int>();

        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            bubbleButtons[i].onClick.RemoveAllListeners();
            if (i < digitCount)
            {
                int capturedIndex = i; // Properly capture the index for the lambda
                bubbleButtons[i].onClick.AddListener(() => {
                    // Never access EventSystem in this handler - use the capturedIndex directly
                    if (!waitingForSelection) return;
                    
                    if (correctIndices.Contains(capturedIndex) && !poppedIndices.Contains(capturedIndex))
                    {
                        // This is a correct bubble and hasn't been popped yet
                        poppedIndices.Add(capturedIndex);
                        bubbleButtons[capturedIndex].interactable = false;
                        
                        // If all required bubbles have been popped, finish as success
                        if (poppedIndices.Count == correctIndices.Count)
                        {
                            waitingForSelection = false;
                            if (timerText != null)
                                timerText.gameObject.SetActive(false);
                            successCount++;
                            taskResults.Add(true);
                            Debug.Log($"[SoapBubbleTaskManager] SUCCESS! All required bubbles popped: {string.Join(", ", poppedIndices)}");
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.RegisterSoapTaskResult(true);
                                Debug.Log("[SoapBubbleTaskManager] Registered SUCCESS with GameManager");
                            }
                            EndRound();
                        }
                        else
                        {
                            // Correct but still need more bubbles
                            Debug.Log($"[SoapBubbleTaskManager] Correct bubble {capturedIndex} ({placeValueNames[capturedIndex]}) popped. Still need {correctIndices.Count - poppedIndices.Count} more");
                        }
                    }
                    else if (!correctIndices.Contains(capturedIndex))
                    {
                        // Wrong bubble clicked
                        waitingForSelection = false;
                        if (timerText != null)
                            timerText.gameObject.SetActive(false);
                        failCount++;
                        taskResults.Add(false);
                        Debug.Log($"[SoapBubbleTaskManager] WRONG bubble {capturedIndex} ({placeValueNames[capturedIndex]}) clicked. Expected: {string.Join(", ", correctIndices)}");
                        if (GameManager.Instance != null)
                            GameManager.Instance.RegisterSoapTaskResult(false);
                        EndRound();
                    }
                });
                bubbleButtons[i].interactable = true;
            }
            else
            {
                bubbleButtons[i].interactable = false;
            }
        }

        // Start timer
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timer = selectionTime;
            waitingForSelection = true;
            StartCoroutine(SelectionTimerCoroutine());
        }
    }

    private IEnumerator SelectionTimerCoroutine()
    {
        while (waitingForSelection && timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(timer).ToString() + "s";
            yield return null;
        }

        if (waitingForSelection)
        {
            waitingForSelection = false;
            if (timerText != null)
                timerText.gameObject.SetActive(false);

            // Time ran out, count as fail
            failCount++;
            taskResults.Add(false);
            EndRound();
        }
    }

    private void PlayPopAnimation(int bubbleIdx)
    {
        // Animation removed: do nothing
    }

    private void CorrectBubbleClicked()
    {
        Debug.Log("Correct bubble clicked.");
        if (!waitingForSelection) return;
        waitingForSelection = false;
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        PlayPopAnimation(currentTargetIndex);

        successCount++;
        taskResults.Add(true);

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterSoapTaskResult(true);

        EndRound(); // No delay needed, bubble disables via Animation Event
    }

    private void WrongBubbleClicked()
    {
        Debug.Log("Wrong bubble clicked.");
        if (!waitingForSelection) return;
        waitingForSelection = false;
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        int idx = GetClickedBubbleIndex();
        PlayPopAnimation(idx);

        failCount++;
        taskResults.Add(false);

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterSoapTaskResult(false);

        EndRound(); // No delay needed, bubble disables via Animation Event
    }

    // Helper to get the index of the clicked bubble (for wrong answers)
    private int GetClickedBubbleIndex()
    {
        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            if (bubbleButtons[i].interactable == false) // Just clicked and now disabled
                return i;
        }
        return -1;
    }

    private void EndRound()
    {
        // Disable all buttons
        foreach (var btn in bubbleButtons)
        {
            btn.interactable = false;
            btn.onClick.RemoveAllListeners();
        }

        completedTasks++;

        // Debug the registered results to ensure they match
        string results = string.Join(", ", taskResults.Select(r => r ? "Success" : "Fail"));
        Debug.Log($"[SoapBubbleTaskManager] Task results so far: [{results}], Success={successCount}, Fail={failCount}");

        if (completedTasks < totalTasks)
        {
            StartCoroutine(StartNextTask());
        }
        else
        {
            // Task finished, show neutral message and return to overworldScene after a delay
            if (promptText != null)
                promptText.text = "Task finished!";
            if (timerText != null)
                timerText.gameObject.SetActive(false);

            // Use SceneManager.sceneLoaded to ensure GameManager's TrySpawnBubbleSoapChaser is called after returning
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnoverworldSceneLoaded;
            StartCoroutine(ReturnTooverworldSceneAfterDelay(2.5f));
        }
    }

    private IEnumerator ReturnTooverworldSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(overworldSceneName);
    }

    private void OnoverworldSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == overworldSceneName)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TrySpawnBubbleSoapChaser();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnoverworldSceneLoaded;
        }
    }
}
