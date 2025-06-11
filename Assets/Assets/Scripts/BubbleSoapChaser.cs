using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BubbleSoapChaser : MonoBehaviour
{
    public float speed = 3f;
    private Transform player;
    private bool chasing = false;

    public string soapTaskSceneName = "SoapScene"; // Default, will be set by GameManager

    // --- NEW: Animator reference for bubble animation ---
    private Animator animator;

    // Minimap icon (assign a sprite in the inspector, set to Minimap layer)
    public GameObject minimapIcon;

    void Start()
    {
        // Ensure collider is trigger and Rigidbody2D is Dynamic
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
        // Always spawn at Z=0
        Vector3 pos = transform.position;
        pos.z = 0;
        transform.position = pos;

        // --- Animator setup for Unity Animator (not Legacy Animation) ---
        animator = GetComponent<Animator>();
        // If you previously used Animation component, REMOVE it from the prefab!
        // Only use Animator with an Animator Controller for frame animation.

        // --- NEW: Get Animator and play animation if needed ---
        // If you want to trigger a specific animation, you can do:
        // if (animator != null) animator.Play("AirSoapBubble");

        // Start chasing after a short delay to ensure player is present
        Invoke(nameof(ActivateChase), 0.5f);

        if (minimapIcon != null)
            minimapIcon.transform.SetParent(transform, false);
    }

    void Update()
    {
        if (!chasing) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                Debug.Log("[BubbleSoapChaser] Found player.");
            }
            else
            {
                return;
            }
        }

        // Only set bubble's Z=0, do not touch player Z
        Vector3 myPos = transform.position;
        myPos.z = 0;
        transform.position = myPos;

        // Move towards player
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
    }

    public void ActivateChase()
    {
        chasing = true;
        Debug.Log("[BubbleSoapChaser] Activated and chasing player.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!chasing) return;
        if (other.CompareTag("Player"))
        {
            chasing = false;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DisableWarningTextIfExists();
                GameManager.Instance.lastPlayerPosition = other.transform.position;
            }
                
            // FIXED: Use Pokemon-style transition for soap encounters (battle-like effect)
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.PokemonStyleTransition(soapTaskSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(soapTaskSceneName);
            }
            
            Destroy(gameObject);
        }
    }
}
