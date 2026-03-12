using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Cài đặt Weapon")]
    [SerializeField] private float rightRotationX = 1.127f;
    [SerializeField] private float rightRotationY = -538f;
    [SerializeField] private float rightPositionX = 0.2f;
    [SerializeField] private float rightPositionY = 0.01f;
    
    [SerializeField] private float leftRotationX = 0f;
    [SerializeField] private float leftRotationY = 0f;
    [SerializeField] private float leftPositionX = -0.2f;
    [SerializeField] private float leftPositionY = -0.01f;
    
    private Transform weaponTransform;
    private int currentFacingDirection = 1;
    
    private void Awake()
    {
        weaponTransform = transform;
    }
    
    public void SetFacingDirection(int facingDirection)
    {
        if (currentFacingDirection != facingDirection)
        {
            currentFacingDirection = facingDirection;
            UpdateWeaponRotation();
        }
    }
    
    private void UpdateWeaponRotation()
    {
        if (weaponTransform == null) return;
        
        Vector3 rotation = weaponTransform.eulerAngles;
        Vector3 position = weaponTransform.localPosition;
        
        if (currentFacingDirection > 0) // Quay phải
        {
            rotation.x = rightRotationX;
            rotation.y = rightRotationY;
            rotation.z = 0f;
            position.x = rightPositionX;
            position.y = rightPositionY;
        }
        else // Quay trái
        {
            rotation.x = leftRotationX;
            rotation.y = leftRotationY;
            rotation.z = 0f;
            position.x = leftPositionX;
            position.y = leftPositionY;
        }
        
        weaponTransform.eulerAngles = rotation;
        weaponTransform.localPosition = position;
    }
    
    public void ForceUpdateRotation()
    {
        UpdateWeaponRotation();
    }
}
