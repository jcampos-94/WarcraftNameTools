using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WarcraftNameTools.Models;
using WarcraftNameTools.Services;

namespace WarcraftNameTools.Pages;

public class IndexModel : PageModel
{
  private readonly CharacterRepository _repository;

  public List<CharacterRecord> Characters { get; private set; } = new();

  public List<string> Races { get; private set; } = new();

  public List<string> AvailableRaces { get; } =
    RaceCatalog.AvailableRaces;

  public string CharactersJson { get; private set; } = "[]";

  public string ExportJson { get; private set; } = "[]";

  public IndexModel(CharacterRepository repository)
  {
    _repository = repository;
  }

  public void OnGet(
    bool sync,
    string? targetRace,
    int categoryPages,
    int pagesRetrieved,
    int totalRecords,
    int newCharacters,
    string? changedIds)
  {
    Sync = sync;
    SyncTargetRace = targetRace;
    SyncCategoryPages = categoryPages;
    SyncPagesRetrieved = pagesRetrieved;
    SyncTotalRecords = totalRecords;
    SyncNewCharacters = newCharacters;
    SyncChangedIds = changedIds;

    Characters = _repository.Load();

    Races = Characters
        .SelectMany(character => character.Races)
        .Where(race => !string.IsNullOrWhiteSpace(race))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(race => race)
        .ToList();

    CharactersJson =
      JsonSerializer.Serialize(
        Characters,
        new JsonSerializerOptions
        {
          PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase
        }
    );

    ExportJson =
      JsonSerializer.Serialize(
        Characters,
        new JsonSerializerOptions
        {
          WriteIndented = true
        }
    );
  }

  [BindProperty]
  public IFormFile? ImportFile { get; set; }

  public IActionResult OnPost()
  {
    if (ImportFile == null || ImportFile.Length == 0)
    {
      return Page();
    }

    try
    {
      using Stream stream = ImportFile.OpenReadStream();

      List<CharacterRecord>? characters =
          JsonSerializer.Deserialize<List<CharacterRecord>>(
              stream
          );

      if (characters == null)
      {
        return Page();
      }

      _repository.Save(characters);

      return RedirectToPage();
    }
    catch (JsonException)
    {
      return Page();
    }
  }

  [BindProperty(SupportsGet = true)]
  public bool Sync { get; set; }

  [BindProperty(SupportsGet = true)]
  public string? SyncTargetRace { get; set; }

  [BindProperty(SupportsGet = true)]
  public int SyncCategoryPages { get; set; }

  [BindProperty(SupportsGet = true)]
  public int SyncPagesRetrieved { get; set; }

  [BindProperty(SupportsGet = true)]
  public int SyncTotalRecords { get; set; }

  [BindProperty(SupportsGet = true)]
  public int SyncNewCharacters { get; set; }

  [BindProperty(SupportsGet = true)]
  public string? SyncChangedIds { get; set; }
}