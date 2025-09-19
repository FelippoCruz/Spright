using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // === Player Data ===
    public float PlayerHealth1;
    public float PlayerHealth2;

    public Vector3 Player1Position;
    public Vector3 Player2Position;

    // === Game Progression ===
    public int TimesTalkedToMainNPC;
    public int AmountOfBossesDefeated;

    public string SceneName;

    // === Enemy Spawner Data ===
    public List<SpawnerData> Spawners = new List<SpawnerData>();
    public List<Spawner2DData> Spawners2D = new List<Spawner2DData>();
}

[Serializable]
public class SpawnerData
{
    public string SpawnerID;                  // Unique spawner identifier
    public bool IsAlive;                      // Whether the spawner is still active
    public int SpawnerHealth;                 // Optional: if you want to restore damaged spawners
    public List<EnemyData> SpawnedEnemies = new List<EnemyData>();
}

[Serializable]
public class Spawner2DData
{
    public string SpawnerID;   // Unique ID
    public bool IsAlive;
    public List<EnemyData> SpawnedEnemies = new List<EnemyData>();
}

[Serializable]
public class EnemyData
{
    public Vector3 Position;
    public float Health;

    // Optional future fields
    // public string EnemyType;               // If you support multiple types
    // public string EnemyID;                 // For uniquely identified enemies
}
