// See https://aka.ms/new-console-template for more information
using HtmlAgilityPack;
using Serilog;

namespace LnCrawler
{
  class Program
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
      var web = new HtmlWeb();
      try
      {
        var doc = web.Load(targetUrl);
        Log.Logger.Information("{result}", new
        {
          url = targetUrl,
          heading = "Chapter 1",
          content = doc.ParsedText
        });
      }
      catch (Exception error)
      {
        Log.Logger.Error("Could not load the provided URL, {reason}", error);
      }
    }
  }
}