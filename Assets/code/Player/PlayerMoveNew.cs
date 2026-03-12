using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player Controller với State Machine Pattern
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerMoveNew : MonoBehaviour
{
    [Header("Cài đặt Di chuyển")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Tham chiếu Component")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    
    [Header("Weapon Control")]
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private string weaponTag = "Weapon";
    
    [Header("Pointer Control")]
    [SerializeField] private PointerController pointerController;
    
    [Header("State Machine")]
    [SerializeField] private StateMachine stateMachine;
    
    [Header("Health System")]
    [SerializeField] private HealthSystem healthSystem;
    
    // Input và Movement
    private Vector2 inputVector;
    private Vector2 currentVelocity;
    public int FacingDirection { get; private set; }
    public Vector2 CurrentVelocity { get { return currentVelocity; } }
    
    // States
    private PlayerIdleState idleState;
    private PlayerMovingState movingState;
    private PlayerDeathState deathState;
    
    private void Awake()
    {
        // Lấy components
        if (animator == null)
            animator = GetComponent<Animator>();
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (stateMachine == null)
            stateMachine = GetComponent<StateMachine>();
        
        // Cấu hình Rigidbody2D cho player movement (nếu có)
        if (rb != null)
        {
            // Đảm bảo Rigidbody2D được cấu hình đúng cho top-down movement
            rb.gravityScale = 0f; // Không bị ảnh hưởng bởi gravity
            rb.linearDamping = 0f; // Không có damping để tốc độ không bị giảm
            rb.angularDamping = 0f; // Không có angular drag
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Khóa rotation
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Phát hiện va chạm liên tục
            rb.interpolation = RigidbodyInterpolation2D.None; // Không interpolation để tránh dịch chuyển lạ
        }
            
        // Tìm weapon controller
        FindWeaponController();
        
        // Khởi tạo states
        InitializeStates();
        
        FacingDirection = 1;

        // Tìm PointerController nếu chưa gán
        if (pointerController == null)
            pointerController = GetComponent<PointerController>() ?? GetComponentInChildren<PointerController>(true);
            
        // Tìm HealthSystem nếu chưa gán
        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();
    }
    
    private void Start()
    {
        // Bắt đầu với idle state
        ChangeToIdleState();
    }
    
    private void InitializeStates()
    {
        // Lấy các state như Component (hỗ trợ kéo thả vào Core/Idle, Core/Move, Core/Death)
        idleState = GetComponentInChildren<PlayerIdleState>(true);
        movingState = GetComponentInChildren<PlayerMovingState>(true);
        deathState = GetComponentInChildren<PlayerDeathState>(true);

        // Nếu chưa có component thì tạo nhanh trên chính Player để không bị null
        if (idleState == null)
            idleState = gameObject.AddComponent<PlayerIdleState>();
        if (movingState == null)
            movingState = gameObject.AddComponent<PlayerMovingState>();
        if (deathState == null)
            deathState = gameObject.AddComponent<PlayerDeathState>();
            
        // Đảm bảo state được khởi tạo đúng
    }
    
    private void FindWeaponController()
    {
        if (weaponController == null)
        {
            GameObject weaponObj = GameObject.FindGameObjectWithTag(weaponTag);
            if (weaponObj != null)
            {
                weaponController = weaponObj.GetComponent<WeaponController>();
                if (weaponController == null)
                {
                    Debug.LogWarning($"PlayerMoveNew: Tìm thấy object với tag '{weaponTag}' nhưng không có WeaponController component!");
                }
            }
            else
            {
                Debug.LogWarning($"PlayerMoveNew: Không tìm thấy object với tag '{weaponTag}'. Vũ khí sẽ không hoạt động!");
            }
        }
    }
    
    private void Update()
    {
        // Cập nhật velocity cho việc di chuyển
        currentVelocity = inputVector * moveSpeed;
        
        HandleInput();
    }
    
    private void HandleInput()
    {
        // 输入处理现在由HealthSystem管理Space键测试
        // 这里不再需要直接处理Space键
    }
    
    // State Machine Methods
    public void ChangeToIdleState()
    {
        if (stateMachine != null)
        {
            stateMachine.ChangeState(idleState);
        }
    }
    
    public void ChangeToMovingState()
    {
        if (stateMachine != null)
        {
            stateMachine.ChangeState(movingState);
        }
    }
    
    public void ChangeToDeathState()
    {
        if (stateMachine != null)
        {
            stateMachine.ChangeState(deathState);
        }
    }
    
    // Movement Methods
    /// <summary>
    /// Xử lý di chuyển trong Update() - chỉ dùng cho trường hợp không có Rigidbody2D
    /// </summary>
    public void HandleMovement()
    {
        // Chỉ di chuyển khi có input và KHÔNG có Rigidbody2D
        // Nếu có Rigidbody2D, movement sẽ được xử lý trong HandleMovementPhysics() (FixedUpdate)
        if (rb == null && inputVector.magnitude > 0.1f)
        {
            // Fallback về transform.Translate nếu không có Rigidbody2D
            Vector2 movement = inputVector * moveSpeed * Time.deltaTime;
            transform.Translate(movement);
        }
    }
    
    public void StopMovement()
    {
        // Dừng di chuyển hoàn toàn
        inputVector = Vector2.zero;
        currentVelocity = Vector2.zero;
        
        // Dừng velocity của Rigidbody2D nếu có
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Xử lý physics di chuyển trong FixedUpdate() - SỬ DỤNG FixedDeltaTime để đồng bộ với physics timestep
    /// </summary>
    public void HandleMovementPhysics()
    {
        // Chỉ di chuyển khi có input
        if (inputVector.magnitude > 0.1f && rb != null)
        {
            // Sử dụng Time.fixedDeltaTime để đồng bộ với physics timestep (giống Enemy)
            Vector2 movement = inputVector * moveSpeed * Time.fixedDeltaTime;
            Vector2 targetPosition = rb.position + movement;
            rb.MovePosition(targetPosition);
        }
    }
    
    public void CheckIfShouldFlip()
    {
        // Chỉ kiểm tra flip khi có input ngang rõ ràng
        if (Mathf.Abs(inputVector.x) > 0.1f)
        {
            // Quy ước: 1 = phải, -1 = trái
            int newFacingDirection = inputVector.x > 0 ? 1 : -1;
            
            // Chỉ flip khi hướng thay đổi - tránh flip liên tục
            if (newFacingDirection != FacingDirection)
            {
                FacingDirection = newFacingDirection;
                Flip();
            }
        }
        // Lưu ý: Không flip khi chỉ di chuyển dọc (y != 0, x == 0) để giữ nguyên hướng
    }
    
    private void Flip()
    {
        // Flip chỉ thay đổi sprite, không ảnh hưởng đến vị trí transform
        if (playerSpriteRenderer != null)
        {
            // Đảo lại: lật khi hướng TRÁI để death hiển thị đúng
            playerSpriteRenderer.flipX = FacingDirection > 0;
        }
            
        // Gửi hướng quay cho weapon controller
        if (weaponController != null)
        {
            weaponController.SetFacingDirection(FacingDirection);
        }
    }
    
    // Input Methods - Tương thích với Player Input component
    public void OnMove(InputValue value)
    {
        // Khi đang Death thì bỏ qua input di chuyển
        if (stateMachine != null && stateMachine.IsInState<PlayerDeathState>())
            return;
            
        inputVector = value.Get<Vector2>();

        if (inputVector.magnitude > 1f)
            inputVector.Normalize();

        // Cập nhật cho PointerController để xoay pointer theo 8 hướng
        if (pointerController != null)
        {
            pointerController.SetInput(inputVector);
        }
        
        // Chỉ chuyển state khi có sự thay đổi input
        if (inputVector.magnitude > 0.1f)
        {
            // Có input di chuyển - chuyển sang Moving state
            if (IsInIdleState())
            {
                ChangeToMovingState();
            }
        }
        else
        {
            // Không có input - chuyển về Idle state
            if (IsInMovingState())
            {
                ChangeToIdleState();
            }
        }
    }
    
    public void OnDeath(InputValue value)
    {
        // Dùng Space (isPressed) để kích hoạt; chỉ cho phép một lần
        if (value.isPressed && !stateMachine.IsInState<PlayerDeathState>())
        {
            ChangeToDeathState();
        }
    }

    // Public helpers cho các State khác dùng
    /// <summary>
    /// Áp dụng flip theo hướng hiện tại
    /// </summary>
    public void ApplyFlipForCurrentDirection()
    {
        Flip();
    }

    /// <summary>
    /// Bật/tắt hiển thị vũ khí
    /// </summary>
    public void SetWeaponActive(bool isActive)
    {
        if (weaponController != null)
        {
            weaponController.gameObject.SetActive(isActive);
        }
    }
    
    // Loại bỏ input Death/Revive
    
    // Public Methods for States
    public Vector2 GetInputVector()
    {
        return inputVector;
    }
    
    public bool IsInIdleState()
    {
        return stateMachine != null && stateMachine.IsInState<PlayerIdleState>();
    }
    
    public bool IsInMovingState()
    {
        return stateMachine != null && stateMachine.IsInState<PlayerMovingState>();
    }
    
    public HealthSystem GetHealthSystem()
    {
        return healthSystem;
    }
    
    // Đã bỏ IsInDeathState()
}
