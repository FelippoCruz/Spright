using UnityEngine;

public class PortalScript : MonoBehaviour
{
    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag ("Player"))
        {
            Invoke("Loader", 1f);
        }
    }
    void Loader()
    {
        LevelLoader.Instance.LoadNextLevel("LoadingScene");
    }
}
