using downloadInfiniteStory.Writers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace downloadInfiniteStory
{
    class HtmlWriter : IWriter
    {
        private static readonly string CHOICES_START_XML = "<div id=\"room-choices\"><h2>You have X choices:</h2><ul class=\"choices\">";
        private static readonly string CHOICES_LINE_XML = "<li><a href=\"PAGEID.html\" title=\"TITLEINFO\">CHOICETEXT</a></li>";
        private static readonly string CHOICES_END_XML = "</ul></div> </div></div></div>";
        private Dictionary<string, ISPage> pageMap;
        private string baseRoom;
        private string dirPath;

        public HtmlWriter(Dictionary<string, ISPage> pageMap, string baseRoom, string dirPath)
        {
            this.pageMap = pageMap;
            this.baseRoom = baseRoom;
            this.dirPath = dirPath;
        }

        public static string GetHtmlContents(ISPage page)
        {
            StringBuilder html = new StringBuilder(page.Contents);

            if (page.EndText != null)
            {
                html.AppendLine(page.EndText);
            }
            else
            {
                html.AppendLine(CHOICES_START_XML.Replace("X", page.Choices.Count.ToString()));
                foreach (Choice choice in page.Choices)
                {
                    html.AppendLine(CHOICES_LINE_XML.Replace("PAGEID", choice.RoomId).Replace("TITLEINFO", choice.ChoiceId + "," + choice.RoomId).Replace("CHOICETEXT", choice.Text));
                }
                html.AppendLine(CHOICES_END_XML);
            }
            return html.ToString();
        }




        private void BuildHtml(Dictionary<string, ISPage> pageMap, string roomId, string dirPath)
        {
            foreach (string pageToAdd in pageMap.Keys)
            {
                ISPage page = pageMap[pageToAdd];
                SavePage(pageToAdd, page, dirPath);
            }
        }

        private static void SavePage(string roomId, ISPage page, string dirPath)
        {
            File.WriteAllText(Path.Combine(dirPath, roomId + ".html"), GetHtmlContents(page));
        }

        public void Write()
        {
            if (dirPath != "") {
                Directory.CreateDirectory(dirPath);
            }
            BuildHtml(pageMap, baseRoom, dirPath);
        }
    }
}
