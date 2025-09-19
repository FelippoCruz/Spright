using UnityEngine;
using System;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueScript dialogue;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnRate = 2f;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int maxEnemies = 10;

    [Header("Health")]
    [SerializeField] private int spawnerMaxHealth = 10;
    public int spawnerHealth { get; private set; }

    [Header("Spawner ID (Must be unique!)")]
    [SerializeField] private string spawnerID;

    private float nextSpawnTime;
    private bool isActive = true;

    private class EnemyPairStatus
    {
        public GameObject enemy3D;
        public GameObject enemy2D;
        public bool is3DAlive = true;
        public bool is2DAlive = true;
    }

    private List<EnemyPairStatus> enemyPairs = new List<EnemyPairStatus>();
    [SerializeField] private EnemySpawner2D paired2DSpawner;

    void Awake()
    {
        spawnerHealth = spawnerMaxHealth;

#if UNITY_EDITOR
        if (string.IsNullOrEmpty(spawnerID))
        {
            spawnerID = Guid.NewGuid().ToString();
            Debug.LogWarning($"Spawner ID auto-generated in Editor: {spawnerID}");
        }
#endif
    }

    void Update()
    {
        if (!isActive) return;

        CleanupDeadEnemies();

        if (spawnerHealth <= 0)
        {
            DisableSpawner();
            return;
        }

        if (Time.time >= nextSpawnTime &&
            enemyPairs.Count < maxEnemies &&
            (dialogue == null || dialogue.Target == null))
        {
            SpawnEnemyPair();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void CleanupDeadEnemies()
    {
        enemyPairs.RemoveAll(pair => (pair.enemy3D == null && pair.enemy2D == null));
    }

    void SpawnEnemyPair()
    {
        if (enemyPrefab == null || spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        GameObject enemy3D = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        Register3DEnemy(enemy3D);

        GameObject enemy2D = null;
        if (paired2DSpawner != null)
        {
            enemy2D = paired2DSpawner.SpawnEnemyFromAnySpawner();
            if (enemy2D != null)
                Register2DEnemy(enemy2D);
        }

        enemyPairs.Add(new EnemyPairStatus
        {
            enemy3D = enemy3D,
            enemy2D = enemy2D,
            is3DAlive = true,
            is2DAlive = true
        });
    }

    public void Register3DEnemy(GameObject enemy)
    {
        EnemyScript enemyScript = enemy.GetComponent<EnemyScript>();
        if (enemyScript != null)
        {
            enemyScript.SetSpawner(this);
            enemyScript.OnDeath += () => Notify3DEnemyDied(enemy);
        }
    }

    public void Register2DEnemy(GameObject enemy2D)
    {
        Enemy2DScript enemyScript = enemy2D.GetComponent<Enemy2DScript>();
        if (enemyScript != null)
        {
            enemyScript.OnDeath += () => Notify2DEnemyDied(enemy2D);
        }
    }

    public void Notify3DEnemyDied(GameObject enemy)
    {
        var pair = enemyPairs.Find(p => p.enemy3D == enemy);
        if (pair != null)
        {
            pair.is3DAlive = false;
            TryDecreaseAliveCount(pair);
        }
    }

    public void Notify2DEnemyDied(GameObject enemy2D)
    {
        var pair = enemyPairs.Find(p => p.enemy2D == enemy2D);
        if (pair != null)
        {
            pair.is2DAlive = false;
            TryDecreaseAliveCount(pair);
        }
    }

    private void TryDecreaseAliveCount(EnemyPairStatus pair)
    {
        if (!pair.is3DAlive && !pair.is2DAlive)
        {
            enemyPairs.Remove(pair);
            Debug.Log($"{gameObject.name} spawner: enemy pair died. Remaining pairs: {enemyPairs.Count}");
        }
    }

    public void TakeDamage(int amount)
    {
        if (!isActive) return;

        spawnerHealth -= amount;
        Debug.Log($"{gameObject.name} spawner took {amount} damage. Remaining HP: {spawnerHealth}");

        if (spawnerHealth <= 0)
        {
            DisableSpawner();
        }
    }

    void DisableSpawner()
    {
        isActive = false;
        DisableSpawnerVisuals();
        Debug.Log($"{gameObject.name} spawner disabled.");
    }

    void DisableSpawnerVisuals()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;
    }

    public void SetPaired2DSpawner(EnemySpawner2D spawner2D)
    {
        paired2DSpawner = spawner2D;
    }

    public float GetNextSpawnTime() => nextSpawnTime;
    public int GetAliveEnemyCount() => enemyPairs.Count;
    public int MaxEnemies => maxEnemies;
    public DialogueScript Dialogue => dialogue;

    public string SpawnerID => spawnerID;
    public bool IsAlive => isActive;

    public void SetHealth(int newHealth)
    {
        spawnerHealth = newHealth;
    }

    public void SetAliveState(bool alive)
    {
        isActive = alive;

        if (alive)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = true;
            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = true;
        }
        else
        {
            DisableSpawnerVisuals();
        }
    }

    public void ClearSpawnedEnemies()
    {
        foreach (var pair in enemyPairs)
        {
            if (pair.enemy3D != null) Destroy(pair.enemy3D);
            if (pair.enemy2D != null) Destroy(pair.enemy2D);
        }
        enemyPairs.Clear();
    }

    public List<GameObject> GetSpawnedEnemies()
    {
        List<GameObject> enemies = new List<GameObject>();
        foreach (var pair in enemyPairs)
        {
            if (pair.enemy3D != null)
                enemies.Add(pair.enemy3D);
        }
        return enemies;
    }

    public GameObject SpawnEnemyAtPosition(Vector3 position)
    {
        if (!isActive || enemyPrefab == null) return null;

        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        Register3DEnemy(enemy);

        enemyPairs.Add(new EnemyPairStatus
        {
            enemy3D = enemy,
            enemy2D = null,
            is3DAlive = true,
            is2DAlive = true
        });

        return enemy;
    }
}
