using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterTubeController : MonoBehaviour
{
    [SerializeField] private Transform _CreationPoint;
    [SerializeField] private GameObject _WaterTubePrefab;
    [SerializeField] private float knockbackForce = 15f; // default if not overridden
    [SerializeField] private float tubeDuration = 1.5f;

    /// <summary>
    /// Spawns the WaterTube at 'spawnPosition', traveling to 'target',
    /// dealing 'damage' in radius 'radius', and applying 'knockbackForce' once it arrives.
    /// </summary>
    public void InstantiateWaterTube(Vector3 spawnPosition, Vector3 target, float damage, float radius, float tubeKnockback)
    {
        Vector3 direction = (target - spawnPosition);
        direction.y = 0;
        direction = direction.normalized;

        GameObject waterTube = Instantiate(_WaterTubePrefab, spawnPosition, Quaternion.identity);
        waterTube.transform.forward = direction;

        // Add movement
        WaterTubeMovement movement = waterTube.AddComponent<WaterTubeMovement>();
        movement.Initialize(target, damage, radius, tubeKnockback, tubeDuration);

        Destroy(waterTube, tubeDuration + 0.5f);
    }
}

public class WaterTubeMovement : MonoBehaviour
{
    private Vector3 targetPosition;
    private float damage;
    private float radius;
    private float knockbackForce;
    private float duration;
    private float moveSpeed;
    private bool hasDealtDamage = false;

    /// <summary>
    /// Called by WaterTubeController to set all needed data.
    /// </summary>
    public void Initialize(Vector3 target, float dmg, float rad, float kbForce, float dur)
    {
        targetPosition = target;
        damage = dmg;
        radius = rad;
        knockbackForce = kbForce;
        duration = dur;

        moveSpeed = Vector3.Distance(transform.position, target) / duration;

        StartCoroutine(MoveAndDamage());
    }

    private IEnumerator MoveAndDamage()
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < duration)
        {
            // Move toward target
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Once arrived, apply damage if not done
        if (!hasDealtDamage)
        {
            ApplyDamageAndKnockback();
            hasDealtDamage = true;
        }
    }

    private void ApplyDamageAndKnockback()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(targetPosition, radius);
        foreach (var enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth))
            {
                enemyHealth.TakeDamage(damage);

                // knockback
                Vector3 direction = (enemy.transform.position - targetPosition).normalized;
                enemyHealth.ApplyKnockback(direction * knockbackForce);
            }
        }
    }
}
