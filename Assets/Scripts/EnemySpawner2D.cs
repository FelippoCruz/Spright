using UnityEngine;
using System;
using System.Collections.Generic;

public class EnemySpawner2D : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private List<EnemySpawner> spawners = new();

    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Random Offset")]
    [SerializeField] private float maxOffset = 0.5f;

    [Header("Spawner ID (Must be unique!)")]
    [SerializeField] private string spawnerID;

    private int alive2DEnemies = 0;
    private bool isActive = true;

    private readonly List<GameObject> spawnedEnemies = new();

    void Awake()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(spawnerID))
        {
            spawnerID = Guid.NewGuid().ToString();
            Debug.LogWarning($"[EnemySpawner2D] Auto-generated ID: {spawnerID}");
        }
#endif
    }

    public string SpawnerID => spawnerID;
    public bool IsAlive => isActive;

    public GameObject SpawnEnemyFromAnySpawner()
    {
        if (!isActive || !AnySpawnerIsAlive()) return null;
        if (enemyPrefab == null || spawnPoints.Length == 0) return null;

        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        Vector2 offset = new(UnityEngine.Random.Range(-maxOffset, maxOffset), UnityEngine.Random.Range(-maxOffset, maxOffset));
        Vector3 spawnPosition = spawnPoint.position + (Vector3)offset;

        return SpawnEnemyAtPosition(spawnPosition);
    }

    public GameObject SpawnEnemyAtPosition(Vector3 position)
    {
        if (!isActive || enemyPrefab == null || !AnySpawnerIsAlive()) return null;

        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        spawnedEnemies.Add(enemy);
        alive2DEnemies++;

        Enemy2DScript enemyScript = enemy.GetComponent<Enemy2DScript>();
        if (enemyScript != null)
        {
            enemyScript.OnDeath += () => Handle2DEnemyDeath(enemy);
        }

        return enemy;
    }

    private void Handle2DEnemyDeath(GameObject enemy)
    {
        alive2DEnemies = Mathf.Max(0, alive2DEnemies - 1);
    }

    private bool AnySpawnerIsAlive()
    {
        foreach (var spawner in spawners)
        {
            if (spawner != null && spawner.IsAlive)
                return true;
        }
        return false;
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }

    public int GetAlive2DEnemies() => alive2DEnemies;

    public List<GameObject> GetSpawnedEnemies()
    {
        spawnedEnemies.RemoveAll(e => e == null);
        return new List<GameObject>(spawnedEnemies);
    }

    public void ClearSpawnedEnemies()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();
        alive2DEnemies = 0;
    }
}
