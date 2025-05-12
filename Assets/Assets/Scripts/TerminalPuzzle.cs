using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerminalPuzzle : MonoBehaviour
{
    [Header("Terminal Setup")]
    [SerializeField] private int numberToConvert = 1823;
    [SerializeField] private Image terminalBackground;
    [SerializeField] private TMP_Text displayNumberText;
    [SerializeField] private TMP_Text instructionText;
    
    [Header("Word Selection UI")]
    [SerializeField] private Transform wordButtonsContainer; // Parent container for word buttons
    [SerializeField] private Transform answerContainer; // Where selected words appear
    [SerializeField] private Button wordButtonPrefab; // Button prefab for words
    [SerializeField] private Button submitButton; // Button to submit answer
    [SerializeField] private Button clearButton; // Button to clear answer
    [SerializeField] private int decoyWordCount = 3; // Number of extra decoy words
    [SerializeField] private float buttonSpacing = 10f; // Space between buttons
    
    [Header("Timer & Attempts")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private float timeLimit = 30f;
    [SerializeField] private int maxAttempts = 3;
    
    [Header("Terminal Messages")]
    [SerializeField] private string instructionMessage = "Formez le nombre en mots en sélectionnant les mots dans le bon ordre.";
    [SerializeField] private string successMessage = "Correct! Terminal déverrouillé.";
    [SerializeField] private string failureMessage = "Incorrect. Essayez encore.";
    [SerializeField] private string timeoutMessage = "Temps écoulé!";
    [SerializeField] private string allAttemptsUsedMessage = "Tentatives épuisées. Terminal déverrouillé par défaut.";
    
    [Header("Settings")]
    [SerializeField] private float terminalDisappearDelay = 2.0f;
    [SerializeField] private bool generateRandomNumber = true;
    [SerializeField] private int minRandomNumber = 1;
    [SerializeField] private int maxRandomNumber = 9999;
    [SerializeField] private bool useFrench = true;
    [SerializeField] private Canvas terminalCanvas; // Reference to the parent canvas

    // The correct answer
    private string correctAnswer = "";
    private List<string> correctWordSequence = new List<string>();
    private List<string> selectedWords = new List<string>();
    private List<Button> wordButtons = new List<Button>();
    private List<Button> answerButtons = new List<Button>();
    
    private bool puzzleSolved = false;
    private float currentTime;
    private int remainingAttempts;
    private bool timerActive = false;
    private Coroutine timerCoroutine;
    
    private string[] decoyWords = {
        "zéro", "vingt", "trente", "quarante", "cinquante", "soixante", "quatre-vingt", 
        "onze", "douze", "treize", "quatorze", "quinze", "seize", "million", "milliard"
    };
    
    private void Start()
    {
        // Find the parent canvas if not assigned
        if (terminalCanvas == null)
        {
            terminalCanvas = GetComponentInParent<Canvas>();
            if (terminalCanvas == null)
            {
                terminalCanvas = FindFirstObjectByType<Canvas>();
            }
        }
        
        // Initialize attempts
        remainingAttempts = maxAttempts;
        UpdateAttemptsText();
        
        // Set up buttons
        if (submitButton != null) submitButton.onClick.AddListener(ValidateAnswer);
        if (clearButton != null) clearButton.onClick.AddListener(ClearSelectedWords);
        
        // Generate a new puzzle
        GenerateNewPuzzle();
    }
    
    private void GenerateNewPuzzle()
    {
        // Generate a random number if specified
        if (generateRandomNumber)
        {
            numberToConvert = Random.Range(minRandomNumber, maxRandomNumber + 1);
        }
        
        // Display the number in the terminal
        if (displayNumberText != null)
        {
            displayNumberText.text = numberToConvert.ToString();
        }
        
        // Set instruction text
        if (instructionText != null)
        {
            instructionText.text = instructionMessage;
        }
        
        // Set the correct answer
        correctAnswer = useFrench ? 
            ConvertNumberToFrenchWords(numberToConvert) : 
            ConvertNumberToWords(numberToConvert);
        
        // Split into words
        correctWordSequence = new List<string>(correctAnswer.Split(new char[] {' ', '-'}, System.StringSplitOptions.RemoveEmptyEntries));
        Debug.Log("New puzzle generated. Answer: " + correctAnswer + " (Words: " + string.Join(", ", correctWordSequence) + ")");
        
        // Clear any previous selections
        ClearSelectedWords();
        ClearWordButtons();
        
        // Re-enable and reset control buttons
        ResetControlButtons();
        
        // Generate word buttons
        GenerateWordButtons();
        
        // Start the timer
        currentTime = timeLimit;
        UpdateTimerText();
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(StartTimer());
    }
    
    private void ResetControlButtons()
    {
        // Re-enable submit and clear buttons and ensure listeners are attached
        if (submitButton != null) 
        {
            submitButton.interactable = true;
            
            // Remove existing listeners to prevent duplicates
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(ValidateAnswer);
        }
        
        if (clearButton != null) 
        {
            clearButton.interactable = true;
            
            // Remove existing listeners to prevent duplicates
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(ClearSelectedWords);
        }
    }
    
    private void ClearWordButtons()
    {
        // Destroy all existing word buttons
        foreach (var button in wordButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        wordButtons.Clear();
        
        // Destroy all answer buttons
        foreach (var button in answerButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        answerButtons.Clear();
    }
    
    private void GenerateWordButtons()
    {
        if (wordButtonsContainer == null || wordButtonPrefab == null) return;
        
        // Create a combined list of words (correct + decoys)
        List<string> allWords = new List<string>(correctWordSequence);
        
        // Add decoy words
        System.Random rng = new System.Random();
        List<string> availableDecoys = new List<string>(decoyWords);
        
        // Shuffle decoy words
        for (int i = 0; i < availableDecoys.Count; i++)
        {
            int j = rng.Next(i, availableDecoys.Count);
            string temp = availableDecoys[i];
            availableDecoys[i] = availableDecoys[j];
            availableDecoys[j] = temp;
        }
        
        // Add some decoy words
        for (int i = 0; i < decoyWordCount && i < availableDecoys.Count; i++)
        {
            if (!allWords.Contains(availableDecoys[i]))
            {
                allWords.Add(availableDecoys[i]);
            }
        }
        
        // Shuffle all words
        for (int i = 0; i < allWords.Count; i++)
        {
            int j = rng.Next(i, allWords.Count);
            string temp = allWords[i];
            allWords[i] = allWords[j];
            allWords[j] = temp;
        }
        
        // Create buttons for each word
        foreach (string word in allWords)
        {
            Button newButton = Instantiate(wordButtonPrefab, wordButtonsContainer);
            
            // Set button text
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = word;
            }
            
            // Set click action
            string wordCapture = word; // Capture the word value for the lambda
            newButton.onClick.AddListener(() => OnWordButtonClicked(newButton, wordCapture));
            
            wordButtons.Add(newButton);
        }
    }
    
    private void OnWordButtonClicked(Button button, string word)
    {
        if (answerContainer == null || button == null) return;
        
        // Add the word to selected words
        selectedWords.Add(word);
        
        // Disable the original button
        button.interactable = false;
        
        // Create a button in the answer area
        Button answerButton = Instantiate(wordButtonPrefab, answerContainer);
        
        // Set button text
        TextMeshProUGUI buttonText = answerButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = word;
        }
        
        // Set click action to remove this word from selection
        int wordIndex = selectedWords.Count - 1;
        Button originalButton = button; // Capture the original button
        answerButton.onClick.AddListener(() => OnAnswerButtonClicked(answerButton, wordIndex, originalButton));
        
        answerButtons.Add(answerButton);
    }
    
    private void OnAnswerButtonClicked(Button answerButton, int wordIndex, Button originalButton)
    {
        if (wordIndex >= 0 && wordIndex < selectedWords.Count)
        {
            // Remove the word from selected
            selectedWords.RemoveAt(wordIndex);
            
            // Re-enable the original button
            if (originalButton != null)
            {
                originalButton.interactable = true;
            }
            
            // Destroy this button
            Destroy(answerButton.gameObject);
            answerButtons.Remove(answerButton);
            
            // Rearrange remaining answer buttons
            RearrangeAnswerButtons();
        }
    }
    
    private void RearrangeAnswerButtons()
    {
        // Reconnect each answer button with its new index
        for (int i = 0; i < answerButtons.Count; i++)
        {
            Button button = answerButtons[i];
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                int wordIndex = i;
                button.onClick.AddListener(() => {
                    if (wordIndex >= 0 && wordIndex < selectedWords.Count)
                    {
                        string word = selectedWords[wordIndex];
                        // Find the original button
                        Button originalButton = null;
                        foreach (Button wordButton in wordButtons)
                        {
                            TextMeshProUGUI buttonText = wordButton.GetComponentInChildren<TextMeshProUGUI>();
                            if (buttonText != null && buttonText.text == word)
                            {
                                originalButton = wordButton;
                                break;
                            }
                        }
                        OnAnswerButtonClicked(button, wordIndex, originalButton);
                    }
                });
            }
        }
    }
    
    private void ClearSelectedWords()
    {
        // Re-enable all word buttons
        foreach (Button button in wordButtons)
        {
            if (button != null)
            {
                button.interactable = true;
            }
        }
        
        // Destroy all answer buttons
        foreach (Button button in answerButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        
        // Clear lists
        selectedWords.Clear();
        answerButtons.Clear();
    }
    
    private void ValidateAnswer()
    {
        // If puzzle is already solved or no attempts left, do nothing
        if (puzzleSolved || remainingAttempts <= 0) return;
        
        // Stop the timer
        timerActive = false;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        // Check if selected words match the correct sequence
        bool isCorrect = CompareLists(selectedWords, correctWordSequence);
        
        if (isCorrect)
        {
            // Update UI for success
            if (instructionText != null)
            {
                instructionText.text = successMessage;
                instructionText.color = Color.green;
            }
            
            // Disable all buttons
            DisableAllButtons();
            
            // Mark puzzle as solved
            puzzleSolved = true;
            
            // Make terminal disappear after delay
            StartCoroutine(DisappearTerminal());
            
            Debug.Log("Terminal puzzle solved correctly!");
        }
        else
        {
            // Decrease attempts
            remainingAttempts--;
            UpdateAttemptsText();
            
            // Check if we're out of attempts
            if (remainingAttempts <= 0)
            {
                AllAttemptsUsed();
                return;
            }
            
            // Show failure message
            if (instructionText != null)
            {
                instructionText.text = failureMessage;
                instructionText.color = Color.red;
            }
            
            // Generate new puzzle after a delay
            StartCoroutine(RegeneratePuzzleAfterDelay(1.5f));
            
            Debug.Log("Incorrect answer. Expected: " + string.Join("-", correctWordSequence));
        }
    }
    
    private bool CompareLists(List<string> listA, List<string> listB)
    {
        if (listA.Count != listB.Count) return false;
        
        for (int i = 0; i < listA.Count; i++)
        {
            if (listA[i] != listB[i])
            {
                return false;
            }
        }
        
        return true;
    }
    
    private void DisableAllButtons()
    {
        // Disable word buttons
        foreach (Button button in wordButtons)
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }
        
        // Disable answer buttons
        foreach (Button button in answerButtons)
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }
        
        // Disable submit and clear buttons
        if (submitButton != null) submitButton.interactable = false;
        if (clearButton != null) clearButton.interactable = false;
    }
    
    private IEnumerator DisappearTerminal()
    {
        yield return new WaitForSeconds(terminalDisappearDelay);
        
        // Just deactivate the terminal canvas
        if (terminalCanvas != null)
        {
            terminalCanvas.gameObject.SetActive(false);
        }
        else
        {
            // Fallback to just disabling this GameObject
            gameObject.SetActive(false);
        }
    }
    
    private string ConvertNumberToFrenchWords(int number)
    {
        if (number == 0)
            return "zéro";
            
        string[] units = { "", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix", 
                           "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf" };
                           
        string[] tens = { "", "", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante-dix", "quatre-vingt", "quatre-vingt-dix" };
        
        string words = "";
        
        // Handle thousands (1000-9999)
        if ((number / 1000) > 0)
        {
            if (number / 1000 == 1)
                words += "mille ";
            else
                words += units[number / 1000] + " mille ";
                
            number %= 1000;
        }
        
        // Handle hundreds (100-999)
        if ((number / 100) > 0)
        {
            if (number / 100 == 1)
                words += "cent ";
            else
                words += units[number / 100] + " cent ";
                
            number %= 100;
        }
        
        // Handle tens and units (1-99)
        if (number > 0)
        {
            // Special cases for French numbers
            if (number < 20)
            {
                words += units[number];
            }
            else
            {
                int ten = number / 10;
                int unit = number % 10;
                
                // Special case for 71-79
                if (ten == 7)
                {
                    words += "soixante";
                    if (unit == 1)
                        words += "-et-onze";
                    else if (unit > 0)
                        words += "-" + units[10 + unit];
                    else
                        words += "-dix";
                }
                // Special case for 91-99
                else if (ten == 9)
                {
                    words += "quatre-vingt";
                    if (unit == 1)
                        words += "-onze";
                    else if (unit > 0)
                        words += "-" + units[10 + unit];
                    else
                        words += "-dix";
                }
                else
                {
                    words += tens[ten];
                    
                    // For numbers like 21, 31, 41, etc. we add "et-un" instead of just "-un"
                    if (unit == 1 && (ten == 2 || ten == 3 || ten == 4 || ten == 5 || ten == 6))
                    {
                        words += "-et-un";
                    }
                    else if (unit > 0)
                    {
                        words += "-" + units[unit];
                    }
                }
            }
        }
        
        return words.Trim();
    }
    
    private string ConvertNumberToWords(int number)
    {
        if (number == 0)
            return "zero";
        
        string words = "";
        
        // Handle thousands
        if ((number / 1000) > 0)
        {
            words += ConvertNumberToWords(number / 1000) + " thousand ";
            number %= 1000;
        }
        
        // Handle hundreds
        if ((number / 100) > 0)
        {
            words += ConvertNumberToWords(number / 100) + " hundred ";
            number %= 100;
        }
        
        // Handle tens and units
        if (number > 0)
        {
            // If there's already some text, add "and"
            if (words != "")
                words += "and ";
            
            string[] units = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
                               "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                               
            string[] tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
            
            if (number < 20)
            {
                words += units[number];
            }
            else
            {
                words += tens[number / 10];
                if ((number % 10) > 0)
                {
                    words += " " + units[number % 10];
                }
            }
        }
        
        return words.Trim();
    }
    
    private void UpdateAttemptsText()
    {
        if (attemptsText != null)
        {
            attemptsText.text = "Tentatives: " + remainingAttempts + "/" + maxAttempts;
        }
    }
    
    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            int seconds = Mathf.FloorToInt(currentTime);
            timerText.text = seconds.ToString() + "s";
            
            // Change color based on time remaining
            if (currentTime <= 10)
                timerText.color = Color.red;
            else if (currentTime <= 20)
                timerText.color = Color.yellow;
        }
    }
    
    private IEnumerator StartTimer()
    {
        timerActive = true;
        
        while (currentTime > 0 && timerActive)
        {
            yield return null; // Wait for the next frame
            
            currentTime -= Time.deltaTime;
            UpdateTimerText();
        }
        
        if (currentTime <= 0 && !puzzleSolved)
        {
            TimerExpired();
        }
    }
    
    private void TimerExpired()
    {
        timerActive = false;
        
        if (instructionText != null)
        {
            instructionText.text = timeoutMessage;
            instructionText.color = Color.red;
        }
        
        // Decrease attempts
        remainingAttempts--;
        UpdateAttemptsText();
        
        // Check if we're out of attempts
        if (remainingAttempts <= 0)
        {
            AllAttemptsUsed();
            return;
        }
        
        // Generate new puzzle after a delay
        StartCoroutine(RegeneratePuzzleAfterDelay(2.0f));
    }
    
    private IEnumerator RegeneratePuzzleAfterDelay(float delay)
    {
        // Disable all buttons during the delay
        DisableAllButtons();
        
        yield return new WaitForSeconds(delay);
        
        // Generate new puzzle (which will reset control buttons)
        GenerateNewPuzzle();
    }
    
    private void AllAttemptsUsed()
    {
        // Show message about default unlock
        if (instructionText != null)
        {
            instructionText.text = allAttemptsUsedMessage;
            instructionText.color = Color.yellow;
        }
        
        // Disable all buttons
        DisableAllButtons();
        
        // Mark puzzle as solved (even though it failed)
        puzzleSolved = true;
        
        // Make terminal disappear after delay
        StartCoroutine(DisappearTerminal());
    }
}
