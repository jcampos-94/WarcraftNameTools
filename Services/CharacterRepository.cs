using System.Text.Json;
using WarcraftNameTools.Models;

namespace WarcraftNameTools.Services;

public class CharacterRepository
{
  private readonly string _fileName;

  public CharacterRepository(string fileName)
  {
    _fileName = fileName;
  }

  public List<CharacterRecord> Load()
  {
    if (!File.Exists(_fileName))
    {
      return new List<CharacterRecord>();
    }

    string json = File.ReadAllText(_fileName);

    return JsonSerializer.Deserialize<List<CharacterRecord>>(json)
        ?? new List<CharacterRecord>();
  }

  public int GetNextId(List<CharacterRecord> characters)
  {
    if (characters.Count == 0)
    {
      return 1;
    }

    return characters.Max(character => character.Id) + 1;
  }

  public void SortById(List<CharacterRecord> characters)
  {
    characters.Sort(
        (a, b) => a.Id.CompareTo(b.Id)
    );
  }

  public void Save(List<CharacterRecord> characters)
  {
    JsonSerializerOptions options = new()
    {
      WriteIndented = true
    };

    string json = JsonSerializer.Serialize(
        characters,
        options
    );

    File.WriteAllText(_fileName, json);
  }
}