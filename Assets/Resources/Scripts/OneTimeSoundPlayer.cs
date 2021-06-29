using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneTimeSoundPlayer : MonoBehaviour
{
    class SoundInfo
    {
        public AudioClip sound;
        public float basicVolume;
        public float spatialBlend = 1.0f;
        public float volumeMod = 1.0f;
    }

    public void Play(AudioClip sound, float basicVolume, float spatialBlend = 1.0f, float volumeMod = 1.0f)
    {
        SoundInfo soundInfo = new SoundInfo
        {
            sound = sound,
            basicVolume = basicVolume,
            spatialBlend = spatialBlend,
            volumeMod = volumeMod
        };
        StartCoroutine(PlaySound(soundInfo));
    }

    IEnumerator PlaySound(SoundInfo soundInfo)
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = soundInfo.spatialBlend;
        audioSource.volume = soundInfo.basicVolume * soundInfo.volumeMod;
        audioSource.clip = soundInfo.sound;
        audioSource.Play();
        yield return new WaitForSecondsRealtime(audioSource.clip.length);
        Destroy(gameObject);
    }
}