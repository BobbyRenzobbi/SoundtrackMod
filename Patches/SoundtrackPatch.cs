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
using Newtonsoft.Json.Linq;
using System.IO;

namespace SoundtrackMod.Patches

{
    internal class SoundtrackPatch : ModulePatch
    {
        private static string[] newClips;
        private static bool _replaceOriginal;
        public static string[] GetTrack()
        {
            newClips = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds");
            return new string[] { "aberration.ogg", "dw_dream.ogg", "dw_escape_from_a_dream.ogg", "dw_new_dawn.ogg", "dw_piotrek.ogg" };
        }
        private static float trackLength = 0f;
        public static Player GetYourPlayer()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld.MainPlayer;
        }
        public static void PlaySoundtrack(Player player)
        {
            string[] clips = GetTrack();
            if (clips == null) return;
            int rndNumber = UnityEngine.Random.Range(0, clips.Length);
            string clip = clips[rndNumber];
            AudioClip audioClip = Plugin.tracks[clip];
            trackLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 0.1f, EOcclusionTest.None, null, false);
        }
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BaseLocalGame<>), nameof(BaseLocalGame<EftGamePlayerOwner>.method_0));
        }
        private static void Shuffle(string[] clips)
        {
            int n = clips.Length;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (clips[k], clips[n]) = (clips[n], clips[k]);
            }
        }

        [PatchPrefix]
        static bool Prefix()
        {
            return true;
        }

        [PatchPostfix]
        static void Postfix()
        {
            PlaySoundtrack(GetYourPlayer());
        }
    }
}