using HarmonyLib;
using NeosModLoader;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using FrooxEngine;
using FrooxEngine.LogiX;
using CodeX;
using FrooxEngine.UIX;

namespace PasteInvidious
{
    public class PasteInvidious : NeosMod
    {
        public override string Name => "PasteInvidious";
        public override string Author => "art0007i";
        public override string Version => "1.1.1";
        public override string Link => "https://github.com/art0007i/PasteInvidious/";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.art0007i.PasteInvidious");
            harmony.PatchAll();
            config = GetConfiguration();
        }

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<string> KEY_INSTANCE = new ModConfigurationKey<string>("invidious_instance",
                                    "The invidious instance to use when importing videos", () => "invidious-us.kavin.rocks");
        public static ModConfiguration config;
        public static string InvidiousInstance { get { return config.GetValue(KEY_INSTANCE); } }

        [HarmonyPatch(typeof(VideoImportDialog))]
        [HarmonyPatch("OpenRoot")]
        class VideoImportDialog_OpenRoot_Patch
        {
            public static void Postfix(VideoImportDialog __instance, UIBuilder ui)
            {
                bool paths = __instance.Paths.Where(str =>
                {
                    Uri uri;
                    if (!Uri.TryCreate(str, UriKind.Absolute, out uri)) return false;
                    return uri.Host.Contains(InvidiousInstance);
                }).Any();
                if (!paths)
                {
                    ui.Button("Import Invidious").LocalPressed += (btn, bed) => {
                        if ((bool)AccessTools.Property(typeof(ImportDialog), "CanInteract").GetValue(__instance))
                        {
                            __instance.Paths = __instance.Paths.ConvertAll(str =>
                            {
                                Uri uri;
                                if (Uri.TryCreate(str, UriKind.Absolute, out uri))
                                {
                                    if (uri.Host.Contains("youtube.com"))
                                        return str.Replace(uri.Host, InvidiousInstance) + "&raw=1&quality=medium";
                                    else if (uri.Host.Contains("youtu.be"))
                                        return str.Replace(uri.Host + "/", InvidiousInstance + "/watch?v=") + "&raw=1&quality=medium";
                                }
                                return str;
                            });
                            // hmm yes reflection is fun
                            AccessTools.Method(typeof(ImportDialog), "Open").Invoke(__instance, new object[] { (Action<UIBuilder>)AccessTools.Method(typeof(VideoImportDialog), "OpenRoot", new Type[] { typeof(UIBuilder) }).CreateDelegate(typeof(Action<UIBuilder>), __instance) });
                        }
                    };
                }
            }
        }
    }
}