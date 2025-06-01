using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.7f;
    [SerializeField] private Color fadeColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Animator specialTransitionAnimator; // Assign in inspector (optional)
    [SerializeField] private string specialTransitionTrigger = "PlayTransition"; // Animator trigger

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(true);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(FadeAndSwitchScenes(sceneName));
    }

    public void PokemonStyleTransition(string sceneName)
    {
        StartCoroutine(PokemonTransitionAndSwitchScenes(sceneName));
    }

    private IEnumerator FadeAndSwitchScenes(string sceneName)
    {
        // Fade out
        yield return StartCoroutine(Fade(0f, 1f));
        // Load scene
        SceneManager.LoadScene(sceneName);
        // Wait one frame for scene to load
        yield return null;
        // Fade in
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator PokemonTransitionAndSwitchScenes(string sceneName)
    {
        // Play special transition animation
        if (specialTransitionAnimator != null)
        {
            specialTransitionAnimator.SetTrigger(specialTransitionTrigger);
            // Wait for animation to finish (adjust duration as needed)
            yield return new WaitForSeconds(1.2f);
        }
        else
        {
            // Fallback to normal fade
            yield return StartCoroutine(Fade(0f, 1f));
        }

        SceneManager.LoadScene(sceneName);
        yield return null;
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        if (fadeImage == null)
            yield break;

        float timer = 0f;
        Color color = fadeColor;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;
            yield return null;
        }
        color.a = endAlpha;
        fadeImage.color = color;
    }
}
