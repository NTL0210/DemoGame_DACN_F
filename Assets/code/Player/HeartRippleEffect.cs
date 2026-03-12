using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý hiệu ứng gợn sóng cho các trái tim trong UI.
/// </summary>
public class HeartRippleEffect : MonoBehaviour
{
    [Header("Ripple Settings")]
    [Tooltip("Trái tim sẽ nhảy lên cao bao nhiêu.")]
    [SerializeField] private float rippleHeight = 0.02f; // Chiều cao gợn sóng
    [SerializeField] private float rippleDuration = 0.15f;    // Thời gian cho một trái tim đi lên và đi xuống
    [SerializeField] private float delayBetweenHearts = 0.07f; // Thời gian chờ trước khi trái tim tiếp theo gợn sóng

    private Transform[] heartContainers;
    private Vector3[] originalPositions; // Đổi từ scale sang position

    /// <summary>
    /// Khởi tạo và lấy tham chiếu đến các container của trái tim.
    /// </summary>
    public void Initialize(Transform heartParent, int maxHealth)
    {
        heartContainers = new Transform[maxHealth];
        originalPositions = new Vector3[maxHealth];

        for (int i = 0; i < maxHealth; i++)
        {
            Transform container = heartParent.Find($"Heart {i + 1}");
            if (container != null)
            {
                heartContainers[i] = container;
                originalPositions[i] = container.localPosition; // Lưu vị trí ban đầu
            }
            else
            {
                Debug.LogWarning($"[HeartRippleEffect] Không tìm thấy container 'Heart {i + 1}'!");
            }
        }
    }

    /// <summary>
    /// Bắt đầu chạy hiệu ứng gợn sóng.
    /// </summary>
    public void PlayRippleEffect(int targetHeartIndex, System.Action onRippleComplete)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RippleCoroutine(targetHeartIndex, onRippleComplete));
        }
        else
        {
            onRippleComplete?.Invoke();
        }
    }

    private IEnumerator RippleCoroutine(int targetHeartIndex, System.Action onRippleComplete)
    {
        int limit = Mathf.Min(targetHeartIndex, heartContainers.Length - 1);

        for (int i = 0; i <= limit; i++)
        {
            if (heartContainers[i] != null)
            {
                StartCoroutine(AnimateSingleHeart(heartContainers[i], originalPositions[i]));
                yield return new WaitForSeconds(delayBetweenHearts);
            }
        }

        yield return new WaitForSeconds(rippleDuration);
        onRippleComplete?.Invoke();
    }

    /// <summary>
    /// Animation đi lên và đi xuống cho một trái tim duy nhất.
    /// </summary>
    private IEnumerator AnimateSingleHeart(Transform heart, Vector3 originalPosition)
    {
        float timer = 0;
        Vector3 targetPosition = originalPosition + new Vector3(0, rippleHeight, 0);

        // Giai đoạn đi lên
        while (timer < rippleDuration / 2)
        {
            heart.localPosition = Vector3.Lerp(originalPosition, targetPosition, timer / (rippleDuration / 2));
            timer += Time.deltaTime;
            yield return null;
        }

        // Giai đoạn đi xuống
        timer = 0;
        while (timer < rippleDuration / 2)
        {
            heart.localPosition = Vector3.Lerp(targetPosition, originalPosition, timer / (rippleDuration / 2));
            timer += Time.deltaTime;
            yield return null;
        }

        // Đảm bảo nó quay về vị trí ban đầu
        heart.localPosition = originalPosition;
    }
}