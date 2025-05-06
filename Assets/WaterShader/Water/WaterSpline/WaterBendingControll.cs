using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SplineMesh;

public class WaterBendingControll : MonoBehaviour
{
    [SerializeField] float _PointCount;
    [SerializeField] float _Radius;
    [SerializeField] float _HeightDelta;
    [SerializeField] Vector3 _Scale;

    [SerializeField] Spline _Spline;
    [SerializeField] ExampleContortAlong _ContortAlong;

    [SerializeField] float _SpeedDelta;
    [SerializeField] float _AnimSpeed;
    [SerializeField] ParticleSystem _PuddleParticle;
    [SerializeField] ParticleSystem _SplashParticle;
    [SerializeField] float _SplashActivationOffset;
    [SerializeField] float _PuddleScaleSpeed;

    private Vector3 _target;
    private float _damage;
    private float _radius;
    private float _knockbackForce;

    /// <summary>
    /// Called by WaterBender Attack(...) before WaterBend(), providing damage & knockback
    /// to be applied after the effect finishes.
    /// </summary>
    public void SetupDamage(float dmg, float rad, float knockforce)
    {
        _damage = dmg;
        _radius = rad;
        _knockbackForce = knockforce;
    }

    public void WaterBend(Vector3 target)
    {
        _target = target;
        StopAllCoroutines();
        StartCoroutine(Coroutine_WaterBend());
    }

    IEnumerator Coroutine_WaterBend()
    {
        // Hide visuals
        _Spline.gameObject.SetActive(false);
        _SplashParticle.gameObject.SetActive(false);
        _PuddleParticle.gameObject.SetActive(false);

        ConfigureSpline();

        _ContortAlong.Init();
        float meshLength = _ContortAlong.MeshBender.Source.Length;
        if (meshLength == 0) meshLength = 1f;
        float totalLength = meshLength + _Spline.Length;

        Vector3 startScale = _Scale; 
        startScale.x = 0;
        Vector3 targetScale = _Scale;

        float speedCurveLerp = 0;
        float distanceAlongSpline = 0;

        // show puddle
        _PuddleParticle.gameObject.SetActive(true);
        _PuddleParticle.transform.localPosition = _Spline.nodes[0].Position;

        // fade puddle in
        Vector3 startPuddleScale = Vector3.zero;
        Vector3 endPuddleScale = _PuddleParticle.transform.localScale;
        float lerp = 0f;
        while (lerp < 1f)
        {
            _PuddleParticle.transform.localScale = Vector3.Lerp(startPuddleScale, endPuddleScale, lerp);
            lerp += Time.deltaTime * _PuddleScaleSpeed;
            yield return null;
        }

        _Spline.gameObject.SetActive(true);
        _PuddleParticle.Play();

        // Move along spline
        while (distanceAlongSpline < totalLength)
        {
            if (distanceAlongSpline < meshLength)
            {
                float t = distanceAlongSpline / meshLength;
                _ContortAlong.ScaleMesh(Vector3.Lerp(startScale, targetScale, t));
            }
            else
            {
                if (_PuddleParticle.isPlaying)
                    _PuddleParticle.Stop();

                float tSpline = (distanceAlongSpline - meshLength) / _Spline.Length;
                _ContortAlong.Contort(tSpline);

                if (distanceAlongSpline + meshLength > totalLength + _SplashActivationOffset)
                {
                    if (!_SplashParticle.isPlaying)
                    {
                        _SplashParticle.gameObject.SetActive(true);
                        _SplashParticle.transform.position = _target;
                        _SplashParticle.Play();
                    }
                }
            }

            distanceAlongSpline += Time.deltaTime * _AnimSpeed * speedCurveLerp;
            speedCurveLerp += _SpeedDelta * Time.deltaTime;
            yield return null;
        }

        // done
        _Spline.gameObject.SetActive(false);
        _SplashParticle.Stop();

        // apply damage at end
        ApplyDamageAtTarget();
        Destroy(gameObject, 2f);
    }

    private void ApplyDamageAtTarget()
    {
        // OverlapSphere to find enemies
        Collider[] hitEnemies = Physics.OverlapSphere(_target, _radius);
        foreach (var enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth))
            {
                enemyHealth.TakeDamage(_damage);

                Vector3 direction = (enemy.transform.position - _target).normalized;
                enemyHealth.ApplyKnockback(direction * _knockbackForce);
            }
        }
    }

    private void ConfigureSpline()
    {
        // remove extra nodes
        List<SplineNode> nodes = new List<SplineNode>(_Spline.nodes);
        for (int i = 2; i < nodes.Count; i++)
        {
            _Spline.RemoveNode(nodes[i]);
        }

        // keep spawn pos
        Vector3 targetDirection = _target - transform.position;
        transform.forward = targetDirection.normalized;

        int sign = (Random.Range(0,2) == 0)? 1 : -1;
        float angle = 90f * sign;
        float height = 0f;

        for (int i = 0; i < _PointCount; i++)
        {
            if (_Spline.nodes.Count <= i)
            {
                _Spline.AddNode(new SplineNode(Vector3.zero, Vector3.forward));
            }

            Vector3 normal = Quaternion.Euler(0, angle, 0) * transform.forward;

            Vector3 pos = transform.position + normal * _Radius;
            pos.y += height;
            Vector3 dir = pos + Quaternion.Euler(Random.Range(-30,30), 
                           Random.Range(60,120)*sign, 
                           Random.Range(-30,30)) * normal * (_Radius/2f);

            if (i == 0)
            {
                dir = pos + Vector3.up * _Radius;
            }

            _Spline.nodes[i].Position = transform.InverseTransformPoint(pos);
            _Spline.nodes[i].Direction = transform.InverseTransformPoint(dir);

            height += _HeightDelta;
            angle += 90f * sign;
        }

        // final node for target
        Vector3 targetNodePos = transform.InverseTransformPoint(_target);

        Quaternion randomRot = Quaternion.Euler(Random.Range(0,90), Random.Range(-40,40), 0);
        float dist = Vector3.Distance(transform.position, _target);
        Vector3 offsetDir = transform.forward * dist * Random.Range(0.2f,1f);
        Vector3 targetNodeDirWorld = _target + randomRot * offsetDir;

        Vector3 targetNodeDirLocal = transform.InverseTransformPoint(targetNodeDirWorld);
        SplineNode node = new SplineNode(targetNodePos, targetNodeDirLocal);
        _Spline.AddNode(node);
    }
}
