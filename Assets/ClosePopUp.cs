using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosePopUp : MonoBehaviour
{
    public GameObject popupToClose;
    public GameObject BgCloser;
    public void closePopup()
    {
        popupToClose.SetActive(false);
        BgCloser.SetActive(false);
    }
}
