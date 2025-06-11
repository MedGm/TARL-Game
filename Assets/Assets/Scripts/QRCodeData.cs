using System;
using UnityEngine;

[System.Serializable]
public class QRCodeData
{
    public string uid;
    public string pin;
    
    // Derived properties populated after database verification
    [System.NonSerialized] public string studentName = "";
    [System.NonSerialized] public string classId = "";
    [System.NonSerialized] public string teacherId = "";
    [System.NonSerialized] public string testId = "";
    [System.NonSerialized] public string linkedSchoolId = "";
    
    // Backward compatibility properties
    public string studentId => uid;
    
    public QRCodeData()
    {
    }
    
    public QRCodeData(string uid, string pin)
    {
        this.uid = uid;
        this.pin = pin;
    }
    
    public static QRCodeData FromJson(string jsonString)
    {
        try
        {
            Debug.Log($"[QRCodeData] Parsing QR JSON: {jsonString}");
            QRCodeData data = JsonUtility.FromJson<QRCodeData>(jsonString);
            
            if (data != null && !string.IsNullOrEmpty(data.uid) && !string.IsNullOrEmpty(data.pin))
            {
                Debug.Log($"[QRCodeData] Successfully parsed QR data for student: {data.uid}");
                return data;
            }
            else
            {
                Debug.LogError("[QRCodeData] Parsed QR data is invalid - missing uid or pin");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[QRCodeData] Failed to parse QR JSON: {e.Message}");
            return null;
        }
    }
    
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(pin);
    }
    
    public string GetStudentId()
    {
        return uid;
    }
    
    public string GetPin()
    {
        return pin;
    }
}
