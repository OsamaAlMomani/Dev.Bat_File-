using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AiSetupUi;

internal sealed record Headline(string Title, string Url, string DiscussionUrl, int Points, int Comments, DateTimeOffset? Posted);

internal sealed record RepoInfo(
    string FullName, string Name, string Url, string Description,
    int Stars, DateTimeOffset? Created, DateTimeOffset? Pushed, string Language)
{
    /// <summary>Set when a PyPI project with this name links back to the repo, so "pip install" is safe to suggest.</summary>
    public string? PipName { get; init; }
    public string? PipVersion { get; init; }
    public bool InCatalog { get; init; }
}

internal sealed record FetchResult<T>(List<T> Items, List<string> Warnings);

/// <summary>
/// Finds what's new in AI engineering: fresh open-source repos (GitHub), community headlines (Hacker News),
/// and whether a repo is already installable from PyPI. No UI code in here.
/// Set a GITHUB_TOKEN environment variable to raise GitHub's anonymous search limit.
/// </summary>
internal static class NewsService
{
    private static readonly HttpClient Http = CreateClient();

    private static readonly string[] HeadlineTopics = { "LLM", "AI agents", "RAG", "MCP server", "machine learning", "open source AI" };
    private static readonly string[] RepoTopics = { "llm", "ai-agents", "rag", "mcp", "machine-learning" };

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AI-Setup-UI/1.1");
        return http;
    }

    // ============================================================
    // Hacker News headlines
    // ============================================================

    public static async Task<FetchResult<Headline>> GetHeadlinesAsync(string? query, int days, CancellationToken ct)
    {
        var warnings = new List<string>();
        var custom = !string.IsNullOrWhiteSpace(query);
        var topics = custom ? new[] { query!.Trim() } : HeadlineTopics;
        var since = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();
        var minPoints = custom ? 3 : 20;

        var tasks = topics.Select(async topic =>
        {
            try
            {
                var filters = Uri.EscapeDataString($"created_at_i>{since},points>{minPoints}");
                var url = "https://hn.algolia.com/api/v1/search?tags=story&hitsPerPage=20" +
                          $"&query={Uri.EscapeDataString(topic)}&numericFilters={filters}";
                return ParseHeadlines(await GetAsync(url, "application/json", ct));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lock (warnings) warnings.Add($"Hacker News: {ex.Message}");
                return new List<Headline>();
            }
        }).ToList();

        var all = (await Task.WhenAll(tasks)).SelectMany(x => x);
        var items = all.GroupBy(h => h.DiscussionUrl).Select(g => g.First())
                       .OrderByDescending(h => h.Points).Take(40).ToList();
        return new FetchResult<Headline>(items, warnings.Distinct().ToList());
    }

    private static List<Headline> ParseHeadlines(string json)
    {
        var list = new List<Headline>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("hits", out var hits)) return list;

        foreach (var h in hits.EnumerateArray())
        {
            var title = Str(h, "title");
            var id = Str(h, "objectID");
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(id)) continue;

            var discussion = $"https://news.ycombinator.com/item?id={id}";
            var url = Str(h, "url");
            DateTimeOffset? posted = h.TryGetProperty("created_at_i", out var c) && c.ValueKind == JsonValueKind.Number
                ? DateTimeOffset.FromUnixTimeSeconds(c.GetInt64()) : null;

            list.Add(new Headline(title, string.IsNullOrWhiteSpace(url) ? discussion : url, discussion,
                Int(h, "points"), Int(h, "num_comments"), posted));
        }
        return list;
    }

    // ============================================================
    // New open-source libraries (GitHub + PyPI check)
    // ============================================================

    public static async Task<FetchResult<RepoInfo>> GetNewLibrariesAsync(
        string? query, bool pythonOnly, int days, CancellationToken ct)
    {
        var warnings = new List<string>();
        var repos = new Dictionary<string, RepoInfo>(StringComparer.OrdinalIgnoreCase);

        var lang = pythonOnly ? " language:python" : "";
        var newSince = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
        var risingSince = DateTime.UtcNow.AddDays(-180).ToString("yyyy-MM-dd");
        var minNewStars = days <= 7 ? 10 : days <= 30 ? 30 : 100;

        var queries = new List<string>();
        if (string.IsNullOrWhiteSpace(query))
        {
            // "new": created inside the chosen window. "rising": younger than 6 months but already popular.
            foreach (var t in RepoTopics) queries.Add($"topic:{t} created:>{newSince} stars:>{minNewStars}{lang}");
            foreach (var t in RepoTopics.Take(3)) queries.Add($"topic:{t} created:>{risingSince} stars:>500{lang}");
        }
        else
        {
            var q = query.Trim();
            queries.Add($"{q} in:name,description,topics created:>{newSince} stars:>5{lang}");
            queries.Add($"{q} in:name,description,topics created:>{risingSince} stars:>200{lang}");
        }

        foreach (var q in queries)
        {
            try
            {
                var url = "https://api.github.com/search/repositories?sort=stars&order=desc&per_page=30" +
                          $"&q={Uri.EscapeDataString(q)}";
                var json = await GetAsync(url, "application/vnd.github+json", ct);
                foreach (var r in ParseRepos(json)) repos.TryAdd(r.FullName, r);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
            {
                warnings.Add("GitHub rate limit reached - showing partial results. " +
                             "Set a GITHUB_TOKEN environment variable for a higher limit.");
                break;
            }
            catch (Exception ex)
            {
                warnings.Add($"GitHub: {ex.Message}");
            }
        }

        var top = repos.Values.OrderByDescending(r => r.Stars).Take(40).ToList();
        using var gate = new SemaphoreSlim(6);
        var enriched = await Task.WhenAll(top.Select(r => EnrichAsync(r, gate, ct)));
        return new FetchResult<RepoInfo>(enriched.ToList(), warnings.Distinct().ToList());
    }

    private static List<RepoInfo> ParseRepos(string json)
    {
        var list = new List<RepoInfo>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items)) return list;

        foreach (var it in items.EnumerateArray())
        {
            var full = Str(it, "full_name");
            var name = Str(it, "name");
            var url = Str(it, "html_url");
            if (full == null || name == null || url == null) continue;

            list.Add(new RepoInfo(full, name, url, Str(it, "description") ?? "", Int(it, "stargazers_count"),
                Date(it, "created_at"), Date(it, "pushed_at"), Str(it, "language") ?? ""));
        }
        return list;
    }

    /// <summary>Flags repos already in the catalog and, for Python repos, finds the matching PyPI package.</summary>
    private static async Task<RepoInfo> EnrichAsync(RepoInfo repo, SemaphoreSlim gate, CancellationToken ct)
    {
        var inCatalog = Catalog.PipNames.Contains(PipName.Normalize(repo.Name));
        if (!string.Equals(repo.Language, "Python", StringComparison.OrdinalIgnoreCase))
            return repo with { InCatalog = inCatalog };

        await gate.WaitAsync(ct);
        try
        {
            var json = await GetAsync($"https://pypi.org/pypi/{Uri.EscapeDataString(repo.Name)}/json", "application/json", ct);
            using var doc = JsonDocument.Parse(json);
            var info = doc.RootElement.GetProperty("info");

            // A project with the same name isn't enough - it has to link back to this repo.
            if (PointsToRepo(info, repo.FullName))
            {
                var pipName = Str(info, "name") ?? repo.Name;
                var catalogHit = inCatalog || Catalog.PipNames.Contains(PipName.Normalize(pipName));
                return repo with { PipName = pipName, PipVersion = Str(info, "version"), InCatalog = catalogHit };
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // not on PyPI under that name, or PyPI unreachable - just show the repo without a pip hint
        }
        finally
        {
            gate.Release();
        }
        return repo with { InCatalog = inCatalog };
    }

    private static bool PointsToRepo(JsonElement info, string fullName)
    {
        var needle = "github.com/" + fullName;
        bool Has(string? s) => s != null && s.Contains(needle, StringComparison.OrdinalIgnoreCase);

        if (Has(Str(info, "home_page")) || Has(Str(info, "project_url"))) return true;
        if (info.TryGetProperty("project_urls", out var urls) && urls.ValueKind == JsonValueKind.Object)
            foreach (var p in urls.EnumerateObject())
                if (p.Value.ValueKind == JsonValueKind.String && Has(p.Value.GetString())) return true;
        return false;
    }

    // ============================================================
    // Outdated catalog packages
    // ============================================================

    /// <summary>Catalog packages that have a newer release on PyPI. Null when pip can't be queried.</summary>
    public static async Task<List<PipOutdated>?> GetOutdatedCatalogAsync(CancellationToken ct)
    {
        var outdated = await PipService.ListOutdatedAsync(ct);
        return outdated?
            .Where(p => Catalog.PipNames.Contains(PipName.Normalize(p.Name)))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ============================================================
    // helpers
    // ============================================================

    private static async Task<string> GetAsync(string url, string accept, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.ParseAdd(accept);

        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrWhiteSpace(token) && url.StartsWith("https://api.github.com/", StringComparison.Ordinal))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await Http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)resp.StatusCode} from {new Uri(url).Host}", null, resp.StatusCode);
        return await resp.Content.ReadAsStringAsync(ct);
    }

    private static string? Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int Int(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : 0;

    private static DateTimeOffset? Date(JsonElement el, string name) =>
        DateTimeOffset.TryParse(Str(el, name), out var d) ? d : null;
}
