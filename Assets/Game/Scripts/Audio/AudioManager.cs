using UnityEngine;

namespace Game
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource[] pool;
        int next;
        AudioSource rain, drone, ui;

        void Awake()
        {
            Instance = this;
            pool = new AudioSource[24];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("Sfx" + i);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 3f;
                src.maxDistance = 90f;
                pool[i] = src;
            }
            ui = gameObject.AddComponent<AudioSource>();
            ui.spatialBlend = 0f;
            ui.playOnAwake = false;
            ui.ignoreListenerPause = true;
        }

        public void StartAmbience(Transform listener)
        {
            rain = listener.gameObject.AddComponent<AudioSource>();
            rain.clip = SoundBank.Rain;
            rain.loop = true;
            rain.volume = 0.35f;
            rain.spatialBlend = 0f;
            rain.Play();
            drone = listener.gameObject.AddComponent<AudioSource>();
            drone.clip = SoundBank.Drone;
            drone.loop = true;
            drone.volume = 0.25f;
            drone.spatialBlend = 0f;
            drone.Play();
        }

        public void SetTension(float t)
        {
            if (drone != null) drone.volume = Mathf.Lerp(0.2f, 0.5f, t);
            if (drone != null) drone.pitch = Mathf.Lerp(1f, 1.12f, t);
        }

        public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (Instance == null || clip == null) return;
            var src = Instance.pool[Instance.next];
            Instance.next = (Instance.next + 1) % Instance.pool.Length;
            src.transform.position = position;
            src.pitch = pitch * Random.Range(0.95f, 1.05f);
            src.volume = volume;
            src.clip = clip;
            src.Play();
        }

        public static void Play2D(AudioClip clip, float volume = 1f)
        {
            if (Instance == null || clip == null) return;
            Instance.ui.PlayOneShot(clip, volume);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
