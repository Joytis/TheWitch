// Syncs a Steam Workshop item's gallery -- the "additional previews" (images + YouTube videos)
// shown under the main thumbnail -- to an ordered marketing manifest.
//
// MegaCrit's ModUploader only sets the main preview (workspace image.png), so this fills the gap
// with the ISteamUGC calls it lacks (AddItemPreviewFile / AddItemPreviewVideo / RemoveItemPreview).
// Normally run via `tools/bundle-workshop.ps1 -UpdateMarketing`.
//
// Usage:
//   WorkshopMarketing --item <publishedFileId> --manifest <marketing.json> [--dry-run] [--allow-empty]
//   WorkshopMarketing --item <publishedFileId> --pull <dir>
//
// --pull downloads the live gallery into <dir> (images under their Steam original filenames) and
// writes <dir>/pulled.json, a manifest-shaped list of what was there -- a backup / reference copy.
//
// The manifest is the source of truth: the gallery ends up holding exactly its entries, in order.
// Anything else on the item's gallery (e.g. images added by hand on the web page) is removed.
//
// Diffing: each image is uploaded under the name "<stem>.<sha256-prefix><ext>" so the original
// filename Steam stores encodes the content. Existing previews are matched against the manifest in
// order; the longest matching prefix is kept, everything after it is removed and re-added. An
// unchanged manifest therefore submits nothing.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Steamworks;

namespace WorkshopMarketing;

internal static class Program
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif"];
    private static readonly TimeSpan CallTimeout = TimeSpan.FromMinutes(10);

    private sealed record Desired(string Kind, string Key, string Label, string? UploadPath);

    private sealed record Existing(EItemPreviewType Type, string UrlOrVideoId, string OriginalFileName)
    {
        public string Key => Type switch
        {
            EItemPreviewType.k_EItemPreviewType_Image => "image:" + OriginalFileName,
            EItemPreviewType.k_EItemPreviewType_YouTubeVideo => "youtube:" + UrlOrVideoId,
            _ => $"{Type}:{UrlOrVideoId}",
        };

        public string Label => Type switch
        {
            EItemPreviewType.k_EItemPreviewType_Image => $"image   {OriginalFileName}",
            EItemPreviewType.k_EItemPreviewType_YouTubeVideo => $"youtube {UrlOrVideoId}",
            _ => $"{Type} {UrlOrVideoId}",
        };
    }

    private static int Main(string[] args)
    {
        ulong itemId = 0;
        string? manifestPath = null, pullDir = null;
        bool dryRun = false, allowEmpty = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--item" when i + 1 < args.Length:
                    if (!ulong.TryParse(args[++i].Trim(), out itemId)) return Usage($"bad --item '{args[i]}'");
                    break;
                case "--manifest" when i + 1 < args.Length:
                    manifestPath = Path.GetFullPath(args[++i]);
                    break;
                case "--pull" when i + 1 < args.Length:
                    pullDir = Path.GetFullPath(args[++i]);
                    break;
                case "--dry-run": dryRun = true; break;
                case "--allow-empty": allowEmpty = true; break;
                default: return Usage($"unknown argument '{args[i]}'");
            }
        }
        if (itemId == 0) return Usage("--item is required");
        if (manifestPath is null == pullDir is null) return Usage("pass exactly one of --manifest or --pull");

        List<Desired> desired = [];
        try
        {
            if (manifestPath is not null) desired = LoadManifest(manifestPath);
        }
        catch (Exception e)
        {
            Error($"manifest: {e.Message}");
            return 1;
        }
        if (manifestPath is not null && desired.Count == 0 && !allowEmpty)
        {
            Error("the manifest lists no previews; refusing to clear the item's gallery (pass --allow-empty to do that on purpose).");
            return 1;
        }

        // SteamAPI.Init reads steam_appid.txt from the working directory; it ships beside the exe.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        if (!SteamAPI.Init())
        {
            Error("SteamAPI.Init failed. Is Steam running and logged in as an account that owns Slay the Spire 2?");
            return 1;
        }

        try
        {
            return pullDir is not null
                ? Pull(new PublishedFileId_t(itemId), pullDir)
                : Sync(new PublishedFileId_t(itemId), desired, dryRun);
        }
        catch (Exception e)
        {
            Error(e.Message);
            return 1;
        }
        finally
        {
            SteamAPI.Shutdown();
        }
    }

    private static int Sync(PublishedFileId_t item, List<Desired> desired, bool dryRun)
    {
        AppId_t appId = SteamUtils.GetAppID();
        Step($"Logged in as '{SteamFriends.GetPersonaName()}' (app {appId.m_AppId}); item {item.m_PublishedFileId}.");

        List<Existing> existing = QueryGallery(item, appId);
        Step($"Current gallery ({existing.Count}):");
        PrintList(existing.Select(e => e.Label));

        int keep = 0;
        while (keep < existing.Count && keep < desired.Count
               && string.Equals(existing[keep].Key, desired[keep].Key, StringComparison.OrdinalIgnoreCase))
        {
            keep++;
        }
        int removeCount = existing.Count - keep;
        List<Desired> toAdd = desired.Skip(keep).ToList();

        Step($"Target gallery ({desired.Count}):");
        PrintList(desired.Select(d => d.Label));

        if (removeCount == 0 && toAdd.Count == 0)
        {
            Step("Gallery already matches the manifest. Nothing to do.");
            return 0;
        }

        Step($"Plan: keep {keep}, remove {removeCount}, add {toAdd.Count}.");
        for (int i = existing.Count - 1; i >= keep; i--) Detail($"- remove [{i}] {existing[i].Label}");
        foreach (Desired d in toAdd) Detail($"+ add    {d.Label}");

        if (dryRun)
        {
            Step("Dry run: nothing submitted.");
            return 0;
        }

        UGCUpdateHandle_t update = SteamUGC.StartItemUpdate(appId, item);
        // Highest index first: a removal never shifts the indices still queued after it.
        for (int i = existing.Count - 1; i >= keep; i--)
        {
            if (!SteamUGC.RemoveItemPreview(update, (uint)i)) throw new Exception($"RemoveItemPreview({i}) was rejected.");
        }
        foreach (Desired d in toAdd)
        {
            bool ok = d.Kind == "image"
                ? SteamUGC.AddItemPreviewFile(update, d.UploadPath!, EItemPreviewType.k_EItemPreviewType_Image)
                : SteamUGC.AddItemPreviewVideo(update, d.Key["youtube:".Length..]);
            if (!ok) throw new Exception($"Steam rejected preview {d.Label}.");
        }

        Step("Submitting gallery update...");
        SubmitItemUpdateResult_t result = Await<SubmitItemUpdateResult_t>(
            SteamUGC.SubmitItemUpdate(update, null),
            "SubmitItemUpdate",
            () =>
            {
                EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(update, out ulong done, out ulong total);
                return total > 0 ? $"{status} {done}/{total} bytes" : status.ToString();
            });
        if (result.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            Warn("Steam reports the Workshop legal agreement has not been accepted for this account.");
        }
        if (result.m_eResult != EResult.k_EResultOK) throw new Exception($"SubmitItemUpdate failed: {result.m_eResult}.");

        // Re-read the live gallery so a run always ends on what Steam actually stored.
        List<Existing> after = QueryGallery(item, appId);
        Step($"Gallery after update ({after.Count}):");
        PrintList(after.Select(e => e.Label));
        bool matches = after.Count == desired.Count
                       && after.Zip(desired).All(p => string.Equals(p.First.Key, p.Second.Key, StringComparison.OrdinalIgnoreCase));
        if (!matches)
        {
            Warn("The live gallery does not match the manifest key-for-key; the next run will re-upload the mismatched tail.");
        }
        Step("Done.");
        return 0;
    }

    private static int Pull(PublishedFileId_t item, string dir)
    {
        AppId_t appId = SteamUtils.GetAppID();
        List<Existing> existing = QueryGallery(item, appId);
        Directory.CreateDirectory(dir);
        using var http = new HttpClient();
        var entries = new List<Dictionary<string, string>>();
        foreach (Existing e in existing)
        {
            if (e.Type == EItemPreviewType.k_EItemPreviewType_YouTubeVideo)
            {
                entries.Add(new() { ["youtube"] = e.UrlOrVideoId });
                Detail($"youtube {e.UrlOrVideoId}");
            }
            else if (e.Type == EItemPreviewType.k_EItemPreviewType_Image)
            {
                string name = Path.GetFileName(e.OriginalFileName);
                if (string.IsNullOrWhiteSpace(name)) name = $"preview-{entries.Count}.png";
                File.WriteAllBytes(Path.Combine(dir, name), http.GetByteArrayAsync(e.UrlOrVideoId).GetAwaiter().GetResult());
                entries.Add(new() { ["image"] = name });
                Detail($"image   {name}");
            }
            else
            {
                Warn($"skipped {e.Label}");
            }
        }
        string json = JsonSerializer.Serialize(new { previews = entries }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(dir, "pulled.json"), json + Environment.NewLine);
        Step($"Pulled {entries.Count} gallery entr{(entries.Count == 1 ? "y" : "ies")} into {dir}.");
        return 0;
    }

    private static List<Existing> QueryGallery(PublishedFileId_t item, AppId_t appId)
    {
        UGCQueryHandle_t query = SteamUGC.CreateQueryUGCDetailsRequest([item], 1);
        if (query == UGCQueryHandle_t.Invalid) throw new Exception("CreateQueryUGCDetailsRequest returned an invalid handle.");
        try
        {
            SteamUGC.SetReturnAdditionalPreviews(query, true);
            SteamUGC.SetAllowCachedResponse(query, 0);
            SteamUGCQueryCompleted_t done = Await<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(query), "SendQueryUGCRequest");
            if (done.m_eResult != EResult.k_EResultOK) throw new Exception($"Workshop query failed: {done.m_eResult}.");
            if (done.m_unNumResultsReturned < 1) throw new Exception($"Workshop item {item.m_PublishedFileId} not found.");

            if (!SteamUGC.GetQueryUGCResult(query, 0, out SteamUGCDetails_t details) || details.m_eResult != EResult.k_EResultOK)
            {
                throw new Exception($"Workshop item {item.m_PublishedFileId} not readable ({details.m_eResult}).");
            }
            if (details.m_nConsumerAppID != appId)
            {
                throw new Exception($"Item {item.m_PublishedFileId} belongs to app {details.m_nConsumerAppID.m_AppId}, not {appId.m_AppId}.");
            }
            if (details.m_ulSteamIDOwner != SteamUser.GetSteamID().m_SteamID)
            {
                throw new Exception($"Item {item.m_PublishedFileId} ('{details.m_rgchTitle}') is not owned by the logged-in Steam account.");
            }

            var list = new List<Existing>();
            uint count = SteamUGC.GetQueryUGCNumAdditionalPreviews(query, 0);
            for (uint i = 0; i < count; i++)
            {
                if (!SteamUGC.GetQueryUGCAdditionalPreview(query, 0, i, out string url, 1024, out string original, 260, out EItemPreviewType type))
                {
                    throw new Exception($"GetQueryUGCAdditionalPreview({i}) failed.");
                }
                list.Add(new Existing(type, url ?? "", original ?? ""));
            }
            return list;
        }
        finally
        {
            SteamUGC.ReleaseQueryUGCRequest(query);
        }
    }

    // Manifest: { "previews": [ { "image": "file.png" } | { "youtube": "<url or id>" }, ... ] }
    // Extra fields (e.g. "note") are ignored. Image paths are relative to the manifest's folder.
    private static List<Desired> LoadManifest(string path)
    {
        if (!File.Exists(path)) throw new Exception($"not found: {path}");
        string dir = Path.GetDirectoryName(path)!;
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
        if (!doc.RootElement.TryGetProperty("previews", out JsonElement previews) || previews.ValueKind != JsonValueKind.Array)
        {
            throw new Exception("expected a top-level \"previews\" array.");
        }

        // Fresh staging folder per run: images are uploaded from here under their content-hashed names.
        string staging = Path.Combine(Path.GetTempPath(), "WorkshopMarketing", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(staging);

        var list = new List<Desired>();
        int index = 0;
        foreach (JsonElement entry in previews.EnumerateArray())
        {
            string where = $"previews[{index++}]";
            bool hasImage = entry.TryGetProperty("image", out JsonElement image);
            bool hasVideo = entry.TryGetProperty("youtube", out JsonElement video);
            if (hasImage == hasVideo) throw new Exception($"{where}: needs exactly one of \"image\" or \"youtube\".");

            if (hasImage)
            {
                string rel = image.GetString() ?? "";
                string src = Path.GetFullPath(Path.Combine(dir, rel));
                if (!File.Exists(src)) throw new Exception($"{where}: image not found: {src}");
                string ext = Path.GetExtension(src).ToLowerInvariant();
                if (!ImageExtensions.Contains(ext)) throw new Exception($"{where}: unsupported image type '{ext}' (png/jpg/gif).");

                string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(src)))[..8].ToLowerInvariant();
                string uploadName = $"{Path.GetFileNameWithoutExtension(src)}.{hash}{ext}";
                string staged = Path.Combine(staging, uploadName);
                File.Copy(src, staged, overwrite: true);
                list.Add(new Desired("image", "image:" + uploadName, $"image   {uploadName}", staged));
            }
            else
            {
                string raw = video.GetString() ?? "";
                string id = ParseYouTubeId(raw) ?? throw new Exception($"{where}: can't read a YouTube video id from '{raw}'.");
                list.Add(new Desired("youtube", "youtube:" + id, $"youtube {id}", null));
            }
        }
        return list;
    }

    // Accepts a bare 11-char id or a youtube.com/watch?v= | youtu.be/ | /shorts/ | /embed/ URL.
    private static string? ParseYouTubeId(string raw)
    {
        raw = raw.Trim();
        if (Regex.IsMatch(raw, "^[A-Za-z0-9_-]{11}$")) return raw;
        Match m = Regex.Match(raw, @"(?:[?&]v=|youtu\.be/|/shorts/|/embed/|/live/)([A-Za-z0-9_-]{11})(?![A-Za-z0-9_-])");
        return m.Success ? m.Groups[1].Value : null;
    }

    // Pumps Steam callbacks until the call result arrives. `progress` is printed every few seconds.
    private static T Await<T>(SteamAPICall_t call, string what, Func<string>? progress = null) where T : struct
    {
        if (call == SteamAPICall_t.Invalid) throw new Exception($"{what} returned an invalid call handle.");
        T result = default;
        bool done = false, ioFailure = false;
        CallResult<T> callResult = CallResult<T>.Create((r, failure) =>
        {
            result = r;
            ioFailure = failure;
            done = true;
        });
        callResult.Set(call);

        var clock = Stopwatch.StartNew();
        TimeSpan nextReport = TimeSpan.FromSeconds(3);
        while (!done)
        {
            SteamAPI.RunCallbacks();
            if (clock.Elapsed > CallTimeout) throw new Exception($"{what} timed out after {CallTimeout.TotalMinutes} minutes.");
            if (progress is not null && clock.Elapsed > nextReport)
            {
                Detail(progress());
                nextReport += TimeSpan.FromSeconds(3);
            }
            Thread.Sleep(100);
        }
        GC.KeepAlive(callResult);
        if (ioFailure) throw new Exception($"{what} failed (Steam I/O failure).");
        return result;
    }

    private static void PrintList(IEnumerable<string> labels)
    {
        int i = 0;
        foreach (string label in labels) Detail($"[{i++}] {label}");
        if (i == 0) Detail("(empty)");
    }

    private static int Usage(string message)
    {
        Error(message);
        Console.Error.WriteLine("usage: WorkshopMarketing --item <publishedFileId> (--manifest <marketing.json> [--dry-run] [--allow-empty] | --pull <dir>)");
        return 2;
    }

    private static void Step(string msg) => Write("==> " + msg, ConsoleColor.Cyan);
    private static void Detail(string msg) => Write("    " + msg, ConsoleColor.DarkGray);
    private static void Warn(string msg) => Write("[warn] " + msg, ConsoleColor.Yellow);
    private static void Error(string msg) => Write("[error] " + msg, ConsoleColor.Red, Console.Error);

    private static void Write(string msg, ConsoleColor color, TextWriter? writer = null)
    {
        ConsoleColor prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        (writer ?? Console.Out).WriteLine(msg);
        Console.ForegroundColor = prev;
    }
}
