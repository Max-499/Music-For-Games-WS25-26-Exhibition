using UnityEngine;
using System.Collections;

public class RandomCubeMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Wait Time Range")]
    [SerializeField] private float minWaitTime = 0.5f;
    [SerializeField] private float maxWaitTime = 4f;

    private Vector3 targetPosition;

    private void Start()
    {
        StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        while (true)
        {
            // Choose a random destination inside the defined cube
            targetPosition = GetRandomPositionInBounds();

            // Move until the destination is reached
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            // Wait for a random duration before next movement
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private Vector3 GetRandomPositionInBounds()
    {
        float x = Random.Range(-15f, 15f);
        float y = Random.Range(0.5f, 6f);
        float z = Random.Range(-22f, 5f);

        return new Vector3(x, y, z);
    }
}

