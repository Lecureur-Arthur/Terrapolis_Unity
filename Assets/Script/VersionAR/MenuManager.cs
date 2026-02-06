using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
   public void PlayGame()
   {
       SceneManager.LoadScene("GamePlayAR");
   }

    public void QuitGame()
    {
         Application.Quit();
    }

    public void AccesPrefab3D()
    {
        SceneManager.LoadScene("VisualizationPrefab3D");
    }

    public void AccesPlacement2D ()
    {
        SceneManager.LoadScene("GamePlay2D");
    }

    public void AccesPlacement3D ()
    {
        SceneManager.LoadScene("GamePlayAR");
    }
}
