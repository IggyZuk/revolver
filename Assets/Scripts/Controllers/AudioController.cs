using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public enum Sound
    {
        Shot,
        Ricoshet,
        DamageBandit,
        BanditDeath,
        BulletDisappear
    }

    [System.Serializable]
    public class SfxClip
    {
        public Sound sound;
        public AudioClip audioClip;
    }

    List<AudioSource> sources = new List<AudioSource>();

    int nextSourceId;

    [SerializeField]
    List<SfxClip> clips = new List<SfxClip>();

    static AudioController instance;
    public static AudioController Instance { get { return instance; } }

    void Awake()
    {
        instance = this;
        for (int i = 0; i < 10; i++)
        {
            sources.Add(gameObject.AddComponent<AudioSource>());
        }
    }

    public void PlaySound(Sound sound)
    {
        sources[nextSourceId].clip = clips.Find(x => x.sound == sound).audioClip;
        sources[nextSourceId].Play();
        nextSourceId++;
        if (nextSourceId > sources.Count - 1) nextSourceId = 0;
    }
}
