using UnityEngine;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase;
using System;
using System.Collections.Generic;

public class FirebaseDatabaseManager : MonoBehaviour
{
    public static FirebaseDatabaseManager Instance { get; private set; }
    
    private DatabaseReference databaseReference;
    private bool isFirebaseInitialized = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitializeFirebase());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator InitializeFirebase()
    {
        Debug.Log("[FirebaseDatabaseManager] Starting Firebase initialization...");
        
        // ADDED: Retry logic for Firebase initialization
        int maxRetries = 3;
        int currentRetry = 0;
        
        while (currentRetry < maxRetries && !isFirebaseInitialized)
        {
            if (currentRetry > 0)
            {
                Debug.Log($"[FirebaseDatabaseManager] Retry attempt {currentRetry}/{maxRetries}");
                yield return new WaitForSeconds(2f); // Wait before retry
            }
            
            // Check Firebase dependencies
            var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
            yield return new WaitUntil(() => dependencyTask.IsCompleted);
            
            if (dependencyTask.Result == DependencyStatus.Available)
            {
                // Firebase is ready to use
                try
                {
                    FirebaseApp app = FirebaseApp.DefaultInstance;
                    databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                    isFirebaseInitialized = true;
                    
                    Debug.Log("[FirebaseDatabaseManager] Firebase initialized successfully!");
                    Debug.Log($"[FirebaseDatabaseManager] Database URL: {FirebaseDatabase.DefaultInstance.App.Options.DatabaseUrl}");
                    break; // Success, exit retry loop
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[FirebaseDatabaseManager] Error initializing Firebase (attempt {currentRetry + 1}): {e.Message}");
                    isFirebaseInitialized = false;
                }
            }
            else
            {
                Debug.LogError($"[FirebaseDatabaseManager] Could not resolve Firebase dependencies (attempt {currentRetry + 1}): {dependencyTask.Result}");
                isFirebaseInitialized = false;
            }
            
            currentRetry++;
        }
        
        if (!isFirebaseInitialized)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Failed to initialize Firebase after {maxRetries} attempts. Using offline mode.");
        }
    }

    public bool IsFirebaseReady()
    {
        return isFirebaseInitialized && databaseReference != null;
    }

    // NEW: Get class information by class ID
    public async Task<ClassInfo> GetClassInfo(string classId)
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("[FirebaseDatabaseManager] Firebase not initialized");
            return null;
        }

        if (string.IsNullOrEmpty(classId))
        {
            Debug.LogError("[FirebaseDatabaseManager] ClassId is null or empty");
            return null;
        }

        try
        {
            Debug.Log($"[FirebaseDatabaseManager] Getting class info for: {classId}");
            
            var dataSnapshot = await databaseReference.Child("classes").Child(classId).GetValueAsync();
            
            if (dataSnapshot != null && dataSnapshot.Exists)
            {
                string json = dataSnapshot.GetRawJsonValue();
                Debug.Log($"[FirebaseDatabaseManager] Raw class data: {json}");
                
                if (!string.IsNullOrEmpty(json))
                {
                    var classData = JsonUtility.FromJson<ClassInfo>(json);
                    if (classData != null)
                    {
                        Debug.Log($"[FirebaseDatabaseManager] Successfully parsed class info: {classData.name} (niveau: {classData.niveau})");
                        return classData;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[FirebaseDatabaseManager] Class {classId} not found in classes collection");
            }
            
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Error getting class info for {classId}: {e.Message}");
            return null;
        }
    }

    // Enhanced student info retrieval with password verification
    public async Task<StudentInfo> GetStudentInfo(string studentId)
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("[FirebaseDatabaseManager] Firebase not initialized or database reference is null");
            return null;
        }

        if (string.IsNullOrEmpty(studentId))
        {
            Debug.LogError("[FirebaseDatabaseManager] StudentId is null or empty");
            return null;
        }

        try
        {
            Debug.Log($"[FirebaseDatabaseManager] Attempting to get student info for: {studentId}");
            
            // Check in 'users' collection (matches your database structure)
            var dataSnapshot = await databaseReference.Child("users").Child(studentId).GetValueAsync();
            
            if (dataSnapshot == null)
            {
                Debug.LogWarning($"[FirebaseDatabaseManager] DataSnapshot is null for student: {studentId}");
                return null;
            }
            
            if (dataSnapshot.Exists)
            {
                string json = dataSnapshot.GetRawJsonValue();
                Debug.Log($"[FirebaseDatabaseManager] Raw student data: {json}");
                
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning($"[FirebaseDatabaseManager] Raw JSON is null or empty for student: {studentId}");
                    return null;
                }
                
                // Parse the user data and convert to StudentInfo
                var userData = JsonUtility.FromJson<UserData>(json);
                if (userData != null && userData.role == "Student")
                {
                    Debug.Log($"[FirebaseDatabaseManager] Parsed UserData: firstName='{userData.firstName}', lastName='{userData.lastName}', role='{userData.role}', schoolGrade='{userData.schoolGrade}', linkedSchoolId='{userData.linkedSchoolId}'");
                    
                    // FIXED: Better name handling for students with minimal data
                    string displayName = "";
                    if (!string.IsNullOrEmpty(userData.firstName) && !string.IsNullOrEmpty(userData.lastName))
                    {
                        displayName = userData.firstName + " " + userData.lastName;
                        Debug.Log($"[FirebaseDatabaseManager] Using full name: '{displayName}'");
                    }
                    else if (!string.IsNullOrEmpty(userData.firstName))
                    {
                        displayName = userData.firstName;
                        Debug.Log($"[FirebaseDatabaseManager] Using first name only: '{displayName}'");
                    }
                    else if (!string.IsNullOrEmpty(userData.lastName))
                    {
                        displayName = userData.lastName;
                        Debug.Log($"[FirebaseDatabaseManager] Using last name only: '{displayName}'");
                    }
                    else
                    {
                        // Fallback: use the student ID as display name
                        displayName = "Student " + studentId.Substring(Math.Max(0, studentId.Length - 4)); // Last 4 chars of ID
                        Debug.Log($"[FirebaseDatabaseManager] Using fallback name: '{displayName}'");
                    }
                    
                    // FIXED: Better class name resolution with fallback
                    string className = "N/A";
                    if (!string.IsNullOrEmpty(userData.schoolGrade))
                    {
                        Debug.Log($"[FirebaseDatabaseManager] Attempting to resolve class ID: {userData.schoolGrade}");
                        
                        try
                        {
                            var classInfo = await GetClassInfo(userData.schoolGrade);
                            if (classInfo != null && !string.IsNullOrEmpty(classInfo.name))
                            {
                                className = classInfo.name;
                                Debug.Log($"[FirebaseDatabaseManager] Successfully resolved class name: {className} from ID: {userData.schoolGrade}");
                            }
                            else
                            {
                                Debug.LogWarning($"[FirebaseDatabaseManager] Class ID {userData.schoolGrade} not found in classes collection");
                                
                                // FALLBACK: Try to extract a meaningful name from the class ID
                                if (userData.schoolGrade.Contains("class_"))
                                {
                                    // Try to use the timestamp part as a fallback
                                    string idPart = userData.schoolGrade.Replace("class_", "");
                                    className = $"Classe_{idPart.Substring(Math.Max(0, idPart.Length - 4))}";
                                }
                                else
                                {
                                    className = userData.schoolGrade; // Use raw ID as fallback
                                }
                                
                                Debug.Log($"[FirebaseDatabaseManager] Using fallback class name: {className}");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[FirebaseDatabaseManager] Error resolving class name: {e.Message}");
                            className = userData.schoolGrade; // Use ID as fallback
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[FirebaseDatabaseManager] userData.schoolGrade is null or empty");
                    }
                    
                    // FIXED: School name is already the actual name, not an ID
                    string schoolName = !string.IsNullOrEmpty(userData.linkedSchoolId) ? userData.linkedSchoolId : "N/A";
                    Debug.Log($"[FirebaseDatabaseManager] School name: '{schoolName}'");
                    
                    var studentInfo = new StudentInfo
                    {
                        id = studentId,
                        name = displayName,
                        firstName = userData.firstName ?? "",
                        lastName = userData.lastName ?? "",
                        classId = className,  // FIXED: Use resolved or fallback class name
                        email = userData.email ?? "",
                        isActive = !userData.frozen,
                        password = userData.password ?? "",
                        linkedTeacherId = userData.linkedTeacherId ?? "",
                        schoolGrade = className,  // FIXED: Use resolved or fallback class name
                        linkedSchoolId = schoolName,
                        role = userData.role
                    };
                    
                    Debug.Log($"[FirebaseDatabaseManager] Final StudentInfo: Name='{studentInfo.name}', Class='{studentInfo.classId}', School='{studentInfo.linkedSchoolId}', Teacher='{studentInfo.linkedTeacherId}'");
                    return studentInfo;
                }
                else
                {
                    Debug.LogWarning($"[FirebaseDatabaseManager] User {studentId} is not a student or has invalid role: {userData?.role}");
                }
            }
            else
            {
                Debug.LogWarning($"[FirebaseDatabaseManager] Student {studentId} not found in users collection");
            }
            
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Error getting student info for {studentId}: {e.Message}");
            Debug.LogError($"[FirebaseDatabaseManager] Stack trace: {e.StackTrace}");
            return null;
        }
    }

    // Get all sent tests for a specific teacher
    public async Task<List<TestModel>> GetSentTestsForTeacher(string teacherId)
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("[FirebaseDatabaseManager] Firebase not initialized");
            return new List<TestModel>();
        }

        if (string.IsNullOrEmpty(teacherId))
        {
            Debug.LogError("[FirebaseDatabaseManager] TeacherId is null or empty");
            return new List<TestModel>();
        }

        try
        {
            Debug.Log($"[FirebaseDatabaseManager] Looking for sent tests for teacher: {teacherId}");
            var dataSnapshot = await databaseReference.Child("tests").GetValueAsync();
            
            if (dataSnapshot == null || !dataSnapshot.Exists)
            {
                Debug.LogWarning("[FirebaseDatabaseManager] No tests found in database");
                return new List<TestModel>();
            }
            
            List<TestModel> sentTests = new List<TestModel>();
            
            foreach (DataSnapshot child in dataSnapshot.Children)
            {
                try
                {
                    string json = child.GetRawJsonValue();
                    if (string.IsNullOrEmpty(json)) continue;
                    
                    TestModel test = JsonUtility.FromJson<TestModel>(json);
                    
                    if (test != null)
                    {
                        Debug.Log($"[FirebaseDatabaseManager] Checking test {test.id}: teacher={test.idTeacher}, isSent={test.isSent}, isActive={test.isActive}");
                        
                        // Check if test belongs to teacher and is sent and active
                        if (test.idTeacher == teacherId && test.isSent && test.isActive)
                        {
                            sentTests.Add(test);
                            Debug.Log($"[FirebaseDatabaseManager] Found sent test: {test.id} ({test.title?.fr}) for teacher {teacherId}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[FirebaseDatabaseManager] Error parsing test data: {e.Message}");
                }
            }
            
            Debug.Log($"[FirebaseDatabaseManager] Found {sentTests.Count} sent tests for teacher {teacherId}");
            return sentTests;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Error getting sent tests: {e.Message}");
            return new List<TestModel>();
        }
    }

    // Test-related methods
    public async Task<TestModel> GetTestModel(string testId)
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("[FirebaseDatabaseManager] Firebase not initialized");
            return null;
        }

        try
        {
            var dataSnapshot = await databaseReference.Child("tests").Child(testId).GetValueAsync();
            if (dataSnapshot.Exists)
            {
                string json = dataSnapshot.GetRawJsonValue();
                return JsonUtility.FromJson<TestModel>(json);
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Error getting test: {e.Message}");
            return null;
        }
    }

    public async Task<List<TestModel>> GetTestsForStudent(string studentId)
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("[FirebaseDatabaseManager] Firebase not initialized");
            return new List<TestModel>();
        }

        try
        {
            var dataSnapshot = await databaseReference.Child("tests")
                .OrderByChild("isActive").EqualTo("active").GetValueAsync();
            
            List<TestModel> tests = new List<TestModel>();
            
            foreach (DataSnapshot child in dataSnapshot.Children)
            {
                string json = child.GetRawJsonValue();
                TestModel test = JsonUtility.FromJson<TestModel>(json);
                
                // Check if this test is for this student (you might need additional logic here)
                if (test != null && test.isActive)
                {
                    tests.Add(test);
                }
            }
            
            return tests;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Error getting tests for student: {e.Message}");
            return new List<TestModel>();
        }
    }

    // Answer-related methods
    public async Task<AnswerModel> GetAnswerModel(string answerId)
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("[FirebaseDatabaseManager] Firebase not initialized");
            return null;
        }

        try
        {
            var dataSnapshot = await databaseReference.Child("answers").Child(answerId).GetValueAsync();
            if (dataSnapshot.Exists)
            {
                string json = dataSnapshot.GetRawJsonValue();
                return JsonUtility.FromJson<AnswerModel>(json);
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Error getting answer: {e.Message}");
            return null;
        }
    }

    public async Task<bool> SaveAnswerModel(string answerId, AnswerModel answerModel)
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("[FirebaseDatabaseManager] Firebase not initialized");
            return false;
        }

        try
        {
            string json = JsonUtility.ToJson(answerModel);
            await databaseReference.Child("answers").Child(answerId).SetRawJsonValueAsync(json);
            Debug.Log($"[FirebaseDatabaseManager] Answer saved successfully: {answerId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Error saving answer: {e.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateAnswerProgress(string answerId, string taskType, string difficulty, object answer, bool isCorrect, int attemptsUsed, int score)
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("[FirebaseDatabaseManager] Firebase not initialized");
            return false;
        }

        try
        {
            var updates = new Dictionary<string, object>();
            string basePath = $"answers/{answerId}/answers/{taskType}/{difficulty}/";
            
            updates[basePath + "studentAnswer"] = answer;
            updates[basePath + "isCorrect"] = isCorrect;
            updates[basePath + "attemptsUsed"] = attemptsUsed;
            updates[basePath + "score"] = score;
            
            await databaseReference.UpdateChildrenAsync(updates);
            Debug.Log($"[FirebaseDatabaseManager] Progress updated for {taskType}/{difficulty}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Error updating progress: {e.Message}");
            return false;
        }
    }

    // Save student test results to Firebase
    public async Task<bool> SaveStudentTestResults(string studentId, string testId, string teacherId, AnswerModel answerModel)
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("[FirebaseDatabaseManager] Firebase not initialized");
            return false;
        }

        try
        {
            // Generate a unique answer ID using timestamp
            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            string answerId = timestamp.ToString();
            
            // Set required fields to match Firebase structure
            answerModel.studentId = studentId;
            answerModel.idTeacher = teacherId;
            answerModel.gameId = testId;
            answerModel.date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
            answerModel.isSent = true;
            
            // ADDED: Add debug logging to verify structure
            Debug.Log($"[FirebaseDatabaseManager] Saving answer with ID: {answerId}");
            Debug.Log($"[FirebaseDatabaseManager] Student: {studentId}, Teacher: {teacherId}, Test: {testId}");
            Debug.Log($"[FirebaseDatabaseManager] Total score: {answerModel.totalScore}");
            Debug.Log($"[FirebaseDatabaseManager] Statistics: correct={answerModel.statistics.correctAnswersCount}, incorrect={answerModel.statistics.incorrectAnswersCount}");
            
            string json = JsonUtility.ToJson(answerModel, true);
            Debug.Log($"[FirebaseDatabaseManager] JSON to save: {json}");
            
            // FIXED: Save to "Answers" collection (capital A to match your database)
            await databaseReference.Child("Answers").Child(answerId).SetRawJsonValueAsync(json);
            
            Debug.Log($"[FirebaseDatabaseManager] Student test results saved successfully: {answerId}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseDatabaseManager] Error saving student test results: {e.Message}");
            Debug.LogError($"[FirebaseDatabaseManager] Stack trace: {e.StackTrace}");
            return false;
        }
    }
}

[Serializable]
public class StudentInfo
{
    public string id;
    public string name;
    public string classId;
    public string email;
    public bool isActive;
    public string firstName;
    public string lastName;
    public string password;
    public string linkedTeacherId;
    public string schoolGrade;
    public string linkedSchoolId;
    public string role;
}

// NEW: Class information model
[Serializable]
public class ClassInfo
{
    public string id;
    public string name;
    public string niveau;
    public string createdAt;
    public string updatedAt;
}
