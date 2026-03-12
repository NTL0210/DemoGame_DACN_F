using UnityEngine;

/// <summary>
/// Trạng thái Moving - Player đang di chuyển
/// </summary>
public class PlayerMovingState : MonoBehaviour, IPlayerState 
{
    private PlayerMoveNew playerMove;
    private Animator animator;
    
    private void Awake()
    {
        // Lấy tham chiếu khi được thêm như một Component
        playerMove = GetComponentInParent<PlayerMoveNew>();
        if (playerMove != null)
        {
            animator = playerMove.GetComponent<Animator>();
        }
    }
    
    public void Enter()
    {
        // Đặt animation moving - chỉ dùng bool parameters
        if (animator != null)
        {
            animator.SetBool("idle", false);
            animator.SetBool("move", true);
        }
    }
    
    public void Update()
    {
        // Chỉ xử lý flip trong Update() - không di chuyển ở đây
        // Movement sẽ được xử lý trong FixedUpdate() để đồng bộ với physics timestep
        if (playerMove != null)
        {
            playerMove.CheckIfShouldFlip();
        }
        
        // Không cần set lại bool mỗi frame, tránh dư thừa
    }
    
    public void FixedUpdate()
    {
        // Xử lý physics di chuyển trong FixedUpdate() - SỬ DỤNG FixedDeltaTime
        // Điều này đảm bảo tốc độ di chuyển ổn định và không phụ thuộc vào framerate
        if (playerMove != null)
        {
            playerMove.HandleMovementPhysics();
        }
    }
    
    public void Exit()
    {
        // Không cần xử lý gì khi thoát Moving
    }
}
