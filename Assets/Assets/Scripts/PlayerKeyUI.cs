using UnityEngine;
using TMPro;

public class PlayerKeyUI : MonoBehaviour
{
    public TMP_Text keysCollectedText;

    void Update()
    {
        if (GameManager.Instance != null)
        {
            keysCollectedText.text = $"Clés obtenues : {GameManager.Instance.keysCollected}";
        }
    }
}
