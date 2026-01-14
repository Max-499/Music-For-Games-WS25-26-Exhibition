using UnityEngine;
using FMODUnity;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform shootOrigin;
    public float launchVelocity = 20f;
    public float cooldown = 0.5f;
    public bool autoShoot = false;

    [Header("FMOD")]
    public EventReference fireEvent;
    public EventReference explosionEvent;
    public bool explode = true;

    private float lastShotTime;

    void Update()
    {
        if ((autoShoot || Input.GetButtonDown("Fire1")) && Time.time - lastShotTime >= cooldown)
        {
            Shoot();
            lastShotTime = Time.time;
        }
    }

    void Shoot()
    {
        if (!projectilePrefab || !shootOrigin)
        {
            Debug.LogWarning("[ProjectileShooter] Missing prefab or shoot origin.");
            return;
        }

        GameObject proj = Instantiate(projectilePrefab, shootOrigin.position, shootOrigin.rotation);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = shootOrigin.forward * launchVelocity;

        // Make sure the projectile has a trigger collider
        Collider col = proj.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        // Add trigger script and initialize with shooter to ignore
        ProjectileTrigger trigger = proj.AddComponent<ProjectileTrigger>();
        trigger.Initialize(this.gameObject, explosionEvent, explode);

        // Play firing sound
        if (!fireEvent.IsNull)
            RuntimeManager.PlayOneShotAttached(fireEvent, proj);

        Destroy(proj, 10f); // Cleanup
    }

    // Internal helper class
    private class ProjectileTrigger : MonoBehaviour
    {
        private GameObject ignoreObject;
        private EventReference explosionEvent;
        private bool shouldExplode;
        private float activationDelay = 0.1f;
        private float spawnTime;

        public void Initialize(GameObject ignore, EventReference evt, bool explode)
        {
            ignoreObject = ignore;
            explosionEvent = evt;
            shouldExplode = explode;
            spawnTime = Time.time;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Time.time - spawnTime < activationDelay)
            {
                Debug.Log("[ProjectileTrigger] Ignored early collision.");
                return;
            }

            if (other.gameObject == ignoreObject)
            {
                Debug.Log("[ProjectileTrigger] Ignored self collision.");
                return;
            }

            Debug.Log($"[ProjectileTrigger] Hit {other.gameObject.name}");

            if (shouldExplode && !explosionEvent.IsNull)
                RuntimeManager.PlayOneShot(explosionEvent, transform.position);

            Destroy(gameObject);
        }
    }
}