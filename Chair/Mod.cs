﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Projectiles;
using Sounds;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

#pragma warning disable 0618 //shut up unity
namespace Chair
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Mod : BaseUnityPlugin
    {
        public const string pluginGuid = "kestrel.iamyourbeast.chair";
        public const string pluginName = "Chair";
        public const string pluginVersion = "2.1.0";
        static string pluginPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        static string sfxPath = Path.Combine(pluginPath, "hitsfx.ogg");

        static Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        static Material defaultMaterial = new Material(defaultShader);
        static Material woodMat;

        static AudioClip customClip;

        static AssetBundle bundle;

        public static ConfigEntry<bool> funnyRagdoll;

        public void Awake() {
            bundle = AssetBundle.LoadFromFile(Path.Combine(pluginPath, "chair"));
            if (!bundle) Logger.LogError("Failed to load assetbundle. :c");
            else Logger.LogMessage("Loaded AssetBundlle! chair time");

            funnyRagdoll = Config.Bind("Toggles", "Funny Ragdolls", true, "Enable/disable funny ragdoll fling. if you turn this off you hate fun and you also suck and are mean and bad and evil");

            if (File.Exists(sfxPath)) {
                Logger.LogInfo("Loading custom hit sfx...");
                StartCoroutine(LoadClip(sfxPath));
                AudioSource.PlayClipAtPoint(customClip, new Vector3(0, 0, 0));
            }

            Logger.LogInfo("Hiiiiiiiiiiii :3");
            new Harmony(pluginGuid).PatchAll();
        }

        private IEnumerator LoadClip(string path) {
            var www = new WWW("file:///" + path);    
            customClip = www.GetAudioClip(false);
            while (!customClip.isReadyToPlay)
                yield return www;
        }

        [HarmonyPatch(typeof(PlayerArmManager))]
        public class HeldModelReplace
        {
            [HarmonyPatch(nameof(PlayerArmManager.EquipWeapon))]
            [HarmonyPostfix]
            public static void Postfix(ref PlayerWeaponArmed ___equippedPrimary) {
                var obj = ___equippedPrimary.gameObject;
                var originalBark = obj.transform.GetChild(0).GetChild(2).gameObject;
                if (!originalBark.activeInHierarchy) return;

                //shut up. i know its bad
                var bark = obj.transform.GetChild(0).GetChild(0).GetChild(3).GetChild(0).GetChild(1);

                var nObj = Object.Instantiate(bundle.LoadAsset<GameObject>("Chair hand"), bark);
                woodMat = originalBark.GetComponent<SkinnedMeshRenderer>().material;
                nObj.GetComponent<MeshRenderer>().material = woodMat;
                originalBark.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(WeaponPickup))]
        public class GroundModelReplace
        {
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            public static void Postfix(WeaponPickup __instance) {
                if (__instance.GetDebugName() == "Tree Bark") {
                    var obj = __instance.gameObject;
                    var nObj = Object.Instantiate(bundle.LoadAsset<GameObject>("Chair ground"), obj.transform);
                    nObj.GetComponent<MeshRenderer>().material = obj.GetComponent<MeshRenderer>().material;
                    obj.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerWeaponToss), "OnCollisionEnter")]
        public class Collide
        {
            [HarmonyPostfix]
            public static void Postfix(Collision collision, Transform ___weapon) {
                //if this throws then we _probably_ arent looking at a thrown tree bark
                try {
                    if (collision.transform.GetComponentInParent<Enemy.Enemy>()) {
                        UnityEngine.Object.Destroy(___weapon.GetChild(0).gameObject);
                    } else {
                        ___weapon.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = woodMat;
                    }
                } catch {
                    //lazy but it works
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerWeaponArmed))]
        public class SfxReplace
        {
            [HarmonyPatch(nameof(PlayerWeaponArmed.Initialize))]
            [HarmonyPostfix]
            public static void Postfix(WeaponPickup pickup, PlayerWeaponArmed __instance) {
                if (pickup.GetDebugName() == "Tree Bark" && customClip) {
                    var sObj = (__instance as PlayerMeleeArmed)?.SFXImpact;
                    if (sObj) sObj.GetComponent<SoundObject>().SetClip(customClip);
                }
            }
        }

        [HarmonyPatch(typeof(SpherecastProjectile), "Fire")]
        public class Bonk
        {
            [HarmonyPrefix]
            public static void Prefix(SpherecastProjectile __instance, ref float ___ragdollForce, ref ProjectileInformation ___projectileInformation) {
                if (funnyRagdoll.Value
                    && ___projectileInformation.GetPenetrationType() == ProjectileInformation.PenetrationType.Dull
                    && __instance.GetCollisionType() == SpherecastProjectile.CollisionType.AllTargets) {
                    ___ragdollForce *= 3;
                }
            }
        }
    }
}
