using System.Collections;
using UnityEngine;

public class TimeScaleManager : MonoBehaviour
{
    [Tooltip("The tag of the objects you want to count (e.g., 'Enemy', 'Collectible').")]
    [SerializeField] string targetTag = "Enemy";
    [SerializeField] string targetTag2 = "2DEnemy";

    [Tooltip("The current number of objects in the scene with the targetTag.")]
    [Header("--- Current Count ---")]
    [SerializeField] int objectCount = 0;

    [Tooltip("How often (in seconds) the count should be updated.")]
    [SerializeField] float updateInterval = 0.5f;

    // A reference to the last found objects (useful for checking if any were destroyed)
    private GameObject[] currentObjects3D;
    private GameObject[] currentObjects2D;

    void Start()
    {
        Time.timeScale = 0f;
        // Check if the update interval is valid
        if (updateInterval <= 0)
        {
            Debug.LogError("Update Interval must be greater than zero. Defaulting to 0.5s.");
            updateInterval = 0.5f;
        }

        // Start the continuous counting process
        StartCoroutine(UpdateCountRoutine());
    }

    /// <summary>
    /// Coroutine that runs periodically to find and count the objects.
    /// This is more performant than running the expensive Find function every frame in Update().
    /// </summary>
    IEnumerator UpdateCountRoutine()
    {
        // Use WaitForSeconds to pause the loop instead of a frame-by-frame check
        WaitForSeconds waitTime = new WaitForSeconds(updateInterval);

        while (true)
        {
            // Find all GameObjects with the specified tag.
            // WARNING: This function is slow, which is why we run it in a coroutine, not Update().
            currentObjects3D = GameObject.FindGameObjectsWithTag(targetTag);
            currentObjects2D = GameObject.FindGameObjectsWithTag(targetTag2);
            // Update the public variable with the new count.
            objectCount = currentObjects3D.Length + currentObjects2D.Length;

            // Log the result for easy debugging.
            Debug.Log($"[TagCounter] Found {objectCount} objects with tag '{targetTag}'.");

            if(objectCount <= 0)
            {
                LevelLoader.Instance.LoadNextLevel("GameScene");
            }

            // Wait for the specified interval before running the check again
            yield return waitTime;
        }
    }

    /// <summary>
    /// Optional public function to get the latest count from other scripts.
    /// </summary>

    //public int GetObjectCount()
    //{
    //    return objectCount;
    //}

    /// <summary>
    /// Optional public function to get the array of currently tracked GameObjects.
    /// </summary>
    /// 
    //public GameObject[] GetTrackedObjects()
    //{
    //    return currentObjects3D;
    //}

    public void Resume()
    {
        Time.timeScale = 1f;
    }
}
