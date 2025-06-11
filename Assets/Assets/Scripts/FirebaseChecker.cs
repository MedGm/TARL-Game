using UnityEngine;
using Firebase;
using System.Collections;

public class FirebaseChecker : MonoBehaviour
{
    [Header("Firebase Diagnostic")]
    public bool runDiagnosticOnStart = true;
    
    private void Start()
    {
        if (runDiagnosticOnStart)
        {
            StartCoroutine(RunFirebaseDiagnostic());
        }
    }
    
    private IEnumerator RunFirebaseDiagnostic()
    {
        Debug.Log("=== FIREBASE DIAGNOSTIC START ===");
        
        // 1. Check google-services.json location
        string configPath = System.IO.Path.Combine(Application.streamingAssetsPath, "google-services.json");
        bool configExists = System.IO.File.Exists(configPath);
        Debug.Log($"[FirebaseChecker] google-services.json in StreamingAssets: {configExists}");
        if (!configExists)
        {
            Debug.LogError("[FirebaseChecker] google-services.json NOT FOUND in StreamingAssets folder!");
            Debug.LogError("[FirebaseChecker] Please move google-services.json from Assets/ to Assets/StreamingAssets/");
        }
        
        // 2. Check Firebase App initialization
        Debug.Log("[FirebaseChecker] Checking Firebase dependencies...");
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        
        yield return new WaitUntil(() => dependencyTask.IsCompleted);
        
        Debug.Log($"[FirebaseChecker] Firebase dependency status: {dependencyTask.Result}");
        
        if (dependencyTask.Result == DependencyStatus.Available)
        {
            Debug.Log("[FirebaseChecker] ✅ Firebase dependencies OK");
            
            // 3. Try to get Firebase App
            try
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                Debug.Log($"[FirebaseChecker] ✅ Firebase App created: {app.Name}");
                Debug.Log($"[FirebaseChecker] ✅ Project ID: {app.Options.ProjectId}");
                Debug.Log($"[FirebaseChecker] ✅ Database URL: {app.Options.DatabaseUrl}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FirebaseChecker] ❌ Firebase App creation failed: {e.Message}");
            }
            
            // 4. Try to get Database Reference
            try
            {
                var database = Firebase.Database.FirebaseDatabase.DefaultInstance;
                var reference = database.RootReference;
                Debug.Log("[FirebaseChecker] ✅ Database reference created successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FirebaseChecker] ❌ Database reference failed: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[FirebaseChecker] ❌ Firebase dependencies not available: {dependencyTask.Result}");
            Debug.LogError("[FirebaseChecker] This usually means Firebase SDK is not properly imported");
        }
        
        // 5. Check network connectivity
        Debug.Log($"[FirebaseChecker] Internet connectivity: {Application.internetReachability}");
        
        Debug.Log("=== FIREBASE DIAGNOSTIC END ===");
    }
    
    [ContextMenu("Run Diagnostic")]
    public void RunDiagnosticManual()
    {
        StartCoroutine(RunFirebaseDiagnostic());
    }
}
