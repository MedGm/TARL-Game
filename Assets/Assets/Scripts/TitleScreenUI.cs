using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class TitleScreenUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button startButton;
    [SerializeField] private string overworldSceneName = "overworldScene"; // Set this to your overworld map scene

    private void Start()
    {
        // Title styling
        if (titleText != null)
        {
            titleText.text = "Number-Game";
            titleText.fontSize = 180; // Huge for 1920x1440
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.74f, 0.18f); // Yellow-orange (#FFBD2E)
            titleText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            titleText.outlineWidth = 0.35f;
            titleText.outlineColor = new Color(0.36f, 0.22f, 0.09f); // Brown border (#5C3917)
            // Optionally add a shadow in the editor for more depth
        }

        // Start button setup: use your playbutton PNG sprite, no TMP text or styling needed
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);

            // Remove TMP_Text child if present (since the sprite already has "Start" text)
            TMP_Text btnText = startButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                Destroy(btnText.gameObject);

            // Optionally, set the button image to your playbutton PNG in the editor
            // No further styling needed
        }

        // Always reset or initialize progression on title screen
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentProgression = GameManager.ProgressionStage.Overworld;
        }
    }

    private void OnStartClicked()
    {
        // Always load the overworld map (overworldScene) as the first step
        SceneTransition.Instance.TransitionToScene(overworldSceneName);
    }
}
