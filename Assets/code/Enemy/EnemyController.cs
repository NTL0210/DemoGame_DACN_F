using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Enemy Controller - Phiên bản đơn giản cho giai đoạn đầu
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Configuration")]
    [SerializeField] private EnemyData enemyData;
    // Các cấu hình sát thương cụ thể (Flame, v.v.) sẽ quản lý ở script riêng, không nằm trong EnemyData
    
    [Header("Unity Events")]
    [SerializeField] private UnityEvent<float> OnHealthChanged;
    [SerializeField] private UnityEvent OnEnemyDeath;
    [SerializeField] private UnityEvent OnEnemySpawn;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Components
    private EnemyMove enemyMove;
    private EnemyDamage enemyDamage;
    private EnemyCollisionAvoidance collisionAvoidance;
    private Animator animator;
    private Collider2D enemyCollider;
    
    // State
    private float currentHealth;
    private Coroutine burnCoroutine;
    private bool isBurning = false;
    private bool isAlive = true;
    private int lastHealthInterval = 0;
    
    public float CurrentHealth => currentHealth;
    public bool IsAlive => isAlive;
    public EnemyData Data => enemyData;
    
    private void Awake()
    {
        // Lấy components - cache để tránh GetComponent nhiều lần
        enemyMove = GetComponent<EnemyMove>();
        enemyDamage = GetComponent<EnemyDamage>();
        collisionAvoidance = GetComponent<EnemyCollisionAvoidance>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();
    }
    
    private void Start()
    {
        InitializeEnemy();
        OnEnemySpawn?.Invoke();
        SubscribeTimer();
    }
    
    /// <summary>
    /// Khởi tạo enemy với dữ liệu từ ScriptableObject
    /// </summary>
    private void InitializeEnemy()
    {
        if (enemyData == null)
        {
            Debug.LogError($"[EnemyController] {name} thiếu Enemy Data. Hãy gán một asset EnemyData trong Inspector.");
            return;
        }

        float configuredHealth = enemyData.health;
        var timer = FindFirstObjectByType<TimerManager>();
        if (timer != null)
        {
            int intervals = Mathf.FloorToInt(timer.GetCurrentTimeInSeconds() / 120f);
            float multiplier = 1f + (0.1f * intervals);
            configuredHealth *= multiplier;
        }
        currentHealth = configuredHealth;
        
        // Cấu hình các component
        if (enemyMove != null)
        {
            float move = enemyData.moveSpeed;
            float stop = enemyData.stopDistance;
            enemyMove.SetMoveSpeed(move);
            enemyMove.SetStopDistance(stop);
        }
        
        if (enemyDamage != null)
        {
            float dmg = enemyData.damageAmount;
            enemyDamage.SetDamageAmount(dmg);
        }
        
        if (collisionAvoidance != null)
        {
            float ar = enemyData.avoidanceRadius;
            float af = enemyData.avoidanceForce;
            float sd = enemyData.separationDistance;
            collisionAvoidance.SetAvoidanceRadius(ar);
            collisionAvoidance.SetAvoidanceForce(af);
            collisionAvoidance.SetSeparationDistance(sd);
        }
        
        // Không lấy damage/tags từ EnemyData; các skill tự quản lý tham số của mình
        
        if (showDebugInfo)
        {
            Debug.Log($"Enemy {gameObject.name} initialized with health: {currentHealth}");
        }
    }

    private void SubscribeTimer()
    {
        var timer = FindFirstObjectByType<TimerManager>();
        if (timer != null)
        {
            lastHealthInterval = Mathf.FloorToInt(timer.GetCurrentTimeInSeconds() / 120f);
            timer.OnTimeUpdate += HandleTimeUpdate;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe khỏi timer để tránh memory leak
        var timer = FindFirstObjectByType<TimerManager>();
        if (timer != null)
        {
            timer.OnTimeUpdate -= HandleTimeUpdate;
        }
    }

    private void HandleTimeUpdate(float seconds)
    {
        if (!isAlive) return;
        
        int interval = Mathf.FloorToInt(seconds / 120f);
        if (interval > lastHealthInterval)
        {
            // Mỗi khi qua mốc mới (2 phút), tăng 10% máu hiện có
            float increaseFactor = 1f + 0.1f * (interval - lastHealthInterval);
            currentHealth *= increaseFactor;
            OnHealthChanged?.Invoke(currentHealth);
            lastHealthInterval = interval;
            
            if (showDebugInfo)
            {
                Debug.Log($"Enemy {gameObject.name} health increased to {currentHealth} at interval {interval}");
            }
        }
    }
    
    /// <summary>
    /// Nhận sát thương - Sử dụng Unity Events
    /// </summary>
    /// <param name="damage">Lượng sát thương</param>
    public void TakeDamage(float damage)
    {
        if (!isAlive) return;
        
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Kích hoạt Unity Event
        OnHealthChanged?.Invoke(currentHealth);
        
        if (showDebugInfo)
        {
            Debug.Log($"Enemy {gameObject.name} took {damage} damage. Health: {currentHealth}");
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Chết - Sử dụng Unity Events
    /// </summary>
    private void Die()
    {
        if (!isAlive) return;
        
        isAlive = false;

        // Dừng hiệu ứng đốt nếu có
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
            isBurning = false;
        }
        
        // Kích hoạt Unity Event trước khi destroy
        OnEnemyDeath?.Invoke();
        
        // Rớt EXP khi chết
        DropExp();
        
        // Vô hiệu hóa collider để tránh va chạm trong frame destroy
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        // Dừng di chuyển nếu có
        if (enemyMove != null)
        {
            enemyMove.StopMoving();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Enemy {gameObject.name} died");
        }
        
        // Destroy ngay lập tức
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Rớt EXP khi chết
    /// </summary>
    private void DropExp()
    {
        // Tìm ExpSpawnManager (dùng object pool)
        ExpSpawnManager expSpawnManager = ExpSpawnManager.Instance;
        
        if (expSpawnManager != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[EnemyController] {gameObject.name} chết tại {transform.position}, đang gọi DropExp...");
            }
            expSpawnManager.DropExp(transform.position);
        }
        else
        {
            Debug.LogError($"[EnemyController] ⚠️ KHÔNG TÌM THẤY ExpSpawnManager! Hãy tạo GameObject 'Exp Spawn' với component ExpSpawnManager trong Scene!");
        }
    }
    
    /// <summary>
    /// Thiết lập dữ liệu enemy
    /// </summary>
    /// <param name="data">Dữ liệu enemy mới</param>
    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;
        InitializeEnemy();
    }
    
    /// <summary>
    /// Hồi máu
    /// </summary>
    /// <param name="healAmount">Lượng hồi máu</param>
    public void Heal(float healAmount)
    {
        if (!isAlive) return;
        
        currentHealth = Mathf.Min(enemyData.health, currentHealth + healAmount);
        OnHealthChanged?.Invoke(currentHealth);
        
        if (showDebugInfo)
        {
            Debug.Log($"Enemy {gameObject.name} healed {healAmount}. Health: {currentHealth}");
        }
    }
    
    /// <summary>
    /// Reset enemy về trạng thái ban đầu
    /// </summary>
    public void ResetEnemy()
    {
        isAlive = true;
        currentHealth = enemyData.health;
        
        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }
        
        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// Thiết lập máu của enemy dựa trên hệ số nhân, dùng cho các event đặc biệt
    /// </summary>
    public void ApplyHealthMultiplier(float multiplier)
    {
        if (multiplier <= 1f) return;

        // Tính toán và cập nhật máu mới trực tiếp
        currentHealth = enemyData.health * multiplier;
        OnHealthChanged?.Invoke(currentHealth);

        if (showDebugInfo)
        {
            Debug.Log($"Enemy {gameObject.name} health multiplied by {multiplier}. New health: {currentHealth}");
        }
    }

    /// <summary>
    /// Áp dụng hiệu ứng đốt sát thương theo thời gian
    /// </summary>
    public void ApplyBurn(float damagePerTick, float tickInterval, float duration)
    {
        if (!isAlive) return;
        if (isBurning)
        {
            // Làm mới hiệu ứng
            if (burnCoroutine != null)
                StopCoroutine(burnCoroutine);
        }
        burnCoroutine = StartCoroutine(BurnRoutine(damagePerTick, tickInterval, duration));
    }

    private System.Collections.IEnumerator BurnRoutine(float damagePerTick, float tickInterval, float duration)
    {
        isBurning = true;
        float elapsed = 0f;
        while (elapsed < duration && isAlive)
        {
            TakeDamage(damagePerTick);
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
        isBurning = false;
        burnCoroutine = null;
    }
}