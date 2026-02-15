using UnityEngine;
using System.Collections;

namespace Core.Bootstrap
{
    public class TransitionUIController : MonoBehaviour
    {
        public static TransitionUIController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void FadeOut(float duration)
        {
            Debug.Log($"[TransitionUI] Fading OUT (to black) over {duration}s");
            // Implement UI Graphic fade logic here
        }

        public void FadeIn(float duration)
        {
            Debug.Log($"[TransitionUI] Fading IN (to clear) over {duration}s");
            // Implement UI Graphic fade logic here
        }
    }
}
