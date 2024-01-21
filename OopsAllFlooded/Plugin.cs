using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using LethalLib.Modules;

namespace OopsAllFlooded
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class TestClass : BaseUnityPlugin
    {
        private static TestClass instance;
        private const string modGUID = "squirrelboy.OopsAllFlooded";
        private const string modName = "Oops! All Flooded";
        private const string modVersion = "0.1.1";

        private readonly Harmony harmony = new Harmony(modGUID);

        internal ManualLogSource mls;

        void Awake() {
            if(instance == null) {
                instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "oxymod");
            AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);

            Item oxycanister = bundle.LoadAsset<Item>("Assets/OxyMod/OxyItem.asset");
            NetworkPrefabs.RegisterNetworkPrefab(oxycanister.spawnPrefab);
            Utilities.FixMixerGroups(oxycanister.spawnPrefab);
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "Limited air supply, useful for flooded facilities.";
            Items.RegisterShopItem(oxycanister, null, null, node, 25);

            mls.LogInfo("Watch out for floods.");

            harmony.PatchAll();
        }
    }
}
