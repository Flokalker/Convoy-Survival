using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CursorRuntimeFix : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "BrowserPrototype";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<CursorRuntimeFix>() != null)
        {
            return;
        }

        GameObject fixObject = new GameObject("CursorRuntimeFix");
        fixObject.AddComponent<CursorRuntimeFix>();
    }

    private void Awake()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(targetSceneName) && scene.name != targetSceneName)
        {
            Destroy(gameObject);
            return;
        }

        BrowserFpsController.SetForceCursorUnlocked(true);
        FirstPersonController.SetForceCursorUnlocked(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Reapply every frame so no gameplay script can re-lock the cursor.
        BrowserFpsController.SetForceCursorUnlocked(true);
        FirstPersonController.SetForceCursorUnlocked(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
