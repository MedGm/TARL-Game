using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

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

    // State tracking 
    private string originalNumber = "";
    private Dictionary<int, string> placedDigits = new Dictionary<int, string>(); // maps zone index to placed digit
    private bool puzzleSolved = false;

    private void Start()
    {
        SetupBook();
        SetupDecompositionTask();
        
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
        Debug.Log("[FinalSpellDecomposeTask] Time expired! The wizard got bored and opened the portal.");
        
        // Show message to the player
        if (feedbackText != null)
        {
            feedbackText.text = "Le magicien s'est lassé et a ouvert le portail pour vous !";
        }
        
        // Disable submit button
        if (submitButton != null)
            submitButton.interactable = false;
        
        // Show next button to proceed
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
        
        // Show the decomposition result
        ShowDecompositionResult();
        
        // Mark puzzle as solved
        puzzleSolved = true;
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
        bool correct = true;
        for (int i = 0; i < originalNumber.Length; i++)
        {
            // Check if this place value has the correct digit
            if (!placedDigits.TryGetValue(i, out string placedDigit) || 
                placedDigit != originalNumber[i].ToString())
            {
                correct = false;
                Debug.Log($"[FinalSpellDecomposeTask] Mismatch at position {i}: expected {originalNumber[i]}, got {placedDigit ?? "nothing"}");
                break;
            }
        }

        // Display the result
        if (correct)
        {
            // Stop the timer
            isTimerRunning = false;

            // Success!
            puzzleSolved = true;
            
            Debug.Log("[FinalSpellDecomposeTask] CORRECT ANSWER! Puzzle solved.");
            
            if (feedbackText != null)
                feedbackText.text = "Bravo ! La décomposition est correcte !";
            
            // Disable submit button
            if (submitButton != null)
                submitButton.interactable = false;
                
            // Show next button to proceed
            if (nextButton != null)
                nextButton.gameObject.SetActive(true);
                
            // Show the decomposition result
            ShowDecompositionResult();
        }
        else
        {
            // Incorrect answer
            Debug.Log("[FinalSpellDecomposeTask] INCORRECT. Digits not placed in correct place values.");
            
            if (feedbackText != null)
                feedbackText.text = "Ce n'est pas la bonne décomposition. Essaie encore.";
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
