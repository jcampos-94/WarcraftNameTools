using WarcraftNameTools.Models;
using WarcraftNameTools.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddSingleton<CharacterRepository>(
    new CharacterRepository("character_names.json")
);

WebApplication app = builder.Build();
app.UseStaticFiles();

CharacterRepository repository =
    new("character_names.json");

WarcraftWikiApi api = new();

CharacterImporter importer =
    new(api, repository);

app.MapPost("/sync", async () =>
{
    await importer.Import("Night elf");

    return Results.Redirect("/");
});

app.MapPost("/ignore", (int id) =>
{
    List<CharacterRecord> characters =
        repository.Load();

    CharacterRecord? character =
        characters.FirstOrDefault(
            character => character.Id == id
        );

    if (character == null)
    {
        return Results.NotFound();
    }

    character.Ignore = true;

    repository.Save(characters);

    return Results.Redirect("/");
});

app.MapPost("/unignore", (int id) =>
{
    List<CharacterRecord> characters =
        repository.Load();

    CharacterRecord? character =
        characters.FirstOrDefault(
            character => character.Id == id
        );

    if (character == null)
    {
        return Results.NotFound();
    }

    character.Ignore = false;

    repository.Save(characters);

    return Results.Redirect("/");
});

app.MapPost("/import", async (IFormFile file) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest("No file was selected.");
    }

    if (!file.FileName.EndsWith(
            ".json",
            StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Only JSON files are allowed.");
    }

    try
    {
        using Stream stream = file.OpenReadStream();

        List<CharacterRecord>? characters =
            await System.Text.Json.JsonSerializer.DeserializeAsync<
                List<CharacterRecord>
            >(stream);

        if (characters == null)
        {
            return Results.BadRequest(
                "The JSON file does not contain a valid character database."
            );
        }

        repository.Save(characters);

        return Results.Redirect("/");
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.BadRequest(
            "The selected file is not valid character database JSON."
        );
    }
});

app.MapRazorPages();

app.Run();