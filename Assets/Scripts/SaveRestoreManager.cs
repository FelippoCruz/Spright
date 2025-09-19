using UnityEngine;

public class SaveRestoreManager : MonoBehaviour
{
    [SerializeField] private GameObject player1; // Should have Player3DScript
    [SerializeField] private GameObject player2; // Should have Player2DScript
    [SerializeField] DialogueScript DialogueScript;

    private void Start()
    {
        var data = GameManager.Instance.LoadedSaveData;
        if (data != null)
        {
            // Restore player 1
            if (player1 != null)
            {
                player1.transform.position = data.Player1Position;
                var p1Script = player1.GetComponent<Player3DScript>();
                if (p1Script != null)
                    p1Script.SetCurrentHealth(data.PlayerHealth1);
            }

            // Restore player 2
            if (player2 != null)
            {
                player2.transform.position = data.Player2Position;
                var p2Script = player2.GetComponent<Player2DScript>();
                if (p2Script != null)
                    p2Script.SetCurrentHealth(data.PlayerHealth2);
            }

            // Restore general game state
            GameManager.Instance.SetTimesTalkedToMainNPC(data.TimesTalkedToMainNPC);
            GameManager.Instance.SetAmountOfBossesDefeated(data.AmountOfBossesDefeated);

            // Restore spawners
            EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
            foreach (var spawner in spawners)
            {
                foreach (var savedSpawner in data.Spawners)
                {
                    if (spawner.SpawnerID == savedSpawner.SpawnerID)
                    {
                        // Restore alive/dead state first
                        spawner.SetAliveState(savedSpawner.IsAlive);

                        // Skip restoring enemies if spawner is dead
                        if (!savedSpawner.IsAlive)
                            break;

                        spawner.ClearSpawnedEnemies();

                        // Restore enemies for this spawner
                        foreach (var enemyData in savedSpawner.SpawnedEnemies)
                        {
                            GameObject enemy = spawner.SpawnEnemyAtPosition(enemyData.Position);
                            if (enemy != null)
                            {
                                var enemyHealth = enemy.GetComponent<EnemyScript>();
                                if (enemyHealth != null)
                                    enemyHealth.SetCurrentHealth(enemyData.Health);
                            }
                        }

                        break;
                    }
                }
            }
            // Restore 2D spawners
            EnemySpawner2D[] spawners2D = FindObjectsByType<EnemySpawner2D>(FindObjectsSortMode.None);
            foreach (var spawner2D in spawners2D)
            {
                foreach (var savedSpawner in data.Spawners2D)
                {
                    if (spawner2D.SpawnerID == savedSpawner.SpawnerID)
                    {
                        spawner2D.SetActive(savedSpawner.IsAlive);

                        if (!savedSpawner.IsAlive)
                            break;

                        spawner2D.ClearSpawnedEnemies();

                        foreach (var enemyData in savedSpawner.SpawnedEnemies)
                        {
                            GameObject enemy = spawner2D.SpawnEnemyAtPosition(enemyData.Position);
                            if (enemy != null)
                            {
                                var enemyHealth = enemy.GetComponent<Enemy2DScript>();
                                if (enemyHealth != null)
                                    enemyHealth.SetCurrentHealth(enemyData.Health);
                            }
                        }

                        break;
                    }
                }
            }
            if (GameManager.Instance.TimesTalkedToMainNPC >= 1)
            {
                DialogueScript.DestroyTarget();
            }
            GameManager.Instance.ClearLoadedSaveData();
            Debug.Log("[LOAD] Game state restored successfully.");
        }
        else
        {
            Debug.Log("[LOAD] No loaded save data found to restore.");
        }
    }
}
