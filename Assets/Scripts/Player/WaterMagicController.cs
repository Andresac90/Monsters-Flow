using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#region Command Pattern Classes
public class ShootWaterBallCommand : ICommand
{
    private WaterMagicController _controller;
    public ShootWaterBallCommand(WaterMagicController controller) { _controller = controller; }
    public void Execute() { _controller.StartCoroutine(_controller.ShootWaterBall()); }
}

public class UseWaterBendCommand : ICommand
{
    private WaterMagicController _controller;
    public UseWaterBendCommand(WaterMagicController controller) { _controller = controller; }
    public void Execute() { _controller.StartCoroutine(_controller.UseWaterBend()); }
}

public class UseWaterTubeCommand : ICommand
{
    private WaterMagicController _controller;
    public UseWaterTubeCommand(WaterMagicController controller) { _controller = controller; }
    public void Execute() { _controller.StartCoroutine(_controller.UseWaterTube()); }
}
#endregion

/// Main controller for water‑based abilities: WaterBall, WaterBend, WaterTube.
public class WaterMagicController : MonoBehaviour
{
    [Header("Water Magic References")]
    [SerializeField] private WaterBallControll   waterBallController;
    [SerializeField] private WaterBender         waterBenderController;
    [SerializeField] private WaterTubeController waterTubeController;

    [Header("Hand Model Animator (6 States)")]
    [SerializeField] private Animator handAnimator;

    [Header("Cooldown Settings")]
    [SerializeField] private float waterBendCooldown  = 5f;
    [SerializeField] private float waterTubeCooldown = 7f;
    [SerializeField] private float waterBallFireRate = 0.2f;

    [Header("Damage Settings")]
    [SerializeField] private float waterBallDamage = 10f;
    [SerializeField] private float waterBendDamage = 15f;
    [SerializeField] private float waterTubeDamage = 20f;

    [Header("Attack Radii (Inspector)")]
    [Tooltip("Splash radius for water ball (if implemented).")]
    [SerializeField] private float waterBallRadius = 1f;
    [Tooltip("Area‑of‑effect radius for Water Bend.")]
    [SerializeField] private float waterBendRadius = 3f;
    [Tooltip("Hit radius for Water Tube.")]
    [SerializeField] private float waterTubeRadius = 3f;

    [Header("Knockback Settings")]
    [SerializeField] private float waterBallKnockback = 5f;
    [SerializeField] private float waterBendKnockback = 8f;
    [SerializeField] private float waterTubeKnockback = 12f;

    [Header("UI & Targeting")]
    [SerializeField] private Image  crosshair;
    [SerializeField] private Camera playerCamera;

    // runtime flags & timers
    private bool _canShootBall = true, _canUseBend = true, _canUseTube = true;
    private float _ballTimer = 0f, _bendTimer = 0f, _tubeTimer = 0f;
    private WaterBall activeWaterBall;

    public bool unlockedWaterBend = false;
    public bool unlockedWaterTube = false;

    public float WaterBallCooldownRemaining => Mathf.Max(0f, _ballTimer);
    public float WaterBendCooldownRemaining => Mathf.Max(0f, _bendTimer);
    public float WaterTubeCooldownRemaining => Mathf.Max(0f, _tubeTimer);
    public float WaterBallCooldownMax => waterBallFireRate;
    public float WaterBendCooldownMax => waterBendCooldown;
    public float WaterTubeCooldownMax => waterTubeCooldown;

    public float WaterBallFireRate
    {
        get => waterBallFireRate;
        set => waterBallFireRate = Mathf.Max(0.01f, value);
    }

    private void Update()
    {
        // Cooldown timers
        if (_ballTimer > 0f) _ballTimer -= Time.deltaTime;
        if (_bendTimer > 0f) _bendTimer -= Time.deltaTime;
        if (_tubeTimer > 0f) _tubeTimer -= Time.deltaTime;

        // Input
        if (Input.GetMouseButton(0) && _canShootBall)
            new ShootWaterBallCommand(this).Execute();

        if (Input.GetKeyDown(KeyCode.Alpha1) && _canUseBend && unlockedWaterBend)
            new UseWaterBendCommand(this).Execute();

        if (Input.GetKeyDown(KeyCode.Alpha2) && _canUseTube && unlockedWaterTube)
            new UseWaterTubeCommand(this).Execute();

        if (activeWaterBall != null)
            AttachWaterBallToCamera();
    }

    public IEnumerator ShootWaterBall()
    {
        _canShootBall = false;
        _ballTimer = waterBallFireRate;

        if (!waterBallController.WaterBallCreated())
        {
            if (handAnimator != null) handAnimator.SetInteger("animation", 1);
            Vector3 spawn = playerCamera.transform.position + playerCamera.transform.forward * 2f;
            activeWaterBall = waterBallController.CreateWaterBall();
            activeWaterBall.transform.position = spawn;
            activeWaterBall.transform.rotation = playerCamera.transform.rotation;
            activeWaterBall.SetDamage(waterBallDamage);
            activeWaterBall.SetKnockback(waterBallKnockback);
        }
        else
        {
            if (handAnimator != null) handAnimator.SetInteger("animation", 3);
            Vector3 target = GetCrosshairTarget();
            waterBallController.ThrowWaterBall(target);
            // If you later apply AOE on impact, use waterBallRadius here.
            activeWaterBall = null;
        }

        yield return new WaitForSeconds(waterBallFireRate);
        _canShootBall = true;
        if (handAnimator != null) handAnimator.SetInteger("animation", 0);
    }

    public IEnumerator UseWaterBend()
    {
        _canUseBend = false;
        _bendTimer = waterBendCooldown;
        if (handAnimator != null) handAnimator.SetInteger("animation", 4);

        Vector3 target = GetCrosshairTarget();
        Vector3 spawn  = playerCamera.transform.position + playerCamera.transform.forward * 2f;
        waterBenderController.Attack(spawn, target, waterBendDamage, waterBendRadius, waterBendKnockback);

        yield return new WaitForSeconds(waterBendCooldown);
        _canUseBend = true;
    }

    public IEnumerator UseWaterTube()
    {
        _canUseTube = false;
        _tubeTimer = waterTubeCooldown;
        if (handAnimator != null) handAnimator.SetInteger("animation", 5);

        Vector3 target = GetCrosshairTarget();
        Vector3 spawn  = playerCamera.transform.position + playerCamera.transform.forward * 2f;
        waterTubeController.InstantiateWaterTube(spawn, target, waterTubeDamage, waterTubeRadius, waterTubeKnockback);

        yield return new WaitForSeconds(waterTubeCooldown);
        _canUseTube = true;
    }

    private Vector3 GetCrosshairTarget()
    {
        var ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2));
        return Physics.Raycast(ray, out RaycastHit hit)
            ? hit.point
            : playerCamera.transform.position + playerCamera.transform.forward * 100f;
    }

    private void AttachWaterBallToCamera()
    {
        float offset = 1.5f;
        Vector3 pos = playerCamera.transform.position 
                    + playerCamera.transform.forward * 2f 
                    + playerCamera.transform.right * offset;
        activeWaterBall.transform.position = pos;
        activeWaterBall.transform.rotation = playerCamera.transform.rotation;
    }
}
