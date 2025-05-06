using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBallControll : MonoBehaviour
{
    [SerializeField] private Transform _CreationPoint;
    [SerializeField] private WaterBall WaterBallPrefab;
    private WaterBall waterBall;

    public bool WaterBallCreated()
    {
        return waterBall != null;
    }

    public WaterBall CreateWaterBall()
    {
        waterBall = Instantiate(WaterBallPrefab, _CreationPoint.position, Quaternion.identity);
        return waterBall;
    }

    public void ThrowWaterBall(Vector3 pos)
    {
        if (waterBall != null)
        {
            waterBall.Throw(pos);
            waterBall = null;
        }
    }
}
