using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using HarmonyLib;
using BepInEx;
using BepInEx.Unity.Mono;
using BepInEx.Configuration;

using UnityEngine;
using Sounds;
using System.Collections;
using UnityEngine.Scripting;
using Projectiles;

#pragma warning disable 0618 //shut up unity
namespace Chair
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Mod : BaseUnityPlugin
    {
        public const string pluginGuid = "kestrel.iamyourbeast.chair";
        public const string pluginName = "Chair";
        public const string pluginVersion = "1.0.0";
        static string pluginPath = Path.Combine(Paths.PluginPath, "Chair");
        static string sfxPath = Path.Combine(pluginPath, "hitsfx.ogg");

        static Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        static Material defaultMaterial = new Material(defaultShader);
        static Material woodMat;

        static AudioClip customClip;

        static AssetBundle bundle;

        public void Awake() {
            bundle = AssetBundle.LoadFromFile(Path.Combine(pluginPath, "chair"));
            if (bundle == null) Logger.LogError("Failed to load assetbundle. :c");
            else Logger.LogMessage("Loaded AssetBundlle! chair time");

            if (File.Exists(sfxPath)) {
                Logger.LogInfo("Loading custom hit sfx...");
                StartCoroutine(LoadClip(sfxPath));
                AudioSource.PlayClipAtPoint(customClip, new Vector3(0, 0, 0));
            }

            Logger.LogInfo("Hiiiiiiiiiiii :3");
            new Harmony(pluginGuid).PatchAll();
        }

        private IEnumerator LoadClip(string path) {
            WWW www = new WWW("file:///" + path);    
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

                var nObj = GameObject.Instantiate(bundle.LoadAsset<GameObject>("Chair hand"), bark);
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
                    var nObj = GameObject.Instantiate(bundle.LoadAsset<GameObject>("Chair ground"), obj.transform);
                    nObj.GetComponent<MeshRenderer>().material = obj.GetComponent<MeshRenderer>().material;
                    obj.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerWeaponToss), "OnCollisionEnter")]
        public class Collide
        {
            [HarmonyPostfix]
            public static void Postfix(Collision collision, ref Transform ___weapon) {
                if (collision.transform.GetComponentInParent<Enemy.Enemy>()) {
                    UnityEngine.Object.Destroy(___weapon.GetChild(0).gameObject);
                } else {
                    ___weapon.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = woodMat;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerWeaponArmed))]
        public class SfxReplace
        {
            [HarmonyPatch(nameof(PlayerWeaponArmed.Initialize))]
            [HarmonyPostfix]
            public static void Postfix(WeaponPickup pickup, PlayerWeaponArmed __instance) {
                if (pickup.GetDebugName() == "Tree Bark" && customClip != null) {
                    GameObject sObj = Traverse.Create(__instance).Field("SFXImpact").GetValue() as GameObject;
                    sObj.GetComponent<SoundObject>().SetClip(customClip);
                }
            }
        }

        [HarmonyPatch(typeof(SpherecastProjectile), "Fire")]
        public class Bonk
        {
            [HarmonyPrefix]
            public static void Prefix(SpherecastProjectile __instance, ref float ___ragdollForce, ref ProjectileInformation ___projectileInformation) {
                if (___projectileInformation.GetPenetrationType() == ProjectileInformation.PenetrationType.Dull && __instance.GetCollisionType() == SpherecastProjectile.CollisionType.AllTargets)
                    ___ragdollForce *= 3;
            }
        }
    }
}
