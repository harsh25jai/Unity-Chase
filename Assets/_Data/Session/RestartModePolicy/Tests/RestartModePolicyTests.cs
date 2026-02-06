using System.Collections;
using UnityEngine;
using Data.Session.Runtime;

namespace Data.Session.Runtime.Tests
{
    public class RestartModePolicyTests : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING RESTART POLICY TESTS <<<");

            yield return Test_ShouldAttemptLoad_Follows_Mode();
            yield return Test_ForceMode_Respects_Override_Flag();
            yield return Test_ResetToDefault_Logic();

            Debug.Log(">>> RESTART POLICY TESTS COMPLETED <<<");
        }

        private IEnumerator Test_ShouldAttemptLoad_Follows_Mode()
        {
            Debug.Log("Running Test: ShouldAttemptLoad Follows Mode");
            
            var policy = ScriptableObject.CreateInstance<RestartModePolicy>();
            
            policy.currentMode = RestartMode.LoadLastCheckpoint;
            bool shouldLoad = policy.ShouldAttemptLoad();
            
            if (shouldLoad)
                Debug.Log("[PASS] Policy returns true for LoadLastCheckpoint.");
            else
                Debug.LogError("[FAIL] Policy returned false for LoadLastCheckpoint!");

            policy.currentMode = RestartMode.RestartFromBeginning;
            bool shouldRestart = !policy.ShouldAttemptLoad();

            if (shouldRestart)
                Debug.Log("[PASS] Policy returns false for RestartFromBeginning.");
            else
                Debug.LogError("[FAIL] Policy returned true for RestartFromBeginning!");

            Object.Destroy(policy);
            yield return null;
        }

        private IEnumerator Test_ForceMode_Respects_Override_Flag()
        {
            Debug.Log("Running Test: ForceMode Respects Overrides");
            
            var policy = ScriptableObject.CreateInstance<RestartModePolicy>();
            policy.currentMode = RestartMode.RestartFromBeginning;
            policy.allowExternalOverrides = false;

            policy.ForceMode(RestartMode.LoadLastCheckpoint);

            if (policy.currentMode == RestartMode.RestartFromBeginning)
                Debug.Log("[PASS] Override blocked correctly when flags are off.");
            else
                Debug.LogError("[FAIL] Override succeeded despite being disabled!");

            policy.allowExternalOverrides = true;
            policy.ForceMode(RestartMode.LoadLastCheckpoint);

            if (policy.currentMode == RestartMode.LoadLastCheckpoint)
                Debug.Log("[PASS] Override allowed when flag is on.");
            else
                Debug.LogError("[FAIL] Override failed when it should have been allowed!");

            Object.Destroy(policy);
            yield return null;
        }

        private IEnumerator Test_ResetToDefault_Logic()
        {
            Debug.Log("Running Test: Reset To Default");
            
            var policy = ScriptableObject.CreateInstance<RestartModePolicy>();
            policy.defaultMode = RestartMode.RestartFromBeginning;
            policy.currentMode = RestartMode.LoadLastCheckpoint;

            policy.ResetToDefault();

            if (policy.currentMode == RestartMode.RestartFromBeginning)
                Debug.Log("[PASS] Policy reset to default mode correctly.");
            else
                Debug.LogError("[FAIL] Policy failed to reset!");

            Object.Destroy(policy);
            yield return null;
        }
    }
}
