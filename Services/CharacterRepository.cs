using MongoDB.Driver;
using WarcraftNameTools.Models;

namespace WarcraftNameTools.Services;

public class CharacterRepository
{
  private readonly IMongoCollection<CharacterRecord> _collection;

  public CharacterRepository(
      IMongoDatabase database)
  {
    _collection =
        database.GetCollection<CharacterRecord>(
            "characters"
        );
  }

  public List<CharacterRecord> Load()
  {
    return _collection
        .Find(_ => true)
        .SortBy(character => character.Id)
        .ToList();
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
    _collection.DeleteMany(_ => true);

    if (characters.Count == 0)
    {
      return;
    }

    _collection.InsertMany(characters);
  }
}