using SPT.Reflection.Patching;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using System.Runtime.CompilerServices;
using SoundtrackMod;
using UnityEngine;
using Comfort.Common;
using System.Threading;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Collections;

namespace SoundtrackMod.Patches
{
    internal class SoundtrackPatch : ModulePatch
    {
        private static ManualResetEvent mre = new ManualResetEvent(false);
        private static string[] clips;
        public static string[] GetTrack()
        {
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds");
        }
        internal static float trackLength = 0f;
        public static void PlaySoundtrack()
        {
            if (clips == null)
            {
                Logger.LogInfo("No audio files found");
                return;
            }
            int rndNumber = UnityEngine.Random.Range(0, clips.Length);
            string clip = clips[rndNumber];
            AudioClip audioClip = Plugin.tracks[clip];
            trackLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, Plugin.musicVolume.Value, EOcclusionTest.None, null, false);
            Logger.LogInfo("Playing " + clip);
        }
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BaseLocalGame<>), nameof(BaseLocalGame<EftGamePlayerOwner>.method_0));
        }

        [PatchPostfix]
        static void Postfix()
        {
            clips = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds").Select(file => Path.GetFileName(file)).ToArray();
            {
                try
                {
                    PlaySoundtrack();
                }
                catch (Exception e)
                {
                    Logger.LogInfo(e.Message);
                }
            }
        }
    }
}