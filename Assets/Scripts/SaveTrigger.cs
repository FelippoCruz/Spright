using UnityEngine;
using System.Collections.Generic;

public class SaveTrigger : MonoBehaviour
{
    public GameObject savePrompt;
    private bool isPlayerInRange;

    public GameObject player1;
    public GameObject player2;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            savePrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            savePrompt.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            SaveGame();
        }
    }

    private void SaveGame()
    {
        var player1Script = player1.GetComponent<Player3DScript>();
        var player2Script = player2.GetComponent<Player2DScript>();

        int timesTalkedToMainNPC = GameManager.Instance.TimesTalkedToMainNPC;
        int bossesDefeated = GameManager.Instance.AmountOfBossesDefeated;

        player1Script.SetHealUsesToMax();
        player2Script.SetHealUsesToMax();

        SaveData data = new SaveData
        {
            PlayerHealth1 = player1Script != null ? player1Script.GetCurrentHealth() : 0f,
            PlayerHealth2 = player2Script != null ? player2Script.GetCurrentHealth() : 0f,
            Player1Position = player1.transform.position,
            Player2Position = player2.transform.position,
            TimesTalkedToMainNPC = timesTalkedToMainNPC,
            AmountOfBossesDefeated = bossesDefeated,
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            Spawners = new List<SpawnerData>()
        };

        // Save 3D spawners
        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            SpawnerData spawnerData = new SpawnerData
            {
                SpawnerID = spawner.SpawnerID,
                IsAlive = spawner.IsAlive,
                SpawnedEnemies = new List<EnemyData>()
            };

            List<GameObject> spawnedEnemies = spawner.GetSpawnedEnemies();
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    EnemyScript enemyHealth = enemy.GetComponent<EnemyScript>();
                    EnemyData enemyData = new EnemyData
                    {
                        Position = enemy.transform.position,
                        Health = enemyHealth != null ? enemyHealth.GetCurrentHealth() : 0f
                    };
                    spawnerData.SpawnedEnemies.Add(enemyData);
                }
            }

            data.Spawners.Add(spawnerData);

            EnemySpawner2D[] spawners2D = FindObjectsByType<EnemySpawner2D>(FindObjectsSortMode.None);
            foreach (var spawner2D in spawners2D)
            {
                Spawner2DData spawner2DData = new Spawner2DData
                {
                    SpawnerID = spawner2D.SpawnerID,
                    IsAlive = spawner2D.IsAlive,
                    SpawnedEnemies = new List<EnemyData>()
                };

                List<GameObject> enemiesFromSpawner = spawner.GetSpawnedEnemies();
                foreach (var enemy in enemiesFromSpawner)
                {
                    if (enemy != null)
                    {
                        Enemy2DScript enemyHealth = enemy.GetComponent<Enemy2DScript>();
                        EnemyData enemyData = new EnemyData
                        {
                            Position = enemy.transform.position,
                            Health = enemyHealth != null ? enemyHealth.GetCurrentHealth() : 0f
                        };
                        spawner2DData.SpawnedEnemies.Add(enemyData);
                    }
                }

                data.Spawners2D.Add(spawner2DData);
            }

            SaveSystem.Save(data);
            Debug.Log("[SAVE] Game saved successfully!");
        }
    }
}
