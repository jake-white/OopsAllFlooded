using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OopsAllFlooded.Patches
{
    class OxyCanisterItem : TetraChemicalItem
    {
        public new IEnumerator UseTZPAnimation() {
            thisAudioSource.PlayOneShot(holdCanSFX);
            WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.itemAudio, holdCanSFX);
            yield return new WaitForSeconds(0.75f);
            emittingGas = true;
            if(base.IsOwner) {
                localHelmetSFX.Play();
                localHelmetSFX.PlayOneShot(twistCanSFX);
            }
            else {
                thisAudioSource.clip = releaseGasSFX;
                thisAudioSource.Play();
                thisAudioSource.PlayOneShot(twistCanSFX);
            }
        }

        public override void Update() {
            if(previousPlayerHeldBy != null) {
                float drunknessInertiaStorage = previousPlayerHeldBy.drunknessInertia;
                base.Update();
                previousPlayerHeldBy.drunknessInertia = drunknessInertiaStorage;
                previousPlayerHeldBy.increasingDrunknessThisFrame = false;
            }
            else {
                base.Update();
            }
            if (emittingGas) {
                if (previousPlayerHeldBy == GameNetworkManager.Instance.localPlayerController) {
                    float previousDrowningTimer = StartOfRound.Instance.drowningTimer;
                    StartOfRound.Instance.drowningTimer = Mathf.Clamp(StartOfRound.Instance.drowningTimer + (Time.deltaTime / 2f), 0f, 1f);
                    if (previousDrowningTimer <= 0.3f && StartOfRound.Instance.drowningTimer > 0.3f) {
                        StartOfRound.Instance.playedDrowningSFX = false;
                    }
                    if (previousPlayerHeldBy.isUnderwater) {
                        previousPlayerHeldBy.sprintMeter = Mathf.Clamp(previousPlayerHeldBy.sprintMeter + (Time.deltaTime * 0.1f), 0f, 1.25f);
                        if (previousPlayerHeldBy.sprintMeter > 0.2f) {
                            previousPlayerHeldBy.isExhausted = false;
                        }
                    }
                }
            }
        }
    }
}
