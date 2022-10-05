using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Log : MonoBehaviour
{
    private Text logText;
    private World world;

    public List<string> log = new List<string>();

    private void Start()
    {
        logText = transform.GetChild(0).GetComponent<Text>();
        world = World.GetInstance();
    }

    public void UpdateLog(string s)
    {
        if (logText == null)
        {
            Debug.Log("Update log: " + s);
            return;
        }
        log.Add(s);
        if (log.Count > 50)
            log.RemoveRange(0, log.Count - 30);
        logText.text = "";
        for (int i = log.Count - 1; i >= 0; i--)
        {
            logText.text += ">" + log[i];
            if (i > 0)
                logText.text += "\n";
        }
    }
}