using System.Collections.Generic;
using UnityEngine;

public static class DungeonManager
{
    private static readonly List<DungeonEntrance> allEntrances = new List<DungeonEntrance>();
    private static DungeonEntrance lastEnteredEntrance = null;

    public static void RegisterEntrance(DungeonEntrance entrance)
    {
        if (!allEntrances.Contains(entrance))
            allEntrances.Add(entrance);
    }

    public static void UnregisterEntrance(DungeonEntrance entrance)
    {
        if (allEntrances.Contains(entrance))
            allEntrances.Remove(entrance);
        if (lastEnteredEntrance == entrance)
            lastEnteredEntrance = null;
    }

    public static void EnterDungeon(DungeonEntrance entrance)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.lastPlayerPosition = entrance.transform.position;
            // No need to set currentDungeonId anymore
            Debug.Log("[DungeonManager] About to load scene: " + entrance.dungeonSceneName + " (from " + entrance.gameObject.name + ")");
        }
        Debug.Log("[DungeonManager] Loading scene: " + entrance.dungeonSceneName);
        UnityEngine.SceneManagement.SceneManager.LoadScene(entrance.dungeonSceneName);
    }
}
