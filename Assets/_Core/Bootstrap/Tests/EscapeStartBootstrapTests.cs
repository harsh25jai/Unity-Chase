using System.Collections;
using UnityEngine;
using Core.Bootstrap;
using Data.Session.Runtime;

namespace Core.Bootstrap.Tests
{
    public class EscapeStartBootstrapTests : MonoBehaviour
    {
        private GameObject _testGo;
        private SessionRuntimeData _sessionData;
        private EscapeStartBootstrap _bootstrap;
        private GameModeInitializer _initializer;
        private EscapeStartSpawnRequestor _requestor;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING ESCAPE START BOOTSTRAP TESTS <<<");

            Setup();
            yield return null;

            yield return Test_Bootstrap_Initializes_SinglePlayer_Session();
            Teardown();

            Setup();
            yield return null;
            yield return Test_SpawnRequest_Selects_Valid_Marker();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Bootstrap_Fails_Gracefully_When_No_Markers();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Startup_Clamps_Multiplayer_Default_To_SinglePlayer();
            Teardown();

            Debug.Log(">>> ESCAPE START BOOTSTRAP TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _testGo = new GameObject("BootstrapTestObject");
            _sessionData = _testGo.AddComponent<SessionRuntimeData>();
            _bootstrap = _testGo.AddComponent<EscapeStartBootstrap>();
            _initializer = _testGo.AddComponent<GameModeInitializer>();
            _requestor = _testGo.AddComponent<EscapeStartSpawnRequestor>();

            _bootstrap.sessionData = _sessionData;
            _bootstrap.modeInitializer = _initializer;
            _bootstrap.spawnRequestor = _requestor;
            _initializer.sessionData = _sessionData;
        }

        private void Teardown()
        {
            if (_testGo != null) Object.Destroy(_testGo);
        }

        private IEnumerator Test_Bootstrap_Initializes_SinglePlayer_Session()
        {
            Debug.Log("Running Test: Bootstrap Initializes SP");
            var markerGo = new GameObject("Marker1");
            markerGo.AddComponent<PlayerSpawnMarker>();

            yield return new WaitForSeconds(0.1f);

            if (_sessionData.gameMode == GameMode.SinglePlayer && _sessionData.playerCount == 1 && _sessionData.isSessionActive)
                Debug.Log("[PASS] Bootstrap initialized successfully.");
            else
                Debug.LogError("[FAIL] Bootstrap initialization failed.");

            Object.Destroy(markerGo);
        }

        private IEnumerator Test_SpawnRequest_Selects_Valid_Marker()
        {
            Debug.Log("Running Test: Marker Selection");
            bool spawnRequested = false;
            _requestor.OnPlayerSpawnRequested += (t) => spawnRequested = true;

            var markerGo = new GameObject("Marker_A");
            markerGo.AddComponent<PlayerSpawnMarker>();

            yield return new WaitForSeconds(0.1f);

            if (spawnRequested)
                Debug.Log("[PASS] Spawn requested correctly.");
            else
                Debug.LogError("[FAIL] Spawn request not fired.");

            Object.Destroy(markerGo);
        }

        private IEnumerator Test_Bootstrap_Fails_Gracefully_When_No_Markers()
        {
            Debug.Log("Running Test: No Markers Failure");
            // No markers added
            
            yield return new WaitForSeconds(0.1f);

            if (!_sessionData.isSessionActive)
                Debug.Log("[PASS] System handled missing markers gracefully.");
            else
                Debug.LogError("[FAIL] System marked session active even with no markers!");
        }

        private IEnumerator Test_Startup_Clamps_Multiplayer_Default_To_SinglePlayer()
        {
            Debug.Log("Running Test: Multiplayer Clamp");
            _initializer.defaultMode = GameMode.SinglePlayer;
            _initializer.defaultPlayerCount = 4;

            _initializer.InitializeGameMode();
            yield return null;

            if (_sessionData.playerCount == 1)
                Debug.Log("[PASS] Player count clamped correctly.");
            else
                Debug.LogError($"[FAIL] Player count not clamped. Value: {_sessionData.playerCount}");
        }
    }
}
