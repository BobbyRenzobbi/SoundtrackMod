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

namespace SoundtrackMod.Patches
{
    internal class SoundtrackPatch : ModulePatch
    {
        public static string[] GetTrack()
        {

            return new string[] { "aberration.ogg", "dw_dream.ogg", "dw_escape_from_a_dream.ogg", "dw_new_dawn.ogg", "dw_piotrek.ogg" };
        }
        private static readonly string[] clips = GetTrack();
        private static float trackLength = 0f;
        public static Player GetYourPlayer()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld.MainPlayer;
        }
        public static void PlaySoundtrack()
        {
            if (clips == null) return;
            int rndNumber = UnityEngine.Random.Range(0, clips.Length);
            string clip = clips[rndNumber];
            AudioClip audioClip = Plugin.tracks[clip];
            trackLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 0.1f, EOcclusionTest.None, null, false);
            Logger.LogInfo("Playing " + clip);
            Thread.Sleep(Convert.ToInt32((trackLength) * 1000));
            PlaySoundtrack();
        }
        static Thread t1 = new Thread(new ThreadStart(PlaySoundtrack));
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BaseLocalGame<>), nameof(BaseLocalGame<EftGamePlayerOwner>.method_0));
        }

        [PatchPostfix]
        static void Postfix()
        {
            t1.Start();
        }
    }
}