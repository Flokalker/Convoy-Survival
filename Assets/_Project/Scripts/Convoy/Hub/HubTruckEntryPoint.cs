using UnityEngine;
using UnityEngine.SceneManagement;

namespace ConvoySurvival.Hub
{
    [DisallowMultipleComponent]
    public class HubTruckEntryPoint : HubInteractable
    {
        [SerializeField] private string runSceneName = "MainRun";

        public void Configure(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                runSceneName = sceneName;
            }
        }

        public override string GetPrompt()
        {
            return "E = Enter truck and start run";
        }

        public override void Interact(HubInteractionController interactor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(runSceneName);
        }
    }
}
