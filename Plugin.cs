using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using BepInEx.Configuration;
using EFT;
using Comfort.Common;
namespace SoundtrackMod
{
    [BepInPlugin("BobbyRenzobbi.SoundtrackMod", "SoundtrackMod", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> musicVolume { get; set; }
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
        private static string[] clips;
        public static string[] GetTrack()
        {
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds");
        }
        private static float trackLength = 0f;
        public static void PlaySoundtrack()
        {
            
        }
        private void Awake()
        {
            clips = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds").Select(file => Path.GetFileName(file)).ToArray();
            string settings = "Soundtrack Settings";

            musicVolume = Config.Bind<float>(settings, "In-raid music volume", 0.025f, new ConfigDescription("Volume of the music heard in raid (This currently does not affect a track if it is already playing)", new AcceptableValueRange<float>(0.001f, 1f)));

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

        private static float trackTimer = 0f;
        private static int rndNumber = 0;
        private static string clip = "";

        private void Update()
        {

            if (Singleton<GameWorld>.Instance == null)
            {
                HasReloadedAudio = false;
                trackTimer = 0f;
                trackLength = 0f;
            }
            else
            {

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
                trackTimer += Time.deltaTime;
                if (trackTimer > trackLength)
                {
                    LogSource.LogInfo("trackTimer: " + trackTimer + " was greater than trackLength: " + trackLength);
                    if (clips == null)
                    {
                        LogSource.LogInfo("No audio files found");
                        return;
                    }
                    rndNumber = UnityEngine.Random.Range(0, clips.Length);
                    LogSource.LogInfo("Random number selected");
                    clip = clips[rndNumber];
                    LogSource.LogInfo("Random clip selected");
                    AudioClip audioClip = tracks[clip];
                    LogSource.LogInfo("audioClip updated");
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, musicVolume.Value, EOcclusionTest.None, null, false);
                    LogSource.LogInfo("Playing " + clip);
                    trackLength = audioClip.length;
                    LogSource.LogInfo("trackLength updated");
                    trackTimer = 0f;
                }
                else
                {
                    LogSource.LogInfo("trackTimer is only at " + trackTimer + " while trackLength is at " + trackLength);
                }
            }
        }
    }
}