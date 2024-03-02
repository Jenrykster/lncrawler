// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Text.RegularExpressions;
using CommandLine;
using HtmlAgilityPack;
using QuickEPUB;
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

        var epubFile = new Epub(options.Title, "Some LN Author");
        if (chapters.Count > 0)
        {
          foreach (var chapter in chapters)
          {
            Log.Logger.Information("Parsing the contents of: {chapter}", chapter.name);
            var chapterContents = ParsePage(chapter.url);

            epubFile.AddSection(chapter.name, chapterContents);
          }

          SaveEpub(options.Title, epubFile);
          Log.Logger.Information("Success! Epub file saved to {path}", Path.Combine(Environment.CurrentDirectory, $"{options.Title}.epub"));
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

        var chapterTitle = doc.DocumentNode.SelectSingleNode("//h1[@class=\"entry-title\"]");
        var chapterBodyParagraphs = doc.DocumentNode.SelectSingleNode("//div[@class=\"entry-content\"]").SelectNodes("./p");

        var result = "";

        result += chapterTitle.OuterHtml;

        foreach (HtmlNode node in chapterBodyParagraphs.Take(chapterBodyParagraphs.Count - 1))
        {
          var cleanSection = WhiteSpaceRegexs().Replace(node.OuterHtml.Trim(), " ");

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
    private static void SaveEpub(string title, Epub file)
    {
      var dir = Environment.CurrentDirectory;
      Directory.CreateDirectory(dir);

      var fileName = Path.Combine(dir, $@"{title}.epub");

      FileStream fileStream = new(fileName, FileMode.Create);

      file.Export(fileStream);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegexs();
  }
}