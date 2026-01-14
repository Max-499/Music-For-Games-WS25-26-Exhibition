using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class HorizontalRemixZone : MonoBehaviour
{
    [System.Serializable]
    public class ParameterTrigger
    {
        public string parameterName = "HorizontalRemix";
        public float value = 0;
    }

    [Tooltip("Parameters to trigger when entering the zone.")]
    public List<ParameterTrigger> parameterTriggers = new List<ParameterTrigger> { new ParameterTrigger() };

    [Header("Visual Settings")]
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.2f); // Cyan transparent
    public Color labelColor = Color.white;
    public Vector3 labelOffset = new Vector3(0, 1.5f, 0);
    public bool showLabelInGame = true;

    private GameObject zoneVisual;
    private GameObject labelObject;
    private GameObject triggerChild;
    private BoxCollider triggerCollider;

    private const string TriggerChildName = "RemixTriggerZone";

    void OnEnable()
    {
        SetupTriggerChild();
        SetupVisualCube();
        SetupLabel();
    }

    void OnDisable()
    {
        if (labelObject) DestroyImmediate(labelObject);
        if (zoneVisual) DestroyImmediate(zoneVisual);
    }

    void SetupTriggerChild()
    {
        triggerChild = transform.Find(TriggerChildName)?.gameObject;

        if (!triggerChild)
        {
            triggerChild = new GameObject(TriggerChildName);
            triggerChild.transform.SetParent(transform);
            triggerChild.transform.localPosition = Vector3.zero;
            triggerChild.transform.localRotation = Quaternion.identity;
        }

        // Ensure trigger collider exists
        triggerCollider = triggerChild.GetComponent<BoxCollider>();
        if (!triggerCollider)
            triggerCollider = triggerChild.AddComponent<BoxCollider>();

        triggerCollider.isTrigger = true;
        triggerCollider.size = Vector3.one;
    }

    void SetupVisualCube()
    {
        if (!zoneVisual)
        {
            zoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zoneVisual.name = "ZoneVisual";
            zoneVisual.transform.SetParent(triggerChild.transform);
            zoneVisual.transform.localPosition = Vector3.zero;
            zoneVisual.transform.localRotation = Quaternion.identity;

            var rend = zoneVisual.GetComponent<Renderer>();
            if (rend)
            {
                var mat = new Material(Shader.Find("Transparent/Diffuse"))
                {
                    color = gizmoColor
                };
                rend.material = mat;
            }

            DestroyImmediate(zoneVisual.GetComponent<Collider>());
        }

        zoneVisual.transform.localScale = triggerCollider.size;
    }

    void SetupLabel()
    {
        if (showLabelInGame && labelObject == null)
        {
            labelObject = new GameObject("RemixLabel");
            labelObject.transform.SetParent(triggerChild.transform);
            labelObject.transform.localPosition = labelOffset;

            var tm = labelObject.AddComponent<TextMesh>();
            tm.text = string.Join(" | ", parameterTriggers.ConvertAll(p => $"{p.parameterName}={p.value}"));
            tm.characterSize = 0.3f;
            tm.fontSize = 80;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = labelColor;

            var renderer = tm.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    void Update()
    {
        if (labelObject && Camera.main)
        {
            labelObject.transform.rotation = Quaternion.LookRotation(labelObject.transform.position - Camera.main.transform.position);
        }
    }

    void OnDrawGizmos()
    {
        if (triggerCollider == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = triggerChild ? triggerChild.transform.localToWorldMatrix : transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, triggerCollider.size);

#if UNITY_EDITOR
        if (showLabelInGame && triggerChild)
        {
            UnityEditor.Handles.color = labelColor;
            UnityEditor.Handles.Label(triggerChild.transform.position + labelOffset,
                string.Join(" | ", parameterTriggers.ConvertAll(p => $"{p.parameterName}={p.value}")));
        }
#endif
    }
}