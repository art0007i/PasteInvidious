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

namespace PasteInvidious
{
    public class PasteInvidious : NeosMod
    {
        public override string Name => "PasteInvidious";
        public override string Author => "art0007i";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/art0007i/PasteInvidious/";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.art0007i.PasteInvidious");
            harmony.PatchAll();

        }

        [HarmonyPatch(typeof(UniversalImporter))]
        [HarmonyPatch("ImportTask")]
        class UniversalImporter_ImportTask_Patch
        {
            public static void Prefix(AssetClass assetClass, ref IEnumerable<string> files)
            {
                List<string> newList = new List<string>();
                foreach (string file in files)
                {
                    string item = file;
                    Uri uri;
                    if (assetClass == AssetClass.Video && Uri.TryCreate(file, UriKind.Absolute, out uri) && (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be")))
                    {
                        item = file.Replace(uri.Host, "ytprivate.com") + "&raw=1";
                    }
                    newList.Add(item);
                }
                files = newList;
            }
        }
    }
}