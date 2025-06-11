using System;

[Serializable]
public class UserData
{
    public string firstName;
    public string lastName;
    public string email;
    public string role;
    public string schoolGrade;      // This is a class ID like "class_1747581106549"
    public string linkedClassId;
    public string linkedTeacherId;
    public string linkedSchoolId;   // This is the actual school name like "Al Amal School"
    public string password;
    public bool frozen;
    public string uid;
    public string birthday;
    public string gender;
    public string createdAt;
    public string qrCode;
}
