using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using BepInEx.Configuration;
using EFT;
using Comfort.Common;
using System.Diagnostics;

namespace SoundtrackMod
{
    [RequireComponent(typeof(AudioSource))]
    public class Audio : MonoBehaviour
    {
        public static AudioSource myaudioSource;
        public static void SetClip(AudioClip clip)
        {
            myaudioSource.clip = clip;
        }
        public static void AdjustVolume(float volume)
        {
            myaudioSource.volume = volume;
        }
    }
    [BepInPlugin("BobbyRenzobbi.SoundtrackMod", "SoundtrackMod", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private static AudioClip unityAudioClip;
        private async void RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            Dictionary<string, AudioType> audioType = new Dictionary<string, AudioType>
            {
                [".wav"] = AudioType.WAV,
                [".ogg"] = AudioType.OGGVORBIS,
                [".mp3"] = AudioType.MPEG
            };
            UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType[extension]);
            UnityWebRequestAsyncOperation sendWeb = uwr.SendWebRequest();

            while (!sendWeb.isDone)
                await Task.Yield();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Logger.LogError("Soundtrack Mod: Failed To Fetch Audio Clip");
                return;
            }
            else
            {
                unityAudioClip = DownloadHandlerAudioClip.GetContent(uwr);
                return;
            }
        }

        public static Dictionary<string, AudioClip> tracks = new Dictionary<string, AudioClip>();
        public static ManualLogSource LogSource;
        private static string track = "";
        private static string trackPath = "";
        public static ConfigEntry<float> MusicVolume { get; set; }
        private void LoadAudioClips()
        {
            string[] musicTracks = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds");

            tracks.Clear();
            rndNumber = UnityEngine.Random.Range(0, musicTracks.Length - 1);
            track = musicTracks[rndNumber];
            trackPath = Path.GetFileName(track);
            RequestAudioClip(track);
            tracks[trackPath] = unityAudioClip;
        }

        public static string[] GetTrack()
        {
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds").Select(file => Path.GetFileName(file)).ToArray();
        }

        private void Awake()
        {
            clips = GetTrack();
            LoadAudioClips();
            string settings = "Soundtrack Settings";
            MusicVolume = Config.Bind<float>(settings, "In-raid music volume", 0.025f, new ConfigDescription("Volume of the music heard in raid", new AcceptableValueRange<float>(0f, 1f)));
            LogSource = Logger;
            LogSource.LogInfo("plugin loaded!");
        }

        private static int rndNumber = 0;
        private static string[] clips;
        private static bool HasReloadedAudio = true;

        private void Update()
        {
            if (Audio.myaudioSource != null)
            {
                try
                {
                    Audio.AdjustVolume(MusicVolume.Value);
                }
                catch (Exception exception)
                {
                    LogSource.LogError(exception);
                }
            }
            if (clips == null)
            {
                return;
            }
            if (Singleton<GameWorld>.Instance == null || (Singleton<GameWorld>.Instance?.MainPlayer is HideoutPlayer))
            {
                HasReloadedAudio = false;
                return;
            }
            if (Audio.myaudioSource == null)
            {
                try
                {
                    Audio.myaudioSource = gameObject.GetOrAddComponent<AudioSource>();
                }
                catch (Exception ex)
                {
                    LogSource.LogInfo(ex.Message);
                }
            }
            if (Singleton<GameWorld>.Instance.MainPlayer == null)
            {
                return;
            }
            if (Audio.myaudioSource.isPlaying)
            {
                return;
            }
            if (!HasReloadedAudio)
            {
                LoadAudioClips();
                HasReloadedAudio = true;
            }
            if (tracks[trackPath] == null)
            {
                return;
            }
            Audio.SetClip(tracks[trackPath]);
            Audio.myaudioSource.Play();
            LogSource.LogInfo("playing " + trackPath);
            HasReloadedAudio = false;
        }
    }
}
