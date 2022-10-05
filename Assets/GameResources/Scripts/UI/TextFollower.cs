using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextFollower : MonoBehaviour
{
    public Transform objectToFollow = null;
    public Text textComponent = null;
    public Vector2 offset = Vector2.zero;

    public void Start()
    {
        textComponent = GetComponent<Text>();
    }

    void Update()
    {
        if (objectToFollow != null)
        {
            if (objectToFollow.gameObject.activeSelf)
            {
                transform.position = Camera.main.WorldToScreenPoint((Vector2)objectToFollow.position + new Vector2(0, 0) + offset);
                //transform.localScale = Vector2.one * Camera.main.orthographicSize;
                return;
            }
        }
        textComponent.text = "";
    }
}