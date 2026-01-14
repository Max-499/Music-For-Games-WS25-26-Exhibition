using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FootstepSurfaceOverviewEditor : EditorWindow
{
    Vector2 scrollPos;

    class SurfaceEntry
    {
        public MeshCollider collider;
        public PhysicsMaterial currentMaterial;
        public PhysicsMaterial newMaterial;
    }

    List<SurfaceEntry> surfaceEntries = new();

    [MenuItem("Tools/Footstep Surface Overview")]
    public static void ShowWindow()
    {
        GetWindow<FootstepSurfaceOverviewEditor>("Footstep Surface Overview");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Scan Scene"))
        {
            ScanScene();
        }

        if (surfaceEntries.Count > 0)
        {
            GUILayout.Space(10);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var entry in surfaceEntries)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(entry.collider.gameObject.name, GUILayout.Width(200));
                entry.newMaterial = (PhysicsMaterial)EditorGUILayout.ObjectField(entry.newMaterial, typeof(PhysicsMaterial), false, GUILayout.Width(200));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            if (GUILayout.Button("Apply New Materials and Rename Objects"))
            {
                ApplyChanges();
            }
        }
    }

    void ScanScene()
    {
        surfaceEntries.Clear();

        var allColliders = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
        foreach (var mc in allColliders)
        {
            surfaceEntries.Add(new SurfaceEntry
            {
                collider = mc,
                currentMaterial = mc.sharedMaterial,
                newMaterial = mc.sharedMaterial
            });
        }

        Debug.Log($"[FootstepSurfaceOverview] Found {surfaceEntries.Count} mesh colliders.");
    }

    void ApplyChanges()
{
    foreach (var entry in surfaceEntries)
    {
        if (entry.collider == null) continue;

        GameObject go = entry.collider.gameObject;
        bool changedMaterial = entry.collider.sharedMaterial != entry.newMaterial;

        if (changedMaterial)
        {
            Undo.RecordObject(entry.collider, "Change Physic Material");
            entry.collider.sharedMaterial = entry.newMaterial;
            EditorUtility.SetDirty(entry.collider);
        }

        // Rename GameObject if it's named like bottom_Wood -> bottom
        if (go.name.StartsWith("bottom_"))
        {
            Undo.RecordObject(go, "Rename Base Object");
            go.name = "bottom";
        }

        // Update the label child
        if (entry.newMaterial != null)
        {
            string matName = entry.newMaterial.name;

            foreach (Transform child in go.transform)
            {
                if (child.name.StartsWith("Label_"))
                {
                    Undo.RecordObject(child.gameObject, "Update Label");
                    child.name = $"Label_{matName}";

                    TextMesh tm = child.GetComponent<TextMesh>();
                    if (tm != null)
                    {
                        tm.text = matName;
                        EditorUtility.SetDirty(tm);
                    }
                }
            }
        }
    }

    Debug.Log("[FootstepSurfaceOverview] Materials, base names, and labels updated.");
}
}