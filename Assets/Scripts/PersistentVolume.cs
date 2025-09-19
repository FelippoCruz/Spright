using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public class PersistentVolume : MonoBehaviour
{
    public static Volume Instance { get; private set; }

    void Awake()
    {
        var thisVolume = GetComponent<Volume>();

        if (Instance != null && Instance != thisVolume)
        {
            Destroy(gameObject);
            return;
        }

        Instance = thisVolume;
        DontDestroyOnLoad(gameObject);
    }
}
