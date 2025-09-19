using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

public class CharacterLoader : MonoBehaviour
{
    List<GameObject> models;
    int SelectedIndex = 0;
    void Awake()
    {
        SelectedIndex = PlayerPrefs.GetInt("CharacterChosen", 0);
        models = new List<GameObject>();
        foreach (Transform t in transform)
        {
            models.Add(t.gameObject);
            t.gameObject.SetActive(false);
        }
        models[SelectedIndex].SetActive(true);
    }
}
