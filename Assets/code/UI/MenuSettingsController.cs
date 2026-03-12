using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal, reliable Menu Settings controller.
/// - Press ESC to toggle the Panel that has tag "MenuSetting" (even if it's inactive).
/// - When opening: Time.timeScale = 0; When closing: Time.timeScale = 1.
/// - If a GameObject named "GameOver" has a child "Panel" that is active, ESC will be ignored.
/// Notes:
/// - Attach this script to ANY always-active object (not the hidden Panel itself).
/// - Ensure the actual menu Panel GameObject carries the tag "MenuSetting".
/// </summary>
[DisallowMultipleComponent]
public class MenuSettingsController : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    // Optional action reference to avoid double toggles when another InputAction handles ESC
    private InputAction escAction;
#endif
    [Header("Lookup")]
    [SerializeField] private string menuPanelTag = "MenuSetting";   // Tag placed directly on the Panel
    [SerializeField] private bool hidePanelOnStart = true;           // Hide panel on start (recommended)

    [Header("Behavior")]
    [SerializeField] private bool blockWhenGameOverActive = true;    // Ignore ESC while GameOver panel showing

    [Header("Debug")] 
    [SerializeField] private bool showDebug = true;

    private GameObject panel; // resolved at runtime (can be inactive)

    private void Awake()
    {
        FindPanelByTag();
        if (panel == null)
        {
            Debug.LogWarning($"[MenuSettingsController] Panel with tag '{menuPanelTag}' not found. ESC will do nothing until a Panel is tagged correctly.");
        }

        if (hidePanelOnStart && panel != null && panel.activeSelf)
        {
            if (showDebug) Debug.Log("[MenuSettingsController] Hiding Panel on start.");
            panel.SetActive(false);
        }

        if (EventSystem.current == null)
        {
            Debug.LogWarning("[MenuSettingsController] EventSystem not found in scene. Create one to ensure UI works properly.");
        }
    }

    private void Update()
    {
        bool pressed = false;
        #if ENABLE_INPUT_SYSTEM
        // Only poll Keyboard when our InputAction is not enabled (avoid double toggles)
        if ((escAction == null || !escAction.enabled) && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            pressed = true;
        }
        #endif
        #if ENABLE_LEGACY_INPUT_MANAGER
        if (!pressed && UnityEngine.Input.GetKeyDown(KeyCode.Escape))
        {
            pressed = true;
        }
        #endif
        if (!pressed) return;

        if (showDebug) Debug.Log("[MenuSettingsController] ESC pressed");

        if (blockWhenGameOverActive && IsGameOverPanelActive())
        {
            if (showDebug) Debug.Log("[MenuSettingsController] Ignored because GameOver Panel is active");
            return;
        }

        if (panel == null)
        {
            // Try resolving again (e.g., panel was instantiated later)
            FindPanelByTag();
            if (panel == null)
            {
                Debug.LogError($"[MenuSettingsController] No Panel with tag '{menuPanelTag}' found.");
                return;
            }
        }

        if (!panel.activeInHierarchy)
        {
            Open();
        }
        else
        {
            Close();
        }
    }

    private void Open()
    {
        Time.timeScale = 0f;
        panel.SetActive(true);
        // Ensure Border is re-activated (it may have been hidden by Resume button)
        var borderTf = panel.transform.Find("Border");
        if (borderTf != null)
        {
            var borderGo = borderTf.gameObject;
            if (!borderGo.activeSelf) borderGo.SetActive(true);
        }
        if (showDebug) Debug.Log("[MenuSettingsController] Open -> timeScale=0, Panel=active, Border=forced active if found");
    }

    private void Close()
    {
        Time.timeScale = 1f;
        panel.SetActive(false);
        if (showDebug) Debug.Log("[MenuSettingsController] Close -> timeScale=1, Panel=inactive");
    }

    private void FindPanelByTag()
    {
        panel = null;
        if (string.IsNullOrEmpty(menuPanelTag)) return;

        // Try active objects first
        try
        {
            var active = GameObject.FindGameObjectWithTag(menuPanelTag);
            if (active != null)
            {
                panel = active;
                return;
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[MenuSettingsController] Tag '{menuPanelTag}' not defined in Tag Manager.");
        }

        // Search inactive objects in the scene
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            var go = t.gameObject;
            if (!go.scene.IsValid()) continue; // skip assets/prefabs
            if (go.CompareTag(menuPanelTag)) { panel = go; break; }
        }
    }

    private bool IsGameOverPanelActive()
    {
        var go = GameObject.Find("GameOver");
        if (go == null) return false;
        var p = go.transform.Find("Panel");
        return p != null && p.gameObject.activeInHierarchy;
    }
}

