using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SoapBubbleTaskManager2 : MonoBehaviour
{
    public Button[] bubbleButtons; // Assign 7 buttons
    public TMP_Text[] bubbleTexts; // Assign 7 TMP_Texts
    public TMP_Text promptText;
    public TMP_Text timerText;

    public string[] placeValueNames = {
        "Unités", "Dizaines", "Centaines", "Milliers", "DixMilliers", "CentMilliers", "Millions"
    };
    public float promptDuration = 5f;
    public float selectionTime = 10f;
    public string overworldSceneName = "overworldScene";

    public int totalTasks = 2;
    private int completedTasks = 0;
    private int currentTargetIndex = -1;
    private float timer = 0f;
    private bool waitingForSelection = false;
    private int displayedNumber = 0;

    private void Start()
    {
        // Use 4-7 digit numbers for progression
        int digitCount = Mathf.Clamp(totalTasks + 2, 4, 7);
        int min = (int)Mathf.Pow(10, digitCount - 1);
        int max = (int)Mathf.Pow(10, digitCount) - 1;
        
        // Generate a number with distinct digits to avoid confusion
        int attempts = 0;
        int maxAttempts = 100; // Prevent infinite loop
        do {
            displayedNumber = Random.Range(min, max + 1);
            attempts++;
        } while (!HasDistinctDigits(displayedNumber) && attempts < maxAttempts);
        
        Debug.Log($"[SoapBubbleTaskManager2] Generated number with distinct digits: {displayedNumber}");

        // FIXED: Set up bubble labels to match place value positions
        string numStr = displayedNumber.ToString();
        for (int i = 0; i < bubbleTexts.Length; i++)
        {
            if (i < digitCount)
            {
                bubbleButtons[i].gameObject.SetActive(true);
                if (bubbleTexts[i] != null)
                {
                    // FIXED: Button i should show the digit that corresponds to place value i
                    // Place value 0 = Ones = rightmost digit
                    // Place value 1 = Tens = second from right, etc.
                    char digitAtThisPlaceValue = numStr[digitCount - 1 - i];
                    bubbleTexts[i].text = digitAtThisPlaceValue.ToString();
                    Debug.Log($"[SoapBubbleTaskManager2] Button {i} ({placeValueNames[i]}) shows digit {digitAtThisPlaceValue}");
                }
            }
            else
            {
                bubbleButtons[i].gameObject.SetActive(false);
            }
        }

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

        // Register that this soap task scene was played
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSoapTaskPlayed();
        }

        StartCoroutine(StartNextTask());
    }

    // Helper function to check if a number has all distinct digits
    private bool HasDistinctDigits(int number)
    {
        HashSet<int> digits = new HashSet<int>();
        while (number > 0)
        {
            int digit = number % 10;
            if (digits.Contains(digit))
                return false;
            digits.Add(digit);
            number /= 10;
        }
        return true;
    }

    private IEnumerator StartNextTask()
    {
        foreach (var btn in bubbleButtons)
            btn.interactable = false;

        string numStr = displayedNumber.ToString();
        int digitCount = numStr.Length;

        // Pick a random place value from the available ones
        int targetPlaceValueIndex = Random.Range(0, digitCount);
        string placeName = placeValueNames[targetPlaceValueIndex];
        
        // FIXED: Get the actual digit at that place value position
        char targetDigit = numStr[digitCount - 1 - targetPlaceValueIndex];
        
        Debug.Log($"[SoapBubbleTaskManager2] Number: {displayedNumber}, targeting place value {targetPlaceValueIndex} ({placeName}), which has digit {targetDigit}");
        Debug.Log($"[SoapBubbleTaskManager2] Button {targetPlaceValueIndex} should be clicked (it shows digit {targetDigit})");

        // FIXED: Set prompt and keep it visible throughout the task
        if (promptText != null)
        {
            promptText.text = $"Dans <b>{displayedNumber}</b>, pop la bulle du chiffre à la position <b>{placeName}</b>.";
            promptText.gameObject.SetActive(true);
            // REMOVED: Do not clear the prompt text anymore
        }

        yield return new WaitForSeconds(promptDuration);

        // REMOVED: No longer clear the prompt text here
        // if (promptText != null)
        //     promptText.text = "";

        // The current target is the place value index where we want the click
        currentTargetIndex = targetPlaceValueIndex;

        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            int capturedIdx = i; // Capture index for lambda
            bubbleButtons[i].onClick.RemoveAllListeners();
            
            if (i == currentTargetIndex && i < digitCount)
            {
                Debug.Log($"[SoapBubbleTaskManager2] Button {i} ({placeValueNames[i]}) is the CORRECT button - shows digit {targetDigit}");
                bubbleButtons[i].onClick.AddListener(() => {
                    Debug.Log($"[SoapBubbleTaskManager2] Clicked correct button {capturedIdx} ({placeValueNames[capturedIdx]})");
                    CorrectBubbleClicked();
                });
            }
            else if (i < digitCount)
            {
                bubbleButtons[i].onClick.AddListener(() => {
                    Debug.Log($"[SoapBubbleTaskManager2] Clicked wrong button {capturedIdx} ({placeValueNames[capturedIdx]})");
                    WrongBubbleClicked();
                });
            }

            bubbleButtons[i].interactable = (i < digitCount);
        }

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
            EndRound(false);
        }
    }

    private void CorrectBubbleClicked()
    {
        Debug.Log($"[SoapBubbleTaskManager2] CorrectBubbleClicked() called with currentTargetIndex={currentTargetIndex}");
        if (!waitingForSelection) return;
        waitingForSelection = false;
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        // Animation removed
        
        // FIXED: Make sure this is always called
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSoapTaskResult(true);
            Debug.Log("[SoapBubbleTaskManager2] Registered SUCCESS with GameManager");
        }
        
        EndRound(true); 
    }

    private void WrongBubbleClicked()
    {
        Debug.Log("[SoapBubbleTaskManager2] WrongBubbleClicked() called");
        if (!waitingForSelection) return;
        waitingForSelection = false;
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        int idx = GetClickedBubbleIndex();
        PlayPopAnimation(idx);

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterSoapTaskResult(false);

        EndRound(false); // No delay needed
    }

    // Helper to play pop animation on a bubble
    private void PlayPopAnimation(int bubbleIdx)
    {
        // Animation removed: do nothing
    }

    // Helper to get the index of the clicked bubble (for wrong answers)
    private int GetClickedBubbleIndex()
    {
        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            if (bubbleButtons[i].interactable == false)
                return i;
        }
        return -1;
    }

    private void EndRound(bool success)
    {
        Debug.Log($"[SoapBubbleTaskManager2] EndRound({success}) called, completedTasks={completedTasks}");
        
        foreach (var btn in bubbleButtons)
        {
            btn.interactable = false;
            btn.onClick.RemoveAllListeners();
        }

        completedTasks++;

        // Save to session manager if in test mode
        if (GameSessionManager.Instance != null && GameSessionManager.Instance.isTestMode)
        {
            string difficulty = "medium"; // SoapScene2 = medium
            
            // Create units answer for the displayed number
            var unitsAnswer = new AnswerModel.IdentifyUnitsAnswers.UnitsAnswer
            {
                units = displayedNumber % 10,
                tens = (displayedNumber / 10) % 10,
                hundreds = (displayedNumber / 100) % 10,
                thousands = (displayedNumber / 1000) % 10
            };
            
            GameSessionManager.Instance.RegisterTaskResult(
                "identifyunits", 
                difficulty, 
                unitsAnswer, 
                success, 
                1, 
                success ? 100 : 0
            );
            
            if (success)
            {
                GameSessionManager.Instance.UpdateTotalScore(100);
            }
        }

        if (completedTasks < totalTasks)
        {
            Debug.Log($"[SoapBubbleTaskManager2] Starting next task ({completedTasks+1}/{totalTasks})");
            StartCoroutine(StartNextTask());
        }
        else
        {
            Debug.Log("[SoapBubbleTaskManager2] All tasks completed");
            // FIXED: Only clear prompt when all tasks are finished
            if (promptText != null)
                promptText.text = success ? "Bravo, tu as terminé !" : "Task finished!";
            if (timerText != null)
                timerText.gameObject.SetActive(false);

            // FIXED: Check if in free roam mode and return to appropriate scene
            string returnScene = FreeRoamManager.GetReturnScene();
            Debug.Log($"[SoapBubbleTaskManager2] Returning to: {returnScene} (Free roam active: {FreeRoamManager.IsFreeRoamActive})");

            // Don't register scene loaded handler if returning to title scene
            if (returnScene == "overworldScene")
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnoverworldSceneLoaded;
            }
            
            StartCoroutine(ReturnToSceneAfterDelay(2.5f, returnScene));
        }
    }

    private IEnumerator ReturnToSceneAfterDelay(float delay, string sceneName)
    {
        yield return new WaitForSeconds(delay);
        
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.TransitionToScene(sceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    private void OnoverworldSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == overworldSceneName)
        {
            // FIXED: Re-add bubble spawning trigger
            if (GameManager.Instance != null)
                GameManager.Instance.TrySpawnBubbleSoapChaser();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnoverworldSceneLoaded;
        }
    }
}
