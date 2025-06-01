using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Player position in the overworld
    public Vector3 lastPlayerPosition = Vector3.zero;

    // Dungeon completion status
    private HashSet<string> completedDungeons = new HashSet<string>();

    // Player progress tracking
    public int keysCollected = 0;
    private HashSet<int> completedPlaceValueTasks = new HashSet<int>();
    public bool hasShield = false;

    // Place value tasks are tied to key levels (1-3)
    public int currentTaskLevel = 0;

    // Soap bubble task results (success/fail for each key)
    public List<bool> soapTaskResults = new List<bool>(); // true=success, false=failure

    // The number of times the SoapScene has been played (i.e., player collided with BubbleSoapChaser)
    public int soapTasksPlayed = 0;

    // --- SOAP BUBBLE TASK TRACKING ---
    // Separate results by task type
    public int soapTask1Count = 0;
    public int soapTask2Count = 0;
    public int soapTask3Count = 0;

    // The current dungeon being entered (needed for DungeonManager)
    public string currentDungeonId = "";

    [Header("Bubble Soap")]
    public GameObject bubbleSoapChaserPrefab; // Assign your prefab in the inspector
    public Vector3 bubbleSpawnPosition = new Vector3(0, 0, 0); // Set to your desired spawn point

    [Header("UI Warning")]
    public GameObject warningTextPrefab; // Assign your WarningText prefab in the inspector
    public Vector3 warningTextSpawnPosition = new Vector3(0, 0, 0); // Set to your desired UI position (screen space or world space)
    public float warningDuration = 5f; // Set to 5 seconds

    private GameObject currentWarningTextInstance;

    // Track if a bubble has already been spawned for the current key
    private int lastBubbleSpawnedForKeyCount = 0;

    public enum ProgressionStage
    {
        None,
        Overworld,
        Dungeon1,
        Dungeon2,
        Dungeon3,
        SoapBubble1,
        SoapBubble2,
        SoapBubble3,
        FinalPortal,
        Completed
    }

    // Hidden progression state (not shown to player)
    public ProgressionStage currentProgression = ProgressionStage.Overworld;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void MarkDungeonCompleted(string dungeonId)
    {
        Debug.Log("[GameManager] MarkDungeonCompleted called with dungeonId=" + dungeonId + " (currentDungeonId=" + currentDungeonId + ")");
        if (!string.IsNullOrEmpty(dungeonId) && !completedDungeons.Contains(dungeonId))
        {
            completedDungeons.Add(dungeonId);
            keysCollected++;
            currentTaskLevel = keysCollected; // The next task will be at this level

            // Reset bubble spawn tracker so a new bubble can spawn for the new key
            lastBubbleSpawnedForKeyCount = keysCollected - 1;

            // Bubble will be spawned after returning to overworldScene (see TrySpawnBubbleSoapChaser)
        }
    }

    public void TrySpawnBubbleSoapChaser()
    {
        // Only spawn in overworldScene and only if a new key was collected since last spawn
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "overworldScene")
            return;

        // Prevent multiple spawns
        if (GameObject.FindAnyObjectByType<BubbleSoapChaser>() != null)
            return;

        // Only spawn if a new key was collected since last spawn
        if (keysCollected > lastBubbleSpawnedForKeyCount)
        {
            lastBubbleSpawnedForKeyCount = keysCollected;
            if (bubbleSoapChaserPrefab != null)
            {
                StartCoroutine(SpawnBubbleSoapChaserWithDelay(0.5f));
            }
        }
    }

    private System.Collections.IEnumerator SpawnBubbleSoapChaserWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameObject bubble = Instantiate(bubbleSoapChaserPrefab, bubbleSpawnPosition, Quaternion.identity);

        // --- Set the correct soap scene based on keysCollected ---
        var chaser = bubble.GetComponent<BubbleSoapChaser>();
        if (chaser != null)
        {
            if (keysCollected == 1)
                chaser.soapTaskSceneName = "SoapScene";
            else if (keysCollected == 2)
                chaser.soapTaskSceneName = "SoapScene2";
            else if (keysCollected == 3)
                chaser.soapTaskSceneName = "SoapScene3";
            else
                chaser.soapTaskSceneName = "SoapScene";
        }

        bubble.SetActive(true);
        Debug.Log("[GameManager] BubbleSoapChaser spawned and enabled.");
        ShowBubbleWarning();
    }

    private void ShowBubbleWarning()
    {
        // Destroy any existing warning text instance
        if (currentWarningTextInstance != null)
        {
            Destroy(currentWarningTextInstance);
            currentWarningTextInstance = null;
        }

        if (warningTextPrefab != null)
        {
            // If prefab is a UI element, make sure to parent it to the Canvas
            Canvas canvas = GameObject.FindAnyObjectByType<Canvas>();
            if (canvas != null && warningTextPrefab.GetComponent<RectTransform>() != null)
            {
                currentWarningTextInstance = Instantiate(warningTextPrefab, canvas.transform);
                currentWarningTextInstance.transform.localPosition = warningTextSpawnPosition;
            }
            else
            {
                // World space fallback
                currentWarningTextInstance = Instantiate(warningTextPrefab, warningTextSpawnPosition, Quaternion.identity);
            }

            currentWarningTextInstance.SetActive(true);

            // Set text if TMP_Text exists
            var tmp = currentWarningTextInstance.GetComponent<TMPro.TMP_Text>();
            if (tmp != null)
                tmp.text = "Attention ! Une bulle de savon vous poursuit !";

            StartCoroutine(HideBubbleWarningAfterDelay(warningDuration));
        }
        else
        {
            Debug.LogWarning("WarningText prefab not assigned in GameManager.");
        }
    }

    private System.Collections.IEnumerator HideBubbleWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentWarningTextInstance != null)
        {
            Destroy(currentWarningTextInstance);
            currentWarningTextInstance = null;
        }
    }

    public void DisableWarningTextIfExists()
    {
        if (currentWarningTextInstance != null)
        {
            Destroy(currentWarningTextInstance);
            currentWarningTextInstance = null;
        }
    }

    public bool IsDungeonCompleted(string dungeonId)
    {
        return !string.IsNullOrEmpty(dungeonId) && completedDungeons.Contains(dungeonId);
    }

    public void MarkPlaceValueTaskCompleted(int taskLevel)
    {
        completedPlaceValueTasks.Add(taskLevel);
        hasShield = true; // Player receives shield after completing task
    }

    public bool IsPlaceValueTaskCompleted(int taskLevel)
    {
        return completedPlaceValueTasks.Contains(taskLevel);
    }

    public bool CanStartPlaceValueTask()
    {
        // Can start a task if we have a key and haven't completed the task for this key level
        return keysCollected > 0 && !IsPlaceValueTaskCompleted(currentTaskLevel);
    }

    // --- SOAP BUBBLE TASK LOGIC ---

    // Call this ONCE per SoapScene play (not per round), from SoapBubbleTaskManager.Start()
    public void RegisterSoapTaskPlayed()
    {
        soapTasksPlayed++;
        
        // Track which type of soap task was played based on active scene
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "SoapScene") soapTask1Count++;
        else if (sceneName == "SoapScene2") soapTask2Count++;
        else if (sceneName == "SoapScene3") soapTask3Count++;
        
        Debug.Log("[GameManager] Soap task scene played: " + sceneName + ". Total played: " + soapTasksPlayed);
    }

    // Call this for each round result (success/fail) in SoapBubbleTaskManager
    public void RegisterSoapTaskResult(bool success)
    {
        soapTaskResults.Add(success);
        Debug.Log("[GameManager] Soap task round result: " + (success ? "Success" : "Fail") + ". Total results: " + soapTaskResults.Count);
    }

    // Returns true if the player has played enough soap scenes to unlock the portal
    public bool HasPlayedAllSoapTasks(int requiredCount = 3)
    {
        return soapTasksPlayed >= requiredCount;
    }

    public bool HasCompletedAllSoapTaskTypes()
    {
        return soapTask1Count > 0 && soapTask2Count > 0 && soapTask3Count > 0;
    }

    public int GetSoapTaskSuccessCount()
    {
        int count = 0;
        foreach (var r in soapTaskResults)
            if (r) count++;
        return count;
    }

    public int GetSoapTaskFailCount()
    {
        int count = 0;
        foreach (var r in soapTaskResults)
            if (!r) count++;
        return count;
    }

    public void ResetSoapTaskResults()
    {
        soapTaskResults.Clear();
        soapTasksPlayed = 0;
    }

    // Call this after loading overworldScene (e.g. in SoapBubbleTaskManager, TerminalPuzzle, etc.)
    public void OnoverworldSceneLoaded()
    {
        DisableWarningTextIfExists();
    }
}
