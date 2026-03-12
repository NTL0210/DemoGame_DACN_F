using UnityEngine;

/// <summary>
/// Component đánh dấu enemy thuộc event
/// Quản lý việc drop exp khi chết hoặc destroy khi event kết thúc
/// </summary>
public class EventEnemy : MonoBehaviour
{
    private Vector2 moveDirection;
    private EventEnemySpawner spawner;
    private bool shouldDropExp = true; // Mặc định drop exp nếu player giết
    private EnemyController controller;
    private bool isBeingDestroyedByEvent = false;
    
    /// <summary>
    /// Khởi tạo event enemy
    /// </summary>
    public void Initialize(Vector2 direction, EventEnemySpawner eventSpawner)
    {
        moveDirection = direction;
        spawner = eventSpawner;
        controller = GetComponent<EnemyController>();
    }
    
    /// <summary>
    /// Lấy hướng di chuyển
    /// </summary>
    public Vector2 GetMoveDirection()
    {
        return moveDirection;
    }
    
    /// <summary>
    /// Destroy enemy mà không drop exp (khi event kết thúc)
    /// </summary>
    public void DestroyWithoutDrop()
    {
        shouldDropExp = false;
        isBeingDestroyedByEvent = true;
        
        // Hack: Tạm thời override DropExp trong EnemyController
        // Cách 1: Disable controller trước khi destroy
        if (controller != null)
        {
            // Sử dụng reflection để set private field
            var dropExpMethod = typeof(EnemyController).GetMethod("DropExp", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Hoặc đơn giản: disable component
            controller.enabled = false;
        }
        
        // Thông báo spawner
        if (spawner != null)
        {
            spawner.RemoveEventEnemy(gameObject);
        }
        
        // Destroy ngay lập tức
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Khi enemy bị destroy (bởi player hoặc event kết thúc)
    /// </summary>
    private void OnDestroy()
    {
        // Nếu shouldDropExp = true, nghĩa là player đã giết
        // EnemyController sẽ tự động drop exp
        // Nếu shouldDropExp = false, ta đã vô hiệu hóa controller
        
        if (spawner != null && Application.isPlaying && !isBeingDestroyedByEvent)
        {
            spawner.RemoveEventEnemy(gameObject);
        }
    }
    
    /// <summary>
    /// Kiểm tra xem có nên drop exp không
    /// </summary>
    public bool ShouldDropExp()
    {
        return shouldDropExp;
    }
}

