using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Core.Bootstrap;
using Core.GameState;
using Data.Session.Runtime;
using Systems.Player;

namespace Core.Bootstrap.Tests
{
    public class CutsceneSignalReceiverTests : MonoBehaviour
    {
        private GameObject _testGo;
        private CutsceneSignalReceiver _receiver;
        private GameStateManager _gameState;
        private SessionRuntimeData _sessionData;
        private GameplaySceneLoader _sceneLoader;
        private PlayerStateController _playerState;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING CUTSCENE SIGNAL RECEIVER TESTS <<<");

            yield return StartCoroutine(Test_NormalFlow());
            Cleanup();

            yield return StartCoroutine(Test_MissingMarkers());
            Cleanup();

            yield return StartCoroutine(Test_DuplicateSignal());
            Cleanup();

            Debug.Log(">>> CUTSCENE SIGNAL RECEIVER TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _testGo = new GameObject("CutsceneTestContext");
            
            _sessionData = _testGo.AddComponent<SessionRuntimeData>();
            _sessionData.currentSessionState = SessionState.CutsceneMode;

            _gameState = _testGo.AddComponent<GameStateManager>();
            _gameState.sessionData = _sessionData;

            _sceneLoader = _testGo.AddComponent<GameplaySceneLoader>();
            
            _playerState = _testGo.AddComponent<PlayerStateController>();
            // Add PlayerRuntimeData for PlayerStateController
            _testGo.AddComponent<Data.Player.Runtime.PlayerRuntimeData>();

            _receiver = _testGo.AddComponent<CutsceneSignalReceiver>();
            _receiver.gameStateManager = _gameState;
            _receiver.sceneLoader = _sceneLoader;
            _receiver.playerStateController = _playerState;
        }

        private void Cleanup()
        {
            if (_testGo != null) DestroyImmediate(_testGo);
            // Destroy Static Instances if any
            if (GameStateManager.Instance != null) DestroyImmediate(GameStateManager.Instance.gameObject);
            if (GameplaySceneLoader.Instance != null) DestroyImmediate(GameplaySceneLoader.Instance.gameObject);
        }

        private IEnumerator Test_NormalFlow()
        {
            Debug.Log("Running Test: Normal Flow");
            Setup();

            // Create markers
            GameObject root = new GameObject("Cutscene_Root");
            GameObject markers = new GameObject("Markers");
            markers.transform.SetParent(root.transform);
            
            GameObject anchor = new GameObject("Handoff_Anchor");
            anchor.transform.SetParent(markers.transform);
            anchor.transform.position = new Vector3(10, 0, 10);

            GameObject facing = new GameObject("Handoff_PlayerFacing");
            facing.transform.SetParent(markers.transform);
            facing.transform.rotation = Quaternion.Euler(0, 90, 0);

            GameObject look = new GameObject("Handoff_LookTarget");
            look.transform.SetParent(markers.transform);
            look.transform.position = new Vector3(20, 0, 20);

            // Mock Signal
            SignalAsset signalAsset = ScriptableObject.CreateInstance<SignalAsset>();
            signalAsset.name = "SIG_Cutscene_RobberyIntro_End";
            SignalEmitter emitter = new SignalEmitter { asset = signalAsset };

            // Trigger Signal
            _receiver.OnNotify(new Playable(), emitter, null);
            yield return null;

            // Verify State
            if (_sessionData.currentSessionState == SessionState.TransitionMode)
                Debug.Log("[PASS] State transitioned to TransitionMode.");
            else
                Debug.LogError($"[FAIL] State is: {_sessionData.currentSessionState}");

            // Verify Data Persistence in Session
            if (_sessionData.activeHandoffData.isValid && _sessionData.activeHandoffData.spawnPosition == new Vector3(10, 0, 10))
                Debug.Log("[PASS] Handoff data persisted in SessionRuntimeData.");
            else
                Debug.LogError("[FAIL] Handoff data not persisted correctly.");
            
            DestroyImmediate(root);
        }

        private IEnumerator Test_MissingMarkers()
        {
            Debug.Log("Running Test: Missing Markers");
            Setup();

            // Mock Signal
            SignalAsset signalAsset = ScriptableObject.CreateInstance<SignalAsset>();
            signalAsset.name = "SIG_Cutscene_RobberyIntro_End";
            SignalEmitter emitter = new SignalEmitter { asset = signalAsset };

            // Trigger Signal
            _receiver.OnNotify(new Playable(), emitter, null);
            yield return null;

            // Verify
            if (_sessionData.currentSessionState == SessionState.TransitionMode)
                Debug.Log("[PASS] State still transitioned despite missing markers.");
            else
                Debug.LogError("[FAIL] State did not transition.");
        }

        private IEnumerator Test_DuplicateSignal()
        {
            Debug.Log("Running Test: Duplicate Signal");
            Setup();

            // Mock Signal
            SignalAsset signalAsset = ScriptableObject.CreateInstance<SignalAsset>();
            signalAsset.name = "SIG_Cutscene_RobberyIntro_End";
            SignalEmitter emitter = new SignalEmitter { asset = signalAsset };

            // Trigger Signal twice
            _receiver.OnNotify(new Playable(), emitter, null);
            _receiver.OnNotify(new Playable(), emitter, null);
            yield return null;

            Debug.Log("[CHECK] Check logs for 'Duplicate transition signal ignored'.");
        }
    }
}
