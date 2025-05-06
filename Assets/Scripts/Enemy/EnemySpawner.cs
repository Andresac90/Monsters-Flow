using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyData
    {
        public GameObject prefab;
        public int baseHealth = 100;
        public int baseDamage = 10;
        public float baseSpeed = 3.5f;
        public int minRoundToAppear = 1;
        [Range(0f, 1f)]
        public float spawnChance = 1f;
    }

    [Header("Enemy Types")]
    public List<EnemyData> enemyTypes;

    [Header("Spawn Settings")]
    [Tooltip("Points where enemies may spawn.")]
    public Transform[] spawnPoints;
    
    [Tooltip("Base enemies in Round 1.")]
    public int initialEnemies = 5;
    
    [Tooltip("Maximum alive enemies allowed concurrently.")]
    public int maxConcurrentEnemies = 24;
    
    [Tooltip("Time in seconds between spawns of individual enemies within a round.")]
    public float spawnDelay = 1f;

    [Header("Round Difficulty")]
    [Tooltip("Percentage added to health each new round. (0.1 = +10%)")]
    public float healthMultiplierPerRound = 0.1f;
    [Tooltip("Percentage added to damage each new round. (0.05 = +5%)")]
    public float damageMultiplierPerRound = 0.05f;
    [Tooltip("Percentage added to speed each new round. (0.02 = +2%)")]
    public float speedMultiplierPerRound = 0.02f;

    [Header("Flow Delays")]
    [Tooltip("Time (secs) before Round 1 spawns. Acts as a 'lobby' period.")]
    public float initialRoundDelay = 5f;

    [Tooltip("Time (secs) after a round is cleared, before next round starts.")]
    public float nextRoundDelay = 5f;

    [Header("UI References")]
    [Tooltip("Reference to the TMP text showing the current round number.")]
    public TextMeshProUGUI roundDisplay;

    [Tooltip("Reference to the TMP text showing the # of enemies currently alive/spawned.")]
    public TextMeshProUGUI enemyCountDisplay;

    private int currentRound = 0;
    public int CurrentRound => currentRound; 

    private int enemiesToSpawn;             
    private int enemiesAlive;               
    private int enemiesRemainingToSpawn;    
    private bool isSpawningRound = false;   
    private Transform playerTransform;

    private void Start()
    {
        // Cache the player's transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Start the entire round flow with an initial "lobby" delay.
        StartCoroutine(RoundFlow());
    }

    private IEnumerator RoundFlow()
    {
        // 1) Wait the initialRoundDelay before Round 1
        if (initialRoundDelay > 0f)
        {
            Debug.Log($"Waiting {initialRoundDelay} seconds before Round 1...");
            yield return new WaitForSeconds(initialRoundDelay);
        }

        // Keep infinite loop for all rounds
        while (true)
        {
            // 2) Start the next round
            currentRound++;
            BeginRound(currentRound);

            // 3) Now spawn the wave
            yield return StartCoroutine(SpawnWaveRoutine());

            // 4) Wait until all those enemies are 100% cleared
            yield return StartCoroutine(WaitForWaveClear());

            // 5) Round is cleared - show debug, or do any "round completed" logic
            Debug.Log($"Round {currentRound} completed! Waiting {nextRoundDelay} secs...");

            // 6) Wait the "breather" period before starting next round
            yield return new WaitForSeconds(nextRoundDelay);
        }
    }

    private void BeginRound(int roundNumber)
    {
        enemiesToSpawn = CalculateEnemiesForRound(roundNumber);
        enemiesRemainingToSpawn = enemiesToSpawn;
        enemiesAlive = 0;
        isSpawningRound = true; 

        UpdateUI();
        Debug.Log($"--- ROUND {currentRound} STARTING --- Spawning up to {enemiesToSpawn} enemies.");
    }

    private IEnumerator SpawnWaveRoutine()
    {
        while (enemiesRemainingToSpawn > 0)
        {
            // Only spawn if we have capacity
            if (enemiesAlive < maxConcurrentEnemies)
            {
                SpawnEnemy();
                enemiesRemainingToSpawn--;
                UpdateUI();

                yield return new WaitForSeconds(spawnDelay);
            }
            else
            {
                // If too many alive, wait a bit
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Done spawning
        isSpawningRound = false;
    }

    private IEnumerator WaitForWaveClear()
    {
        // Wait until all are dead
        while (enemiesAlive > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }
        yield break;
    }

    private int CalculateEnemiesForRound(int roundNum)
    {
        return initialEnemies + (roundNum * 3) + Mathf.FloorToInt(Mathf.Pow(roundNum, 1.3f));
    }

    private void SpawnEnemy()
    {
        if (enemyTypes.Count == 0 || spawnPoints.Length == 0) return;

        List<EnemyData> valid = enemyTypes.FindAll(e => e.minRoundToAppear <= currentRound);
        if (valid.Count == 0) return; // no valid enemies

        EnemyData data = SelectEnemyWeighted(valid);
        Transform sp = SelectBestSpawnPoint();
        GameObject newEnemy = Instantiate(data.prefab, sp.position, Quaternion.identity);

        enemiesAlive++;

        EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
        if (ai != null)
            StartCoroutine(ConfigureEnemy(ai, data));
    }

    private EnemyData SelectEnemyWeighted(List<EnemyData> candidates)
    {
        float totalChance = 0f;
        foreach (var c in candidates)
            totalChance += c.spawnChance;

        float randVal = Random.Range(0, totalChance);
        float running = 0f;
        foreach (var c in candidates)
        {
            running += c.spawnChance;
            if (randVal <= running)
                return c;
        }
        // fallback
        return candidates[0];
    }

    private IEnumerator ConfigureEnemy(EnemyAI ai, EnemyData data)
    {
        yield return null; 
        ai.SetTarget(playerTransform);

        // Scale stats
        int scaledHealth = Mathf.RoundToInt(data.baseHealth * (1 + (healthMultiplierPerRound * (currentRound - 1))));
        int scaledDamage = Mathf.RoundToInt(data.baseDamage * (1 + (damageMultiplierPerRound * (currentRound - 1))));
        float scaledSpeed = data.baseSpeed * (1 + (speedMultiplierPerRound * (currentRound - 1)));

        ai.SetStats(scaledHealth, scaledDamage, scaledSpeed);
        ai.player = playerTransform;

        // On death, notify spawner
        ai.onDeath += OnEnemyDeath;
    }

    private Transform SelectBestSpawnPoint()
    {
        if (playerTransform == null)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        List<Transform> sorted = new List<Transform>(spawnPoints);
        sorted.Sort((a, b) =>
            Vector3.Distance(a.position, playerTransform.position)
            .CompareTo(Vector3.Distance(b.position, playerTransform.position)));

        int pickCount = Mathf.Min(2, sorted.Count);
        return sorted[Random.Range(0, pickCount)];
    }

    private void OnEnemyDeath()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (roundDisplay != null)
        {
            roundDisplay.text = "Round: " + currentRound;
        }
        if (enemyCountDisplay != null)
        {
            enemyCountDisplay.text = $"Enemies: {enemiesAlive}/{enemiesToSpawn}";
        }
    }
}
