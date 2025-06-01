using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SoapBubbleTaskManager3 : MonoBehaviour
{
    public Button[] bubbleButtons; // Assign 7 buttons
    public TMP_Text[] bubbleTexts; // Assign 7 TMP_Texts
    public TMP_Text promptText;
    public TMP_Text timerText;

    public string[] placeValueNames = {
        "Ones", "Tens", "Hundreds", "Thousands", "TenThousands", "HundredThousands", "Millions"
    };
    public float promptDuration = 5f;
    public float selectionTime = 8f;
    public string overworldSceneName = "overworldScene";

    private int totalTasks = 1;
    private int completedTasks = 0;
    private int currentTargetIndex = -1;
    private float timer = 0f;
    private bool waitingForSelection = false;

    private int number1, number2;
    private int[] digits1;
    private int[] digits2;

    private void Start()
    {
        // --- Make the task harder: Always use exactly 4 digits (thousands) ---
        int digitCount = 4; // Fixed at 4 digits for the final challenge

        System.Random rng = new System.Random();
        
        // Create arrays to store individual digits
        digits1 = new int[digitCount];
        digits2 = new int[digitCount];
        
        // First, fill both arrays with random digits 0-9
        for (int i = 0; i < digitCount; i++)
        {
            digits1[i] = rng.Next(0, 10);
            digits2[i] = rng.Next(0, 10);
        }
        
        // Choose indices that will have matching digits
        int matchCount = rng.Next(1, 3); // 1-2 matches to maintain difficulty
        List<int> matchIndices = new List<int>();
        
        while (matchIndices.Count < matchCount && matchIndices.Count < digitCount)
        {
            int idx = rng.Next(0, digitCount);
            if (!matchIndices.Contains(idx))
                matchIndices.Add(idx);
        }
        
        // Set the same digit at the matching indices
        foreach (int idx in matchIndices)
        {
            int matchDigit = rng.Next(1, 10);
            digits1[idx] = matchDigit;
            digits2[idx] = matchDigit;
        }

        // Ensure first digits aren't zero (for proper 4-digit numbers)
        if (digits1[0] == 0) digits1[0] = rng.Next(1, 10);
        if (digits2[0] == 0) digits2[0] = rng.Next(1, 10);
        
        // Convert digit arrays to numbers
        number1 = 0;
        number2 = 0;
        for (int i = 0; i < digitCount; i++)
        {
            number1 = number1 * 10 + digits1[i];
            number2 = number2 * 10 + digits2[i];
        }

        // --- FIX: Correctly map digits to place values (reversed order) ---
        Debug.Log($"[SoapBubbleTaskManager3] Generated numbers: {number1} and {number2}");
        string placeValueDebug = "";
        for (int i = 0; i < digitCount; i++) {
            int placeValueIndex = digitCount - 1 - i;
            placeValueDebug += $"{placeValueIndex}:{placeValueNames[placeValueIndex]}={digits1[i]},{digits2[i]} ";
        }
        Debug.Log($"[SoapBubbleTaskManager3] Place values: {placeValueDebug}");

        // Set up bubble labels (place values)
        for (int i = 0; i < bubbleTexts.Length; i++)
        {
            if (i < digitCount)
            {
                bubbleButtons[i].gameObject.SetActive(true);
                if (bubbleTexts[i] != null)
                    bubbleTexts[i].text = placeValueNames[i];
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

        StartCoroutine(StartNextTask(digitCount));
    }

    private IEnumerator StartNextTask(int digitCount)
    {
        foreach (var btn in bubbleButtons)
            btn.interactable = false;

        if (promptText != null)
        {
            promptText.text = "Les bulles sont furieuses ! Pop la plus petite et la plus grande unité.";
            promptText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(promptDuration);

        int smallestIdx = 0;
        int biggestIdx = digitCount - 1;

        int popCount = 0;
        bool[] popped = new bool[bubbleButtons.Length];

        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            bubbleButtons[i].onClick.RemoveAllListeners();
            int capturedIdx = i;
            
            if ((i == smallestIdx || i == biggestIdx) && i < digitCount)
            {
                bubbleButtons[i].onClick.AddListener(() => {
                    if (!popped[capturedIdx])
                    {
                        Debug.Log($"[SoapBubbleTaskManager3] Popped bubble {capturedIdx} (smallest/biggest unit)");
                        popped[capturedIdx] = true;
                        bubbleButtons[capturedIdx].interactable = false;
                        popCount++;
                        if (popCount == 2)
                        {
                            StartCoroutine(SecondStep(digitCount));
                        }
                    }
                });
                bubbleButtons[i].interactable = true;
            }
            else if (i < digitCount)
            {
                bubbleButtons[i].onClick.AddListener(() => {
                    Debug.Log($"[SoapBubbleTaskManager3] Clicked wrong bubble {capturedIdx} in first step");
                    WrongBubbleClicked();                                   
                });
                bubbleButtons[i].interactable = true;
            }
            else
            {
                bubbleButtons[i].interactable = false;
            }
        }
    }

    private IEnumerator SecondStep(int digitCount)
    {
        foreach (var btn in bubbleButtons)
        {
            btn.onClick.RemoveAllListeners();
            btn.interactable = false;
        }

        yield return new WaitForSeconds(1f);

        if (promptText != null)
        {
            promptText.text = $"Bravo ! Les bulles t'ont donné deux nombres : <b>{number1}</b> et <b>{number2}</b>.\n" +
                $"Trouve l'unité où les deux nombres ont le même chiffre et pop la bulle correspondante.";
            promptText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(promptDuration);

        // --- FIXED MATCHING LOGIC: Match the correct place values ---
        List<int> correctIndices = new List<int>();
        for (int i = 0; i < digitCount; i++)
        {
            if (digits1[i] == digits2[i])
            {
                // The correct UI button index is (digitCount - 1 - i)
                // This maps the digit position to the correct place value
                int placeValueIndex = digitCount - 1 - i;
                correctIndices.Add(placeValueIndex);
                Debug.Log($"[SoapBubbleTaskManager3] Position {i} has matching digit {digits1[i]} → UI Button {placeValueIndex} ({placeValueNames[placeValueIndex]})");
            }
        }

        // Update prompt if multiple matching positions
        if (promptText != null && correctIndices.Count > 1)
        {
            promptText.text += $"\n<b>Attention :</b> Il y a plusieurs unités où les deux nombres ont le même chiffre. Pop <b>toutes</b> les bulles correspondantes.";
        }

        // Reset for final step
        waitingForSelection = true;
        HashSet<int> poppedCorrectIndices = new HashSet<int>();

        // Configure buttons for the final step - simplified approach
        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            // Only process active buttons
            if (i < digitCount)
            {
                int buttonIndex = i; // Capture for lambda
                
                // Set up the click handler
                bubbleButtons[i].onClick.RemoveAllListeners();
                bubbleButtons[i].onClick.AddListener(() => {
                    if (!waitingForSelection) return;
                    
                    Debug.Log($"[SoapBubbleTaskManager3] Clicked bubble {buttonIndex} ({placeValueNames[buttonIndex]})");
                    
                    // Check if this is a correct position
                    if (correctIndices.Contains(buttonIndex))
                    {
                        // Correct position clicked
                        bubbleButtons[buttonIndex].interactable = false;
                        poppedCorrectIndices.Add(buttonIndex);
                        Debug.Log($"[SoapBubbleTaskManager3] Correct position {buttonIndex}! Popped {poppedCorrectIndices.Count}/{correctIndices.Count}");
                        
                        // Check if all correct positions have been clicked
                        if (poppedCorrectIndices.Count == correctIndices.Count)
                        {
                            waitingForSelection = false;
                            if (timerText != null)
                                timerText.gameObject.SetActive(false);
                                
                            if (GameManager.Instance != null)
                                GameManager.Instance.RegisterSoapTaskResult(true);
                                
                            Debug.Log($"[SoapBubbleTaskManager3] Success! All {correctIndices.Count} matches found.");
                            EndRound(true);
                        }
                    }
                    else
                    {
                        // Wrong position clicked
                        waitingForSelection = false;
                        if (timerText != null)
                            timerText.gameObject.SetActive(false);
                            
                        if (GameManager.Instance != null)
                            GameManager.Instance.RegisterSoapTaskResult(false);
                            
                        // Include more helpful debug info
                        Debug.Log($"[SoapBubbleTaskManager3] Wrong position {buttonIndex} ({placeValueNames[buttonIndex]}) clicked! Needed {string.Join(",", correctIndices)} ({string.Join(",", correctIndices.Select(idx => placeValueNames[idx]))})");
                        EndRound(false);
                    }
                });
                
                bubbleButtons[i].interactable = true;
            }
        }

        // Start the timer
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timer = selectionTime;
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
        Debug.Log("Correct bubble clicked.");
        if (!waitingForSelection) return;
        waitingForSelection = false;
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        int idx = GetClickedBubbleIndex();
        PlayPopAnimation(idx);

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterSoapTaskResult(true);

        EndRound(true); // No delay needed
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

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterSoapTaskResult(false);

        EndRound(false); // No delay needed
    }

    private void PlayPopAnimation(int bubbleIdx)
    {
        // Animation removed: do nothing
    }

    // Helper to get the index of the clicked bubble (for correct/wrong answers)
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
        foreach (var btn in bubbleButtons)
        {
            btn.interactable = false;
            btn.onClick.RemoveAllListeners();
        }

        completedTasks++;

        if (promptText != null)
            promptText.text = success ? "Bravo, tu as réussi !" : "Raté !";

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnoverworldSceneLoaded;
        StartCoroutine(ReturnTooverworldSceneAfterDelay(2.5f));
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
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnoverworldSceneLoaded;
        }
    }
}
