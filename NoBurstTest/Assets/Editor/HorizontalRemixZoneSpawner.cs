using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class HorizontalRemixZoneSpawner : EditorWindow
{
    public GameObject remixZonePrefab;
    public int numberOfZones = 8;
    public float spacing = 5f;
    public bool allowRespawn = false;

    private static readonly string parentName = "HorizontalRemixControl";

    [MenuItem("Tools/Spawn Horizontal Remix Zones")]
    public static void ShowWindow()
    {
        GetWindow<HorizontalRemixZoneSpawner>("Remix Zone Spawner");
    }

    void OnGUI()
    {
        remixZonePrefab = (GameObject)EditorGUILayout.ObjectField("Remix Zone Prefab", remixZonePrefab, typeof(GameObject), false);
        numberOfZones = EditorGUILayout.IntField("Number of Zones", numberOfZones);
        spacing = EditorGUILayout.FloatField("Spacing", spacing);
        allowRespawn = EditorGUILayout.Toggle("Allow Respawn", allowRespawn);

        if (GUILayout.Button("Spawn Zones"))
        {
            SpawnZones();
        }
    }

    void SpawnZones()
    {
        if (remixZonePrefab == null)
        {
            Debug.LogWarning("No prefab assigned.");
            return;
        }

        GameObject existingParent = GameObject.Find(parentName);
        if (existingParent && !allowRespawn)
        {
            Debug.Log("Already spawned. Enable 'Allow Respawn' to overwrite.");
            return;
        }

        if (existingParent && allowRespawn)
        {
            DestroyImmediate(existingParent);
        }

        GameObject parent = new GameObject(parentName);

       for (int i = 0; i < numberOfZones; i++)
{
    GameObject zone = (GameObject)PrefabUtility.InstantiatePrefab(remixZonePrefab);
    zone.transform.SetParent(parent.transform);
    
    // Change here
    zone.transform.position = new Vector3(0, 0, i * spacing);

    HorizontalRemixZone zoneScript = zone.GetComponent<HorizontalRemixZone>();
    if (zoneScript != null && zoneScript.parameterTriggers.Count > 0)
    {
        zoneScript.parameterTriggers[0].value = i;
    }
    zone.name = $"RemixZone_{i}";
}

        Debug.Log($"Spawned {numberOfZones} remix zones.");
    }
}