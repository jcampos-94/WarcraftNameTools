using System.Text.RegularExpressions;
using WarcraftNameTools.Models;

namespace WarcraftNameTools.Services;

public class CharacterImporter
{
  private readonly WarcraftWikiApi _api;
  private readonly CharacterRepository _repository;

  public CharacterImporter(
      WarcraftWikiApi api,
      CharacterRepository repository)
  {
    _api = api;
    _repository = repository;
  }

  public async Task<ImportResult> Import(string targetRace)
  {
    List<CharacterRecord> results =
        _repository.Load();

    List<string> pageNames =
        await _api.GetCategoryMembers(
            $"Category:{targetRace} characters"
        );

    Dictionary<string, string> pages =
        await _api.GetWikitextBatch(pageNames);

    int nextId =
        _repository.GetNextId(results);

    int newCharacters = 0;

    List<int> changedSourceRaces = new();

    foreach (KeyValuePair<string, string> page in pages)
    {
      string sourceName = page.Key;
      string wikitext = page.Value;

      // Ignore names that start with a number.
      if (string.IsNullOrWhiteSpace(sourceName) ||
          char.IsDigit(sourceName[0]))
      {
        continue;
      }

      Match genderMatch = Regex.Match(
          wikitext,
          @"^\s*\|\s*gender\s*=\s*(.+)$",
          RegexOptions.Multiline |
          RegexOptions.IgnoreCase
      );

      if (!genderMatch.Success)
      {
        continue;
      }

      string gender =
          genderMatch.Groups[1].Value.Trim();

      if (gender != "Male" &&
          gender != "Female")
      {
        continue;
      }

      string sourceRaces;

      Match raceMatch = Regex.Match(
          wikitext,
          @"^\s*\|\s*race\s*=\s*(.+)$",
          RegexOptions.Multiline |
          RegexOptions.IgnoreCase
      );

      if (raceMatch.Success)
      {
        sourceRaces =
            raceMatch.Groups[1].Value.Trim();

        if (!sourceRaces.Equals(
                targetRace,
                StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }
      }
      else
      {
        Match racesMatch = Regex.Match(
            wikitext,
            @"^\s*\|\s*races\s*=\s*(.+)$",
            RegexOptions.Multiline |
            RegexOptions.IgnoreCase
        );

        if (!racesMatch.Success)
        {
          continue;
        }

        sourceRaces =
            racesMatch.Groups[1].Value.Trim();

        if (!sourceRaces.Contains(
                "formerly",
                StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }
      }

      CharacterRecord? existing =
          results.FirstOrDefault(
              character =>
                  character.SourceName.Equals(
                      sourceName,
                      StringComparison.OrdinalIgnoreCase
                  )
          );

      if (existing != null)
      {
        if (!existing.SourceRaces.Equals(
                sourceRaces,
                StringComparison.Ordinal))
        {
          existing.SourceRaces =
              sourceRaces;

          changedSourceRaces.Add(
              existing.Id
          );
        }

        continue;
      }

      results.Add(new CharacterRecord
      {
        Id = nextId++,
        SourceName = sourceName,
        SourceRaces = sourceRaces,
        Name = sourceName,
        FamilyName = "",
        Gender = gender,
        Races = [""]
      });

      newCharacters++;
    }

    _repository.SortById(results);

    _repository.Save(results);

    return new ImportResult
    {
      TargetRace = targetRace,
      CategoryPages = pageNames.Count,
      PagesRetrieved = pages.Count,
      TotalRecords = results.Count,
      NewCharacters = newCharacters,
      ChangedSourceRaces = changedSourceRaces
    };
  }
}

public class ImportResult
{
  public string TargetRace { get; set; } = "";

  public int CategoryPages { get; set; }

  public int PagesRetrieved { get; set; }

  public int TotalRecords { get; set; }

  public int NewCharacters { get; set; }

  public List<int> ChangedSourceRaces { get; set; } = new();
}