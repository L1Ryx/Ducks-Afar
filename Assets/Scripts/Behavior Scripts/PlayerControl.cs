using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControl : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField, Min(1f)] private float sprintSpeedMultiplier = 2f;

    // [Header("Events")] 
    // [SerializeField] private UnityEvent lockInteractions;
    // [SerializeField] private UnityEvent unlockInteractions;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float inputDeadzone = 0.15f;
    [SerializeField] private float movingVelocityThreshold = 0.001f;
    [Header("Locking")] [SerializeField] private bool shouldLockFacingDirectionChangeWhenInteractionLocked = true;

    private Rigidbody2D rb;

    private Vector2 moveInput;
    private Vector2 moveVector;
    private bool isSprinting;

    // 0=Down, 1=Up, 2=Left, 3=Right
    private int facing = 0;

    // 0=none, 1=vertical, 2=horizontal
    private int lockedAxis = 0;
    private CapsuleCollider2D capsule;
    
    [Header("Debug Console")]
    public bool IsNoclipEnabled { get; private set; }
    public bool IsHyperspeedEnabled { get; private set; }
    [SerializeField] private float hyperspeedMultiplier = 3f;
    // Add near other fields:
    [Header("Events")]
    [SerializeField] private UnityEvent OnPlayerMoved;
    [SerializeField] private bool invokeOnPlayerMovedEveryFrameWhileMoving = false;

    private bool wasMovingLastFrame = false;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        capsule = GetComponent<CapsuleCollider2D>();

        if (capsule == null)
        {
            Debug.LogError($"No Player Capsule Collider");
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        ApplyCollisionState();
        IsHyperspeedEnabled = false;
    }
    
    public void ToggleNoclip()
    {
        IsNoclipEnabled = !IsNoclipEnabled;
        ApplyCollisionState();
    }

    public void ToggleHyperspeed()
    {
        if (IsHyperspeedEnabled)
        {
            moveSpeed /= hyperspeedMultiplier;
        }
        else
        {
            moveSpeed *= hyperspeedMultiplier;
        }
        IsHyperspeedEnabled = !IsHyperspeedEnabled;
    }

    public void SetNoclip(bool enabled)
    {
        IsNoclipEnabled = enabled;
        ApplyCollisionState();
    }

    private void ApplyCollisionState()
    {
        // noclip ON  => collider disabled
        // noclip OFF => collider enabled
        capsule.enabled = !IsNoclipEnabled;
    }

    private void FixedUpdate()
    {
        if (Game.IsReady && Game.Ctx?.InteractionLock?.IsLocked == true)
        {
            rb.linearVelocity = Vector2.zero;   
            moveVector = Vector2.zero;          // optional, for clarity/debug
            return;
        }

        moveVector = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
        float speedMultiplier = isSprinting ? sprintSpeedMultiplier : 1f;
        rb.linearVelocity = moveVector * moveSpeed * speedMultiplier;
    }


    private void Update()
    {
        if (Game.IsReady && Game.Ctx?.InteractionLock?.IsLocked == true && shouldLockFacingDirectionChangeWhenInteractionLocked)
        {
            return; // THIS IS A DESIGN CHOICE???
        }
        UpdateFacingLock(moveInput);
    }

    private void LateUpdate()
    {
        bool isMoving = rb.linearVelocity.sqrMagnitude > movingVelocityThreshold;
        animator.SetBool("IsMoving", isMoving);
        animator.SetInteger("Facing", facing);

        if (OnPlayerMoved != null)
        {
            if (invokeOnPlayerMovedEveryFrameWhileMoving)
            {
                if (isMoving)
                    OnPlayerMoved.Invoke();
            }
            else
            {
                // Invoke once when movement begins (not every frame)
                if (isMoving && !wasMovingLastFrame)
                    OnPlayerMoved.Invoke();
            }
        }

        wasMovingLastFrame = isMoving;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (PauseUtility.IsPaused)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (PauseUtility.IsPaused || context.canceled)
        {
            isSprinting = false;
            return;
        }

        isSprinting = context.ReadValueAsButton();
    }

    // public void LockInteractions()
    // {
    //     lockInteractions?.Invoke();
    // }
    //
    // public void unLockInteractions()
    // {
    //     unlockInteractions?.Invoke();
    // }

    private void UpdateFacingLock(Vector2 input)
    {
        float x = input.x;
        float y = input.y;

        bool hasX = Mathf.Abs(x) > inputDeadzone;
        bool hasY = Mathf.Abs(y) > inputDeadzone;

        if (lockedAxis == 0)
        {
            if (!hasX && !hasY) return;

            if (hasY && (!hasX || Mathf.Abs(y) >= Mathf.Abs(x)))
                lockedAxis = 1;
            else
                lockedAxis = 2;
        }

        if (lockedAxis == 1)
        {
            if (hasY)
            {
                facing = (y > 0f) ? 1 : 0;
                return;
            }

            lockedAxis = 0;
            UpdateFacingLock(input);
            return;
        }

        if (lockedAxis == 2)
        {
            if (hasX)
            {
                facing = (x > 0f) ? 3 : 2;
                return;
            }

            lockedAxis = 0;
            UpdateFacingLock(input);
        }
    }
    private void OnEnable()
    {
        if (Game.IsReady && Game.Ctx?.InteractionLock != null)
            Game.Ctx.InteractionLock.OnLockChanged += HandleLockChanged;
    }

    private void OnDisable()
    {
        if (Game.IsReady && Game.Ctx?.InteractionLock != null)
            Game.Ctx.InteractionLock.OnLockChanged -= HandleLockChanged;
    }

    private void HandleLockChanged(bool locked)
    {
        if (locked)
        {
            moveInput = Vector2.zero;
            moveVector = Vector2.zero;
            isSprinting = false;
            rb.linearVelocity = Vector2.zero;
            lockedAxis = 0; 
        }
    }
}
