using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEditor.Timeline;
using System.IO;
using System.Collections.Generic;

public class CleanupBrokenFmodTracks : EditorWindow
{
    [MenuItem("Tools/FMOD/Cleanup Broken FMOD Timeline Tracks")]
    public static void CleanBrokenFMODTracks()
    {
        string[] guids = AssetDatabase.FindAssets("t:TimelineAsset");

        int totalRemoved = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);

            if (timeline == null) continue;

            bool modified = false;
            List<TrackAsset> tracksToRemove = new();

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track == null) continue;

                string typeName = track.GetType().FullName;

                if (typeName != null && typeName.Contains("FMODUnity.FMODEventTrack"))
                {
                    // Extra safety: check if any clips are null or empty
                    bool hasBrokenClips = false;
                    foreach (var clip in track.GetClips())
                    {
                        if (clip == null || clip.asset == null)
                        {
                            hasBrokenClips = true;
                            break;
                        }
                    }

                    if (hasBrokenClips)
                    {
                        tracksToRemove.Add(track);
                    }
                }
            }

            foreach (var t in tracksToRemove)
            {
                timeline.DeleteTrack(t);
                modified = true;
                totalRemoved++;
                Debug.Log($"[Cleanup] Removed broken FMOD track in timeline: {timeline.name}");
            }

            if (modified)
            {
                EditorUtility.SetDirty(timeline);
                AssetDatabase.SaveAssets();
            }
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("FMOD Cleanup", $"Removed {totalRemoved} broken FMOD timeline track(s).", "OK");
    }
}