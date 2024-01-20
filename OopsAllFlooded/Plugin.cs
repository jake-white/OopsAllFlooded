using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OopsAllFlooded
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class TestClass : BaseUnityPlugin
    {
        private static TestClass instance;
        private const string modGUID = "squirrelboy.OopsAllFlooded";
        private const string modName = "Oops! All Flooded";
        private const string modVersion = "0.0.1";

        private readonly Harmony harmony = new Harmony(modGUID);

        internal ManualLogSource mls;

        void Awake() {
            if(instance == null) {
                instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            mls.LogInfo("Watch out for floods.");

            harmony.PatchAll();
        }

        void Start() {

        }
    }
}
