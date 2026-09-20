using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using WarcraftNameTools.Models;
using WarcraftNameTools.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets(
    "bb30e19a-1f37-45bc-a0c0-2fe8199ed174"
);

string mongoConnectionString =
    builder.Configuration["MongoDb:ConnectionString"]
    ?? throw new InvalidOperationException(
        "MongoDB connection string is not configured."
    );

string mongoDatabaseName =
    builder.Configuration["MongoDb:DatabaseName"]
    ?? throw new InvalidOperationException(
        "MongoDB database name is not configured."
    );

MongoClient mongoClient =
    new(mongoConnectionString);

IMongoDatabase mongoDatabase =
    mongoClient.GetDatabase(mongoDatabaseName);

builder.Services.AddSingleton<CharacterRepository>(
    new CharacterRepository(mongoDatabase)
);

mongoDatabase.RunCommand<BsonDocument>(
    new BsonDocument("ping", 1)
);

builder.Services.AddRazorPages();

WebApplication app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

CharacterRepository repository =
    new(mongoDatabase);

WarcraftWikiApi api = new();

CharacterImporter importer =
    new(api, repository);

app.MapPost("/sync", async ([FromForm] string targetRace) =>
{
    if (!RaceCatalog.AvailableRaces.Contains(
            targetRace,
            StringComparer.OrdinalIgnoreCase))
    {
        return Results.BadRequest(
            "The selected race is not supported."
        );
    }

    ImportResult result =
        await importer.Import(targetRace);

    string changedIds =
        string.Join(
            ",",
            result.ChangedSourceRaces
        );

    return Results.Redirect(
        $"/?sync=true" +
        $"&targetRace={Uri.EscapeDataString(result.TargetRace)}" +
        $"&categoryPages={result.CategoryPages}" +
        $"&pagesRetrieved={result.PagesRetrieved}" +
        $"&totalRecords={result.TotalRecords}" +
        $"&newCharacters={result.NewCharacters}" +
        $"&changedIds={Uri.EscapeDataString(changedIds)}"
    );
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