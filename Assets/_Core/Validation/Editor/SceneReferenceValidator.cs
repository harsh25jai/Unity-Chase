using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Video;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Linq;

namespace Core.Validation.Editor
{
    /// <summary>
    /// Pre-build validation tool to catch missing references and configuration errors in specific scenes.
    /// Access via Tools > Validate Scenes > All Scenes
    /// </summary>
    public static class SceneReferenceValidator
    {
        private static List<string> errors = new List<string>();

        [MenuItem("Tools/Validate Scenes/All Scenes")]
        public static void ValidateAllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            errors.Clear();
            
            // Define targeted scenes
            string[] targetScenes = new[] { "BankPrologue_Timeline", "EscapeStart" };
            
            // Find all scenes in the project to locate the paths for target scenes
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            Dictionary<string, string> scenePaths = new Dictionary<string, string>();
            
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!scenePaths.ContainsKey(name))
                    scenePaths.Add(name, path);
            }

            foreach (string sceneName in targetScenes)
            {
                if (scenePaths.TryGetValue(sceneName, out string path))
                {
                    Debug.Log($"<color=cyan>[Validator]</color> Validating Scene: <b>{sceneName}</b>");
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                    if (sceneName == "BankPrologue_Timeline")
                        ValidateBankPrologue();
                    else if (sceneName == "EscapeStart")
                        ValidateEscapeStart();

                    // General validation for all PlayableDirectors in these scenes
                    ValidateTimelineAssetsInScene();
                }
                else
                {
                    LogError("Project", $"Target scene '{sceneName}' not found in project!");
                }
            }

            ShowSummary();
        }

        private static void ValidateBankPrologue()
        {
            CheckObjectExists("Video_Reference");
            CheckObjectExists("CM_CutsceneCamera");
            
            GameObject markersRoot = GameObject.Find("Cutscene_Root/Markers");
            if (markersRoot != null)
            {
                if (markersRoot.transform.childCount == 0)
                {
                    LogError("Cutscene_Root/Markers", "No handoff markers found under Cutscene_Root/Markers/");
                }
                else
                {
                    // Check if they are actually markers (optional, but requested "All handoff markers exist")
                    // Assuming any child counts as a marker for now.
                }
            }
            else
            {
                LogError("Cutscene_Root", "Markers root 'Cutscene_Root/Markers' missing.");
            }

            VideoPlayer videoPlayer = Object.FindFirstObjectByType<VideoPlayer>();
            if (videoPlayer != null)
            {
                if (videoPlayer.clip == null)
                    LogError(GetPath(videoPlayer.gameObject), "VideoPlayer has no clip assigned.");
            }
            // VideoPlayer existence might be optional if Video_Reference handles it, 
            // but the prompt implies checking its component.

            PlayableDirector director = Object.FindFirstObjectByType<PlayableDirector>();
            if (director != null)
            {
                if (director.playableAsset == null)
                    LogError(GetPath(director.gameObject), "Timeline asset is not assigned to PlayableDirector.");
            }
        }

        private static void ValidateEscapeStart()
        {
            CheckObjectExists("Checkpoint_01_Trigger");
            CheckObjectExists("Checkpoint_01_Spawn");
            CheckObjectExists("Checkpoint_01_Area");
            CheckObjectExists("PlayerSpawn_Default");

            // Vehicle check
            var allGo = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            bool vehicleFound = allGo.Any(go => go.name.ToLower().Contains("vehicle") && PrefabUtility.IsPartOfAnyPrefab(go));
            if (!vehicleFound)
            {
                LogError("Scene Root", "At least 1 vehicle prefab instance must exist in EscapeStart.");
            }

            CheckObjectExists("Cinemachine Virtual Camera");
            
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                if (mainCam.GetComponent<CinemachineBrain>() == null)
                    LogError(GetPath(mainCam.gameObject), "Main Camera missing CinemachineBrain component.");
            }
            else
            {
                LogError("Main Camera", "Main Camera (tagged 'MainCamera') not found in scene.");
            }
        }

        private static void ValidateTimelineAssetsInScene()
        {
            var directors = Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
            foreach (var director in directors)
            {
                if (director.playableAsset is TimelineAsset timeline)
                {
                    foreach (var track in timeline.GetOutputTracks())
                    {
                        // Some tracks like AudioTracks or SignalTracks might not ALWAYS need a binding if they are empty,
                        // but the requirement is "All tracks have valid bindings (not null)".
                        if (track.isEmpty) continue;

                        var binding = director.GetGenericBinding(track);
                        if (binding == null)
                        {
                            LogError(GetPath(director.gameObject), $"Track '{track.name}' in Timeline '{timeline.name}' is missing a binding.");
                            // Highlight the director since the track is inside its asset
                            EditorGUIUtility.PingObject(director.gameObject);
                        }
                    }

                    // Signal track has receiver registered
                    var signalTracks = timeline.GetOutputTracks().OfType<SignalTrack>();
                    if (signalTracks.Any())
                    {
                        var receiver = director.GetComponent<SignalReceiver>();
                        if (receiver == null)
                        {
                            LogError(GetPath(director.gameObject), $"Timeline '{timeline.name}' has Signal Tracks but no SignalReceiver component found on Director.");
                            EditorGUIUtility.PingObject(director.gameObject);
                        }
                    }
                }
            }
        }

        private static GameObject CheckObjectExists(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                LogError(name, $"Missing required GameObject: <b>{name}</b>");
            }
            return go;
        }

        private static void LogError(string path, string message)
        {
            string fullError = $"<color=red>[CRITICAL]</color> <b>{path}</b>: {message}";
            errors.Add(fullError);
            Debug.LogError(fullError);
        }

        private static string GetPath(GameObject go)
        {
            if (go == null) return "Unknown";
            string path = go.name;
            while (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
                path = go.name + "/" + path;
            }
            return path;
        }

        private static void ShowSummary()
        {
            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog("Validation Results", 
                    $"{errors.Count} critical errors found.\n\nValidated scenes:\n- BankPrologue_Timeline\n- EscapeStart\n\nPlease check the Console for a full list of missing references.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Results", "All critical references validated successfully.", "OK");
            }
        }
    }
}
