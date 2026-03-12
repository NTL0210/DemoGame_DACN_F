using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Standalone Resume button logic.
/// - Unpause game (Time.timeScale = 1)
/// - Hide Border (parent container)
/// - Optionally hide Panel
/// Auto-finds references based on common hierarchy:
///   MenuSetting/Panel/Border/ResumeButton (this)
/// </summary>
[RequireComponent(typeof(Button))]
public class ResumeGameButton : MonoBehaviour
{
    [Header("Hierarchy References")]
    [SerializeField] private GameObject panel;  // MenuSetting/Panel
    [SerializeField] private GameObject border; // MenuSetting/Panel/Border

    [Header("Behavior")]
    [Tooltip("Ẩn Panel khi Resume. Nếu tắt, chỉ ẩn Border.")]
    [SerializeField] private bool hidePanelOnResume = false;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        AutoFind();

        // Wire click
        btn.onClick.RemoveListener(OnClickResume);
        btn.onClick.AddListener(OnClickResume);
    }

    private void AutoFind()
    {
        // Border: ưu tiên lấy từ cha trực tiếp
        if (border == null)
        {
            if (transform.parent != null)
            {
                border = transform.parent.gameObject;
            }
            // Fallback: tìm theo tên trong cha gần nhất
            if (border == null)
            {
                var b = transform.GetComponentInParent<Transform>()?.Find("Border");
                if (b != null) border = b.gameObject;
            }
        }

        // Panel: thường là cha của Border
        if (panel == null && border != null)
        {
            panel = border.transform.parent != null ? border.transform.parent.gameObject : null;
        }
        if (panel == null)
        {
            // Fallback: tìm theo tên trong parent chain
            var p = transform.GetComponentInParent<Transform>()?.Find("Panel");
            if (p != null) panel = p.gameObject;
        }
    }

    public void OnClickResume()
    {
        Time.timeScale = 1f;
        if (border != null) border.SetActive(false);
        if (hidePanelOnResume && panel != null) panel.SetActive(false);
    }
}

