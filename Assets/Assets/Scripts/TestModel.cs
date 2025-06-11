using System;

[Serializable]
public class TestModel
{
    public string id;
    public string idTeacher;
    public bool isActive;
    public bool isSent;
    public TestTitle title;
    public TestDescription description;
    public string endDate;
    public string[] games;
    public string @class; // "class" is a reserved keyword, so use @class
    public long createdAt;
    public long updatedAt;
    public string status;
    
    [Serializable]
    public class TestTitle
    {
        public string ar;
        public string en;
        public string fr;
    }
    
    [Serializable] 
    public class TestDescription
    {
        public string fr;
    }
    
    // Helper method to get title in French (fallback to English, then ID)
    public string GetTitle()
    {
        if (title?.fr != null) return title.fr;
        if (title?.en != null) return title.en;
        return id ?? "Test";
    }
}

// REMOVED: Config classes moved to TaskConfigs.cs
