using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    LevelLoader LevelLoader;

    public void PlayGame()
    {
        Invoke("Play", 3.0f);
    }

    void Play()
    {
        SaveSystem.DeleteSave();
        LevelLoader.Instance.LoadNextLevel("CharacterScene");
    }

    public void LoadGame()
    {
        Invoke("Load", 3.0f);
    }

    void Load()
    {
        SaveData data = SaveSystem.Load();

        if (data != null)
        {
            GameManager.Instance.SetLoadedSaveData(data);               // Use setter method
            GameManager.Instance.TargetSceneAfterLoad = data.SceneName; // Set target scene to load
            LevelLoader.Instance.LoadNextLevel(data.SceneName);
        }
        else
        {
            Debug.LogWarning("No Saved Data Found!");
            LevelLoader.Instance.LoadNextLevel("GameScene");
        }
    }

    public void QuitGame()
    {
        Invoke("Quit", 2f);
    }

    void Quit()
    {
        Application.Quit();
    }

    public void MainMenu()
    {
        Invoke("Menu", 3f);
    }

    void Menu()
    {
        LevelLoader.Instance.LoadNextLevel("StartScene");
    }

    public void ReturnToGame()
    {
        Invoke("Return", 3f);
    }

    void Return()
    {
        LevelLoader.Instance.LoadNextLevel("GameScene");
    }
}
