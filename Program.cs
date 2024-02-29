// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Serilog;

namespace LnCrawler
{
  partial class Program
  {
    static void Main(string[] args)
    {
      if (args.Length == 0)
      {
        Console.Error.WriteLine("Please provide the LN URL");
        return;
      }

      var target = args[0];

      Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger();
      Log.Logger.Information("Starting Crawler");

      ParsePage(target);
    }

    private static void ParsePage(string targetUrl)
    {
      var website = new HtmlWeb();

      try
      {
        var doc = website.Load(targetUrl);
        doc.Load(WebUtility.HtmlDecode(doc.Text));

        var chapterTitle = doc.DocumentNode.SelectSingleNode("//h1[@class=\"entry-title\"]").InnerText;
        var chapterBodyParagraphs = doc.DocumentNode.SelectSingleNode("//div[@class=\"entry-content\"]").SelectNodes("./p");

        var result = "";

        foreach (HtmlNode node in chapterBodyParagraphs.Take(chapterBodyParagraphs.Count - 1))
        {
          var cleanSection = WhiteSpaceRegexs().Replace(node.InnerText.Trim(), " ");

          result += cleanSection + '\n';
        }

        WriteToFile(chapterTitle, result);

        Log.Logger.Information("Success: {result}", new
        {
          url = targetUrl,
          heading = chapterTitle,
        });

      }
      catch (Exception error)
      {
        Log.Logger.Error("Could not load the provided URL, {reason}", error);
      }
    }
    private static void WriteToFile(string chapterName, string contents)
    {
      var fileName = $@"{chapterName}.txt";

      FileStream file = new(fileName, FileMode.Create);

      using StreamWriter outputFile = new(file);
      outputFile.WriteLine(contents);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegexs();
  }
}