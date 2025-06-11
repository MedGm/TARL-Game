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
    [SerializeField] private Image pokemonFlashImage; // White flash image for Pokemon effect
    [SerializeField] private AudioSource pokemonTransitionSound; // Optional battle sound

    private bool isTransitioning = false; // ADDED: Transition state flag

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
                fadeImage.raycastTarget = false;
            }
            
            // FIXED: Better Pokemon flash image validation and setup
            if (pokemonFlashImage != null)
            {
                pokemonFlashImage.gameObject.SetActive(true);
                pokemonFlashImage.color = new Color(1f, 1f, 1f, 0f); // White, transparent
                pokemonFlashImage.raycastTarget = false;
                Debug.Log("[SceneTransition] Pokemon flash image initialized successfully");
            }
            else
            {
                Debug.LogWarning("[SceneTransition] Pokemon flash image not assigned! Creating fallback...");
                CreateFallbackPokemonFlash();
            }
            
            // FIXED: Validate AudioSource
            if (pokemonTransitionSound != null)
            {
                pokemonTransitionSound.playOnAwake = false;
                Debug.Log("[SceneTransition] Pokemon transition sound initialized");
            }
            else
            {
                Debug.LogWarning("[SceneTransition] Pokemon transition sound not assigned! Pokemon transitions will be silent.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ADDED: Create fallback Pokemon flash if not assigned
    private void CreateFallbackPokemonFlash()
    {
        // Find or create a Canvas for the flash effect
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        
        if (canvas != null)
        {
            // Create a new GameObject for the flash
            GameObject flashGO = new GameObject("PokemonFlashImage_Auto");
            flashGO.transform.SetParent(canvas.transform, false);
            
            // Add Image component
            pokemonFlashImage = flashGO.AddComponent<UnityEngine.UI.Image>();
            
            // Set up the Image
            RectTransform rt = flashGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            
            pokemonFlashImage.color = new Color(1f, 1f, 1f, 0f);
            pokemonFlashImage.raycastTarget = false;
            
            Debug.Log("[SceneTransition] Created fallback Pokemon flash image");
        }
        else
        {
            Debug.LogError("[SceneTransition] No Canvas found for creating Pokemon flash image!");
        }
    }

    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning) return;
        
        // FIXED: Scene name mapping for compatibility
        string actualSceneName = GetActualSceneName(sceneName);
        
        Debug.Log($"[SceneTransition] Transitioning from '{sceneName}' to actual scene '{actualSceneName}'");
        StartCoroutine(TransitionCoroutine(actualSceneName));
    }
    
    public void PokemonStyleTransition(string sceneName)
    {
        Debug.Log($"[SceneTransition] Pokemon-style transition to: {sceneName}");
        
        // FIXED: Check if this is actually a soap scene
        if (IsSoapScene(sceneName))
        {
            StartCoroutine(PokemonTransitionAndSwitchScenes(sceneName));
        }
        else
        {
            // Fallback to normal transition for non-soap scenes
            Debug.Log($"[SceneTransition] {sceneName} is not a soap scene, using standard transition");
            StartCoroutine(FadeAndSwitchScenes(sceneName));
        }
    }

    // FIXED: Helper method to identify soap scenes
    private bool IsSoapScene(string sceneName)
    {
        string lowerSceneName = sceneName.ToLower();
        return lowerSceneName.Contains("soap") || 
               lowerSceneName.Contains("bubble") ||
               lowerSceneName == "SoapScene" ||
               lowerSceneName == "SoapScene2" ||
               lowerSceneName == "SoapScene3";
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true; // Set flag to prevent re-entrance

        yield return StartCoroutine(Fade(0f, 1f));
        SceneManager.LoadScene(sceneName);
        yield return null;
        yield return StartCoroutine(Fade(1f, 0f));

        isTransitioning = false; // Reset flag after transition
    }

    private IEnumerator FadeAndSwitchScenes(string sceneName)
    {
        yield return StartCoroutine(Fade(0f, 1f));
        SceneManager.LoadScene(sceneName);
        yield return null;
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator PokemonTransitionAndSwitchScenes(string sceneName)
    {
        // ENHANCED: Eye-friendly Pokemon flash effect with proper scene transition coverage
        if (pokemonFlashImage != null)
        {
            Debug.Log("[SceneTransition] Playing enhanced Pokemon flash effect");
            
            // Play sound effect if available
            if (pokemonTransitionSound != null)
            {
                pokemonTransitionSound.Play();
            }
            
            yield return StartCoroutine(EnhancedPokemonFlashEffect(sceneName));
        }
        else
        {
            Debug.LogWarning("[SceneTransition] Pokemon flash image missing, using fallback fade");
            yield return StartCoroutine(FadeAndSwitchScenes(sceneName));
        }
    }

    // ENHANCED: More eye-friendly and properly timed Pokemon effect
    private IEnumerator EnhancedPokemonFlashEffect(string sceneName)
    {
        pokemonFlashImage.raycastTarget = true;
        
        // PHASE 1: Quick warning flashes (eye-friendly)
        for (int i = 0; i < 2; i++)
        {
            // Gentle flash on (reduced opacity)
            pokemonFlashImage.color = new Color(1f, 1f, 1f, 0.6f);
            yield return new WaitForSeconds(0.06f);
            
            // Flash off
            pokemonFlashImage.color = new Color(1f, 1f, 1f, 0f);
            yield return new WaitForSeconds(0.06f);
        }
        
        // PHASE 2: Build-up flash (slightly stronger)
        pokemonFlashImage.color = new Color(1f, 1f, 1f, 0.8f);
        yield return new WaitForSeconds(0.1f);
        pokemonFlashImage.color = new Color(1f, 1f, 1f, 0f);
        yield return new WaitForSeconds(0.05f);
        
        // PHASE 3: Final transition flash - covers scene loading
        pokemonFlashImage.color = new Color(1f, 1f, 1f, 1f);
        yield return new WaitForSeconds(0.1f);
        
        // CRITICAL: Load scene while screen is completely white
        SceneManager.LoadScene(sceneName);
        yield return null; // Wait one frame for scene to start loading
        yield return new WaitForSeconds(0.3f); // Hold white screen during scene switch
        
        // PHASE 4: Gentle fade out from white
        float fadeOutTime = 0.4f;
        float timer = 0f;
        
        while (timer < fadeOutTime)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            pokemonFlashImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        
        // Final cleanup
        pokemonFlashImage.color = new Color(1f, 1f, 1f, 0f);
        pokemonFlashImage.raycastTarget = false;
        
        Debug.Log("[SceneTransition] Enhanced Pokemon flash effect completed");
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        if (fadeImage == null) yield break;

        // Only block interactions during actual fade
        fadeImage.raycastTarget = (startAlpha > 0f || endAlpha > 0f);

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

        // Disable blocking when completely transparent
        if (endAlpha <= 0f)
        {
            fadeImage.raycastTarget = false;
        }
    }

    // ADDED: Helper method to map scene names to actual build settings names
    private string GetActualSceneName(string requestedScene)
    {
        // Map common scene name variations to actual build settings names
        switch (requestedScene.ToLower())
        {
            case "overworld":
            case "overworldscene":
            case "overworld scene":
                return "overworldScene"; // FIXED: Use correct scene name
                
            case "title":
            case "titlescene":
            case "title scene":
                return "TitleScene";
                
            case "dungeon":
            case "dungeon1":
            case "dungeonscene":
                return "DungeonScene";
                
            case "dungeon2":
            case "dungeonscene2":
                return "DungeonScene2";
                
            case "dungeon3":
            case "dungeonscene3":
                return "DungeonScene3";
                
            case "soap":
            case "soap1":
            case "soapscene":
                return "SoapScene";
                
            case "soap2":
            case "soapscene2":
                return "SoapScene2";
                
            case "soap3":
            case "soapscene3":
                return "SoapScene3";
                
            case "final":
            case "finalscene":
                return "FinalScene";
                
            case "portal":
            case "portalscene":
                return "PortalScene";
                
            default:
                return requestedScene; // Return as-is if no mapping found
        }
    }
}
