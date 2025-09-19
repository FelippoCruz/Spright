using System.Collections;
using UnityEngine;

public class MechanicAll : MonoBehaviour
{
    LevelLoader LevelLoader;
    [SerializeField] float Time = 8f;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        PlayerPrefs.SetInt("InvertX", 0);
        PlayerPrefs.SetInt("InvertY", 0);

        StartCoroutine(Wait());
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(Time);
        LevelLoader.Instance.LoadNextLevel("StartScene");
    }
}
