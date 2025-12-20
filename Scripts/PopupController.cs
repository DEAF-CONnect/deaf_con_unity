using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class PopupController : MonoBehaviour
{
    public Image popupBox;
    public TMP_Text messageText;
    // Start is called before the first frame update
    private void Start()
    {
        popupBox.gameObject.SetActive(false);
    }
    public void ShowPopup(string message)
    {
        messageText.text = message;
        popupBox.gameObject.SetActive(true);
        StartCoroutine(HideAfterSeconds(2f)); //2초동안 띄우기
    }
    private System.Collections.IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        popupBox.gameObject.SetActive(false);
    }
}
