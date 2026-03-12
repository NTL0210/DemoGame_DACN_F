using UnityEngine;

/// <summary>
/// Component cho Flame Attack Lv1 - quản lý animation và damage
/// </summary>
public class IsFlameLv1 : MonoBehaviour
{
	[Header("Animator")]
	[SerializeField] private Animator animator;
	[SerializeField] private string stateName = "FlameLv1";
	
	[Header("Duration")]
	[SerializeField] private float activeDuration = 0.6f;
	
	[Header("Damage Component")]
	[SerializeField] private FlameAttackDamage damageComponent;
	[SerializeField] private int flameLevel = 1; // Level của flame này (1, 2, hoặc 3)

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
		
		// Set level cho damage component
		if (damageComponent != null)
		{
			damageComponent.SetFlameLevel(flameLevel);
		}
		else
		{
			Debug.LogWarning($"IsFlameLv1 on {gameObject.name}: Không tìm thấy FlameAttackDamage component! Hãy thêm FlameAttackDamage vào GameObject này.");
		}
	}
	
	/// <summary>
	/// Thiết lập level của flame
	/// </summary>
	public void SetLevel(int level)
	{
		flameLevel = Mathf.Clamp(level, 1, 3); // Lv1 chỉ có level 1-3
		if (damageComponent != null)
		{
			damageComponent.SetFlameLevel(flameLevel);
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
