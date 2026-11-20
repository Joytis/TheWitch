// Syncs a Steam Workshop item's marketing material to an ordered manifest: the gallery -- the
// "additional previews" (images + YouTube videos) shown under the main thumbnail -- and, when the
// manifest names one, the item's BBCode description.
//
// MegaCrit's ModUploader only sets the main preview (workspace image.png) and is given a null
// description, so this fills the gap with the ISteamUGC calls it lacks (AddItemPreviewFile /
// AddItemPreviewVideo / RemoveItemPreview / SetItemDescription).
// Normally run via `tools/bundle-workshop.ps1 -UpdateMarketing`.
//
// Usage:
//   WorkshopMarketing --item <publishedFileId> --manifest <marketing.json> [--dry-run] [--allow-empty]
//   WorkshopMarketing --item <publishedFileId> --pull <dir>
//
// --pull downloads the live gallery into <dir> (images under their Steam original filenames) plus
// the live description (<dir>/description.bbcode.txt), and writes <dir>/pulled.json, a
// manifest-shaped list of what was there -- a backup / reference copy.
//
// The manifest is the source of truth: the gallery ends up holding exactly its entries, in order,
// and the description exactly the named file. Anything else on the item's gallery (e.g. images
// added by hand on the web page) is removed, and a description edited on the web page is
// overwritten.
//
// Gallery diffing: each image is uploaded under the name "<stem>.<sha256-prefix><ext>" so the
// original filename Steam stores encodes the content. Existing previews are matched against the
// manifest in order; the longest matching prefix is kept, everything after it is removed and
// re-added. An unchanged manifest therefore submits nothing.
//
// Description: compared with the live one after normalizing line endings and trailing whitespace,
// and set only when it differs. Before anything is sent (dry run included) the file is checked:
// not empty, within Steam's length cap, BBCode tags balanced (errors, nothing is submitted), and
// every [url] / [img] link is fetched (warnings: a dead link is reported, never blocks a sync).

using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Steamworks;

namespace WorkshopMarketing;

internal static class Program
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif"];
    private static readonly TimeSpan CallTimeout = TimeSpan.FromMinutes(10);
    private const long MaxPreviewBytes = 1024 * 1024;
    private const string PulledDescriptionFile = "description.bbcode.txt";

    // Steam BBCode tags that take a closing tag. [hr] is written [hr][/hr] on Steam.
    private static readonly HashSet<string> PairedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "h1", "h2", "h3", "b", "u", "i", "strike", "spoiler", "noparse", "url", "list", "olist",
        "quote", "code", "table", "tr", "th", "td", "img", "previewyoutube", "hr",
    };

    // Tags with no closing tag.
    private static readonly HashSet<string> VoidTags = ["*"];

    // Inside these, other tags are shown as text.
    private static readonly HashSet<string> RawTags = new(StringComparer.OrdinalIgnoreCase) { "noparse", "code" };

    private sealed record Desired(string Kind, string Key, string Label, string? UploadPath);

    private sealed record Manifest(List<Desired> Previews, string? Description, string? DescriptionPath, bool AllowEmptyGallery = false);

    private sealed record LiveItem(List<Existing> Gallery, string Description);

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
        // The description holds non-ASCII text (language names); print it as such.
        Console.OutputEncoding = Encoding.UTF8;

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

        Manifest manifest = new([], null, null);
        try
        {
            if (manifestPath is not null) manifest = LoadManifest(manifestPath);
        }
        catch (Exception e)
        {
            Error($"manifest: {e.Message}");
            return 1;
        }
        if (manifestPath is not null && manifest.Previews.Count == 0 && !allowEmpty && !manifest.AllowEmptyGallery)
        {
            Error("the manifest lists no previews; refusing to clear the item's gallery (pass --allow-empty, or set \"allowEmptyGallery\": true in the manifest, to do that on purpose).");
            return 1;
        }
        if (manifest.Description is { } description)
        {
            List<string> errors = CheckDescription(description);
            if (errors.Count > 0)
            {
                foreach (string e in errors) Error($"description ({manifest.DescriptionPath}): {e}");
                Error("description has errors; nothing submitted.");
                return 1;
            }
            CheckLinks(description);
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
                : Sync(new PublishedFileId_t(itemId), manifest, dryRun);
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

    private static int Sync(PublishedFileId_t item, Manifest manifest, bool dryRun)
    {
        AppId_t appId = SteamUtils.GetAppID();
        Step($"Logged in as '{SteamFriends.GetPersonaName()}' (app {appId.m_AppId}); item {item.m_PublishedFileId}.");

        List<Desired> desired = manifest.Previews;
        LiveItem live = QueryItem(item, appId);
        List<Existing> existing = live.Gallery;
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

        bool setDescription = false;
        if (manifest.Description is null)
        {
            Step("Description: not managed (the manifest has no \"description\").");
        }
        else if (NormalizeDescription(live.Description) == manifest.Description)
        {
            Step($"Description: matches {manifest.DescriptionPath}.");
        }
        else
        {
            setDescription = true;
            Step($"Description: differs from {manifest.DescriptionPath} (- live, + file):");
            PrintLineDiff(NormalizeDescription(live.Description), manifest.Description);
        }

        bool galleryChanges = removeCount > 0 || toAdd.Count > 0;
        if (!galleryChanges && !setDescription)
        {
            Step("Workshop page already matches the manifest. Nothing to do.");
            return 0;
        }

        if (galleryChanges)
        {
            Step($"Gallery plan: keep {keep}, remove {removeCount}, add {toAdd.Count}.");
            for (int i = existing.Count - 1; i >= keep; i--) Detail($"- remove [{i}] {existing[i].Label}");
            foreach (Desired d in toAdd) Detail($"+ add    {d.Label}");
        }
        else
        {
            Step("Gallery already matches the manifest.");
        }
        if (setDescription) Step("Description plan: replace the live description with the file.");

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
        if (setDescription && !SteamUGC.SetItemDescription(update, manifest.Description!))
        {
            throw new Exception("SetItemDescription was rejected.");
        }

        Step("Submitting Workshop update...");
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

        // Re-read the live item so a run always ends on what Steam actually stored.
        LiveItem after = QueryItem(item, appId);
        Step($"Gallery after update ({after.Gallery.Count}):");
        PrintList(after.Gallery.Select(e => e.Label));
        bool matches = after.Gallery.Count == desired.Count
                       && after.Gallery.Zip(desired).All(p => string.Equals(p.First.Key, p.Second.Key, StringComparison.OrdinalIgnoreCase));
        if (!matches)
        {
            Warn("The live gallery does not match the manifest key-for-key; the next run will re-upload the mismatched tail.");
        }
        if (manifest.Description is not null)
        {
            if (NormalizeDescription(after.Description) == manifest.Description)
            {
                Step("Description after update: matches the file.");
            }
            else
            {
                Warn("The live description does not match the file after the update (- live, + file):");
                PrintLineDiff(NormalizeDescription(after.Description), manifest.Description);
            }
        }
        Step("Done.");
        return 0;
    }

    private static int Pull(PublishedFileId_t item, string dir)
    {
        AppId_t appId = SteamUtils.GetAppID();
        LiveItem live = QueryItem(item, appId);
        Directory.CreateDirectory(dir);
        using var http = new HttpClient();
        var entries = new List<Dictionary<string, string>>();
        foreach (Existing e in live.Gallery)
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
        File.WriteAllText(Path.Combine(dir, PulledDescriptionFile), NormalizeDescription(live.Description) + "\n", new UTF8Encoding(false));
        Detail($"description -> {PulledDescriptionFile}");
        string json = JsonSerializer.Serialize(
            new { description = PulledDescriptionFile, previews = entries },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(dir, "pulled.json"), json + Environment.NewLine);
        Step($"Pulled {entries.Count} gallery entr{(entries.Count == 1 ? "y" : "ies")} and the description into {dir}.");
        return 0;
    }

    private static LiveItem QueryItem(PublishedFileId_t item, AppId_t appId)
    {
        UGCQueryHandle_t query = SteamUGC.CreateQueryUGCDetailsRequest([item], 1);
        if (query == UGCQueryHandle_t.Invalid) throw new Exception("CreateQueryUGCDetailsRequest returned an invalid handle.");
        try
        {
            SteamUGC.SetReturnAdditionalPreviews(query, true);
            // Without this the details carry a shortened description.
            SteamUGC.SetReturnLongDescription(query, true);
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
            return new LiveItem(list, details.m_rgchDescription ?? "");
        }
        finally
        {
            SteamUGC.ReleaseQueryUGCRequest(query);
        }
    }

    // Manifest: { "description": "<file>", "previews": [ { "image": "file.png" } | { "youtube": "<url or id>" }, ... ] }
    // "description" is optional (absent = the live description is left alone). An empty "previews"
    // list is refused unless "allowEmptyGallery": true (a description-only item). Extra fields (e.g.
    // "note") are ignored. File paths are relative to the manifest's folder.
    private static Manifest LoadManifest(string path)
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

        string? description = null, descriptionPath = null;
        if (doc.RootElement.TryGetProperty("description", out JsonElement descEntry))
        {
            if (descEntry.ValueKind != JsonValueKind.String) throw new Exception("\"description\" must be a file path.");
            descriptionPath = Path.GetFullPath(Path.Combine(dir, descEntry.GetString() ?? ""));
            if (!File.Exists(descriptionPath)) throw new Exception($"description file not found: {descriptionPath}");
            description = NormalizeDescription(File.ReadAllText(descriptionPath, Encoding.UTF8));
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
                // Steam documents a 1 MB cap on preview images; over it, SubmitItemUpdate fails with k_EResultLimitExceeded.
                long bytes = new FileInfo(src).Length;
                if (bytes > MaxPreviewBytes) Warn($"{where}: {rel} is {bytes / 1048576.0:F2} MB; Steam may reject previews over 1 MB (k_EResultLimitExceeded). Save it as .jpg.");

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
        bool allowEmptyGallery = doc.RootElement.TryGetProperty("allowEmptyGallery", out JsonElement allowEntry)
            && allowEntry.ValueKind == JsonValueKind.True;
        return new Manifest(list, description, descriptionPath, allowEmptyGallery);
    }

    // LF line endings, no trailing whitespace on any line, no trailing blank lines. Both sides of the
    // comparison go through this, so CRLF from the web editor never reads as a change.
    private static string NormalizeDescription(string text)
    {
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        return string.Join("\n", lines.Select(l => l.TrimEnd())).TrimEnd('\n').TrimStart('﻿');
    }

    // Problems that would break the page or be refused by Steam. Each one blocks the run.
    private static List<string> CheckDescription(string text)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            errors.Add("the file is empty; refusing to blank the Workshop description.");
            return errors;
        }
        int bytes = Encoding.UTF8.GetByteCount(text);
        if (bytes >= Constants.k_cchPublishedDocumentDescriptionMax)
        {
            errors.Add($"{bytes} bytes (UTF-8); Steam's limit is {Constants.k_cchPublishedDocumentDescriptionMax - 1}.");
        }

        var open = new Stack<(string Name, int Line)>();
        foreach (Match m in Regex.Matches(text, @"\[(/?)([A-Za-z0-9*]+)(=[^\]\n]*)?\]"))
        {
            bool closing = m.Groups[1].Length > 0;
            string name = m.Groups[2].Value.ToLowerInvariant();
            int line = LineOf(text, m.Index);

            // Inside [noparse] / [code], only the matching close tag means anything.
            if (open.Count > 0 && RawTags.Contains(open.Peek().Name))
            {
                if (closing && name == open.Peek().Name) open.Pop();
                continue;
            }
            if (VoidTags.Contains(name)) continue;
            if (!PairedTags.Contains(name))
            {
                Warn($"description line {line}: unknown tag {m.Value} (Steam shows it as text).");
                continue;
            }
            if (!closing)
            {
                open.Push((name, line));
                continue;
            }
            if (open.Count == 0)
            {
                errors.Add($"line {line}: {m.Value} closes nothing.");
                return errors;
            }
            if (open.Peek().Name != name)
            {
                errors.Add($"line {line}: {m.Value}, but [{open.Peek().Name}] from line {open.Peek().Line} is still open.");
                return errors;
            }
            open.Pop();
        }
        foreach ((string name, int line) in open)
        {
            errors.Add($"line {line}: [{name}] is never closed.");
        }

        foreach ((string url, int line, _) in ExtractLinks(text))
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add($"line {line}: '{url}' is not an absolute http(s) link.");
            }
        }
        return errors;
    }

    // Every [url=X], [url]X[/url] and [img]X[/img] target, with its line and whether it is an image.
    private static List<(string Url, int Line, bool IsImage)> ExtractLinks(string text)
    {
        var links = new List<(string, int, bool)>();
        foreach (Match m in Regex.Matches(text, @"\[url=([^\]\n]+)\]", RegexOptions.IgnoreCase))
            links.Add((m.Groups[1].Value.Trim().Trim('"'), LineOf(text, m.Index), false));
        foreach (Match m in Regex.Matches(text, @"\[url\]([^\[\n]+)\[/url\]", RegexOptions.IgnoreCase))
            links.Add((m.Groups[1].Value.Trim(), LineOf(text, m.Index), false));
        foreach (Match m in Regex.Matches(text, @"\[img\]([^\[\n]+)\[/img\]", RegexOptions.IgnoreCase))
            links.Add((m.Groups[1].Value.Trim(), LineOf(text, m.Index), true));
        return links;
    }

    // Fetches every link once. Report only: a site that refuses bots or is briefly down must not
    // block a release, so nothing here fails the run.
    private static void CheckLinks(string text)
    {
        var byUrl = ExtractLinks(text)
            .GroupBy(l => l.Url)
            .Select(g => (Url: g.Key, Lines: string.Join(",", g.Select(l => l.Line).Distinct()), IsImage: g.Any(l => l.IsImage)))
            .ToList();
        if (byUrl.Count == 0) return;
        Step($"Checking {byUrl.Count} description link(s)...");

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; WorkshopMarketing link check)");
        string?[] problems = Task.WhenAll(byUrl.Select(l => CheckLink(http, l.Url, l.IsImage))).GetAwaiter().GetResult();

        int ok = 0;
        for (int i = 0; i < byUrl.Count; i++)
        {
            if (problems[i] is null) ok++;
            else Warn($"description line {byUrl[i].Lines}: {byUrl[i].Url} -- {problems[i]}");
        }
        Detail($"{ok}/{byUrl.Count} link(s) OK.");
    }

    // Null when the link works; otherwise what is wrong with it.
    private static async Task<string?> CheckLink(HttpClient http, string url, bool isImage)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _)) return "not a valid URL";
        try
        {
            using HttpResponseMessage response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            int code = (int)response.StatusCode;
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone) return $"DEAD (HTTP {code})";
            if (!response.IsSuccessStatusCode) return $"could not verify (HTTP {code}; the site may refuse automated requests)";
            string? type = response.Content.Headers.ContentType?.MediaType;
            if (isImage && type is not null && !type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return $"[img] target is '{type}', not an image";
            }
            return null;
        }
        catch (TaskCanceledException)
        {
            return "could not verify (timed out)";
        }
        catch (HttpRequestException e)
        {
            return $"UNREACHABLE ({e.Message})";
        }
    }

    private static int LineOf(string text, int index)
    {
        int line = 1;
        for (int i = 0; i < index; i++)
        {
            if (text[i] == '\n') line++;
        }
        return line;
    }

    // Changed lines only (LCS over lines), numbered by their line in each side.
    private static void PrintLineDiff(string before, string after)
    {
        string[] a = before.Split('\n'), b = after.Split('\n');
        int[,] lcs = new int[a.Length + 1, b.Length + 1];
        for (int i = a.Length - 1; i >= 0; i--)
        {
            for (int j = b.Length - 1; j >= 0; j--)
            {
                lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }
        int x = 0, y = 0;
        while (x < a.Length || y < b.Length)
        {
            if (x < a.Length && y < b.Length && a[x] == b[y]) { x++; y++; }
            else if (x < a.Length && (y == b.Length || lcs[x + 1, y] >= lcs[x, y + 1])) { Detail($"- {x + 1,3}: {a[x]}"); x++; }
            else { Detail($"+ {y + 1,3}: {b[y]}"); y++; }
        }
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
