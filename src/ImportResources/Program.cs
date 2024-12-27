// See https://aka.ms/new-console-template for more information
using ImportResources;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.WriteLine("Hello, World!");

var httpClient = new HttpClient
{
	BaseAddress = new Uri("https://eternity2.azurewebsites.net"),
};

var response = await httpClient.GetAsync("pieces.html");

var htmlContent = await response.Content.ReadAsStringAsync();

var imageRegEx = new Regex(@"images\/[\d]+\.png");
//< td > acxx </ td >
var codeRegEx = new Regex(@"<td>([a-z]{4})");
var imageUrls = imageRegEx.Matches(htmlContent).Select(m => m.Groups[0].Value).ToArray();
var codeUrls = codeRegEx.Matches(htmlContent).Select(m => m.Groups[1].Value).ToArray();


//var tableEntries = matches.Select(ReadMatch).ToArray();
var tableEntries = imageUrls.Zip(codeUrls).Select(
	(contents, index) =>
	{
		(string image, string code) = contents;
		return new SourceTableEntry
		{
			Description = code,
			ImageUrl = image,
			Index = index
		};
	}
).ToArray();


var destinations = new List<DestinationEntry>();

const string destinationFolder = "C:\\Development\\github\\eternity-sandbox\\src\\Eternity\\Resources\\";

string PadNumber(int n)
{
	var str = $"{n}";
	StringBuilder sb = new();
	for(int i = 0; i < 3 - str.Length; ++i)
	{
		sb.Append("0");
	}
	sb.Append(str);
	return sb.ToString();
}

foreach (var tableEntry in tableEntries)
{
	var imageFileName = $"piece_{PadNumber(tableEntry.Index)}.png";
	var imagePath = Path.Join(destinationFolder, imageFileName);
	if (!File.Exists(imagePath))
	{
		var httpResponse = await httpClient.GetAsync(tableEntry.ImageUrl);
		using (var outStream = File.Open(imagePath, FileMode.Create))
		{
			await httpResponse.Content.CopyToAsync(outStream);
			outStream.Close();
		}
	}
	destinations.Add(
		new DestinationEntry
		{ 
			Code = tableEntry.Description,
			ImageName = imageFileName,
			Index = tableEntry.Index,
		}
	);
}

JsonSerializerOptions options = new JsonSerializerOptions
{
	WriteIndented = true,
	PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};

string jsonString = JsonSerializer.Serialize(destinations, options);

var destinationJsonFile = Path.Join(destinationFolder, "pieces.json");
if (File.Exists(destinationJsonFile))
{
	File.Delete(destinationJsonFile);
}

using(var outStream = new StreamWriter(destinationJsonFile))
{
	await outStream.WriteAsync(jsonString);
	await outStream.FlushAsync();
	outStream.Close();
}


Console.WriteLine(htmlContent);
