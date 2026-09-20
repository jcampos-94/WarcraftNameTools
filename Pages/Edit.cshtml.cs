using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WarcraftNameTools.Models;
using WarcraftNameTools.Services;

namespace WarcraftNameTools.Pages;

public class EditModel : PageModel
{
  private readonly CharacterRepository _repository;

  public CharacterRecord Character { get; private set; } = new();

  [BindProperty]
  public string Name { get; set; } = "";

  [BindProperty]
  public string FamilyName { get; set; } = "";

  [BindProperty]
  public string RacesText { get; set; } = "";

  public string? Message { get; private set; }

  public string? ErrorMessage { get; private set; }

  public EditModel(CharacterRepository repository)
  {
    _repository = repository;
  }

  public IActionResult OnGet(int id)
  {
    return LoadCharacter(id);
  }

  public IActionResult OnPost(int id)
  {
    try
    {
      List<CharacterRecord> characters =
          _repository.Load();

      CharacterRecord? character =
          characters.FirstOrDefault(
              character => character.Id == id
          );

      if (character == null)
      {
        return NotFound();
      }

      if (string.IsNullOrWhiteSpace(RacesText))
      {
        Character = character;

        Name = Name ?? "";
        FamilyName = FamilyName ?? "";

        ErrorMessage = "At least one race is required.";

        return Page();
      }

      character.Name = (Name ?? "").Trim();
      character.FamilyName = (FamilyName ?? "").Trim();

      character.Races = RacesText
          .Split(
              ',',
              StringSplitOptions.RemoveEmptyEntries |
              StringSplitOptions.TrimEntries
          )
          .ToList();

      _repository.Save(characters);

      Character = character;

      Message = "Character saved successfully.";

      return Page();
    }
    catch (Exception)
    {
      ErrorMessage =
          "An error occurred while saving the character.";

      return LoadCharacter(id);
    }
  }

  private IActionResult LoadCharacter(int id)
  {
    List<CharacterRecord> characters =
        _repository.Load();

    CharacterRecord? character =
        characters.FirstOrDefault(
            character => character.Id == id
        );

    if (character == null)
    {
      return NotFound();
    }

    Character = character;

    Name = character.Name ?? "";
    FamilyName = character.FamilyName ?? "";

    RacesText =
        string.Join(
            ", ",
            character.Races
                .Where(race =>
                    !string.IsNullOrWhiteSpace(race))
        );

    return Page();
  }
}