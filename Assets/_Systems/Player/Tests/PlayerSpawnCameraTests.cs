using System.Collections;
using UnityEngine;
using Systems.Player;
using Unity.Cinemachine;

namespace Systems.Player.Tests
{
    public class PlayerSpawnCameraTests : MonoBehaviour
    {
        private GameObject _testGo;
        private PlayerSpawnController _spawnController;
        private CameraBindingController _bindingController;
        private PerspectiveStateRouter _router;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING PLAYER SPAWN CAMERA TESTS <<<");

            Setup();
            yield return null;
            yield return Test_PerspectiveStateRouter_Toggles_Priorities();
            Teardown();

            Setup();
            yield return null;
            yield return Test_CameraBinding_Finds_ViewTarget_On_Spawn();
            Teardown();

            Debug.Log(">>> PLAYER SPAWN CAMERA TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _testGo = new GameObject("PlayerSystemTestObject");
            _spawnController = _testGo.AddComponent<PlayerSpawnController>();
            _bindingController = _testGo.AddComponent<CameraBindingController>();
            _router = _testGo.AddComponent<PerspectiveStateRouter>();

            _bindingController.spawnController = _spawnController;
        }

        private void Teardown()
        {
            if (_testGo != null) Object.Destroy(_testGo);
        }

        private IEnumerator Test_PerspectiveStateRouter_Toggles_Priorities()
        {
            Debug.Log("Running Test: Perspective Toggling");
            var fpsObj = new GameObject("FPS_Cam");
            var fpsCam = fpsObj.AddComponent<CinemachineCamera>();
            var tpsObj = new GameObject("TPS_Cam");
            var tpsCam = tpsObj.AddComponent<CinemachineCamera>();

            _router.fpsCamera = fpsCam;
            _router.tpsCamera = tpsCam;

            yield return null; 
            _router.SetPerspective(CameraPerspective.ThirdPerson);

            if (tpsCam.Priority > fpsCam.Priority)
            {
                _router.TogglePerspective();
                if (fpsCam.Priority > tpsCam.Priority)
                    Debug.Log("[PASS] Perspective context priorities swapped correctly.");
                else
                    Debug.LogError("[FAIL] Perspective priority did not swap on toggle.");
            }
            else
                Debug.LogError("[FAIL] Initial perspective priority incorrect.");

            Object.Destroy(fpsObj);
            Object.Destroy(tpsObj);
        }

        private IEnumerator Test_CameraBinding_Finds_ViewTarget_On_Spawn()
        {
            Debug.Log("Running Test: Camera Binding to ViewTarget");
            var fpsObj = new GameObject("FPS_Cam");
            var fpsCam = fpsObj.AddComponent<CinemachineCamera>();
            _bindingController.fpsCamera = fpsCam;
            var tpsObj = new GameObject("TPS_Cam");
            var tpsCam = tpsObj.AddComponent<CinemachineCamera>();
            _bindingController.tpsCamera = tpsCam;

            var playerPrefab = new GameObject("PlayerPrefab");
            var viewTarget = new GameObject("Player_ViewTarget");
            viewTarget.transform.SetParent(playerPrefab.transform);

            bool bound = false;
            _bindingController.OnCamerasBound += (_) => bound = true;

            _spawnController.playerPrefab = playerPrefab;
            _spawnController.SpawnPlayer(_testGo.transform);

            yield return null;

            if (bound && fpsCam.Follow == viewTarget.transform)
                Debug.Log("[PASS] Camera bound to ViewTarget correctly.");
            else
                Debug.LogError("[FAIL] Camera binding failed.");

            Object.Destroy(playerPrefab);
            Object.Destroy(fpsObj);
            Object.Destroy(tpsObj);
        }
    }
}
