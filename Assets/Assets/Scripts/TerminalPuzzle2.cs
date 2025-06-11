using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerminalPuzzle2 : MonoBehaviour
{
    [Header("Terminal Setup")]
    [SerializeField] private int numberToConvert = 1823;
    [SerializeField] private Image terminalBackground;
    [SerializeField] private TMP_Text displayNumberText;
    [SerializeField] private TMP_Text instructionText;

    [Header("Gap Fill UI")]
    [SerializeField] private Transform wordSelectionPanel; // Grid Layout Group for word options
    [SerializeField] private Transform answerPanel; // Where selected answers are placed
    [SerializeField] private Button wordButtonPrefab; // Prefab for word option button
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;

    [Header("Timer & Attempts")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private float timeLimit = 30f;
    [SerializeField] private int maxAttempts = 3;

    [Header("Terminal Messages")]
    [SerializeField] private string instructionMessage = "Complétez les mots manquants pour écrire le nombre en toutes lettres.";
    [SerializeField] private string successMessage = "Bravo !";
    [SerializeField] private string failureMessage = "Incorrect. Essayez encore.";
    [SerializeField] private string timeoutMessage = "Temps écoulé !";
    [SerializeField] private string allAttemptsUsedMessage = "Tentatives épuisées. Terminal déverrouillé par défaut.";

    [Header("Settings")]
    [SerializeField] private float terminalDisappearDelay = 2.0f;
    [SerializeField] private bool generateRandomNumber = true;
    [SerializeField] private int minRandomNumber = 1;
    [SerializeField] private int maxRandomNumber = 9999;
    [SerializeField] private bool useFrench = true;
    [SerializeField] private Canvas terminalCanvas;

    [Header("Post-Terminal UI")]
    [SerializeField] private Canvas keyAcquiredCanvas;
    [SerializeField] private Image keyBackgroundImage;
    [SerializeField] private Image keyImage;
    [SerializeField] private TMP_Text keyMessageText;
    [SerializeField] private Button returnButton;
    [SerializeField] private string previousSceneName = "overworldScene";

    private string correctAnswer = "";
    private List<string> correctWordSequence = new List<string>();
    private List<int> gapIndices = new List<int>();
    private List<string> selectedWords = new List<string>();
    private List<Button> wordButtons = new List<Button>();
    private List<Button> answerButtons = new List<Button>();

    private bool puzzleSolved = false;
    private float currentTime;
    private int remainingAttempts;
    private bool timerActive = false;
    private Coroutine timerCoroutine;

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
        if (clearButton != null) clearButton.onClick.AddListener(ClearGaps);

        GenerateNewPuzzle();
    }

    private void GenerateNewPuzzle()
    {
        // Check if in test mode and get configuration
        if (GameSessionManager.Instance != null && GameSessionManager.Instance.isTestMode)
        {
            string difficulty = "medium"; // Second dungeon = medium
            var config = GameSessionManager.Instance.GetWriteNumberConfig(difficulty);
            if (config != null)
            {
                numberToConvert = config.number;
                timeLimit = config.time;
                maxAttempts = config.attemptsAllowed;
                generateRandomNumber = false;
                Debug.Log($"[TerminalPuzzle2] Using test config: number={config.number}, time={config.time}, attempts={config.attemptsAllowed}");
            }
        }
        else if (generateRandomNumber)
        {
            numberToConvert = Random.Range(minRandomNumber, maxRandomNumber + 1);
        }

        if (displayNumberText != null)
            displayNumberText.text = numberToConvert.ToString();

        if (instructionText != null)
        {
            instructionText.text = instructionMessage;
            instructionText.color = Color.white;
        }

        correctAnswer = useFrench ?
            ConvertNumberToFrenchWords(numberToConvert) :
            ConvertNumberToWords(numberToConvert);

        correctWordSequence = new List<string>(correctAnswer.Split(new char[] { ' ', '-' }, System.StringSplitOptions.RemoveEmptyEntries));

        // Choose random gaps (e.g., 2 gaps)
        gapIndices.Clear();
        int gapCount = Mathf.Min(2, correctWordSequence.Count / 2);
        System.Random rng = new System.Random();
        while (gapIndices.Count < gapCount)
        {
            int idx = rng.Next(0, correctWordSequence.Count);
            if (!gapIndices.Contains(idx))
                gapIndices.Add(idx);
        }
        gapIndices.Sort();

        foreach (Transform child in answerPanel)
            Destroy(child.gameObject);
        answerButtons.Clear();
        selectedWords.Clear();

        for (int i = 0; i < correctWordSequence.Count; i++)
        {
            Button btn = Instantiate(wordButtonPrefab, answerPanel);
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (gapIndices.Contains(i))
            {
                txt.text = "___";
                btn.interactable = false;
                answerButtons.Add(btn);
                selectedWords.Add("");
            }
            else
            {
                txt.text = correctWordSequence[i];
                btn.interactable = false;
                answerButtons.Add(btn);
                selectedWords.Add(correctWordSequence[i]);
            }
        }

        foreach (Transform child in wordSelectionPanel)
            Destroy(child.gameObject);
        wordButtons.Clear();

        List<string> options = new List<string>();
        foreach (int idx in gapIndices)
            options.Add(correctWordSequence[idx]);
        string[] decoyWords = {
            "zéro", "vingt", "trente", "quarante", "cinquante", "soixante", "quatre-vingt",
            "onze", "douze", "treize", "quatorze", "quinze", "seize", "million", "milliard"
        };
        System.Random decoyRng = new System.Random();
        while (options.Count < gapIndices.Count + 3)
        {
            string decoy = decoyWords[decoyRng.Next(decoyWords.Length)];
            if (!options.Contains(decoy))
                options.Add(decoy);
        }
        for (int i = 0; i < options.Count; i++)
        {
            int j = decoyRng.Next(i, options.Count);
            string temp = options[i];
            options[i] = options[j];
            options[j] = temp;
        }
        foreach (string opt in options)
        {
            Button btn = Instantiate(wordButtonPrefab, wordSelectionPanel);
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = opt;
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnWordButtonClicked(btn, opt));
            wordButtons.Add(btn);
        }

        ResetControlButtons();

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
            clearButton.onClick.AddListener(ClearGaps);
        }
    }

    private void OnWordButtonClicked(Button btn, string word)
    {
        int slotIdx = -1;
        for (int i = 0; i < gapIndices.Count; i++)
        {
            int answerIdx = gapIndices[i];
            if (string.IsNullOrEmpty(selectedWords[answerIdx]))
            {
                slotIdx = answerIdx;
                break;
            }
        }
        if (slotIdx == -1) return;

        selectedWords[slotIdx] = word;
        TMP_Text txt = answerButtons[slotIdx].GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = word;
        btn.interactable = false;

        answerButtons[slotIdx].onClick.RemoveAllListeners();
        answerButtons[slotIdx].onClick.AddListener(() => OnAnswerButtonCleared(slotIdx, word));
        answerButtons[slotIdx].interactable = true;
    }

    private void OnAnswerButtonCleared(int slotIdx, string word)
    {
        if (!gapIndices.Contains(slotIdx)) return;

        foreach (var btn in wordButtons)
        {
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null && txt.text == word)
            {
                btn.interactable = true;
                break;
            }
        }
        selectedWords[slotIdx] = "";
        TMP_Text slotTxt = answerButtons[slotIdx].GetComponentInChildren<TMP_Text>();
        if (slotTxt != null) slotTxt.text = "___";
        answerButtons[slotIdx].interactable = false;
    }

    private void ClearGaps()
    {
        foreach (int idx in gapIndices)
        {
            if (!string.IsNullOrEmpty(selectedWords[idx]))
                OnAnswerButtonCleared(idx, selectedWords[idx]);
        }
    }

    private void ValidateAnswer()
    {
        if (puzzleSolved || remainingAttempts <= 0) return;

        timerActive = false;
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        bool isCorrect = true;
        foreach (int idx in gapIndices)
        {
            string expected = correctWordSequence[idx];
            string user = selectedWords[idx].Trim().ToLower();
            if (user != expected.ToLower())
            {
                isCorrect = false;
                break;
            }
        }

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

            // Save result to session manager if in test mode
            if (GameSessionManager.Instance != null && GameSessionManager.Instance.isTestMode)
            {
                List<string> userAnswer = new List<string>();
                foreach (int idx in gapIndices)
                {
                    userAnswer.Add(selectedWords[idx]);
                }

                GameSessionManager.Instance.RegisterTaskResult(
                    "writenumber",
                    "medium",
                    userAnswer,
                    true,
                    maxAttempts - remainingAttempts + 1,
                    100
                );
                GameSessionManager.Instance.UpdateTotalScore(100);
            }
        }
        else
        {
            remainingAttempts--;
            UpdateAttemptsText();

            if (instructionText != null)
            {
                instructionText.text = failureMessage;
                instructionText.color = Color.red;
            }

            if (remainingAttempts <= 0)
            {
                // Save failed result
                if (GameSessionManager.Instance != null && GameSessionManager.Instance.isTestMode)
                {
                    List<string> userAnswer = new List<string>();
                    foreach (int idx in gapIndices)
                    {
                        userAnswer.Add(selectedWords[idx]);
                    }

                    GameSessionManager.Instance.RegisterTaskResult(
                        "writenumber",
                        "medium",
                        userAnswer,
                        false,
                        maxAttempts,
                        0
                    );
                }

                AllAttemptsUsed();
                return;
            }

            StartCoroutine(RegeneratePuzzleAfterDelay(1.5f));
        }
    }

    private void DisableAllButtons()
    {
        foreach (var btn in wordButtons)
            btn.interactable = false;
        foreach (var ans in answerButtons)
            ans.interactable = false;
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

        // REMOVED: Don't mark dungeon as completed here anymore
        // if (GameManager.Instance != null)
        //     GameManager.Instance.MarkDungeonCompleted("dungeon2");

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
        // FIXED: Check if in free roam mode
        if (FreeRoamManager.IsFreeRoamActive)
        {
            Debug.Log("[TerminalPuzzle2] Free roam mode - returning to TitleScene");
            
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.TransitionToScene("TitleScene");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
            }
            return;
        }

        // Normal mode - mark dungeon as completed and return to overworld
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MarkDungeonCompleted("dungeon2");
        }

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.TransitionToScene(previousSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(previousSceneName);
        }

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnoverworldSceneLoaded;
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
        GenerateNewPuzzle();
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