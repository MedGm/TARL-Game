using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }
    
    [Header("Session Settings")]
    public bool isTestMode = false;
    public bool isFreeRoamMode = false;
    
    // Test mode data
    public string studentId = "";
    public string testId = "";
    public string studentName = "";
    public string classId = "";
    public string teacherId = "";
    
    // Session tracking
    public float testStartTime = 0f;
    public int totalScore = 0;
    public Dictionary<string, TaskResult> taskResults = new Dictionary<string, TaskResult>();
    
    // ADDED: Enhanced progress tracking for Firebase
    public int totalAttemptsUsed = 0;
    public float totalTimeSpent = 0f;
    public int correctAnswersCount = 0;
    public int incorrectAnswersCount = 0;
    
    // ADDED: Prevent duplicate saves
    private bool testCompleted = false;
    private bool testSaved = false;
    
    [System.Serializable]
    public class TaskResult
    {
        public object answer;
        public bool isCorrect;
        public int attemptsUsed;
        public int score;
        public float timeSpent;
    }
    
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
    
    public void StartTestMode(string studentId, string testId)
    {
        this.isTestMode = true;
        this.isFreeRoamMode = false;
        this.studentId = studentId;
        this.testId = testId;
        this.testStartTime = Time.time;
        this.totalScore = 0;
        this.taskResults.Clear();
        
        // ADDED: Reset completion flags
        this.testCompleted = false;
        this.testSaved = false;
        this.totalAttemptsUsed = 0;
        this.totalTimeSpent = 0f;
        this.correctAnswersCount = 0;
        this.incorrectAnswersCount = 0;
        
        Debug.Log($"[GameSessionManager] Started test mode: student={studentId}, test={testId}");
    }
    
    public void StartFreeRoamMode()
    {
        this.isTestMode = false;
        this.isFreeRoamMode = true;
        this.studentId = "";
        this.testId = "";
        
        Debug.Log("[GameSessionManager] Started free roam mode");
    }
    
    public void SetTestData(string studentId, string testId, string studentName, string classId, string teacherId)
    {
        this.studentId = studentId;
        this.testId = testId;
        this.studentName = studentName;
        this.classId = classId;
        this.teacherId = teacherId;
        
        Debug.Log($"[GameSessionManager] Test data set: Student={studentName} ({studentId}), Test={testId}, Teacher={teacherId}, Class={classId}");
    }
    
    // Configuration methods for different task types
    public FindCompositionConfig GetFindCompositionConfig(string difficulty)
    {
        if (!isTestMode) return null;
        
        // Use test data from Firebase if available, otherwise use defaults
        switch (difficulty.ToLower())
        {
            case "easy":
                return new FindCompositionConfig { 
                    number = UnityEngine.Random.Range(1000, 2000), 
                    time = 120, 
                    attemptsAllowed = 3 
                };
            case "medium":
                return new FindCompositionConfig { 
                    number = UnityEngine.Random.Range(2000, 5000), 
                    time = 90, 
                    attemptsAllowed = 3 
                };
            case "hard":
                return new FindCompositionConfig { 
                    number = UnityEngine.Random.Range(5000, 9999), 
                    time = 60, 
                    attemptsAllowed = 3 
                };
            default:
                return new FindCompositionConfig { 
                    number = UnityEngine.Random.Range(1000, 9999), 
                    time = 120, 
                    attemptsAllowed = 3 
                };
        }
    }
    
    public WriteNumberConfig GetWriteNumberConfig(string difficulty)
    {
        if (!isTestMode) return null;
        
        switch (difficulty.ToLower())
        {
            case "easy":
                return new WriteNumberConfig { 
                    number = UnityEngine.Random.Range(100, 999), 
                    time = 60, 
                    attemptsAllowed = 3 
                };
            case "medium":
                return new WriteNumberConfig { 
                    number = UnityEngine.Random.Range(1000, 4999), 
                    time = 45, 
                    attemptsAllowed = 3 
                };
            case "hard":
                return new WriteNumberConfig { 
                    number = UnityEngine.Random.Range(5000, 9999), 
                    time = 30, 
                    attemptsAllowed = 3 
                };
            default:
                return new WriteNumberConfig { 
                    number = UnityEngine.Random.Range(100, 9999), 
                    time = 60, 
                    attemptsAllowed = 3 
                };
        }
    }
    
    public IdentifyUnitsConfig GetIdentifyUnitsConfig(string difficulty)
    {
        if (!isTestMode) return null;
        
        switch (difficulty.ToLower())
        {
            case "easy":
                return new IdentifyUnitsConfig { 
                    number = UnityEngine.Random.Range(100, 999), 
                    time = 30, 
                    attemptsAllowed = 3 
                };
            case "medium":
                return new IdentifyUnitsConfig { 
                    number = UnityEngine.Random.Range(1000, 9999), 
                    time = 25, 
                    attemptsAllowed = 3 
                };
            case "hard":
                return new IdentifyUnitsConfig { 
                    number = UnityEngine.Random.Range(10000, 99999), 
                    time = 20, 
                    attemptsAllowed = 3 
                };
            default:
                return new IdentifyUnitsConfig { 
                    number = UnityEngine.Random.Range(100, 9999), 
                    time = 30, 
                    attemptsAllowed = 3 
                };
        }
    }
    
    // Task result registration
    public void RegisterTaskResult(string taskType, string difficulty, object answer, bool isCorrect, int attemptsUsed, int score)
    {
        string key = $"{taskType}_{difficulty}";
        taskResults[key] = new TaskResult
        {
            answer = answer,
            isCorrect = isCorrect,
            attemptsUsed = attemptsUsed,
            score = score,
            timeSpent = Time.time - testStartTime
        };
        
        // ADDED: Update aggregate statistics
        totalAttemptsUsed += attemptsUsed;
        if (isCorrect)
            correctAnswersCount++;
        else
            incorrectAnswersCount++;
        
        Debug.Log($"[GameSessionManager] Registered result: {key} = {isCorrect} (score: {score}, attempts: {attemptsUsed})");
        Debug.Log($"[GameSessionManager] Total stats: correct={correctAnswersCount}, incorrect={incorrectAnswersCount}, attempts={totalAttemptsUsed}");
    }

    public void UpdateTotalScore(int additionalScore)
    {
        totalScore += additionalScore;
        Debug.Log($"[GameSessionManager] Total score updated: {totalScore}");
    }

    // ADDED: Method to complete the test and save to Firebase
    public void CompleteTest()
    {
        if (!isTestMode)
        {
            Debug.LogWarning("[GameSessionManager] Not in test mode, cannot complete test");
            return;
        }
        
        if (testCompleted)
        {
            Debug.LogWarning("[GameSessionManager] Test already completed, ignoring duplicate call");
            Debug.LogWarning($"[GameSessionManager] Duplicate call stack trace: {System.Environment.StackTrace}");
            return;
        }
        
        testCompleted = true;
        totalTimeSpent = Time.time - testStartTime;
        Debug.Log($"[GameSessionManager] Test completed! Total time: {totalTimeSpent}s, Total score: {totalScore}");
        Debug.Log($"[GameSessionManager] CompleteTest called from: {System.Environment.StackTrace}");
        
        // Save to Firebase
        SaveTestResultsToFirebase();
    }

    // Save results to Firebase at end of test
    public async void SaveTestResultsToFirebase()
    {
        if (!isTestMode || FirebaseDatabaseManager.Instance == null)
        {
            Debug.Log("[GameSessionManager] Not in test mode or Firebase unavailable, skipping save");
            return;
        }

        if (testSaved)
        {
            Debug.LogWarning("[GameSessionManager] Test results already saved, ignoring duplicate call");
            return;
        }

        if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(testId) || string.IsNullOrEmpty(teacherId))
        {
            Debug.LogError("[GameSessionManager] Missing required data for saving test results");
            return;
        }

        testSaved = true; // Mark as saved to prevent duplicates
        
        Debug.Log($"[GameSessionManager] Saving test results for student {studentId}, test {testId}");
        Debug.Log($"[GameSessionManager] Final statistics: correct={correctAnswersCount}, incorrect={incorrectAnswersCount}, attempts={totalAttemptsUsed}, time={totalTimeSpent}s");

        // Create answer model with all task results
        var answerModel = new AnswerModel
        {
            totalScore = totalScore,
            statistics = new AnswerModel.Statistics
            {
                totalTimeSpent = (int)totalTimeSpent,
                totalAttemptsUsed = totalAttemptsUsed,
                correctAnswersCount = correctAnswersCount,
                incorrectAnswersCount = incorrectAnswersCount
            },
            answers = new AnswerModel.Answers()
        };

        // Initialize answer structures
        answerModel.answers.findcomposition = new AnswerModel.FindCompositionAnswers();
        answerModel.answers.WritetheFollowingNumberinLetters = new AnswerModel.WriteNumberAnswers();
        answerModel.answers.IdentifthUnitsTensHundredsandThousands = new AnswerModel.IdentifyUnitsAnswers();

        // Fill in task results
        foreach (var result in taskResults)
        {
            string[] parts = result.Key.Split('_');
            if (parts.Length != 2) continue;
            
            string taskType = parts[0];
            string difficulty = parts[1];

            Debug.Log($"[GameSessionManager] Processing result: {taskType}_{difficulty} = {result.Value.isCorrect} (score: {result.Value.score}, attempts: {result.Value.attemptsUsed})");

            switch (taskType.ToLower())
            {
                case "findcomposition":
                    SetFindCompositionAnswer(answerModel.answers.findcomposition, difficulty, result.Value);
                    break;
                case "writenumber":
                    SetWriteNumberAnswer(answerModel.answers.WritetheFollowingNumberinLetters, difficulty, result.Value);
                    break;
                case "identifyunits":
                    SetIdentifyUnitsAnswer(answerModel.answers.IdentifthUnitsTensHundredsandThousands, difficulty, result.Value);
                    break;
                default:
                    Debug.LogWarning($"[GameSessionManager] Unknown task type: {taskType}");
                    break;
            }
        }

        // Save to Firebase
        bool saved = await FirebaseDatabaseManager.Instance.SaveStudentTestResults(studentId, testId, teacherId, answerModel);

        if (saved)
        {
            Debug.Log("[GameSessionManager] Test results saved successfully to Firebase");
        }
        else
        {
            Debug.LogError("[GameSessionManager] Failed to save test results to Firebase");
            testSaved = false; // Allow retry if save failed
        }
    }
    
    private void SetFindCompositionAnswer(AnswerModel.FindCompositionAnswers answers, string difficulty, TaskResult result)
    {
        var answer = new AnswerModel.FindCompositionAnswers.DifficultyAnswer
        {
            studentAnswer = result.answer as List<int> ?? new List<int>(),
            isCorrect = result.isCorrect,
            attemptsUsed = result.attemptsUsed,
            score = result.score,
            time = (int)result.timeSpent // ADDED: Time field
        };
        
        switch (difficulty)
        {
            case "easy": answers.easy = answer; break;
            case "medium": answers.medium = answer; break;
            case "hard": answers.hard = answer; break;
        }
    }
    
    private void SetWriteNumberAnswer(AnswerModel.WriteNumberAnswers answers, string difficulty, TaskResult result)
    {
        var answer = new AnswerModel.WriteNumberAnswers.DifficultyAnswer
        {
            studentAnswer = result.answer as List<string> ?? new List<string>(),
            isCorrect = result.isCorrect,
            attemptsUsed = result.attemptsUsed,
            score = result.score,
            time = (int)result.timeSpent // ADDED: Time field
        };
        
        switch (difficulty)
        {
            case "easy": answers.easy = answer; break;
            case "medium": answers.medium = answer; break;
            case "hard": answers.hard = answer; break;
        }
    }
    
    private void SetIdentifyUnitsAnswer(AnswerModel.IdentifyUnitsAnswers answers, string difficulty, TaskResult result)
    {
        var answer = new AnswerModel.IdentifyUnitsAnswers.DifficultyAnswer
        {
            studentAnswer = result.answer as AnswerModel.IdentifyUnitsAnswers.UnitsAnswer ?? new AnswerModel.IdentifyUnitsAnswers.UnitsAnswer(),
            isCorrect = result.isCorrect,
            attemptsUsed = result.attemptsUsed,
            score = result.score,
            time = (int)result.timeSpent // ADDED: Time field
        };
        
        switch (difficulty)
        {
            case "easy": answers.easy = answer; break;
            case "medium": answers.medium = answer; break;
            case "hard": answers.hard = answer; break;
        }
    }
}
