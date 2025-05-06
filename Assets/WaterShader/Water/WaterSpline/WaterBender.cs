using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBender : MonoBehaviour
{
    [SerializeField] private WaterBendingControll _WaterPrefab;

    /// <summary>
    /// Attack spawns the water bending effect & defers actual damage/knockback to WaterBendingControll.
    /// </summary>
    /// <param name="spawnPosition">Where to place the bending prefab initially</param>
    /// <param name="target">Where the bend effect aims</param>
    /// <param name="damage">Damage to apply at the end</param>
    /// <param name="radius">Area of effect radius</param>
    /// <param name="knockbackForce">Knockback for each enemy in radius</param>
    public void Attack(Vector3 spawnPosition, Vector3 target, float damage, float radius, float knockbackForce)
    {
        WaterBendingControll water = Instantiate(_WaterPrefab, spawnPosition, Quaternion.identity);
        water.SetupDamage(damage, radius, knockbackForce);
        water.WaterBend(target);
    }
}
