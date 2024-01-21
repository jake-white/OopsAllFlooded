using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameNetcodeStuff;

namespace OopsAllFlooded.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class RoundPatch {
        public static FloodWeather facilityFlood;
        public static QuicksandTrigger facilityTrigger;
        [HarmonyPatch("OnShipLandedMiscEvents")]
        [HarmonyPostfix]
        static void FloodSpawnPatch() {
            Debug.Log("StartOfRound injection successful.");
            FloodWeather flood = GameObject.FindObjectOfType<FloodWeather>(true);
            LevelWeatherType currentWeather = TimeOfDay.Instance.currentLevelWeather;
            float intensity = TimeOfDay.Instance.currentWeatherVariable2;
            int seed = StartOfRound.Instance.randomMapSeed;
            Debug.Log("Seed = " + seed);
            if (flood != null) {
                GameObject floodObj = flood.gameObject;
                GameObject newFloodObj = GameObject.Instantiate(floodObj);
                newFloodObj.name = "Flooding (Facility)";
                facilityFlood = newFloodObj.GetComponent<FloodWeather>();
                facilityFlood.transform.parent = flood.transform.parent;
                facilityFlood.transform.position = new Vector3(0, TimeOfDay.Instance.currentWeatherVariable + FloodPatch.baseFlood, 0);
                Debug.Log(facilityFlood.transform.position);
                facilityTrigger = facilityFlood.GetComponentInChildren<QuicksandTrigger>();
                if (currentWeather == LevelWeatherType.Rainy || currentWeather == LevelWeatherType.Stormy) {
                    if (seed % 5 == 0) {
                        Debug.Log("Randomly flooding the interior!");
                        facilityFlood.gameObject.SetActive(true);
                    }
                }
            }
            else {
                Debug.Log("Flood hasn't spawned yet :(");
            }
        }

        [HarmonyPatch("ShipHasLeft")]
        [HarmonyPrefix]
        static void FloodDisable() { // destroy the facility flooding when the main flood is disabled (end of round)
            if (RoundPatch.facilityFlood != null) {
                GameObject.Destroy(RoundPatch.facilityFlood.gameObject);
            }
        }
    }
    [HarmonyPatch(typeof(FloodWeather))]
    internal class FloodPatch {
        public const float baseFlood = -225;
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool FloodLevelPatch(ref FloodWeather __instance) {
            if (__instance == RoundPatch.facilityFlood) {
                float effectiveFloodLevelOffset = Mathf.Clamp(__instance.floodLevelOffset, 2, 5);
                float y_pos = TimeOfDay.Instance.currentWeatherVariable + FloodPatch.baseFlood + (effectiveFloodLevelOffset * 4);
                y_pos = Mathf.Clamp(y_pos, -225, -212);
                __instance.transform.position = Vector3.MoveTowards(__instance.transform.position, new Vector3(0f, y_pos, 0f), 0.5f * Time.deltaTime);
                if (__instance.waterAudio != null) {
                    __instance.waterAudio.transform.position = new Vector3(GameNetworkManager.Instance.localPlayerController.transform.position.x, __instance.transform.position.y + 1f, GameNetworkManager.Instance.localPlayerController.transform.position.z);
                    float waterDistance = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, __instance.waterAudio.transform.position);
                    __instance.waterAudio.volume = Mathf.Lerp(__instance.waterAudio.volume, 3f - waterDistance, 0.1f);
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        static void FloodLevelStart(ref FloodWeather __instance) {
            if (__instance == RoundPatch.facilityFlood) {
                __instance.transform.position = new Vector3(0, TimeOfDay.Instance.currentWeatherVariable + FloodPatch.baseFlood, 0);
            }
        }

    }

    [HarmonyPatch(typeof(QuicksandTrigger))]
    internal class QuicksandPatch {
        [HarmonyPatch("OnTriggerStay")]
        [HarmonyPrefix]
        static void FacilityQuicksand(Collider other, ref QuicksandTrigger __instance) {
            if (__instance == RoundPatch.facilityTrigger) {
                if (__instance.isWater) {
                    if (!other.gameObject.CompareTag("Player")) {
                        return;
                    }
                    PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
                    if (component != GameNetworkManager.Instance.localPlayerController && component != null && component.underwaterCollider != __instance) {
                        component.underwaterCollider = __instance.gameObject.GetComponent<Collider>();
                        return;
                    }
                }
                if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory || GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom || (!__instance.isWater && !other.gameObject.CompareTag("Player"))) {
                    return;
                }
                PlayerControllerB component2 = other.gameObject.GetComponent<PlayerControllerB>();
                if (component2 != GameNetworkManager.Instance.localPlayerController) {
                    return;
                }
                if (__instance.isWater && !component2.isUnderwater) {
                    component2.underwaterCollider = __instance.gameObject.GetComponent<Collider>();
                    component2.isUnderwater = true;
                }
                component2.statusEffectAudioIndex = __instance.audioClipIndex;
                if(component2.isUnderwater && !component2.underwaterCollider.bounds.Contains(component2.gameplayCamera.transform.position)) { // underwater but not drowning
                    component2.statusEffectAudio.volume = component2.statusEffectAudio.volume * 0.25f;
                }
                if (component2.isSinking) {
                    return;
                }
                if (__instance.sinkingLocalPlayer) {
                    if (!component2.CheckConditionsForSinkingInQuicksand()) {
                        __instance.StopSinkingLocalPlayer(component2);
                    }
                }
                else if (component2.CheckConditionsForSinkingInQuicksand()) {
                    Debug.Log("Set local player to sinking!");
                    __instance.sinkingLocalPlayer = true;
                    component2.sourcesCausingSinking++;
                    component2.isMovementHindered++;
                    component2.hinderedMultiplier *= __instance.movementHinderance;
                    if (__instance.isWater) {
                        component2.sinkingSpeedMultiplier = 0f;
                    }
                    else {
                        component2.sinkingSpeedMultiplier = __instance.sinkingSpeedMultiplier;
                    }
                }
                return;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerPatch {
        static int footstepSurfaceStorage;

        [HarmonyPatch("CheckConditionsForSinkingInQuicksand")]
        [HarmonyPrefix]
        static void CheckPrefix(ref PlayerControllerB __instance) {
            if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) {
                footstepSurfaceStorage = __instance.currentFootstepSurfaceIndex;
                __instance.currentFootstepSurfaceIndex = 1;
            }
        }

        [HarmonyPatch("CheckConditionsForSinkingInQuicksand")]
        [HarmonyPostfix]
        static void CheckPostfix(ref PlayerControllerB __instance) {
            if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) {
                __instance.currentFootstepSurfaceIndex = footstepSurfaceStorage;
            }
        }
    }
}
