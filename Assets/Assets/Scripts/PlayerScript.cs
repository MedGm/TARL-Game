using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    // Singleton
    public static PlayerScript Instance { get; private set; }

    public float movementSpeed = 5f;
    private bool isMoving;
    private Vector2 position;

    private Animator animator;
    public Transform spriteTransform;
    private int facingDirection = 1;
    public LayerMask solidObjectsLayer;
    public float collisionRadius = 0.2f;

    private Vector2 joystickInput = Vector2.zero;

    // Joystick reference
    public Joystick joystick; // Assign in Inspector

    // State design pattern - Player states
    private PlayerState currentState;
    private IdleState idleState;
    private MovingState movingState;

    // Audio components
    private AudioSource audioSource;
    public AudioClip footstepSound;
    [Range(0f, 1f)]
    public float footstepVolume = 2f;
    [Range(0f, 2f)]
    public float footstepRate = 0.1f; // Time between footstep sounds
    private float footstepTimer = 0f;

    // Minimap icon (assign a sprite in the inspector, set to Minimap layer)
    public GameObject minimapIcon;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Initialize states
        idleState = new IdleState(this);
        movingState = new MovingState(this);
        currentState = idleState;

        // Get audio source or add one if missing
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        animator = GetComponent<Animator>();

        // Restore player position if coming back from dungeon
        if (GameManager.Instance != null && GameManager.Instance.lastPlayerPosition != Vector3.zero)
        {
            transform.position = GameManager.Instance.lastPlayerPosition;
        }

        // Make sure we start in the idle state
        currentState.Enter();

        // Set up audio source defaults
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = footstepVolume;

        if (minimapIcon != null)
            minimapIcon.transform.SetParent(transform, false);
    }

    void Update()
    {
        // Use joystick input for movement
        Vector2 movement = new Vector2(joystick.Horizontal, joystick.Vertical);

        // Handle state transitions
        if (movement.sqrMagnitude > 0)
        {
            Vector2 targetPosition = (Vector2)transform.position + movement * movementSpeed * Time.deltaTime;

            if (IsWalkable(targetPosition))
            {
                if (currentState != movingState)
                {
                    ChangeState(movingState);
                }
            }
            else if (currentState != idleState)
            {
                ChangeState(idleState);
            }
        }
        else if (currentState != idleState)
        {
            ChangeState(idleState);
        }

        // Update the current state
        currentState.Update(movement);

        // Handle sprite flipping
        HandleSpriteFlipping(movement.x);

        // Update footstep timer
        UpdateFootstepTimer();

        // Update the player's position in GameManager for chaser tracking
        if (GameManager.Instance != null)
            GameManager.Instance.lastPlayerPosition = transform.position;
    }

    // Called by UI joystick (e.g., OnDrag event)
    public void SetJoystickInput(Vector2 input)
    {
        joystickInput = input;
    }

    public void ChangeState(PlayerState newState)
    {
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void HandleSpriteFlipping(float moveX)
    {
        if (spriteTransform != null)
        {
            if (moveX < 0 && facingDirection != -1)
            {
                Vector3 scale = spriteTransform.localScale;
                scale.x = Mathf.Abs(scale.x) * -1;
                spriteTransform.localScale = scale;
                facingDirection = -1;
            }
            else if (moveX > 0 && facingDirection != 1)
            {
                Vector3 scale = spriteTransform.localScale;
                scale.x = Mathf.Abs(scale.x);
                spriteTransform.localScale = scale;
                facingDirection = 1;
            }
        }
    }

    public bool IsWalkable(Vector2 targetPosition)
    {
        Collider2D collision = Physics2D.OverlapCircle(targetPosition, collisionRadius, solidObjectsLayer);
        return collision == null;
    }

    public void SetAnimationState(bool moving)
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", moving);
            isMoving = moving;
        }
    }

    public void MovePlayer(Vector2 movement)
    {
        transform.Translate(movement * movementSpeed * Time.deltaTime);
        position = transform.position;
    }

    // Footstep sound functions
    public void PlayFootstepSound()
    {
        if (footstepSound != null && audioSource != null && !audioSource.isPlaying)
        {
            if (footstepTimer <= 0)
            {
                audioSource.clip = footstepSound;
                audioSource.volume = footstepVolume;
                audioSource.Play();
                footstepTimer = footstepRate;
            }
        }
    }

    public void UpdateFootstepTimer()
    {
        if (footstepTimer > 0)
            footstepTimer -= Time.deltaTime;
    }

    public void StopFootstepSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}