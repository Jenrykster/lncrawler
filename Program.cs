// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CommandLine;
using HtmlAgilityPack;
using Serilog;

namespace LnCrawler
{
  partial class Program
  {

    public class Options
    {
      [Option('t', "title", Required = true, HelpText = "The title of the light novel")]
      public required string Title { get; set; }

      [Option('u', "src", Required = true, HelpText = "The url of the light novel index, it must contain urls pointing to all the chapters")]
      public required string Source { get; set; }
    }

    class Chapter(string name, string url)
    {
      readonly public string url = url;
      readonly public string name = name;
    }

    static void Main(string[] args)
    {

      Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger();
      Log.Logger.Information("Starting Crawler");

      Parser.Default.ParseArguments<Options>(args).WithParsed((options) =>
      {
        Log.Logger.Information("Finding chapters...");
        var chapters = GetChaptersByIndex(options.Source);
        Log.Logger.Information($"{chapters.Count} chapters found!");

        if (chapters.Count > 0)
        {
          foreach (var chapter in chapters)
          {
            Log.Logger.Information("Parsing the contents of: {chapter}", chapter.name);
            var chapterContents = ParsePage(chapter.url);
            WriteToFile(options.Title, chapter.name, chapterContents);
          }
          Log.Logger.Information("Success! Chapters saved to {path}", Path.Combine(Environment.CurrentDirectory, options.Title));
        }
      });
    }

    static List<Chapter> GetChaptersByIndex(string indexPageUrl)
    {
      var xPathQuery = "//*[@class='entry-content']//a[contains(text(), 'Chapter')]";

      var webLoader = new HtmlWeb();
      var indexPage = webLoader.Load(indexPageUrl);

      indexPage.LoadHtml(WebUtility.HtmlDecode(indexPage.Text));

      var chapterNodes = indexPage.DocumentNode.SelectNodes(xPathQuery);

      return chapterNodes.Select((node) => new Chapter(node.InnerText, node.GetAttributeValue("href", ""))).ToList();
    }

    private static string ParsePage(string targetUrl)
    {
      var website = new HtmlWeb();

      try
      {
        var doc = website.Load(targetUrl);

        doc.LoadHtml(WebUtility.HtmlDecode(doc.Text));

        var chapterTitle = doc.DocumentNode.SelectSingleNode("//h1[@class=\"entry-title\"]").InnerText;
        var chapterBodyParagraphs = doc.DocumentNode.SelectSingleNode("//div[@class=\"entry-content\"]").SelectNodes("./p");

        var result = "";

        foreach (HtmlNode node in chapterBodyParagraphs.Take(chapterBodyParagraphs.Count - 1))
        {
          var cleanSection = WhiteSpaceRegexs().Replace(node.InnerText.Trim(), " ");

          result += cleanSection + '\n';
        }

        return result;
      }
      catch (Exception error)
      {
        Log.Logger.Error("Could not load the provided URL, {reason}", error);
        return "";
      }
    }
    private static void WriteToFile(string title, string chapterName, string contents)
    {
      var dir = Path.Combine(Environment.CurrentDirectory, title);
      Directory.CreateDirectory(dir);

      var fileName = Path.Combine(dir, $@"{chapterName}.txt");

      FileStream file = new(fileName, FileMode.Create);

      using StreamWriter outputFile = new(file);
      outputFile.WriteLine(contents);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegexs();
  }
}