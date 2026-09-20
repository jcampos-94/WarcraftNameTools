namespace WarcraftNameTools.Models;

public class CharacterRecord
{
  public int Id { get; set; }

  public string SourceName { get; set; } = "";

  public string SourceRaces { get; set; } = "";

  public string Name { get; set; } = "";

  public string FamilyName { get; set; } = "";

  public string Gender { get; set; } = "";

  public List<string> Races { get; set; } = [""];

  public bool Ignore { get; set; }
}