using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using ZXing;
using ZXing.QrCode;

public class QRCodeScanner : MonoBehaviour
{
    [Header("UI References")]
    public GameObject cameraQuad;           // Assign your Quad here in the Inspector
    public TMP_Text instructionText;
    public TMP_Text statusText;
    public Button startScanButton;
    public Button stopScanButton;
    public Button cancelButton;
    public GameObject scanningFrame;

    [Header("Camera Settings")]
    public int targetFrameRate = 30;
    public int requestedWidth = 1920;
    public int requestedHeight = 1080;

    private WebCamTexture webCamTexture;
    private IBarcodeReader barcodeReader;
    private TitleScreenUI titleScreenUI;
    private bool isScanning = false;
    private Coroutine scanningCoroutine;

    private void Start()
    {
        titleScreenUI = FindFirstObjectByType<TitleScreenUI>();
        barcodeReader = new BarcodeReader();

        if (instructionText != null)
            instructionText.text = "Scanner QR Code\n\nPointez la caméra vers le QR Code fourni par votre professeur";
        if (statusText != null)
            statusText.text = "Appuyez sur 'Démarrer Scanner' pour commencer";

        if (startScanButton != null)
            startScanButton.onClick.AddListener(StartCameraScanning);
        if (stopScanButton != null)
        {
            stopScanButton.onClick.AddListener(StopCameraScanning);
            stopScanButton.gameObject.SetActive(false);
        }
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        // Hide the quad at start
        if (cameraQuad != null)
            cameraQuad.SetActive(false);

        // FIX: Hide scanningFrame at start and never show it as a full black image
        if (scanningFrame != null)
        {
            scanningFrame.SetActive(false);
            // If scanningFrame has an Image, make it transparent or outline only
            var img = scanningFrame.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = new Color(1, 1, 1, 0); // Fully transparent
                img.raycastTarget = false;
            }
        }
    }

    private void StartCameraScanning()
    {
        Debug.Log("[QRCodeScanner] Starting camera scanning");
        if (statusText != null)
            statusText.text = "Initialisation de la caméra...";
        StartCoroutine(InitializeCamera());
    }

    private IEnumerator InitializeCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[QRCodeScanner] No camera devices found!");
            if (statusText != null)
                statusText.text = "Aucune caméra trouvée. Utilisez le scanner test.";
            yield break;
        }

        // Pick the first non-IR camera
        string deviceName = null;
        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].name.ToLower().Contains("ir"))
            {
                deviceName = devices[i].name;
                break;
            }
        }
        if (deviceName == null)
            deviceName = devices[0].name;

        int fallbackWidth = 320;
        int fallbackHeight = 240;
        int fallbackFPS = 10;

        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            webCamTexture = null;
        }

        webCamTexture = new WebCamTexture(deviceName, fallbackWidth, fallbackHeight, fallbackFPS);

        // Assign the WebCamTexture to the Quad's material
        if (cameraQuad != null)
        {
            cameraQuad.SetActive(true);
            var renderer = cameraQuad.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Use Unlit/Texture for best results
                if (renderer.material == null || renderer.material.shader.name != "Unlit/Texture")
                    renderer.material = new Material(Shader.Find("Unlit/Texture"));
                renderer.material.mainTexture = webCamTexture;
            }
        }

        try
        {
            webCamTexture.Play();
            Debug.Log($"[QRCodeScanner] Camera started: {deviceName} {fallbackWidth}x{fallbackHeight}@{fallbackFPS}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[QRCodeScanner] Failed to start camera {deviceName}: {e.Message}");
            if (statusText != null)
                statusText.text = "Erreur caméra. Utilisez le scanner test.";
            yield break;
        }

        float timeout = 8f;
        float elapsed = 0f;
        int warmupFrames = 0;
        while ((webCamTexture.width <= 16 || webCamTexture.height <= 16 || warmupFrames < 10) && elapsed < timeout)
        {
            if (!webCamTexture.isPlaying)
            {
                Debug.LogError("[QRCodeScanner] Camera stopped during initialization");
                break;
            }
            if (webCamTexture.didUpdateThisFrame)
                warmupFrames++;
            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (webCamTexture.width > 16 && webCamTexture.height > 16)
        {
            Debug.Log($"[QRCodeScanner] Camera initialized and returning real frames: {webCamTexture.width}x{webCamTexture.height}");
        }
        else
        {
            Debug.LogWarning($"[QRCodeScanner] Camera {deviceName} failed to initialize, trying next...");
            webCamTexture.Stop();
            webCamTexture = null;
            if (cameraQuad != null)
                cameraQuad.SetActive(false);
            // FIX: Always hide scanningFrame if camera fails
            if (scanningFrame != null)
                scanningFrame.SetActive(false);
            yield break;
        }

        isScanning = true;
        if (statusText != null)
            statusText.text = "Caméra active. Pointez vers le QR Code.";

        if (startScanButton != null)
            startScanButton.gameObject.SetActive(false);
        if (stopScanButton != null)
            stopScanButton.gameObject.SetActive(true);

        // FIX: Only show scanningFrame if it's a border/outline, not a full image
        if (scanningFrame != null)
        {
            // If you want a border, enable and set alpha to 1, else keep hidden
            var img = scanningFrame.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = new Color(1, 1, 1, 0); // Keep transparent
            }
            scanningFrame.SetActive(true);
        }

        scanningCoroutine = StartCoroutine(ScanForQRCode());
    }

    private IEnumerator ScanForQRCode()
    {
        Debug.Log("[QRCodeScanner] Starting QR code scanning loop");
        Result result = null;
        while (isScanning && webCamTexture != null && webCamTexture.isPlaying)
        {
            bool shouldContinue = true;
            Color32[] pixels = null;
            try
            {
                if (webCamTexture.didUpdateThisFrame && webCamTexture.width > 16 && webCamTexture.height > 16)
                {
                    bool pixelsRetrieved = false;
                    try
                    {
                        pixels = webCamTexture.GetPixels32();
                        pixelsRetrieved = true;
                    }
                    catch (System.Exception pixelError)
                    {
                        Debug.LogWarning($"[QRCodeScanner] Failed to get camera pixels: {pixelError.Message}");
                        shouldContinue = false;
                    }

                    if (pixelsRetrieved && pixels != null && pixels.Length > 0)
                    {
                        result = barcodeReader.Decode(pixels, webCamTexture.width, webCamTexture.height);
                        if (result != null && !string.IsNullOrEmpty(result.Text))
                        {
                            Debug.Log($"[QRCodeScanner] QR Code detected: {result.Text.Substring(0, Mathf.Min(100, result.Text.Length))}...");
                            break;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[QRCodeScanner] Error during QR scanning: {e.Message}");
                shouldContinue = false;
            }
            if (!shouldContinue)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }
            yield return new WaitForSeconds(0.2f);
        }
        if (isScanning && result != null && !string.IsNullOrEmpty(result.Text))
        {
            yield return StartCoroutine(ProcessQRCodeData(result.Text));
        }
    }

    private IEnumerator ProcessQRCodeData(string qrData)
    {
        if (statusText != null)
            statusText.text = "QR Code détecté! Vérification...";
        StopCameraScanning();
        QRCodeData parsedData = QRCodeData.FromJson(qrData);
        if (parsedData == null || !parsedData.IsValid())
        {
            Debug.LogError("[QRCodeScanner] Invalid QR code format");
            if (statusText != null)
                statusText.text = "QR Code invalide. Format incorrect.";
            yield break;
        }
        yield return StartCoroutine(VerifyQRCodeWithDatabase(parsedData, qrData));
    }

    private IEnumerator VerifyQRCodeWithDatabase(QRCodeData qrData, string originalJson)
    {
        if (statusText != null)
            statusText.text = "Vérification avec la base de données...";
        
        bool isValid = false;
        
        if (FirebaseDatabaseManager.Instance != null && FirebaseDatabaseManager.Instance.IsFirebaseReady())
        {
            Debug.Log($"[QRCodeScanner] Verifying student {qrData.GetStudentId()} with PIN {qrData.GetPin()}");
            
            // Step 1: Check if student exists and get student info
            var studentTask = FirebaseDatabaseManager.Instance.GetStudentInfo(qrData.GetStudentId());
            
            // Wait for the task to complete with timeout
            float timeout = 10f; // 10 second timeout
            float elapsed = 0f;
            
            while (!studentTask.IsCompleted && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!studentTask.IsCompleted)
            {
                Debug.LogError("[QRCodeScanner] Firebase request timed out");
                if (statusText != null)
                    statusText.text = "Délai d'attente dépassé. Vérifiez votre connexion.";
                yield break;
            }
            
            if (studentTask.IsFaulted)
            {
                Debug.LogError($"[QRCodeScanner] Firebase error: {studentTask.Exception?.GetBaseException()?.Message}");
                if (statusText != null)
                    statusText.text = "Erreur de connexion à la base de données.";
                yield break;
            }
            
            if (studentTask.Result != null)
            {
                var student = studentTask.Result;
                Debug.Log($"[QRCodeScanner] Found student: {student.name} (ID: {student.id}, Teacher: {student.linkedTeacherId})");
                Debug.Log($"[QRCodeScanner] Student details: Name='{student.name}', Class='{student.classId}', School='{student.linkedSchoolId}'");
                
                // Step 2: Verify PIN matches password
                if (student.password == qrData.GetPin())
                {
                    Debug.Log("[QRCodeScanner] PIN verified successfully");
                    
                    // Update QRCodeData with student information
                    qrData.studentName = student.name;
                    qrData.classId = student.classId;
                    qrData.teacherId = student.linkedTeacherId;
                    qrData.linkedSchoolId = student.linkedSchoolId;
                    
                    Debug.Log($"[QRCodeScanner] Updated QRCodeData: Name='{qrData.studentName}', Class='{qrData.classId}', School='{qrData.linkedSchoolId}'");
                    
                    // Step 3: Check for available sent tests for this student's teacher
                    if (!string.IsNullOrEmpty(student.linkedTeacherId))
                    {
                        var testsTask = FirebaseDatabaseManager.Instance.GetSentTestsForTeacher(student.linkedTeacherId);
                        
                        // Wait for tests query with timeout
                        elapsed = 0f;
                        while (!testsTask.IsCompleted && elapsed < timeout)
                        {
                            elapsed += Time.deltaTime;
                            yield return null;
                        }
                        
                        if (!testsTask.IsCompleted)
                        {
                            Debug.LogError("[QRCodeScanner] Tests query timed out");
                            if (statusText != null)
                                statusText.text = "Délai d'attente pour récupérer les tests.";
                            yield break;
                        }
                        
                        if (testsTask.Result != null && testsTask.Result.Count > 0)
                        {
                            // Found sent tests - student can proceed
                            isValid = true;
                            Debug.Log($"[QRCodeScanner] Found {testsTask.Result.Count} sent test(s) for teacher {student.linkedTeacherId}");
                            
                            // Store student and test information for the game session
                            if (GameSessionManager.Instance != null)
                            {
                                // Use the first available sent test
                                var availableTest = testsTask.Result[0];
                                qrData.testId = availableTest.id;
                                
                                GameSessionManager.Instance.SetTestData(
                                    qrData.GetStudentId(), 
                                    availableTest.id, 
                                    student.name, 
                                    student.classId, 
                                    student.linkedTeacherId
                                );
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[QRCodeScanner] No sent tests found for this student's teacher");
                            if (statusText != null)
                                statusText.text = "Aucun test envoyé trouvé pour votre enseignant.";
                            
                            // Create fallback test for development/testing but keep student data
                            Debug.Log("[QRCodeScanner] Creating fallback test for development");
                            qrData.testId = "test_dev_" + System.DateTime.Now.Ticks;
                            
                            if (GameSessionManager.Instance != null)
                            {
                                GameSessionManager.Instance.SetTestData(
                                    qrData.GetStudentId(), 
                                    qrData.testId, 
                                    student.name, 
                                    student.classId, 
                                    student.linkedTeacherId
                                );
                            }
                            
                            isValid = true; // Allow to proceed for testing
                            if (statusText != null)
                                statusText.text = "Mode développement - test créé automatiquement.";
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[QRCodeScanner] Student has no linked teacher");
                        
                        // Create fallback for students without teacher but keep student data
                        qrData.teacherId = "dev_teacher";
                        qrData.testId = "test_no_teacher_" + System.DateTime.Now.Ticks;
                        
                        if (GameSessionManager.Instance != null)
                        {
                            GameSessionManager.Instance.SetTestData(
                                qrData.GetStudentId(), 
                                qrData.testId, 
                                student.name, 
                                student.classId, 
                                qrData.teacherId
                            );
                        }
                        
                        isValid = true;
                        if (statusText != null)
                            statusText.text = "Étudiant trouvé - mode développement activé.";
                    }
                }
                else
                {
                    Debug.LogWarning($"[QRCodeScanner] PIN mismatch. Expected: {student.password}, Got: {qrData.GetPin()}");
                    if (statusText != null)
                        statusText.text = "Code PIN incorrect.";
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning("[QRCodeScanner] Student not found in database");
                if (statusText != null)
                    statusText.text = "Étudiant non trouvé dans la base de données.";
                yield break;
            }
        }
        else
        {
            // SIMPLIFIED: Firebase unavailable - show error instead of fallback
            Debug.LogError("[QRCodeScanner] Firebase not available - cannot verify QR code");
            if (statusText != null)
                statusText.text = "Connexion à la base de données requise. Vérifiez votre connexion internet.";
            yield break;
        }
        
        if (isValid)
        {
            if (statusText != null)
                statusText.text = "QR Code valide! Authentification réussie.";
            
            // Log the final QR data state before passing to title screen
            Debug.Log($"[QRCodeScanner] Final QR data before passing to TitleScreenUI: studentName='{qrData.studentName}', classId='{qrData.classId}', linkedSchoolId='{qrData.linkedSchoolId}'");
            
            // Pass the SAME qrData object, not the original JSON string
            if (titleScreenUI != null)
            {
                titleScreenUI.OnQRCodeScannedWithData(qrData);
            }
        }
    }

    private void StopCameraScanning()
    {
        Debug.Log("[QRCodeScanner] Stopping camera scanning");
        isScanning = false;
        if (scanningCoroutine != null)
        {
            StopCoroutine(scanningCoroutine);
            scanningCoroutine = null;
        }
        try
        {
            if (webCamTexture != null)
            {
                if (webCamTexture.isPlaying)
                    webCamTexture.Stop();
                webCamTexture = null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[QRCodeScanner] Error stopping camera: {e.Message}");
        }
        if (cameraQuad != null)
            cameraQuad.SetActive(false);
        if (startScanButton != null)
            startScanButton.gameObject.SetActive(true);
        if (stopScanButton != null)
            stopScanButton.gameObject.SetActive(false);

        // FIX: Always hide scanningFrame when stopping
        if (scanningFrame != null)
            scanningFrame.SetActive(false);

        if (statusText != null)
            statusText.text = "Scanner arrêté.";
    }

    private void Cancel()
    {
        StopCameraScanning();
        if (titleScreenUI != null)
            titleScreenUI.ReturnToMainMenu();
    }

    private void OnDestroy()
    {
        StopCameraScanning();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            StopCameraScanning();
    }
}
