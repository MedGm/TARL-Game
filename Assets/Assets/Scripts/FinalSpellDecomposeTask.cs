using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

public class FinalSpellDecomposeTask : MonoBehaviour
{
    [Header("Book UI")]
    public Image bookBackgroundImage;
    public TMP_Text bookNumbersText;
    public Color highlightColor = Color.green;
    public int numbersInBook = 10;

    [Header("Result Display")]
    public TMP_Text resultText;    // Renamed from hintText - will show the decomposed number

    [Header("Timer System")]
    public TMP_Text timerText;    
    public float taskTimeLimit = 120f; // 2 minutes time limit
    private float remainingTime;
    private bool isTimerRunning = false;

    [Header("Puzzle UI")]
    public Transform digitContainer;      // Contains the draggable digit buttons
    public Transform placeValueContainer; // Contains the place value drop zones
    public Button submitButton;
    public Button nextButton;             // To proceed to final scene
    public TMP_Text feedbackText;         // Shows success/failure messages

    [Header("Prefabs")]
    public GameObject digitButtonPrefab;  // Draggable digit
    public GameObject dropZonePrefab;     // Place value zone

    [Header("Settings")]
    public string finalSceneName = "FinalScene";
    public string[] placeValueLabels = { "1000", "100", "10", "1" }; // Thousands, hundreds, tens, ones

    [Header("Multi-Level Configuration")]
    public int[] difficultyRanges = { 2000, 5000, 9999 }; // Easy: 0-2000, Medium: 2000-5000, Hard: 5000-9999
    public string[] difficultyNames = { "easy", "medium", "hard" };
    
    private int currentDifficultyIndex = 0;
    private int completedLevels = 0;
    private List<DecompositionResult> results = new List<DecompositionResult>();

    [Serializable]
    public class DecompositionResult
    {
        public string difficulty;
        public int number;
        public List<int> playerAnswer;
        public bool isCorrect;
        public int attemptsUsed;
        public float timeSpent;
    }

    private float levelStartTime;
    private int currentAttempts = 0;

    private string originalNumber = "";
    private Dictionary<int, string> placedDigits = new Dictionary<int, string>(); // maps zone index to placed digit
    private bool puzzleSolved = false;

    private void Start()
    {
        // Start with the first difficulty level
        currentDifficultyIndex = 0;
        completedLevels = 0;
        results.Clear();
        
        SetupCurrentLevel();
        
        // Configure UI buttons
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(CheckAnswer);
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() => {
                UnityEngine.SceneManagement.SceneManager.LoadScene(finalSceneName);
            });
            nextButton.gameObject.SetActive(false); // Hidden until puzzle is solved
        }
        
        if (feedbackText != null)
        {
            feedbackText.text = "Décompose le nombre en vert en plaçant chaque chiffre à sa valeur correcte.";
        }

        // Hide result text initially
        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
        }

        // Initialize and start task timer
        remainingTime = taskTimeLimit;
        isTimerRunning = true;
        UpdateTimerDisplay();
    }

    private void Update()
    {
        // Update task timer
        if (isTimerRunning && !puzzleSolved)
        {
            remainingTime -= Time.deltaTime;
            UpdateTimerDisplay();
            
            // Check if time has expired
            if (remainingTime <= 0)
            {
                isTimerRunning = false;
                HandleTimeExpired();
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            
            // Change color as time gets low
            if (remainingTime <= 30)
                timerText.color = Color.red;
            else if (remainingTime <= 60)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.white;
        }
    }

    private void HandleTimeExpired()
    {
        Debug.Log($"[FinalSpellDecomposeTask] Time expired for {difficultyNames[currentDifficultyIndex]} level!");
        
        // Save current level as failed
        SaveCurrentLevelResult(false);
        
        // Show appropriate message
        if (feedbackText != null)
        {
            if (completedLevels > 0)
            {
                feedbackText.text = $"Temps écoulé ! Tu as complété {completedLevels} niveau(x). Le magicien ouvre le portail !";
            }
            else
            {
                feedbackText.text = "Le magicien s'est lassé et a ouvert le portail pour vous !";
            }
        }
        
        // End the task
        EndTask();
    }

    private void SetupCurrentLevel()
    {
        levelStartTime = Time.time;
        currentAttempts = 0;
        
        // Get difficulty configuration if in test mode
        if (GameSessionManager.Instance != null && GameSessionManager.Instance.isTestMode)
        {
            var config = GameSessionManager.Instance.GetFindCompositionConfig(difficultyNames[currentDifficultyIndex]);
            if (config != null)
            {
                // Use configured number and time limit
                originalNumber = config.number.ToString();
                taskTimeLimit = config.time;
                
                Debug.Log($"[FinalSpellDecomposeTask] Using test config for {difficultyNames[currentDifficultyIndex]}: number={config.number}, time={config.time}s");
            }
            else
            {
                GenerateNumberForDifficulty();
            }
        }
        else
        {
            GenerateNumberForDifficulty();
        }

        SetupBook();
        SetupDecompositionTask();
        
        // Reset timer
        remainingTime = taskTimeLimit;
        isTimerRunning = true;
        puzzleSolved = false;
        
        // Update UI
        if (feedbackText != null)
        {
            feedbackText.text = $"Niveau {difficultyNames[currentDifficultyIndex].ToUpper()}: Décompose le nombre en vert en plaçant chaque chiffre à sa valeur correcte.";
        }
        
        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
        }
        
        if (submitButton != null)
        {
            submitButton.interactable = true;
        }
        
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }

        UpdateTimerDisplay();
    }

    private void GenerateNumberForDifficulty()
    {
        System.Random rng = new System.Random();
        int minRange = currentDifficultyIndex == 0 ? 1000 : difficultyRanges[currentDifficultyIndex - 1];
        int maxRange = difficultyRanges[currentDifficultyIndex];
        
        int number = rng.Next(minRange, maxRange + 1);
        originalNumber = number.ToString();
        
        Debug.Log($"[FinalSpellDecomposeTask] Generated {difficultyNames[currentDifficultyIndex]} number: {originalNumber} (range: {minRange}-{maxRange})");
    }

    private void SetupBook()
    {
        // Generate random numbers for the book
        List<string> bookNumbers = new List<string>();
        
        // We want a 4-digit number for the puzzle
        System.Random rng = new System.Random();
        originalNumber = rng.Next(1000, 9999).ToString();
        
        // Create book content with one highlighted number
        int highlightIdx = rng.Next(numbersInBook);
        for (int i = 0; i < numbersInBook; i++)
        {
            if (i == highlightIdx)
            {
                // Highlight the original number in green
                bookNumbers.Add($"<color=#{ColorUtility.ToHtmlStringRGB(highlightColor)}>{originalNumber}</color>");
            }
            else
            {
                bookNumbers.Add(rng.Next(1000, 9999).ToString());
            }
        }
        
        // Update the book text display
        if (bookNumbersText != null)
        {
            bookNumbersText.text = string.Join("   ", bookNumbers);
        }
    }

    private void SetupDecompositionTask()
    {
        // Clear any existing UI elements
        foreach (Transform child in digitContainer) 
            Destroy(child.gameObject);
        
        foreach (Transform child in placeValueContainer) 
            Destroy(child.gameObject);
        
        // Create place value drop zones (thousands, hundreds, tens, ones)
        for (int i = 0; i < 4; i++)
        {
            GameObject dropZone = Instantiate(dropZonePrefab, placeValueContainer);
            
            // Make sure drop zone has rect transform with good size
            RectTransform rt = dropZone.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(100, 100); // Adjust size as needed
            }
            
            // Add or get component
            PlaceValueDropZone zone = dropZone.GetComponent<PlaceValueDropZone>();
            if (zone == null)
                zone = dropZone.AddComponent<PlaceValueDropZone>();
                
            zone.Index = i;
            zone.Task = this;
            
            // Set label text (1000, 100, 10, 1)
            TMP_Text label = dropZone.GetComponentInChildren<TMP_Text>();
            if (label != null && i < placeValueLabels.Length)
            {
                label.text = placeValueLabels[i];
            }
        }
        
        // Create draggable digit buttons for each digit in the original number
        for (int i = 0; i < originalNumber.Length; i++)
        {
            GameObject digitButton = Instantiate(digitButtonPrefab, digitContainer);
            
            // Set the digit text
            TMP_Text digitText = digitButton.GetComponentInChildren<TMP_Text>();
            if (digitText != null)
            {
                digitText.text = originalNumber[i].ToString();
            }
            
            // Add drag handler
            DigitDragHandler dragHandler = digitButton.AddComponent<DigitDragHandler>();
            dragHandler.Digit = originalNumber[i].ToString();
            dragHandler.OriginParent = digitContainer;
        }
    }

    public void RegisterDigitPlacement(int dropZoneIndex, string digit)
    {
        placedDigits[dropZoneIndex] = digit;
        Debug.Log($"Digit {digit} placed at position {dropZoneIndex} ({placeValueLabels[dropZoneIndex]})");
    }

    public void UnregisterDigitPlacement(int dropZoneIndex)
    {
        if (placedDigits.ContainsKey(dropZoneIndex))
        {
            placedDigits.Remove(dropZoneIndex);
        }
    }

    private void CheckAnswer()
    {
        currentAttempts++;
        
        // First check if all zones have been filled
        if (placedDigits.Count < 4)
        {
            if (feedbackText != null)
                feedbackText.text = "Place tous les chiffres dans les zones.";
            
            Debug.Log($"[FinalSpellDecomposeTask] Not all zones filled. Current count: {placedDigits.Count}/4");
            return;
        }

        // Debug the expected answer and user's decomposition
        Debug.Log($"[FinalSpellDecomposeTask] Original number: {originalNumber}");
        
        // Log what was placed in each zone
        string placedValues = "";
        for (int i = 0; i < 4; i++)
        {
            string digit = placedDigits.TryGetValue(i, out string d) ? d : "?";
            placedValues += $"Zone {i} ({placeValueLabels[i]}): {digit}, ";
        }
        Debug.Log($"[FinalSpellDecomposeTask] Placed values: {placedValues}");

        // Check if each digit is placed in the correct place value position
        bool correct = ValidatePlayerAnswer();

        // Display the result
        if (correct)
        {
            // Save successful result
            SaveCurrentLevelResult(true);
            
            // Check if we have more levels
            if (currentDifficultyIndex < difficultyNames.Length - 1)
            {
                // Move to next difficulty
                currentDifficultyIndex++;
                completedLevels++;
                
                if (feedbackText != null)
                    feedbackText.text = $"Bravo ! Niveau {difficultyNames[currentDifficultyIndex - 1]} complété ! Passons au niveau {difficultyNames[currentDifficultyIndex]}...";
                
                // Start next level after a delay
                StartCoroutine(StartNextLevelAfterDelay(2f));
            }
            else
            {
                // All levels completed
                completedLevels++;
                SaveCurrentLevelResult(true);
                
                if (feedbackText != null)
                    feedbackText.text = "Félicitations ! Tu as complété tous les niveaux !";
                
                EndTask();
            }
        }
        else
        {
            // Get attempts allowed from test config
            int maxAttempts = 3; // Default
            if (GameSessionManager.Instance != null && GameSessionManager.Instance.isTestMode)
            {
                var config = GameSessionManager.Instance.GetFindCompositionConfig(difficultyNames[currentDifficultyIndex]);
                if (config != null)
                {
                    maxAttempts = config.attemptsAllowed;
                }
            }
            
            if (currentAttempts >= maxAttempts)
            {
                // Failed this level
                SaveCurrentLevelResult(false);
                
                if (feedbackText != null)
                    feedbackText.text = $"Tentatives épuisées pour le niveau {difficultyNames[currentDifficultyIndex]}. Le magicien ouvre le portail !";
                
                EndTask();
            }
            else
            {
                if (feedbackText != null)
                    feedbackText.text = $"Ce n'est pas la bonne décomposition. Essaie encore. ({currentAttempts}/{maxAttempts} tentatives)";
            }
        }
    }

    private bool ValidatePlayerAnswer()
    {
        // Check if all digits are placed correctly
        if (placedDigits.Count != originalNumber.Length)
            return false;
            
        for (int i = 0; i < originalNumber.Length; i++)
        {
            if (!placedDigits.ContainsKey(i))
                return false;
                
            if (placedDigits[i] != originalNumber[i].ToString())
                return false;
        }
        
        return true;
    }

    private IEnumerator StartNextLevelAfterDelay(float delay)
    {
        // Disable interactions during transition
        if (submitButton != null)
            submitButton.interactable = false;
        
        yield return new WaitForSeconds(delay);
        
        SetupCurrentLevel();
    }

    private void SaveCurrentLevelResult(bool isCorrect)
    {
        float timeSpent = Time.time - levelStartTime;
        
        // Create player answer from placed digits
        List<int> playerAnswer = new List<int>();
        for (int i = 0; i < originalNumber.Length; i++)
        {
            if (placedDigits.TryGetValue(i, out string digit))
            {
                if (int.TryParse(digit, out int digitValue))
                {
                    // Store place value decomposition (digit * place value)
                    int placeValue = (int)Mathf.Pow(10, originalNumber.Length - 1 - i);
                    playerAnswer.Add(digitValue * placeValue);
                }
            }
        }
        
        var result = new DecompositionResult
        {
            difficulty = difficultyNames[currentDifficultyIndex],
            number = int.Parse(originalNumber),
            playerAnswer = playerAnswer,
            isCorrect = isCorrect,
            attemptsUsed = currentAttempts,
            timeSpent = timeSpent
        };
        
        results.Add(result);
        
        // Save to session manager if in test mode
        if (GameSessionManager.Instance != null && GameSessionManager.Instance.isTestMode)
        {
            int score = isCorrect ? 100 : 0; // You can customize scoring logic
            GameSessionManager.Instance.RegisterTaskResult(
                "findcomposition", 
                difficultyNames[currentDifficultyIndex], 
                playerAnswer, 
                isCorrect, 
                currentAttempts, 
                score
            );
            
            if (isCorrect)
            {
                GameSessionManager.Instance.UpdateTotalScore(score);
            }
        }
        
        Debug.Log($"[FinalSpellDecomposeTask] Saved result for {difficultyNames[currentDifficultyIndex]}: {isCorrect}, attempts: {currentAttempts}, time: {timeSpent:F1}s");
    }

    private void EndTask()
    {
        // Stop the timer
        isTimerRunning = false;
        puzzleSolved = true;
        
        // Disable submit button
        if (submitButton != null)
            submitButton.interactable = false;
        
        // Show next button to proceed
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
        
        // Show final decomposition result
        ShowDecompositionResult();
        
        // ONLY SAVE HERE: Complete the test and save to Firebase only at the very end
        if (GameSessionManager.Instance != null && GameSessionManager.Instance.isTestMode)
        {
            Debug.Log("[FinalSpellDecomposeTask] Final task completed, saving ALL results to Firebase");
            GameSessionManager.Instance.CompleteTest();
        }
        
        // Log final results
        Debug.Log($"[FinalSpellDecomposeTask] Task completed. Levels completed: {completedLevels}/{difficultyNames.Length}");
        foreach (var result in results)
        {
            Debug.Log($"[FinalSpellDecomposeTask] {result.difficulty}: {result.isCorrect} in {result.attemptsUsed} attempts, {result.timeSpent:F1}s");
        }

        // FIXED: Check if in free roam mode
        if (FreeRoamManager.IsFreeRoamActive)
        {
            // In free roam mode, return to title scene
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => {
                    Debug.Log("[FinalSpellDecomposeTask] Free roam mode - returning to TitleScene");
                    
                    if (SceneTransition.Instance != null)
                    {
                        SceneTransition.Instance.TransitionToScene("TitleScene");
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
                    }
                });
            }
        }
        else
        {
            // Normal mode - return to final scene
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(finalSceneName);
                });
            }
        }
    }

    // NEW: Shows the decomposition result as a sum (e.g., 4000+500+30+2)
    private void ShowDecompositionResult()
    {
        if (resultText != null)
        {
            // Build decomposition string
            string decomposition = "";
            
            for (int i = 0; i < originalNumber.Length; i++)
            {
                char digit = originalNumber[i];
                if (digit != '0') // Skip zeros for cleaner presentation
                {
                    // Add plus sign between terms
                    if (decomposition.Length > 0)
                        decomposition += " + ";
                        
                    // Add the decomposed value (e.g., 4000, 500, 30, 2)
                    int placeValue = (int)Mathf.Pow(10, originalNumber.Length - i - 1);
                    decomposition += ((int)(digit - '0') * placeValue).ToString();
                }
            }
            
            // Special case for number with all zeros
            if (decomposition.Length == 0)
                decomposition = "0";
                
            resultText.text = "Decomposition: " + decomposition;
            resultText.gameObject.SetActive(true);
        }
    }
}

// Improved drag handler with better drop detection
public class DigitDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string Digit;
    public Transform OriginParent;
    
    private Vector3 startPosition;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private GraphicRaycaster raycaster;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Find the canvas more reliably
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
        
        // Get or add CanvasGroup component
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Get GraphicRaycaster from canvas
        if (canvas != null)
            raycaster = canvas.GetComponent<GraphicRaycaster>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Save the original position
        startPosition = transform.position;
        
        // Disable raycast blocking so the drop zone can be detected underneath
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.6f; // Slightly transparent to indicate dragging
        }
        
        // Move to the root canvas temporarily for dragging
        if (canvas != null)
        {
            transform.SetParent(canvas.transform);
            transform.SetAsLastSibling(); // Bring to front
        }
        
        // Move to the cursor position immediately
        UpdatePosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Update position to follow cursor
        UpdatePosition(eventData);
    }

    private void UpdatePosition(PointerEventData eventData)
    {
        // Direct position mapping for better visual feedback
        if (rectTransform != null && canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera,
                out mousePos);
            rectTransform.localPosition = mousePos;
        }
        else
        {
            // Fallback for world space canvas
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore appearance
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1.0f;
        }

        // Check for drop zones under the pointer
        PlaceValueDropZone dropZone = FindDropZoneUnderPointer(eventData);
        
        // If no drop zone found, return to origin
        if (dropZone == null && OriginParent != null)
        {
            transform.SetParent(OriginParent);
            transform.localPosition = Vector3.zero;
            Debug.Log("[DigitDragHandler] No drop zone found, returning to origin");
        }
        // Drop zone handling is now taken care of by the IDropHandler on the drop zone
    }

    // Helper method to find drop zones manually 
    private PlaceValueDropZone FindDropZoneUnderPointer(PointerEventData eventData)
    {
        // First try the standard event system results
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (var result in results)
        {
            PlaceValueDropZone dropZone = result.gameObject.GetComponent<PlaceValueDropZone>();
            if (dropZone != null)
            {
                Debug.Log("[DigitDragHandler] Drop zone found with raycast: " + dropZone.gameObject.name);
                return dropZone;
            }
        }
        
        // If that fails, try Physics2D raycasts as a backup
        RaycastHit2D[] hits = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(eventData.position), Vector2.zero);
        foreach (var hit in hits)
        {
            if (hit.collider != null)
            {
                PlaceValueDropZone dropZone = hit.collider.GetComponent<PlaceValueDropZone>();
                if (dropZone != null)
                {
                    Debug.Log("[DigitDragHandler] Drop zone found with Physics2D raycast: " + dropZone.gameObject.name);
                    return dropZone;
                }
            }
        }
        
        return null;
    }
}

// Improved drop zone handling
public class PlaceValueDropZone : MonoBehaviour, IDropHandler
{
    public int Index { get; set; }
    public FinalSpellDecomposeTask Task { get; set; }
    private Image backgroundImage;
    
    private void Awake()
    {
        // Get or add required components
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
            backgroundImage = gameObject.AddComponent<Image>();
        
        // Make sure the image allows raycasting but is mostly transparent
        backgroundImage.raycastTarget = true;
        
        // Optional - make background slightly visible to see drop zone
        Color c = backgroundImage.color;
        c.a = 0.2f;
        backgroundImage.color = c;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("[PlaceValueDropZone] OnDrop called on zone " + Index);
        
        if (eventData.pointerDrag != null)
        {
            // Get the digit handler from the dragged object
            DigitDragHandler digitHandler = eventData.pointerDrag.GetComponent<DigitDragHandler>();
            if (digitHandler != null && Task != null)
            {
                // First check if there's already a digit here and return it to its origin
                foreach (Transform child in transform)
                {
                    DigitDragHandler existingDigit = child.GetComponent<DigitDragHandler>();
                    if (existingDigit != null)
                    {
                        Debug.Log("[PlaceValueDropZone] Found existing digit in zone " + Index + ", returning to origin");
                        if (existingDigit.OriginParent != null)
                        {
                            existingDigit.transform.SetParent(existingDigit.OriginParent);
                            existingDigit.transform.localPosition = Vector3.zero;
                            Task.UnregisterDigitPlacement(Index);
                        }
                        break;
                    }
                }
                
                // Snap the dropped digit to this zone
                Debug.Log("[PlaceValueDropZone] Placing digit " + digitHandler.Digit + " in zone " + Index);
                eventData.pointerDrag.transform.SetParent(transform);
                eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
                
                // Register with the task
                Task.RegisterDigitPlacement(Index, digitHandler.Digit);
                
                // Highlight zone to show it's filled (optional)
                if (backgroundImage != null)
                {
                    Color c = backgroundImage.color;
                    c.a = 0.4f;
                    backgroundImage.color = c;
                }
            }
        }
    }
}
