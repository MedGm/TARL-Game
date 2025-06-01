using UnityEngine;
using System.Threading.Tasks;
using Firebase.Database;
using System;

public class FirebaseDatabaseManager : MonoBehaviour
{
    public async Task<GameModel> GetGameModel(string gameId)
    {
        var dataSnapshot = await FirebaseDatabase.DefaultInstance.GetReference("games").Child(gameId).GetValueAsync();
        if (dataSnapshot.Exists)
        {
            string json = dataSnapshot.GetRawJsonValue();
            return JsonUtility.FromJson<GameModel>(json);
        }
        return null;
    }

    public async Task<AnswerModel> GetAnswerModel(string answerId)
    {
        var dataSnapshot = await FirebaseDatabase.DefaultInstance.GetReference("answers").Child(answerId).GetValueAsync();
        if (dataSnapshot.Exists)
        {
            string json = dataSnapshot.GetRawJsonValue();
            return JsonUtility.FromJson<AnswerModel>(json);
        }
        return null;
    }
}
