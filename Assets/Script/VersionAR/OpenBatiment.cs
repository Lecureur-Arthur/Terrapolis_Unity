using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenBatiment : MonoBehaviour
{

    public GameObject BuildBatiment;
    public GameObject Menu;
    // Start is called before the first frame update
    void Start()
    {
        if(BuildBatiment != null) BuildBatiment.SetActive(false);
    }

    public void OpenMenuBuild()
    {
        if(Menu != null) Menu.SetActive(false);
        if(BuildBatiment != null) BuildBatiment.SetActive(true);
    }

    public void CloseMenuBuild()
    {
        if(Menu != null) Menu.SetActive(true);
        if(BuildBatiment != null) BuildBatiment.SetActive(false);
    }
}
