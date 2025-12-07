using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using downloadInfiniteStory.Writers;

namespace downloadInfiniteStory
{
    class Program
    {
        public static readonly string INFINITE_STORY_URL_BASE = "https://infinite-story.com/";
        public static readonly string ISUrl = "https://infinite-story.com/story/room.php?id=";

        //public static String baseFilePath = "D:\\Documents\\chooseYourOwnStory\\";
        //public static string baseFileName = "myPdf.pdf";

        //ground zero "36382"
        //eternal "94415"

        static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine("Failed to parse command line arguments.  Try running help command (-h).");
                return;
            }
            else if (!(options.BuildEpub || options.BuildHtml || options.BuildPdf)) 
            {
                Console.WriteLine("One of the build options (HTML, EPUB, PDF) must be specified..  Try running help command (-h).");
                return;
            }

            //baseFilePath = Path.GetDirectoryName(options.OutputPath);
            //baseFileName = Path.GetFileName(options.OutputPath);

            String baseRoom = options.RoomId;
            string dirPath = null;
            if(options.BuildEpub || options.BuildPdf)
            {
                dirPath = Path.GetTempPath();
            }
            else
            {
                dirPath = Path.GetDirectoryName(options.OutputPath);
            }
            var pageMap = BuildPageMap(baseRoom, dirPath);

            IWriter writer = null;
            if(options.BuildHtml)
            {
                writer = BuildHtml(pageMap, baseRoom, options.OutputPath);
            }
            else if(options.BuildPdf)
            {
                writer = BuildPDF(pageMap, baseRoom, options.OutputPath);
            }
            else if(options.BuildEpub)
            {
                writer = BuildEpub(pageMap, baseRoom, options.OutputPath);
            }
            else
            {
                Console.WriteLine("One of the build options (HTML, EPUB, PDF) must be specified..  Try running help command (-h).");
                return;
            }
            writer.Write();
        }

        private static IWriter BuildHtml(Dictionary<string, ISPage> pageMap, string baseRoom, string dirPath)
        {
            return new HtmlWriter(pageMap, baseRoom, dirPath);
        }

        private static IWriter BuildPDF(Dictionary<string, ISPage> pageMap, string baseRoom, string filePath)
        {
            return new PdfWriter(pageMap, baseRoom, filePath);
        }

        private static IWriter BuildEpub(Dictionary<string, ISPage> pageMap, string baseRoom, string filePath)
        {
            return new EpubWriter(pageMap, baseRoom, filePath);
        }

        private static Dictionary<string, ISPage> BuildPageMap(string baseRoom, string dirPath)
        {
            var pageMap = new Dictionary<string, ISPage>();
            ISPageHtmlParser.BuildAllISPages(pageMap, baseRoom, dirPath).Wait();
            return pageMap;
        }

    }
}
