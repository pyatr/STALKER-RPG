using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public GameObject soundScrollbar;
    public GameObject musicScrollbar;
    public GameObject uiScaleScrollbar;

    public void Start()
    {
        soundScrollbar.GetComponent<Scrollbar>().value = PlayerPrefs.GetFloat("SoundVolume");
        musicScrollbar.GetComponent<Scrollbar>().value = PlayerPrefs.GetFloat("MusicVolume");
        uiScaleScrollbar.GetComponent<Scrollbar>().value = PlayerPrefs.GetFloat("UIScale") / 2;
    }

    public void SetSoundVolume()
    {
        float volume = soundScrollbar.GetComponent<Scrollbar>().value;
        soundScrollbar.GetComponent<Scrollbar>().value = (float)Math.Round(volume, 1);
        volume = soundScrollbar.GetComponent<Scrollbar>().value;
        PlayerPrefs.SetFloat("SoundVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume()
    {
        float music = musicScrollbar.GetComponent<Scrollbar>().value;
        musicScrollbar.GetComponent<Scrollbar>().value = (float)Math.Round(music, 1);
        music = musicScrollbar.GetComponent<Scrollbar>().value;
        PlayerPrefs.SetFloat("MusicVolume", music);
        PlayerPrefs.Save();
    }

    public void SetUIScale()
    {
        float scale = uiScaleScrollbar.GetComponent<Scrollbar>().value;
        uiScaleScrollbar.GetComponent<Scrollbar>().value = (float)Math.Round(scale, 1);
        scale = uiScaleScrollbar.GetComponent<Scrollbar>().value;
        PlayerPrefs.SetFloat("UIScale", scale * 2);
        PlayerPrefs.Save();
    }
}