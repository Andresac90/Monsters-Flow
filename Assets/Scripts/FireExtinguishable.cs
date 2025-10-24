using UnityEngine;
using System.Collections;

public class FireExtinguishable : MonoBehaviour
{
    [Header("Fire Settings")]
    public float maxFireHealth = 100f;
    public float currentFireHealth;
    public bool isExtinguished = false;
    public float extinguishCooldown = 30f; // Time before fire reignites
    public int moneyReward = 10; // Money reward per extinguish action
    public int waterNeededToExtinguish = 3; // How many water hits needed to extinguish

    [Header("Visual Effects")]
    public ParticleSystem fireParticleSystem;
    public ParticleSystem steamParticleSystem;
    public Light fireLight;
    
    [Header("Interaction")]
    public float interactionRange = 5f;
    public KeyCode interactionKey = KeyCode.F;
    public int hitCount = 0;

    private bool isReigniting = false;
    private Collider fireCollider;

    private void Start()
    {
        currentFireHealth = maxFireHealth;
        fireCollider = GetComponent<Collider>();
        
        // Ensure the fire is burning at start
        SetFireState(true);
    }

    private void Update()
    {
        // If player is in range and presses the interaction key
        if (!isExtinguished && !isReigniting && IsPlayerInRange())
        {
            if (Input.GetKeyDown(interactionKey))
            {
                // Player is attempting to extinguish the fire
                TryExtinguishFire();
            }
        }
    }

    public void ApplyWaterDamage(float damage)
    {
        if (isExtinguished || isReigniting) return;

        hitCount++;
        
        if (hitCount >= waterNeededToExtinguish)
        {
            ExtinguishFire();
        }
    }

    private void TryExtinguishFire()
    {
        hitCount++;
        
        if (hitCount >= waterNeededToExtinguish)
        {
            ExtinguishFire();
        }
    }

    private void ExtinguishFire()
    {
        isExtinguished = true;
        hitCount = 0;
        
        // Visual feedback
        SetFireState(false);
        
        // Award money to player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterFireExtinguished();
        }
        
        // Start cooldown before reigniting
        StartCoroutine(ReigniteAfterDelay());
    }

    private IEnumerator ReigniteAfterDelay()
    {
        isReigniting = true;
        
        yield return new WaitForSeconds(extinguishCooldown);
        
        // Reignite the fire
        isExtinguished = false;
        isReigniting = false;
        SetFireState(true);
    }

    private void SetFireState(bool active)
    {
        // Enable/disable visual effects based on fire state
        if (fireParticleSystem != null)
        {
            if (active)
                fireParticleSystem.Play();
            else
            {
                fireParticleSystem.Stop();
                if (steamParticleSystem != null)
                    steamParticleSystem.Play();
            }
        }
        
        if (fireLight != null)
            fireLight.enabled = active;
    }

    private bool IsPlayerInRange()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            return distance <= interactionRange;
        }
        return false;
    }

    // For water projectile collision detection
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collision is with water projectile
        if (other.CompareTag("WaterProjectile"))
        {
            ApplyWaterDamage(10f); // Apply standard water damage amount
        }
    }
}