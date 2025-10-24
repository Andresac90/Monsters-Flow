using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth healthSystem;
    private PlayerHealth playerHealth;

    [Header("Enemy Stats")]
    public float attackRange = 2f;
    public float corneringDistance = 5f;
    public int damage = 10;
    public float attackCooldown = 1.5f;

    [Header("Movement")]
    public float baseSpeed = 3.5f;
    public float runSpeed = 5f;

    private bool isCornering = false;
    private bool canAttack = true;
    private float lastAttackTime;

    // Event for enemy death
    public event System.Action onDeath;

    private bool isDead = false; // Flag to avoid state updates after death

    // Provide public access for EnemyHealth to disable/enable
    public NavMeshAgent Agent => agent;
    public bool IsDead => isDead;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        healthSystem = GetComponent<EnemyHealth>();

        // Find player
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        if (agent != null)
        {
            agent.speed = baseSpeed;
        }
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (!canAttack && Time.time > lastAttackTime + attackCooldown)
        {
            canAttack = true;
        }

        if (distance > attackRange)
        {
            ChasePlayer(distance);
        }
        else if (canAttack)
        {
            AttackPlayer();
        }
    }

    private void ChasePlayer(float distance)
    {
        if (agent == null) return;

        if (distance < corneringDistance && !isCornering)
        {
            Vector3 offset = GetCorneringPosition();
            agent.SetDestination(player.position + offset);
            isCornering = true;
        }
        else
        {
            agent.SetDestination(player.position);
            isCornering = false;
        }
        if (animator != null)
        {
            animator.SetInteger("animation", 2); 
        }
    }

    private Vector3 GetCorneringPosition()
    {
        Vector3 direction = (transform.position - player.position).normalized;
        Vector3 sideOffset = Quaternion.Euler(0, Random.Range(-45, 45), 0) * direction * corneringDistance;
        return sideOffset;
    }

    private void AttackPlayer()
    {
        if (animator != null)
        {
            animator.SetInteger("animation", 3); 
        }
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
        canAttack = false;
        lastAttackTime = Time.time;
    }

    public void SetTarget(Transform targetTransform)
    {
        player = targetTransform;
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Disable agent + collider
        if (agent != null) agent.enabled = false;
        if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;

        if (animator != null)
        {
            animator.SetInteger("animation", 5); 
        }
        onDeath?.Invoke();

        Destroy(gameObject, 1f);
    }

    public void SetStats(int health, int damage, float speed = 3.5f)
    {
        this.damage = damage;
        if (healthSystem != null)
        {
            healthSystem.SetStats(health);
        }
        if (agent != null)
        {
            agent.speed = speed;
            baseSpeed = speed;
        }
    }
}
