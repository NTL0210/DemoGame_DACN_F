using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Testlan01";
    [SerializeField] private float fadeDuration = 0.8f;

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("MainMenu: Chưa cấu hình tên scene cần load.");
            return;
        }
        SceneTransition.LoadSceneWithFade(sceneToLoad, fadeDuration);
        Time.timeScale = 1f;
    }

    public void ExitGame()
    {
        SceneTransition.QuitGameWithFade(fadeDuration);
    }
}

