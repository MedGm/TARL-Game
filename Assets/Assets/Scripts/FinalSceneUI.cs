using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FinalSceneUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text congratsTitleText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private Button exitButton;

    [Header("Settings")]
    [SerializeField] private string congratsTitle = "Félicitations !";
    [SerializeField] private string congratsMessage = "Bravo, tu as terminé le jeu !";

    private void Start()
    {
        // Show congrats title
        if (congratsTitleText != null)
        {
            congratsTitleText.text = congratsTitle;
            congratsTitleText.fontSize = 120;
            congratsTitleText.alignment = TextAlignmentOptions.Center;
            congratsTitleText.color = new Color(1f, 0.85f, 0.2f); // Gold/yellow
            congratsTitleText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            congratsTitleText.outlineWidth = 0.3f;
            congratsTitleText.outlineColor = new Color(0.36f, 0.22f, 0.09f); // Brown border
        }

        // Show only a congratulatory message, no result
        if (summaryText != null)
        {
            summaryText.text = congratsMessage;
            summaryText.fontSize = 60;
            summaryText.alignment = TextAlignmentOptions.Center;
            summaryText.color = Color.white;
        }

        // Exit button closes the game or returns to title
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitClicked);
        }
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
