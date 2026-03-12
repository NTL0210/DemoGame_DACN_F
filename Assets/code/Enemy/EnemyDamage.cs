using UnityEngine;

/// <summary>
/// Hệ thống gây sát thương Enemy - Khi player va chạm với enemy sẽ gây sát thương
/// Không sử dụng AnimationEvent, chỉ dựa vào va chạm collider
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Cài đặt sát thương")]
    [SerializeField] private float damageAmount = 0.5f; // Lượng sát thương mỗi lần (nửa tim)
    
    [Header("Cài đặt va chạm")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool isTrigger = true; // Có sử dụng trigger va chạm không
    
    [Header("Cài đặt Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveParameterName = "Move";
    
    // Biến riêng tư
    private HealthSystem playerHealthSystem;
    private bool isPlayerInRange = false; // Player có đang trong tầm sát thương không
    
    private void Awake()
    {
        // Tự động tìm Animator nếu chưa được gán
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
    
    private void Start()
    {
        // Tìm HealthSystem của player
        FindPlayerHealthSystem();
        
        // Khởi tạo animation di chuyển
        InitializeAnimation();
    }
    
    private void Update()
    {
        // Kiểm tra sát thương liên tục nếu player đang trong tầm
        if (isPlayerInRange)
        {
            TryDamagePlayer();
        }
    }
    
    /// <summary>
    /// Khởi tạo animation
    /// </summary>
    private void InitializeAnimation()
    {
        if (animator != null)
        {
            // Bắt đầu với animation di chuyển
            animator.SetBool(moveParameterName, true);
        }
    }
    
    /// <summary>
    /// Tìm component HealthSystem của player
    /// </summary>
    private void FindPlayerHealthSystem()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerHealthSystem = player.GetComponent<HealthSystem>();
            if (playerHealthSystem == null)
            {
                Debug.LogWarning($"EnemyDamage: Không tìm thấy component HealthSystem trên {player.name}!");
            }
        }
        else
        {
            Debug.LogWarning($"EnemyDamage: Không tìm thấy object player với tag {playerTag}!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = true;
            // Gây sát thương ngay lập tức khi player vào tầm
            TryDamagePlayer();
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = true;
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = false;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isTrigger && collision.gameObject.CompareTag(playerTag))
        {
            isPlayerInRange = true;
            TryDamagePlayer();
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isTrigger && collision.gameObject.CompareTag(playerTag))
        {
            isPlayerInRange = true;
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!isTrigger && collision.gameObject.CompareTag(playerTag))
        {
            isPlayerInRange = false;
        }
    }
    
    /// <summary>
    /// Thử gây sát thương cho player
    /// </summary>
    private void TryDamagePlayer()
    {
        // Đảm bảo có tham chiếu HealthSystem
        if (playerHealthSystem == null)
        {
            FindPlayerHealthSystem();
            if (playerHealthSystem == null)
            {
                return;
            }
        }
        
        // Kiểm tra player còn sống không
        if (playerHealthSystem.IsDead())
        {
            return;
        }
        
        // Kiểm tra player có đang trong thời gian delay nhận damage không
        if (!playerHealthSystem.CanTakeDamage())
        {
            // Player đang trong thời gian invincibility, không gây damage
            return;
        }
        
        // Gây sát thương (chỉ khi player có thể nhận damage)
        playerHealthSystem.TakeDamage(damageAmount);
        
    }
    
    /// <summary>
    /// Thiết lập lượng sát thương
    /// </summary>
    /// <param name="damage">Lượng sát thương mới</param>
    public void SetDamageAmount(float damage)
    {
        damageAmount = damage;
    }
    
    /// <summary>
    /// Lấy lượng sát thương
    /// </summary>
    /// <returns>Lượng sát thương hiện tại</returns>
    public float GetDamageAmount()
    {
        return damageAmount;
    }
    
    
    /// <summary>
    /// Dừng animation di chuyển
    /// </summary>
    public void StopMoving()
    {
        if (animator != null)
        {
            animator.SetBool(moveParameterName, false);
        }
    }
    
    /// <summary>
    /// Bắt đầu animation di chuyển
    /// </summary>
    public void StartMoving()
    {
        if (animator != null)
        {
            animator.SetBool(moveParameterName, true);
        }
    }
}
