using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SoundtrackMod.Patches;
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
            Plugin.HasReloadedAudio = true;
        }

        private void Awake()
        {
            string settings = "Soundtrack Settings";

            musicVolume = Config.Bind<float>(settings, "In-raid music volume", 0.025f, new ConfigDescription("This slider will control the volume of the music heard in raid", new AcceptableValueRange<float>(0.001f, 1f)));

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
            new SoundtrackPatch().Enable();
        }

        private static float trackTimer = 0f;

        private void Update()
        {
            if (Singleton<GameWorld>.Instance != null)
            {
                trackTimer += Time.deltaTime;
                if (trackTimer >= SoundtrackPatch.trackLength)
                {
                    trackTimer = 0f;
                    SoundtrackPatch.PlaySoundtrack();
                }
            }
            else
            {
                trackTimer = 0f;
                SoundtrackPatch.trackLength = 0f;
            }
        }

    }
}