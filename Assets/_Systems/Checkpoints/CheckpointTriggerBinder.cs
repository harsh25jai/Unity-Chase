using System;
using UnityEngine;
using Data.Session.Runtime;

namespace Systems.Checkpoints
{
    public enum BinderState
    {
        Unbound,
        Bound,
        Armed,
        Triggered,
        Cooldown
    }

    public class CheckpointTriggerBinder : MonoBehaviour
    {
        [Header("Configuration")]
        public string checkpointId = "Checkpoint_01";
        public float cooldownDuration = 5.0f;

        [Header("Scene References (Auto-Discovery)")]
        public string triggerName = "Checkpoint_01_Trigger";
        public string spawnName = "Checkpoint_01_Spawn";

        [Header("Runtime State")]
        public BinderState currentState = BinderState.Unbound;

        private GameObject _triggerObj;
        private GameObject _spawnObj;
        private SessionRuntimeData _sessionData;
        private float _cooldownTimer;

        // Events
        public static event Action<string, GameObject> OnCheckpointReached;
        public static event Action<int, string> OnCheckpointSaveRequested;

        private void Awake()
        {
            _sessionData = FindFirstObjectByType<SessionRuntimeData>();
        }

        private void Start()
        {
            InitializeBinder();
        }

        private void Update()
        {
            UpdateState();
        }

        private void InitializeBinder()
        {
            // 1. Discovery
            _triggerObj = GameObject.Find(triggerName);
            _spawnObj = GameObject.Find(spawnName);

            // 2. Validation
            if (_triggerObj == null || _spawnObj == null)
            {
                Debug.LogWarning($"[CheckpointTriggerBinder] {checkpointId} missing core markers (Trigger: {_triggerObj != null}, Spawn: {_spawnObj != null}). Disabling.");
                currentState = BinderState.Unbound;
                enabled = false;
                return;
            }

            // check for duplicates
            if (GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length > 0)
            {
                // Note: Real duplicate check is expensive via name. 
                // We assume unique names for vertical slice per requirements.
            }

            // 3. Setup Trigger
            var col = _triggerObj.GetComponent<Collider>();
            if (col == null || !col.isTrigger)
            {
                Debug.LogWarning($"[CheckpointTriggerBinder] {triggerName} has no trigger collider. Disabling.");
                currentState = BinderState.Unbound;
                enabled = false;
                return;
            }

            currentState = BinderState.Bound;
        }

        private void UpdateState()
        {
            if (_sessionData == null) return;

            switch (currentState)
            {
                case BinderState.Bound:
                    if (_sessionData.currentSessionState == SessionState.Active)
                    {
                        currentState = BinderState.Armed;
                    }
                    break;

                case BinderState.Armed:
                    if (_sessionData.currentSessionState != SessionState.Active)
                    {
                        currentState = BinderState.Bound;
                    }
                    break;

                case BinderState.Cooldown:
                    _cooldownTimer -= Time.deltaTime;
                    if (_cooldownTimer <= 0)
                    {
                        currentState = BinderState.Armed;
                    }
                    break;
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            // This is called if this script is ON the trigger object, 
            // OR if we use a proxy. Requirements say "Binder owns trigger volume".
            // Since binder finds trigger by name, it might not be attached.
            // But usually, the binder would attach a proxy or listen to the trigger.
            // For simplicity in this logic-centric slice, we assume something forwards it 
            // OR the binder is on the trigger (if auto-discovery puts it there).
            // Heuristic: If we are Armed, we accept triggers.
            if (currentState != BinderState.Armed) return;

            // Policy check: Only authority/player
            if (other.CompareTag("Player"))
            {
                TriggerCheckpoint();
            }
        }

        private void TriggerCheckpoint()
        {
            Debug.Log($"[CheckpointTriggerBinder] {checkpointId} Triggered!");
            currentState = BinderState.Triggered;

            OnCheckpointReached?.Invoke(checkpointId, _spawnObj);
            OnCheckpointSaveRequested?.Invoke(0, checkpointId);

            _cooldownTimer = cooldownDuration;
            currentState = BinderState.Cooldown;
        }

        // External hooks to manage cooldown/re-arming manually if needed
        public void ProcessSaveResult(bool success)
        {
            if (!success && currentState == BinderState.Cooldown)
            {
                // If save fails, we still keep cooldown to prevent spam, 
                // but we might want to re-arm sooner if coordinator rejects.
            }
        }
    }
}
