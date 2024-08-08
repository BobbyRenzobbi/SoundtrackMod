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
        public static float GetCurrentLength()
        {
            return myaudioSource.clip.length;
        }
    }

    [BepInPlugin("BobbyRenzobbi.SoundtrackMod", "SoundtrackMod", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private async Task<AudioClip> RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            AudioType audioType = AudioType.WAV;
            switch (extension)
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
                case ".mp3":
                    audioType = AudioType.MPEG;
                    break;
            }
            UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            UnityWebRequestAsyncOperation sendWeb = uwr.SendWebRequest();

            while (!sendWeb.isDone)
                await Task.Yield();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Logger.LogError("Soundtrack Mod: Failed To Fetch Audio Clip");
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(uwr);
                return audioclip;
            }
        }

        public static bool HasReloadedAudio = false;
        public static Dictionary<string, AudioClip> tracks = new Dictionary<string, AudioClip>();
        public static ManualLogSource LogSource;
        public static ConfigEntry<float> MusicVolume { get; set; }
        private async void LoadAudioClips()
        {
            string[] musicTracks = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds");

            tracks.Clear();

            foreach (string fileDir in musicTracks)
            {
                tracks[Path.GetFileName(fileDir)] = await RequestAudioClip(fileDir);
            }
            HasReloadedAudio = true;
        }
        
        public static string[] GetTrack()
        {
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds").Select(file => Path.GetFileName(file)).ToArray();
        }

        private void Awake()
        {
            clips = GetTrack();
            string settings = "Soundtrack Settings";
            MusicVolume = Config.Bind<float>(settings, "In-raid music volume", 0.025f, new ConfigDescription("Volume of the music heard in raid (This currently does not affect a track if it is already playing)", new AcceptableValueRange<float>(0.001f, 1f)));
            timer = new Stopwatch();
            LogSource = Logger;
            LogSource.LogInfo("plugin loaded!");
            try
            {
                LoadAudioClips();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }
        }

        private static int rndNumber = 0;
        private static string clip = "";
        private static Stopwatch timer;
        private static float trackLength = 0f;
        private static string[] clips;

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
            if (Singleton<GameWorld>.Instance == null)
            {
                HasReloadedAudio = false;
                timer.Restart();
                trackLength = 0f;
                return;
            }
            float currentTimer = (timer.ElapsedMilliseconds / 1000);
            if (!HasReloadedAudio)
            {
                try
                {
                    LoadAudioClips();
                    HasReloadedAudio = true;
                }
                catch (Exception ex)
                {
                    LogSource.LogError(ex);
                }
            }
            if (currentTimer <= trackLength)
            {
                return;
            }
            if (clips == null)
            {
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
            rndNumber = UnityEngine.Random.Range(0, clips.Length);
            clip = clips[rndNumber];
            Audio.SetClip(tracks[clip]);
            Audio.myaudioSource.Play();
            LogSource.LogInfo("playing " + clip);
            trackLength = Audio.GetCurrentLength();
            timer.Restart();
        }
    }
}