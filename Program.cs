// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Text.RegularExpressions;
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

    static void Main(string[] args)
    {

      Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger();
      Log.Logger.Information("Starting Crawler");

      Parser.Default.ParseArguments<Options>(args).WithParsed((options) =>
      {
        var result = ParsePage(options.Source);
        WriteToFile(options.Title, "Chapter 1", result);
      });
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