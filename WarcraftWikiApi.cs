using System.Net.Http;
using System.Text.Json;

public class WarcraftWikiApi
{
  private const string ApiUrl = "https://warcraft.wiki.gg/api.php";

  private readonly HttpClient _client;

  public WarcraftWikiApi()
  {
    _client = new HttpClient();

    _client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "WoWNameCollector/1.0 (personal project)"
    );
  }

  public async Task<(string Json, string RequestUrl)> GetPageWikitext(string pageName)
  {
    var parameters = new Dictionary<string, string>
    {
      ["action"] = "parse",
      ["page"] = pageName,
      ["prop"] = "wikitext",
      ["format"] = "json"
    };

    string query = string.Join(
        "&",
        parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}")
    );

    string requestUrl = $"{ApiUrl}?{query}";

    HttpResponseMessage response = await _client.GetAsync(requestUrl);

    Console.WriteLine($"Status: {(int)response.StatusCode}");

    response.EnsureSuccessStatusCode();

    string json = await response.Content.ReadAsStringAsync();

    return (json, requestUrl);
  }

  public async Task<Dictionary<string, string>> GetWikitextBatch(
    List<string> pageNames)
  {
    const int batchSize = 50;

    Dictionary<string, string> results = new();

    for (int i = 0; i < pageNames.Count; i += batchSize)
    {
      List<string> batch = pageNames
          .Skip(i)
          .Take(batchSize)
          .ToList();

      var parameters = new Dictionary<string, string>
      {
        ["action"] = "query",
        ["prop"] = "revisions",
        ["titles"] = string.Join("|", batch),
        ["rvprop"] = "content",
        ["rvslots"] = "main",
        ["format"] = "json",
        ["formatversion"] = "2"
      };

      string query = string.Join(
          "&",
          parameters.Select(parameter =>
              $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}")
      );

      string requestUrl = $"{ApiUrl}?{query}";

      HttpResponseMessage response = await _client.GetAsync(requestUrl);

      Console.WriteLine(
          $"Batch {i + 1}-{Math.Min(i + batchSize, pageNames.Count)}: " +
          $"Status {(int)response.StatusCode}"
      );

      response.EnsureSuccessStatusCode();

      string json = await response.Content.ReadAsStringAsync();

      using JsonDocument document = JsonDocument.Parse(json);

      JsonElement pages = document.RootElement
          .GetProperty("query")
          .GetProperty("pages");

      foreach (JsonElement page in pages.EnumerateArray())
      {
        if (!page.TryGetProperty("revisions", out JsonElement revisions))
        {
          continue;
        }

        if (revisions.GetArrayLength() == 0)
        {
          continue;
        }

        JsonElement revision = revisions[0];

        if (!revision.TryGetProperty("slots", out JsonElement slots))
        {
          continue;
        }

        JsonElement main = slots.GetProperty("main");

        if (!main.TryGetProperty("content", out JsonElement content))
        {
          continue;
        }

        string title = page.GetProperty("title").GetString()!;
        string wikitext = content.GetString()!;

        results[title] = wikitext;
      }

      if (i + batchSize < pageNames.Count)
      {
        await Task.Delay(1000);
      }
    }

    return results;
  }

  public async Task<List<string>> GetCategoryMembers(string categoryName)
  {
    List<string> pageNames = new();

    string? continueToken = null;

    do
    {
      var parameters = new Dictionary<string, string>
      {
        ["action"] = "query",
        ["list"] = "categorymembers",
        ["cmtitle"] = categoryName,
        ["cmnamespace"] = "0",
        ["cmlimit"] = "500",
        ["format"] = "json"
      };

      if (continueToken != null)
      {
        parameters["cmcontinue"] = continueToken;
      }

      string query = string.Join(
          "&",
          parameters.Select(parameter =>
              $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}")
      );

      string requestUrl = $"{ApiUrl}?{query}";

      HttpResponseMessage response = await _client.GetAsync(requestUrl);

      Console.WriteLine($"Status: {(int)response.StatusCode}");

      response.EnsureSuccessStatusCode();

      string json = await response.Content.ReadAsStringAsync();

      using JsonDocument document = JsonDocument.Parse(json);

      JsonElement categoryMembers = document.RootElement
          .GetProperty("query")
          .GetProperty("categorymembers");

      foreach (JsonElement member in categoryMembers.EnumerateArray())
      {
        string title = member.GetProperty("title").GetString()!;

        pageNames.Add(title);
      }

      if (document.RootElement.TryGetProperty("continue", out JsonElement continuation))
      {
        continueToken = continuation
            .GetProperty("cmcontinue")
            .GetString();
      }
      else
      {
        continueToken = null;
      }

    } while (continueToken != null);

    return pageNames;
  }
}