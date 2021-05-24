using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private Object[] clips;
    private Game game;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        clips = Resources.LoadAll("Music", typeof(AudioClip));
        game = GameObject.Find("World").GetComponent<Game>();
        StartCoroutine(PlayMusic());
    }

    IEnumerator PlayMusic()
    {
        audioSource.clip = clips[Random.Range(0, clips.Length - 1)] as AudioClip;
        audioSource.Play();
        yield return new WaitWhile(() => true);
        StartCoroutine(PlayMusic());
    }

    public void RandomTrack()
    {
        audioSource.Stop();
        StopCoroutine(PlayMusic());
        StartCoroutine(PlayMusic());
    }

    private void Update()
    {
        audioSource.volume = game.MusicVolume;
    }
}