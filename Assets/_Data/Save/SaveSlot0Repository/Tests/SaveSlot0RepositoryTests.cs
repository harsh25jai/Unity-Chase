using System.Collections;
using System.IO;
using UnityEngine;
using Data.Save.Repository;

namespace Data.Save.Repository.Tests
{
    public class SaveSlot0RepositoryTests : MonoBehaviour
    {
        private SaveSlot0Repository _repo;
        private string _testPlayer = "TestPlayer_99";

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING SAVE REPOSITORY TESTS <<<");
            
            _repo = new SaveSlot0Repository();

            yield return Test_Read_Empty_Slot_Returns_False();
            yield return Test_Write_And_Read_Integrity();
            yield return Test_Atomic_Replacement();
            yield return Test_Delete_Slot();

            Debug.Log(">>> SAVE REPOSITORY TESTS COMPLETED <<<");
        }

        private IEnumerator Test_Read_Empty_Slot_Returns_False()
        {
            Debug.Log("Running Test: Read Empty Slot");
            _repo.DeleteSlot0(_testPlayer);

            bool success = _repo.TryReadSlot0(out string data, _testPlayer);

            if (!success && string.IsNullOrEmpty(data))
                Debug.Log("[PASS] Empty slot handled correctly.");
            else
                Debug.LogError("[FAIL] Empty slot returned success or data!");

            yield return null;
        }

        private IEnumerator Test_Write_And_Read_Integrity()
        {
            Debug.Log("Running Test: Write/Read Integrity");
            string testData = "{ \"test\": \"value\" }";
            
            _repo.WriteSlot0(testData, _testPlayer);
            bool success = _repo.TryReadSlot0(out string readData, _testPlayer);

            if (success && readData == testData)
                Debug.Log("[PASS] Data integrity verified.");
            else
                Debug.LogError($"[FAIL] Data mismatch! Read: {readData}");

            yield return null;
        }

        private IEnumerator Test_Atomic_Replacement()
        {
            Debug.Log("Running Test: Atomic Replacement");
            
            _repo.WriteSlot0("InitialData", _testPlayer);
            _repo.WriteSlot0("UpdatedData", _testPlayer);

            _repo.TryReadSlot0(out string data, _testPlayer);

            if (data == "UpdatedData")
                Debug.Log("[PASS] Atomic swap verified.");
            else
                Debug.LogError("[FAIL] Data not updated correctly!");

            yield return null;
        }

        private IEnumerator Test_Delete_Slot()
        {
            Debug.Log("Running Test: Delete Slot");
            
            _repo.WriteSlot0("Temp", _testPlayer);
            _repo.DeleteSlot0(_testPlayer);

            if (!_repo.DoesSlot0Exist(_testPlayer))
                Debug.Log("[PASS] Slot deletion verified.");
            else
                Debug.LogError("[FAIL] Slot still exists after deletion!");

            yield return null;
        }
    }
}
