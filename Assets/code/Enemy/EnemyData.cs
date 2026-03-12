using UnityEngine;

/// <summary>
/// Enemy Data ScriptableObject - Sử dụng Unity ScriptableObject để quản lý dữ liệu enemy
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Stats")]
    public float health = 40f; // Default cho Enemy Small, Enemy Big cấu hình riêng bằng SO khác
    public float moveSpeed = 2f;
    public float damageAmount = 0.5f;
    public float stopDistance = 0.5f;
    
    [Header("Collision Avoidance")]
    public float avoidanceRadius = 1.5f;
    public float avoidanceForce = 2f;
    public float separationDistance = 0.8f;
    
    [Header("Visual")]
    public Sprite sprite;
    public Color color = Color.white;
    public Vector2 scale = Vector2.one;
    
    [Header("Physics")]
    public float drag = 5f;
    public float angularDrag = 5f;
    public bool useGravity = false;
    
    [Header("Animation")]
    public RuntimeAnimatorController animatorController;
    public string moveParameter = "Move";
    public string speedParameter = "Speed";
}
