using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spin Damage - Xử lý collision và damage của mỗi spin object
/// - Gây damage khi chạm enemy
/// - Tránh gây damage nhiều lần cho cùng một enemy trong một lần xoay
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpinDamage : MonoBehaviour
{
    [Header("References")]
    private SpinAttackManager spinManager;

    [Header("Damage Settings")]
    [SerializeField] private float damageInterval = 0.5f; // Thời gian giữa các lần damage cho cùng một enemy

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // Tracking enemies đã bị damage gần đây
    private Dictionary<EnemyController, float> enemyLastHitTime = new Dictionary<EnemyController, float>();

    private Collider2D spinCollider;

    private void Awake()
    {
        spinCollider = GetComponent<Collider2D>();
        
        // Đảm bảo collider là trigger
        if (spinCollider != null)
        {
            spinCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// Khởi tạo SpinDamage với reference đến SpinAttackManager
    /// </summary>
    public void Initialize(SpinAttackManager manager)
    {
        spinManager = manager;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem có phải enemy không
        EnemyController enemy = collision.GetComponent<EnemyController>();
        if (enemy == null || !enemy.IsAlive) return;

        // Kiểm tra xem spin có đang active không
        if (spinManager == null || !spinManager.IsActive()) return;

        // Kiểm tra cooldown cho enemy này
        if (CanDamageEnemy(enemy))
        {
            DealDamage(enemy);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Tiếp tục gây damage nếu enemy vẫn trong vùng trigger
        EnemyController enemy = collision.GetComponent<EnemyController>();
        if (enemy == null || !enemy.IsAlive) return;

        if (spinManager == null || !spinManager.IsActive()) return;

        if (CanDamageEnemy(enemy))
        {
            DealDamage(enemy);
        }
    }

    /// <summary>
    /// Kiểm tra xem có thể gây damage cho enemy này không (dựa trên cooldown)
    /// </summary>
    private bool CanDamageEnemy(EnemyController enemy)
    {
        if (!enemyLastHitTime.ContainsKey(enemy))
        {
            return true;
        }

        float timeSinceLastHit = Time.time - enemyLastHitTime[enemy];
        return timeSinceLastHit >= damageInterval;
    }

    /// <summary>
    /// Gây damage cho enemy
    /// </summary>
    private void DealDamage(EnemyController enemy)
    {
        if (spinManager == null) return;

        float damage = spinManager.GetSpinDamage();
        
        if (damage <= 0)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[SpinDamage] Damage = 0, không gây sát thương!");
            }
            return;
        }

        // Gây damage
        enemy.TakeDamage(damage);

        // Cập nhật thời gian hit
        enemyLastHitTime[enemy] = Time.time;

        if (showDebugInfo)
        {
            Debug.Log($"[SpinDamage] {gameObject.name} gây {damage} damage cho {enemy.name}");
        }
    }

    /// <summary>
    /// Dọn dẹp enemies đã chết khỏi dictionary
    /// </summary>
    private void Update()
    {
        // Dọn dẹp enemies đã chết hoặc quá lâu không hit
        List<EnemyController> toRemove = new List<EnemyController>();
        
        foreach (var kvp in enemyLastHitTime)
        {
            // Xóa nếu enemy null hoặc đã chết
            if (kvp.Key == null || !kvp.Key.IsAlive)
            {
                toRemove.Add(kvp.Key);
            }
            // Xóa nếu đã quá lâu không hit (5 giây)
            else if (Time.time - kvp.Value > 5f)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var enemy in toRemove)
        {
            enemyLastHitTime.Remove(enemy);
        }
    }

    private void OnDisable()
    {
        // Clear dictionary khi spin bị tắt
        enemyLastHitTime.Clear();
    }
}

