using HarmonyLib;
using NeosModLoader;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using FrooxEngine;
using FrooxEngine.UIX;
using System.Text.RegularExpressions;
namespace PasteInvidious
{
    public class PasteInvidious : NeosMod
    {
        public override string Name => "PasteInvidious";
        public override string Author => "art0007i";
        public override string Version => "2.0.1";
        public override string Link => "https://github.com/art0007i/PastePiped/";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.art0007i.PasteInvidious");
            harmony.PatchAll();
        }
        public const string PIPED_PATH = "https://pipedapi.kavin.rocks/streams/";

        [HarmonyPatch(typeof(VideoImportDialog))]
        [HarmonyPatch("OpenRoot")]
        class VideoImportDialog_OpenRoot_Patch
        {
            struct VideoInfo
            {
                public string mimeType { get; set; }
                public string quality { get; set; }
                public string url { get; set; }
                public bool videoOnly { get; set; }
            }
            class PipedResponse
            {
                public List<VideoInfo> videoStreams { get; set; }
            }

            public static void Postfix(VideoImportDialog __instance, UIBuilder ui)
            {
                bool paths = __instance.Paths.Where(str =>
                {
                    Uri uri;
                    if (!Uri.TryCreate(str, UriKind.Absolute, out uri)) return false;

                    // this is not the most reliable method...
                    // but it works for now and its not too critical if it doesn't work
                    return uri.Host.Contains("pipedproxy");
                }).Any();
                if (!paths)
                {
                    ui.Button("Import Piped").LocalPressed += async (btn, bed) => {
                        if ((bool)AccessTools.Property(typeof(ImportDialog), "CanInteract").GetValue(__instance))
                        {
                            // required to use ToBackground and ToWorld
                            CoroutineManager.Manager.Value = __instance.World.Coroutines;
                            
                            // Because we are in an async context, we should prevent the user from clicking again
                            btn.Enabled = false;
                            btn.LabelText = "Importing...";
          
                            await default(ToBackground);
                            __instance.Paths = (await Task.WhenAll(__instance.Paths.ConvertAll(async str =>
                            {
                                Uri uri;
                                if (Uri.TryCreate(str, UriKind.Absolute, out uri))
                                {
                                    if (!(uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be"))) return str;

                                    // regex from https://stackoverflow.com/a/6904504
                                    string videoId = Regex.Match(str,
                                        "(?:youtube\\.com\\/(?:[^\\/]+\\/.+\\/|(?:v|e(?:mbed)?)\\/|.*[?&]v=)|youtu\\.be\\/)([^\"&?\\/\\s]{11})")
                                        .Groups[1].Value;
                                    if (videoId == null) return str;
                                    Debug("Got video id: " + videoId);

                                    // Piped requires an api request to get stream urls that we can use in a video player.
                                    var t = await __instance.Engine.Cloud.GET<PipedResponse>(PIPED_PATH + videoId);
                                    Debug("Got response from piped server, status code: " + t.State);

                                    int parseYtQuality(VideoInfo v)
                                    {
                                        // converts 360p -> 3600 and 1080p60 -> 1080060
                                        // used for sorting the list of available streams
                                        return int.TryParse(v.quality.Replace('p', '0'), out var vid) ? vid : 0;
                                    };
                                    // Find the best quality video stream, that has both audio and video channels
                                    var s = t.Entity.videoStreams
                                        .Where((v) => v.videoOnly == false)
                                        .Aggregate((i1, i2) => parseYtQuality(i1) > parseYtQuality(i2) ? i1 : i2);

                                    Debug("Found piped video url, quality: " + s.quality + ", mime type: " + s.mimeType);
                                    // neos doesn't want to play the video unless it ends with mp4, so doing this actually works...
                                    return s.url + "&neossucks=.mp4";
                                }
                                return str;
                            }))).ToList();
                            await default(ToWorld);

                            // hmm yes reflection is fun
                            AccessTools.Method(typeof(ImportDialog), "Open")
                                .Invoke(__instance, new object[] {
                                    (Action<UIBuilder>)AccessTools.Method(typeof(VideoImportDialog),
                                        "OpenRoot",
                                        new Type[] { typeof(UIBuilder) })
                                        .CreateDelegate(typeof(Action<UIBuilder>), __instance) });
                        }
                    };
                }
            }
        }
    }
}