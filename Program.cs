using Spectre.Console;
using System.Xml;


var filesPath = AnsiConsole.Ask<string>("Please Enter the Location of the sitemaps files: ");

if (!Directory.Exists(filesPath))
{
    AnsiConsole.MarkupLine("[red]Directory not found[/]");
    return;
}

var files = new Dictionary<string, XmlDocument>();
    
AnsiConsole.Status()
    .Spinner(Spinner.Known.Star)
    .SpinnerStyle(Style.Parse("green"))
    .Start("Loadging files...", ctx =>
    {
        files = LoadFiles(filesPath);
    });

if (!files.Any())
{
    AnsiConsole.MarkupLine("[red]No files found[/]");
    return;
}

foreach (var file in files)
{
    StreamWriter sw = new StreamWriter(filesPath + $@"\{file.Key}-failed.txt", true);

    if (!AnsiConsole.Confirm($"Start prossesing {file.Key}?"))
    {
        AnsiConsole.MarkupLine("Ok... :(");
        return;
    }


    var xmlns = new XmlNamespaceManager(file.Value.NameTable);
    xmlns.AddNamespace("default", "http://www.sitemaps.org/schemas/sitemap/0.9");
    var locs = file.Value.SelectNodes("//default:loc", xmlns)
        .OfType<XmlElement>()
        .Select(element => element.InnerText)
        .ToList();

    if (locs == null || !locs.Any())
    {
        AnsiConsole.MarkupLine("[red]No urls found[/]");
        continue;
    }

    var table = new Table()
        .AddColumn("Status code")
        .AddColumn("Status")
        .AddColumn("Page url");

    await AnsiConsole.Live(table)
        .StartAsync(async ctx =>
        {
            foreach (var loc in locs)
            {
                var result = await CheckPageHeaders(loc);
                table.AddRow($"{(int)result.StatusCode}", $"{(result.IsSuccessStatusCode ? "[green]":"[red]")}{result.StatusCode}[/]", loc);
                ctx.Refresh();

                if (!result.IsSuccessStatusCode)
                {
                    sw.WriteLine($"{(int)result.StatusCode} - {loc}");
                }
            }
        });
}


static async Task<HttpResponseMessage> CheckPageHeaders(string url)
{
    using var client = new HttpClient();
    return await client.GetAsync(url);
}

static Dictionary<string, XmlDocument> LoadFiles(string filesPath)
{
    string[] filePaths = Directory.GetFiles(filesPath);
    var docs = new Dictionary<string, XmlDocument>();

    foreach (string path in filePaths.Where(f => f.EndsWith(".xml")))
    {
        XmlDocument? xDoc = null;

        try
        {
            xDoc = new XmlDocument();
            xDoc.Load(path);
        }
        catch (XmlException ex)
        {
            AnsiConsole.MarkupLine("[red]Error loading file[/]", ex.Message);
        }

        if (xDoc != null)
            docs.Add(Path.GetFileName(path), xDoc);
    }

    return docs;

}