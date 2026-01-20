using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnlockAllRecipes.UnlockAllRecipes;
using static UnlockAllRecipes.SharedState;

namespace UnlockAllRecipes
{
    public static class SharedState
    {
    }

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class UnlockAllRecipes : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public const string pluginGuid = "shushu.casualtiesunknown.unlockallrecipes";
        public const string pluginName = "Unlock All Recipes";
        // Year.Month.Version.Bugfix
        public const string pluginVersion = "26.1.1.0";

        public static UnlockAllRecipes Instance;

        public static int isOkayToPatch = 0;

        public void Awake()
        {
            Instance = this;
            logger = Logger;

            logger.LogInfo("Awake() ran - mod loaded!");

            Harmony harmony = new Harmony(pluginGuid);

            List<MethodInfo> patches = typeof(MyPatches).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList();
            foreach (MethodInfo patch in patches)
            {
                try
                {

                    string[] splitName = patch.Name.Replace("__", "$").Split('_');
                    for (int i = 0; i < splitName.Length; i++)
                        splitName[i] = splitName[i].Replace("$", "_");

                    string targetType = splitName[0];
                    MethodType targetMethodType = splitName[1].Contains("get_") ? MethodType.Getter : splitName[1].Contains("set_") ? MethodType.Setter : MethodType.Normal;
                    string ogTargetMethod = splitName[1];
                    string targetMethod = splitName[1].Replace("get_", "").Replace("set_", "");
                    string patchType = splitName[2];

                    MethodInfo ogScript = null;
                    switch (targetMethodType)
                    {
                        case MethodType.Normal:
                            ogScript = AccessTools.Method(AccessTools.TypeByName(targetType), targetMethod);
                            break;

                        case MethodType.Getter:
                            ogScript = AccessTools.PropertyGetter(AccessTools.TypeByName(targetType), targetMethod);
                            break;

                        case MethodType.Setter:
                        case MethodType.Constructor:
                        case MethodType.StaticConstructor:
                        case MethodType.Enumerator:
                        default:
                            throw new Exception($"Unknown patch method\nPatch method type \"{targetMethodType}\" currently has no handling");
                    }

                    MethodInfo patchScript = typeof(MyPatches).GetMethod(patch.Name);
                    if (ogScript == null || patchScript == null || (patchType != "Prefix" && patchType != "Postfix"))
                    {
                        throw new Exception("Patch method is named incorrectly\nPlease make sure the Patch method is named in the following pattern:\n\tTargetClass_TargetMethod_PatchType");
                    }
                    HarmonyMethod harmonyMethod = new HarmonyMethod(patchScript)
                    {
                        methodType = targetMethodType
                    };
                    HarmonyMethod postfix = null;
                    HarmonyMethod prefix = null;
                    if (patchType == "Prefix") prefix = harmonyMethod;
                    if (patchType == "Postfix") postfix = harmonyMethod;
                    harmony.Patch(ogScript, prefix: prefix, postfix: postfix);
                    Log("Patched " + targetType + "." + targetMethod + " as a " + patchType);
                }
                catch (Exception exception)
                {
                    logger.LogError("Failed to patch " + patch.Name);
                    logger.LogError(exception);
                }
            }
        }

        public static void Log(string message)
        {
            logger.LogInfo(message);
        }
    }

    public class MyPatches
    {
        [HarmonyPatch(typeof(Body))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Body_Start_Postfix(Body __instance)
        {
            for (int i = 0; i < Recipes.recipes.Count; i++)
            {
                Recipe recipe = Recipes.recipes[i];
                recipe.INT = 0;
            }
        }
    }
}
