using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBall : MonoBehaviour
{
    [SerializeField] ParticleSystem _WaterBallParticleSystem;
    [SerializeField] AnimationCurve _SpeedCurve;
    [SerializeField] float _Speed;
    [SerializeField] ParticleSystem _SplashPrefab;
    [SerializeField] ParticleSystem _SpillPrefab;

    private float damage;
    private float knockbackForce;

    public void Throw(Vector3 target)
    {
        StopAllCoroutines();
        StartCoroutine(Coroutine_Throw(target));
    }

    IEnumerator Coroutine_Throw(Vector3 target) 
    {
        float lerp = 0;
        Vector3 startPos = transform.position;
        while (lerp < 1)
        {
            transform.position = Vector3.Lerp(startPos, target, _SpeedCurve.Evaluate(lerp));
            float magnitude = (transform.position - target).magnitude;
            if (magnitude < 0.4f)
            {
                break;
            }
            lerp += Time.deltaTime * _Speed;
            yield return null;
        }
        _WaterBallParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        // spawn splash
        ParticleSystem splas = Instantiate(_SplashPrefab, target, Quaternion.identity);
        Vector3 forward = target - startPos;
        forward.y = 0;
        splas.transform.forward = forward;

        // if angled enough, spawn spill
        if (Vector3.Angle(startPos - target, Vector3.up) > 30)
        {
            ParticleSystem spill = Instantiate(_SpillPrefab, target, Quaternion.identity);
            spill.transform.forward = forward;
        }

        Destroy(gameObject, 0.5f);
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void SetKnockback(float kb)
    {
        knockbackForce = kb;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out EnemyHealth enemyHealth))
        {
            Debug.Log("Hit enemy: " + collision.gameObject.name);
            enemyHealth.TakeDamage(damage);

            // Apply knockback
            Vector3 direction = collision.transform.position - transform.position;
            direction.y = 0; 
            direction.Normalize();

            enemyHealth.ApplyKnockback(direction * knockbackForce);

            // Optionally destroy the water ball
            //Destroy(gameObject);
        }
        else
        {
            Debug.Log("Hit non-enemy object: " + collision.gameObject.name);
        }
    }
}
