using UnityEngine;

/// <summary>
/// Component cho Flame Attack Max - quản lý animation và damage
/// </summary>
public class IsFlameMax : MonoBehaviour
{
	[Header("Animator")]
	[SerializeField] private Animator animator;
	[SerializeField] private string stateName = "FlameMax";
	
	[Header("Duration")]
	[SerializeField] private float activeDuration = 0.6f;
	
	[Header("Damage Component")]
	[SerializeField] private FlameAttackDamage damageComponent;

	public float ActiveDuration => activeDuration;

	private void Awake()
	{
		// Tự động tìm FlameAttackDamage component
		// Ưu tiên tìm trên cùng GameObject, sau đó mới tìm trong children
		if (damageComponent == null)
		{
			damageComponent = GetComponent<FlameAttackDamage>();
			if (damageComponent == null)
			{
				damageComponent = GetComponentInChildren<FlameAttackDamage>(true);
			}
		}
		
		// Set level cho damage component (luôn là 4 cho Max)
		if (damageComponent != null)
		{
			damageComponent.SetFlameLevel(4);
		}
		else
		{
			Debug.LogWarning($"IsFlameMax on {gameObject.name}: Không tìm thấy FlameAttackDamage component! Hãy thêm FlameAttackDamage vào GameObject này.");
		}
	}

	private void Reset()
	{
		if (animator == null)
			animator = GetComponentInChildren<Animator>();
	}

	public void PlayOnce()
	{
		gameObject.SetActive(true);
		
		// Bật animator
		if (animator != null && !string.IsNullOrEmpty(stateName))
		{
			animator.Play(stateName, 0, 0f);
		}
		
		// Bật damage component nếu có
		if (damageComponent != null)
		{
			damageComponent.enabled = true;
		}
	}

	public void SetVisible(bool visible)
	{
		gameObject.SetActive(visible);
		
		// Tắt damage component khi ẩn
		if (!visible && damageComponent != null)
		{
			damageComponent.enabled = false;
		}
	}
}
