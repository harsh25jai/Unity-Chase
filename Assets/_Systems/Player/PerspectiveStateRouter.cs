using System;
using UnityEngine;
using Unity.Cinemachine;

namespace Systems.Player
{
    public enum CameraPerspective
    {
        ThirdPerson,
        FirstPerson
    }

    public class PerspectiveStateRouter : MonoBehaviour
    {
        [Header("Cameras")]
        public CinemachineCamera fpsCamera;
        public CinemachineCamera tpsCamera;

        [Header("State")]
        public CameraPerspective currentPerspective = CameraPerspective.ThirdPerson;

        // Events
        public event Action<CameraPerspective> OnPerspectiveChanged;

        private void Start()
        {
            // Initialize priorities
            ApplyPerspective(currentPerspective);
        }

        public void TogglePerspective()
        {
            var newPerspective = currentPerspective == CameraPerspective.ThirdPerson 
                ? CameraPerspective.FirstPerson 
                : CameraPerspective.ThirdPerson;
            
            SetPerspective(newPerspective);
        }

        public void SetPerspective(CameraPerspective perspective)
        {
            currentPerspective = perspective;
            ApplyPerspective(currentPerspective);
            OnPerspectiveChanged?.Invoke(currentPerspective);
        }

        private void ApplyPerspective(CameraPerspective perspective)
        {
            // Ensure logic handles missing cameras gracefully
            if (fpsCamera == null || tpsCamera == null) return;

            if (perspective == CameraPerspective.FirstPerson)
            {
                fpsCamera.Priority = 11;
                tpsCamera.Priority = 10;
            }
            else
            {
                fpsCamera.Priority = 10;
                tpsCamera.Priority = 11;
            }

            Debug.Log($"[PerspectiveStateRouter] Switched to {perspective}");
        }
    }
}
