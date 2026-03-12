using UnityEngine;

/// <summary>
/// Enum định nghĩa loại EXP
/// </summary>
public enum ExpType
{
    Small = 5,   // 5 điểm EXP (đơn vị chuẩn)
    Large = 50   // 50 điểm EXP (= 10 Small)
}

/// <summary>
/// Component cho viên EXP - Thu thập tự động khi Player đến gần
/// - Tự động di chuyển về phía Player khi trong vùng thu thập
/// - Cộng EXP cho Player khi chạm vào
/// - Hỗ trợ cả Small và Large EXP
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class ExpItem : MonoBehaviour
{
    [Header("EXP Settings")]
    [SerializeField] private ExpType expType = ExpType.Small;
    [SerializeField] private float expValue = 1f; // Giá trị EXP (tự động set theo type)
    
    [Header("Collection Settings")]
    [SerializeField] private float collectionRadius = 2f; // Bán kính để bắt đầu bay về Player
    [SerializeField] private float moveSpeed = 5f; // Tốc độ bay về Player
    [SerializeField] private float acceleration = 2f; // Gia tốc khi bay về Player
    
    [Header("Visual Settings")]
    [SerializeField] private float floatSpeed = 1f; // Tốc độ lơ lửng lên xuống
    [SerializeField] private float floatAmount = 0.2f; // Độ cao lơ lửng
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Components
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;
    private Transform _player;
    private PlayerLevelSystem _playerLevelSystem;
    
    // State
    private bool _isBeingCollected = false;
    private float _currentSpeed = 0f;
    private Vector3 _initialPosition;
    private float _floatTimer = 0f;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        
        // Cấu hình Rigidbody2D
        _rb.gravityScale = 0f;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Cấu hình Collider
        _collider.isTrigger = true;
        _collider.radius = 0.3f;
        
        // Set giá trị EXP theo loại
        expValue = (float)expType;
        
        _initialPosition = transform.position;
        _floatTimer = Random.Range(0f, Mathf.PI * 2f); // Random start phase
    }
    
    private void Start()
    {
        // Tìm Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerLevelSystem = playerObj.GetComponent<PlayerLevelSystem>();
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("[ExpItem] Không tìm thấy Player! Đảm bảo Player có tag 'Player'");
        }
    }
    
    private void Update()
    {
        if (_player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
        
        // Kiểm tra trong vùng thu thập
        if (!_isBeingCollected && distanceToPlayer <= collectionRadius)
        {
            _isBeingCollected = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"[ExpItem] Bắt đầu thu thập {expType} EXP");
            }
        }
        
        // Di chuyển về Player
        if (_isBeingCollected)
        {
            MoveTowardsPlayer();
        }
        else
        {
            // Hiệu ứng lơ lửng khi chưa thu thập
            FloatEffect();
        }
    }
    
    /// <summary>
    /// Di chuyển về phía Player với gia tốc
    /// </summary>
    private void MoveTowardsPlayer()
    {
        Vector2 direction = (_player.position - transform.position).normalized;
        
        // Tăng tốc độ theo thời gian
        _currentSpeed += acceleration * Time.deltaTime;
        _currentSpeed = Mathf.Min(_currentSpeed, moveSpeed * 3f); // Giới hạn tốc độ tối đa
        
        _rb.linearVelocity = direction * _currentSpeed;
    }
    
    /// <summary>
    /// Hiệu ứng lơ lửng lên xuống khi chưa thu thập
    /// </summary>
    private void FloatEffect()
    {
        _floatTimer += Time.deltaTime * floatSpeed;
        float newY = _initialPosition.y + Mathf.Sin(_floatTimer) * floatAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    
    /// <summary>
    /// Khi chạm vào Player
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CollectExp();
        }
    }
    
    /// <summary>
    /// Thu thập EXP và cộng vào Player
    /// </summary>
    private void CollectExp()
    {
        if (_playerLevelSystem != null)
        {
            _playerLevelSystem.AddExp(expValue);
            
            if (showDebugInfo)
            {
                Debug.Log($"[ExpItem] Player thu thập {expValue} EXP ({expType})");
            }
        }
        
        // Destroy viên EXP (giống như enemy chết)
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Set loại EXP (gọi từ ExpDropManager khi spawn)
    /// </summary>
    public void SetExpType(ExpType type)
    {
        expType = type;
        expValue = (float)type;
    }
    
    /// <summary>
    /// Lấy loại EXP
    /// </summary>
    public ExpType GetExpType()
    {
        return expType;
    }
    
    /// <summary>
    /// Lấy giá trị EXP
    /// </summary>
    public float GetExpValue()
    {
        return expValue;
    }
    
    /// <summary>
    /// Vẽ Gizmos để debug vùng thu thập
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectionRadius);
    }
}

