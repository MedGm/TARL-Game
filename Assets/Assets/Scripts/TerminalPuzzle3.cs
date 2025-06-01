using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerminalPuzzle3 : MonoBehaviour
{
    [Header("Terminal Setup")]
    [SerializeField] private int[] numbersToSum = { 1920, 55 };
    [SerializeField] private Image terminalBackground;
    [SerializeField] private TMP_Text displayNumberText;
    [SerializeField] private TMP_Text instructionText;

    [Header("Word Selection UI")]
    [SerializeField] private Transform wordButtonsContainer;
    [SerializeField] private Transform answerContainer;
    [SerializeField] private Button wordButtonPrefab;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private int decoyWordCount = 3;

    [Header("Timer & Attempts")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private float timeLimit = 30f;
    [SerializeField] private int maxAttempts = 3;

    [Header("Terminal Messages")]
    [SerializeField] private string instructionMessage = "Écrivez la somme en toutes lettres en sélectionnant les mots dans le bon ordre.";
    [SerializeField] private string successMessage = "Bravo !";
    [SerializeField] private string failureMessage = "Incorrect. Essayez encore.";
    [SerializeField] private string timeoutMessage = "Temps écoulé !";
    [SerializeField] private string allAttemptsUsedMessage = "Tentatives épuisées. Terminal déverrouillé par défaut.";

    [Header("Settings")]
    [SerializeField] private float terminalDisappearDelay = 2.0f;
    [SerializeField] private bool useFrench = true;
    [SerializeField] private Canvas terminalCanvas;

    [Header("Post-Terminal UI")]
    [SerializeField] private Canvas keyAcquiredCanvas;
    [SerializeField] private Image keyBackgroundImage;
    [SerializeField] private Image keyImage;
    [SerializeField] private TMP_Text keyMessageText;
    [SerializeField] private Button returnButton;
    [SerializeField] private string previousSceneName = "overworldScene";

    private int sum;
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

    // --- New: Contextual problem data ---
    private struct ContextProblem
    {
        public string contextText; // The instruction/context
        public int answer;         // The correct number
    }

    // Add a list of context problems (expand as needed)
    private List<ContextProblem> contextProblems = new List<ContextProblem>
    {
        new ContextProblem {
            contextText = "Mohammed essaie d'ouvrir la porte et voit un code. Il lit le code de droite à gauche : il voit 5 en premier, 8 en dernier, et entre eux il voit 56. Peux-tu reconstituer ce nombre ?",
            answer = 5658
        },
        new ContextProblem {
            contextText = "Sur la boîte, il y a un code mystérieux. En le lisant à l'envers, tu vois 2, puis 1, puis 43. Quel est ce nombre ?",
            answer = 4312
        },
        new ContextProblem {
            contextText = "Le coffre affiche un code : 7 à gauche, 0 à droite, et 89 au milieu. Quel est ce nombre ?",
            answer = 7890
        }
        // Add more problems as desired
    };

    private ContextProblem currentProblem;

    private void Start()
    {
        if (terminalCanvas == null)
        {
            terminalCanvas = GetComponentInParent<Canvas>();
            if (terminalCanvas == null)
                terminalCanvas = FindFirstObjectByType<Canvas>();
        }

        remainingAttempts = maxAttempts;
        UpdateAttemptsText();

        if (submitButton != null) submitButton.onClick.AddListener(ValidateAnswer);
        if (clearButton != null) clearButton.onClick.AddListener(ClearSelectedWords);

        GenerateNewProblem();
    }

    private void GenerateNewProblem()
    {
        // Pick a random context problem
        System.Random rng = new System.Random();
        currentProblem = contextProblems[rng.Next(contextProblems.Count)];

        // Show the context as instruction
        if (instructionText != null)
        {
            instructionText.text = currentProblem.contextText;
            instructionText.color = Color.white;
        }

        // Prepare the answer as a sequence of words
        correctAnswer = useFrench ?
            ConvertNumberToFrenchWords(currentProblem.answer) :
            ConvertNumberToWords(currentProblem.answer);

        correctWordSequence = new List<string>(correctAnswer.Split(new char[] { ' ', '-' }, System.StringSplitOptions.RemoveEmptyEntries));

        ClearSelectedWords();
        ClearWordButtons();
        ResetControlButtons();
        GenerateWordButtons();

        currentTime = timeLimit;
        UpdateTimerText();

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(StartTimer());
    }

    private void ResetControlButtons()
    {
        if (submitButton != null)
        {
            submitButton.interactable = true;
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(ValidateAnswer);
        }
        if (clearButton != null)
        {
            clearButton.interactable = true;
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(ClearSelectedWords);
        }
    }

    private void ClearWordButtons()
    {
        foreach (var button in wordButtons)
            if (button != null)
                Destroy(button.gameObject);
        wordButtons.Clear();

        foreach (var button in answerButtons)
            if (button != null)
                Destroy(button.gameObject);
        answerButtons.Clear();
    }

    private void GenerateWordButtons()
    {
        if (wordButtonsContainer == null || wordButtonPrefab == null) return;

        List<string> allWords = new List<string>(correctWordSequence);
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

        // Add more decoys for higher difficulty
        int decoyCount = Mathf.Max(5, decoyWordCount);
        for (int i = 0; i < decoyCount && i < availableDecoys.Count; i++)
        {
            if (!allWords.Contains(availableDecoys[i]))
                allWords.Add(availableDecoys[i]);
        }

        // Shuffle all words
        for (int i = 0; i < allWords.Count; i++)
        {
            int j = rng.Next(i, allWords.Count);
            string temp = allWords[i];
            allWords[i] = allWords[j];
            allWords[j] = temp;
        }

        foreach (string word in allWords)
        {
            Button newButton = Instantiate(wordButtonPrefab, wordButtonsContainer);
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = word;
            string wordCapture = word;
            newButton.onClick.AddListener(() => OnWordButtonClicked(newButton, wordCapture));
            wordButtons.Add(newButton);
        }
    }

    private void OnWordButtonClicked(Button button, string word)
    {
        if (answerContainer == null || button == null) return;
        selectedWords.Add(word);
        button.interactable = false;
        Button answerButton = Instantiate(wordButtonPrefab, answerContainer);
        TextMeshProUGUI buttonText = answerButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
            buttonText.text = word;
        int wordIndex = selectedWords.Count - 1;
        Button originalButton = button;
        answerButton.onClick.AddListener(() => OnAnswerButtonClicked(answerButton, wordIndex, originalButton));
        answerButtons.Add(answerButton);
    }

    private void OnAnswerButtonClicked(Button answerButton, int wordIndex, Button originalButton)
    {
        if (wordIndex >= 0 && wordIndex < selectedWords.Count)
        {
            selectedWords.RemoveAt(wordIndex);
            if (originalButton != null)
                originalButton.interactable = true;
            Destroy(answerButton.gameObject);
            answerButtons.Remove(answerButton);
            RearrangeAnswerButtons();
        }
    }

    private void RearrangeAnswerButtons()
    {
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
        foreach (Button button in wordButtons)
            if (button != null)
                button.interactable = true;
        foreach (Button button in answerButtons)
            if (button != null)
                Destroy(button.gameObject);
        selectedWords.Clear();
        answerButtons.Clear();
    }

    private void ValidateAnswer()
    {
        if (puzzleSolved || remainingAttempts <= 0) return;
        timerActive = false;
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        bool isCorrect = CompareLists(selectedWords, correctWordSequence);

        if (isCorrect)
        {
            if (instructionText != null)
            {
                instructionText.text = successMessage;
                instructionText.color = Color.green;
            }
            DisableAllButtons();
            puzzleSolved = true;
            StartCoroutine(DisappearTerminal());
        }
        else
        {
            remainingAttempts--;
            UpdateAttemptsText();
            if (remainingAttempts <= 0)
            {
                AllAttemptsUsed();
                return;
            }
            if (instructionText != null)
            {
                instructionText.text = failureMessage;
                instructionText.color = Color.red;
            }
            StartCoroutine(RegeneratePuzzleAfterDelay(1.5f));
        }
    }

    private bool CompareLists(List<string> listA, List<string> listB)
    {
        if (listA.Count != listB.Count) return false;
        for (int i = 0; i < listA.Count; i++)
            if (listA[i] != listB[i])
                return false;
        return true;
    }

    private void DisableAllButtons()
    {
        foreach (Button button in wordButtons)
            if (button != null)
                button.interactable = false;
        foreach (Button button in answerButtons)
            if (button != null)
                button.interactable = false;
        if (submitButton != null) submitButton.interactable = false;
        if (clearButton != null) clearButton.interactable = false;
    }

    private IEnumerator DisappearTerminal()
    {
        yield return new WaitForSeconds(terminalDisappearDelay);
        if (terminalCanvas != null)
            terminalCanvas.gameObject.SetActive(false);
        else
            gameObject.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.MarkDungeonCompleted("dungeon3");
        ShowKeyAcquiredOverlay();
    }

    private void ShowKeyAcquiredOverlay()
    {
        if (keyAcquiredCanvas != null)
        {
            keyAcquiredCanvas.gameObject.SetActive(true);
            if (keyBackgroundImage != null)
                keyBackgroundImage.enabled = true;
            if (keyImage != null)
                keyImage.enabled = true;
            if (keyMessageText != null)
            {
                keyMessageText.text = "Nous avons obtenu la clé de ce donjon !";
                keyMessageText.fontSize = 64;
                keyMessageText.alignment = TextAlignmentOptions.Center;
                keyMessageText.color = new Color(1f, 0.85f, 0.2f);
                keyMessageText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            }
            if (returnButton != null)
            {
                returnButton.gameObject.SetActive(true);
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(ReturnToPreviousScene);
            }
        }
    }

    private void ReturnToPreviousScene()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnoverworldSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.LoadScene(previousSceneName);
    }

    private void OnoverworldSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == previousSceneName)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TrySpawnBubbleSoapChaser();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnoverworldSceneLoaded;
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

        if ((number / 1000) > 0)
        {
            if (number / 1000 == 1)
                words += "mille ";
            else
                words += units[number / 1000] + " mille ";

            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            if (number / 100 == 1)
                words += "cent ";
            else
                words += units[number / 100] + " cent ";

            number %= 100;
        }

        if (number > 0)
        {
            if (number < 20)
            {
                words += units[number];
            }
            else
            {
                int ten = number / 10;
                int unit = number % 10;

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

        if ((number / 1000) > 0)
        {
            words += ConvertNumberToWords(number / 1000) + " thousand ";
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words += ConvertNumberToWords(number / 100) + " hundred ";
            number %= 100;
        }

        if (number > 0)
        {
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
            attemptsText.text = "Tentatives: " + remainingAttempts + "/" + maxAttempts;
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            int seconds = Mathf.FloorToInt(currentTime);
            timerText.text = seconds.ToString() + "s";
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
            yield return null;
            currentTime -= Time.deltaTime;
            UpdateTimerText();
        }
        if (currentTime <= 0 && !puzzleSolved)
            TimerExpired();
    }

    private void TimerExpired()
    {
        timerActive = false;
        if (instructionText != null)
        {
            instructionText.text = timeoutMessage;
            instructionText.color = Color.red;
        }
        remainingAttempts--;
        UpdateAttemptsText();
        if (remainingAttempts <= 0)
        {
            AllAttemptsUsed();
            return;
        }
        StartCoroutine(RegeneratePuzzleAfterDelay(2.0f));
    }

    private IEnumerator RegeneratePuzzleAfterDelay(float delay)
    {
        DisableAllButtons();
        yield return new WaitForSeconds(delay);
        GenerateNewProblem();
    }

    private void AllAttemptsUsed()
    {
        if (instructionText != null)
        {
            instructionText.text = allAttemptsUsedMessage;
            instructionText.color = Color.yellow;
        }
        DisableAllButtons();
        puzzleSolved = true;
        StartCoroutine(DisappearTerminal());
    }
}