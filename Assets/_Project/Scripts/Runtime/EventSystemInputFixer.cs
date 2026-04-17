using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class EventSystemInputFixer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureCompatibleEventSystem()
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (systems == null || systems.Length == 0)
        {
            GameObject created = new GameObject("EventSystem", typeof(EventSystem));
            systems = new[] { created.GetComponent<EventSystem>() };
        }

        for (int i = 0; i < systems.Length; i++)
        {
            EventSystem system = systems[i];
            if (system == null)
            {
                continue;
            }

#if ENABLE_INPUT_SYSTEM
            StandaloneInputModule legacy = system.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Object.Destroy(legacy);
            }

            if (system.GetComponent<InputSystemUIInputModule>() == null)
            {
                system.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (system.GetComponent<StandaloneInputModule>() == null)
            {
                system.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }
    }
}
