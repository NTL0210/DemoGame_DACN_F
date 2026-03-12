using UnityEngine;

/// <summary>
/// Enemy Movement - Phiên bản đơn giản
/// </summary>
public class EnemyMove : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.5f;
    [SerializeField] private float rotationThreshold = 0.3f;
    
    [Header("Separation")]
    [SerializeField] private float separationWeight = 0.3f; // trọng số trộn vector tách từ EnemyCollisionAvoidance
    
    [Header("Animation")]
    [SerializeField] private string moveParameter = "Move";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    
    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private EnemyCollisionAvoidance avoidance;
    
    // State
    private Transform player;
    private bool isMoving = false;
    private int facingDirection = -1;

    // State for Event Mode
    private bool isInEventMode = false;
    private Vector2 eventMoveDirection;
    private float originalLinearDamping;
    private float originalStopDistance;
    private float? speedOverride = null; // Tốc độ tạm thời được ưu tiên hơn moveSpeed

    // Lấy tốc độ hiện tại, ưu tiên tốc độ override
    private float CurrentSpeed => speedOverride ?? moveSpeed;

    /// <summary>
    /// Ghi đè tốc độ di chuyển mặc định. Gửi 'null' để xóa ghi đè.
    /// </summary>
    public void SetSpeedOverride(float? speed)
    {
        speedOverride = speed;
    }
    
    public float MoveSpeed => moveSpeed;
    public bool IsMoving => isMoving;
    public int FacingDirection => facingDirection;
    
    public void EnableEventMode(Vector2 direction)
    {
        isInEventMode = true;
        eventMoveDirection = direction.normalized;

        // Store original Rigidbody settings
        if (rb != null)
        {
            originalLinearDamping = rb.linearDamping;
            rb.linearDamping = 0f; // No damping for smooth, constant movement
        }

        // Store and override stop distance
        originalStopDistance = stopDistance;
        stopDistance = -1f; // Ensure it never stops near the player

        UpdateFacingDirection(eventMoveDirection.x);
    }

    public void DisableEventMode()
    {
        isInEventMode = false;
        SetSpeedOverride(null); // Xóa ghi đè tốc độ

        // Restore original Rigidbody settings
        if (rb != null)
        {
            rb.linearDamping = originalLinearDamping;
        }

        // Restore stop distance
        stopDistance = originalStopDistance;
    }

    private void Awake()
    {
        // Cache components tránh GetComponent nhiều lần
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        avoidance = GetComponent<EnemyCollisionAvoidance>();
        
        // Cấu hình Rigidbody2D cho top-down movement
        if (rb != null)
        {
            rb.gravityScale = 0f; // Không bị ảnh hưởng bởi gravity (top-down 2D)
            rb.linearDamping = 5f; // Damping để dừng mượt mà
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Khóa rotation để không bị xoay
        }
        else
        {
            Debug.LogWarning($"EnemyMove on {gameObject.name} không tìm thấy Rigidbody2D!");
        }
    }
    
    private void Start()
    {
        FindPlayer();
        SetFacingDirection(-1);
    }
    
    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }
        
        UpdateAnimation();
    }
    
    /// <summary>
    /// Cập nhật di chuyển - Sử dụng FixedUpdate cho physics
    /// </summary>
    private void FixedUpdate()
    {
        if (rb == null) return;

        if (isInEventMode)
        {
            // Chế độ Event: Di chuyển thẳng với tốc độ không đổi
            Vector2 nextPos = rb.position + eventMoveDirection * CurrentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(nextPos);
            UpdateFacingDirection(eventMoveDirection.x);
        }
        else
        {
            // Chế độ thường: Đuổi theo người chơi
            if (player == null)
            {
                StopMoving();
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= stopDistance)
            {
                StopMoving();
                return;
            }

            Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            Vector2 separation = avoidance != null ? avoidance.GetSeparationVector() : Vector2.zero;
            Vector2 desiredDirection = (toPlayer + separationWeight * separation).normalized;

            // Sử dụng velocity để di chuyển mượt mà hơn với linear damping
            rb.linearVelocity = desiredDirection * CurrentSpeed;

            UpdateFacingDirection(toPlayer.x);
        }
    }
    
    /// <summary>
    /// Cập nhật animation
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;

        // Theo yêu cầu: Luôn bật animation di chuyển
        animator.SetBool(moveParameter, true);

        // Cập nhật trạng thái isMoving một cách riêng biệt để logic khác không bị ảnh hưởng
        float currentSpeed = rb != null ? rb.linearVelocity.magnitude : 0f;
        isMoving = currentSpeed > 0.1f;
    }
    
    /// <summary>
    /// Cập nhật hướng
    /// </summary>
    private void UpdateFacingDirection(float directionX)
    {
        if (Mathf.Abs(directionX) > rotationThreshold)
        {
            int newFacingDirection = directionX > 0 ? 1 : -1;
            
            if (newFacingDirection != facingDirection)
            {
                SetFacingDirection(newFacingDirection);
            }
        }
    }
    
    /// <summary>
    /// Thiết lập hướng
    /// </summary>
    public void SetFacingDirection(int direction)
    {
        facingDirection = direction;
        
        // Sử dụng Transform.rotation thay vì SpriteRenderer.flipX
        if (direction > 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
    
    /// <summary>
    /// Thiết lập trạng thái di chuyển
    /// </summary>
    private void SetMoving(bool moving)
    {
        isMoving = moving;
    }
    
    /// <summary>
    /// Bắt đầu di chuyển
    /// </summary>
    public void StartMoving()
    {
        SetMoving(true);
    }
    
    /// <summary>
    /// Dừng di chuyển
    /// </summary>
    public void StopMoving()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        SetMoving(false);
    }
    
    /// <summary>
    /// Tìm player
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }
    
    /// <summary>
    /// Thiết lập tốc độ di chuyển
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }
    
    /// <summary>
    /// Thiết lập khoảng cách dừng
    /// </summary>
    public void SetStopDistance(float distance)
    {
        stopDistance = Mathf.Max(0f, distance);
    }
    
    /// <summary>
    /// Lấy khoảng cách đến player
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector2.Distance(transform.position, player.position);
    }
    
    /// <summary>
    /// Lấy velocity hiện tại
    /// </summary>
    public Vector2 GetCurrentVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Vẽ hướng di chuyển
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
        }
        
        // Vẽ khoảng cách dừng
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        
        // Vẽ hướng đến player
        if (player != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}