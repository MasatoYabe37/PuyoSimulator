using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextController : MonoBehaviour
{
    public Text text = null;

    public void SetText(string in_text)
    {
        if (text != null)
        {
            text.text = in_text;
        }
    }
}
