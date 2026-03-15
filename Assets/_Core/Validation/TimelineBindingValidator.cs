using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Video;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Linq;

namespace Core.Validation
{
    /// <summary>
    /// Runtime utility to validate Timeline track bindings before playback.
    /// Can be called before PlayableDirector.Play() to prevent broken cutscenes.
    /// </summary>
    public static class TimelineBindingValidator
    {
        public enum ValidationResult
        {
            Success,
            Warning, // Non-critical missing tracks
            Failure  // Critical missing tracks (cannot play)
        }

        /// <summary>
        /// Validates all tracks in the director's Timeline asset.
        /// </summary>
        /// <param name="director">The director to validate.</param>
        /// <param name="errors">Output list of error messages.</param>
        /// <returns>Severity of the validation issues found.</returns>
        public static ValidationResult ValidateBindings(PlayableDirector director, out List<string> errors)
        {
            errors = new List<string>();
            if (director == null || director.playableAsset == null)
            {
                errors.Add("PlayableDirector or PlayableAsset is null.");
                return ValidationResult.Failure;
            }

            if (!(director.playableAsset is TimelineAsset timeline))
            {
                errors.Add("PlayableAsset is not a TimelineAsset.");
                return ValidationResult.Failure;
            }

            bool hasCriticalFailure = false;
            bool hasWarning = false;

            foreach (var track in timeline.GetOutputTracks())
            {
                // Skip empty tracks if they are allowed (e.g. organizational markers)
                if (track.isEmpty) continue;

                var binding = director.GetGenericBinding(track);
                
                // 1. Basic Binding Existence
                if (binding == null)
                {
                    string msg = $"[Timeline] Track '{track.name}' is missing a binding.";
                    errors.Add(msg);
                    Debug.LogError(msg, director);
                    
                    if (IsCriticalTrack(track))
                        hasCriticalFailure = true;
                    else
                        hasWarning = true;
                    
                    continue; // Skip further checks for this track if binding is null
                }

                // 2. Specific Track Type Validation
                GameObject boundGo = binding as GameObject;
                if (boundGo == null && binding is Component comp)
                    boundGo = comp.gameObject;

                if (track is ControlTrack)
                {
                    // For Control Tracks, we often look for VideoPlayer if it's meant to control video
                    // Note: Control tracks can control many things (prefabs, scripts), 
                    // but the prompt specifically mentions VideoPlayer.
                    if (boundGo != null && boundGo.GetComponent<VideoPlayer>() == null)
                    {
                        // Some control tracks might not need VideoPlayer, so we check if it's likely a video track
                        if (track.name.ToLower().Contains("video"))
                        {
                            errors.Add($"[Timeline] Control Track '{track.name}' bound to '{boundGo.name}' which has no VideoPlayer.");
                            hasWarning = true;
                        }
                    }
                }
                else if (track is ActivationTrack)
                {
                    if (boundGo == null)
                    {
                        errors.Add($"[Timeline] Activation Track '{track.name}' has no bound GameObject.");
                        hasCriticalFailure = true;
                    }
                }
                else if (track is CinemachineTrack)
                {
                    if (boundGo != null && boundGo.GetComponent<CinemachineCamera>() == null)
                    {
                        errors.Add($"[Timeline] Cinemachine Track '{track.name}' bound to '{boundGo.name}' which is NOT a Cinemachine Virtual Camera.");
                        hasCriticalFailure = true;
                    }
                }
                else if (track is SignalTrack)
                {
                    var receiver = director.GetComponent<SignalReceiver>();
                    if (receiver == null && boundGo != null)
                        receiver = boundGo.GetComponent<SignalReceiver>();

                    if (receiver == null)
                    {
                        errors.Add($"[Timeline] Signal Track '{track.name}' has no registered SignalReceiver on Director or bound object.");
                        hasWarning = true;
                    }
                }
            }

            if (hasCriticalFailure) return ValidationResult.Failure;
            if (hasWarning) return ValidationResult.Warning;
            return ValidationResult.Success;
        }

        private static bool IsCriticalTrack(TrackAsset track)
        {
            // Define which tracks are considered 'Critical' (cannot play without them)
            return track is ActivationTrack || track is CinemachineTrack;
        }
    }
}
