using UnityEngine;

/// <summary>
/// Enemy Collision Avoidance - chỉ tính vector tách, không tự áp lực
/// </summary>
public class EnemyCollisionAvoidance : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float avoidanceRadius = 1.5f;
    [SerializeField] private float avoidanceForce = 2f;
    [SerializeField] private float separationDistance = 0.8f;
    
    /// <summary>
    /// Tính vector tách đám đông, để EnemyMove trộn vào hướng di chuyển
    /// </summary>
    public Vector2 GetSeparationVector()
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, avoidanceRadius);
        
        Vector2 separationVector = Vector2.zero;
        int neighborCount = 0;
        
        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (enemy.gameObject == gameObject) continue;
            if (!enemy.CompareTag("Enemy")) continue;
            
            Vector2 toEnemy = (Vector2)(enemy.transform.position - transform.position);
            float distance = toEnemy.magnitude;
            if (distance <= 0.0001f) continue;
            
            if (distance < separationDistance)
            {
                // Lực mạnh hơn khi càng gần, suy giảm ~ 1/d^2 để đẩy ra nhanh khi dính sát
                float strength = Mathf.Clamp01((separationDistance - distance) / separationDistance);
                separationVector += (-toEnemy.normalized) * (avoidanceForce * strength / (distance * distance));
                neighborCount++;
            }
        }
        
        if (neighborCount > 0)
        {
            separationVector /= neighborCount;
        }
        return separationVector;
    }
    
    public void SetAvoidanceRadius(float radius)
    {
        avoidanceRadius = Mathf.Max(0.5f, radius);
    }
    
    public void SetAvoidanceForce(float force)
    {
        avoidanceForce = Mathf.Max(0f, force);
    }
    
    public void SetSeparationDistance(float distance)
    {
        separationDistance = Mathf.Max(0.1f, distance);
    }
}