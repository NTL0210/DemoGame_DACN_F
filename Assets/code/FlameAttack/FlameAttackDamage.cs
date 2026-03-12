using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(PolygonCollider2D))]
public class FlameAttackDamage : MonoBehaviour
{
    [Header("Manager Reference")]
    [SerializeField] private FlameAttackManager flameAttackManager;

    [Header("Damage Settings")]
    [SerializeField] private float damagePerHit;
    [SerializeField] private float damageInterval = 0.2f;
    [SerializeField] private int flameLevel = 1;

    [Header("Damage Multipliers (from Player Damage)")]
    private readonly float[] damageMultipliers = { 1.10f, 1.25f, 1.40f, 1.55f }; // Lv1 to Lv4

    [Header("Burn Effect - Only Lv Max")]
    [SerializeField] private bool applyBurnEffect = false;
    [SerializeField] private float burnDamagePercent = 0.30f;
    [SerializeField] private float burnTickInterval = 0.5f;
    [SerializeField] private float burnDuration = 3f;

    [Header("Collider Settings")]
    [SerializeField] private PolygonCollider2D polygonCollider;
    [SerializeField] private bool isTrigger = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private HashSet<Collider2D> enemiesInRange = new HashSet<Collider2D>();

    private void Awake()
    {
        if (polygonCollider == null) polygonCollider = GetComponent<PolygonCollider2D>();
        if (polygonCollider != null) polygonCollider.isTrigger = isTrigger;

        // Tìm Manager trung tâm
        if (flameAttackManager == null) flameAttackManager = GetComponentInParent<FlameAttackManager>();
        if (flameAttackManager == null) Debug.LogError($"FlameAttackDamage on {gameObject.name}: Không tìm thấy FlameAttackManager!");

        CalculateDamageByLevel();
    }

    private void CalculateDamageByLevel()
    {
        if (PlayerDamage.Instance == null) return;
        float playerCurrentDamage = PlayerDamage.Instance.CurrentDamage;
        int levelIndex = Mathf.Clamp(flameLevel - 1, 0, damageMultipliers.Length - 1);
        damagePerHit = playerCurrentDamage * damageMultipliers[levelIndex];
        applyBurnEffect = (flameLevel == 4);
    }

    private void OnEnable()
    {
        enemiesInRange.Clear();
        CalculateDamageByLevel();
    }

    private void OnDisable()
    {
        enemiesInRange.Clear();
    }

    private void Update()
    {
        DamageEnemiesInRange();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Add(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Add(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Remove(other);
        }
    }

    private void DamageEnemiesInRange()
    {
        if (flameAttackManager == null) return;

        var enemiesCopy = enemiesInRange.ToList();
        foreach (var enemyCollider in enemiesCopy)
        {
            if (enemyCollider == null || !TryDamageEnemy(enemyCollider))
            {
                enemiesInRange.Remove(enemyCollider);
            }
        }
    }

    private bool TryDamageEnemy(Collider2D enemyCollider)
    {
        if (enemyCollider == null) return false;
        EnemyController enemy = enemyCollider.GetComponent<EnemyController>();
        if (enemy == null || !enemy.IsAlive) return false;

        // Hỏi Manager xem có được phép gây sát thương không
        if (flameAttackManager.CanDamage(enemy))
        {
            enemy.TakeDamage(damagePerHit);
            // Báo cho Manager biết là đã gây sát thương
            flameAttackManager.RecordHit(enemy);

            if (applyBurnEffect)
            {
                float burnDamagePerTick = (damagePerHit * burnDamagePercent) / (burnDuration / burnTickInterval);
                enemy.ApplyBurn(burnDamagePerTick, burnTickInterval, burnDuration);
            }

            if (showDebugInfo)
            {
                Debug.Log($"FlameAttackDamage Lv{flameLevel}: Dealt {damagePerHit:F2} damage to {enemy.name}");
            }
        }

        return true;
    }

    public void SetFlameLevel(int level)
    {
        flameLevel = Mathf.Clamp(level, 1, 4);
        CalculateDamageByLevel();
    }
}