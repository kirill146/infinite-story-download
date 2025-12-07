using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.XPath;

namespace downloadInfiniteStory
{
    class ISPageHtmlParser
    {
        private static readonly String STORY_MAIN = "<div id=\"story-main\">";
        private static readonly String END_MAIN = "<!-- end description -->";
        private static readonly String END_CHOICES = "<!-- end choices -->";
        private static readonly String THE_END_FOOTER = "<div class=\"room-footer";

        private static readonly IDictionary<String, String> imageMap = new ConcurrentDictionary<String, String>();

        public static async Task BuildAllISPages(Dictionary<string, ISPage> pageMap, string roomId, string dirPath)
        {
            String baseHtml = null;
            ISPage page = null;
            try 
            {
                baseHtml = await ReadHtml(roomId);
            }
            catch(Exception e)
            {
                throw new ISParseException("Could not read Infinite Story webpage", e);
            }
            try
            {
                page = ISPageHtmlParser.ParseRawISHtml(baseHtml, roomId, dirPath);
            }
            catch (Exception e)
            {
                throw new ISParseException("Could not parse Infinite Story webpage", e);
            }
            
            pageMap.Add(roomId, page);
            List<Task> waitTasks = new List<Task>();
            foreach (String childRoomId in page.Choices.Select(x => x.RoomId))
            {
                if (!pageMap.ContainsKey(childRoomId))
                {
                    waitTasks.Add(BuildAllISPages(pageMap, childRoomId, dirPath));
                }
            }
            Task.WaitAll(waitTasks.ToArray());
        }

        public static async Task<string> ReadHtml(string roomId)
        {
            try
            {
                HttpClient client = new HttpClient();
                string url = Program.ISUrl + roomId;
                Console.WriteLine("fetching " + url);
                Stream data = await client.GetStreamAsync(url);
                //WebRequest request = WebRequest.Create(url);
                //WebResponse response = request.GetResponse();
                //Stream data = response.GetResponseStream();
                string html = String.Empty;
                using (StreamReader sr = new StreamReader(data))
                {
                    html = sr.ReadToEnd();
                }
                return html;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }





        public static ISPage ParseRawISHtml(String html, String roomId, string dirPath)
        {
            var page = new ISPage(roomId, html);

            int mainStart = html.IndexOf(STORY_MAIN);
            int mainEnd = html.IndexOf(END_MAIN);
            int choiceEnd = html.IndexOf(END_CHOICES);

            if (choiceEnd > 0)
            {
                List<Choice> choices = ParseChoices(html.Substring(mainEnd + END_MAIN.Length, choiceEnd - mainEnd - END_MAIN.Length));
                page.Choices = choices;
            }
            else
            {
                //we're at a game over screen
                choiceEnd = html.IndexOf(THE_END_FOOTER, mainEnd);
                page.EndText = html.Substring(mainEnd + END_MAIN.Length, choiceEnd - mainEnd - END_MAIN.Length);
            }

            String newHtml = html.Substring(mainStart, choiceEnd - mainStart);
            String mainTextHtml = html.Substring(mainStart, mainEnd - mainStart);

            page.Contents = ParseSaveAndFixImages(mainTextHtml, dirPath);

            return page;
        }

        private static String ParseSaveAndFixImages(string contents, string dirPath)
        {
            contents = System.Web.HttpUtility.HtmlDecode(contents);
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(new StringReader(contents));
            var nav = doc.CreateNavigator();
            var strExpression = "//img";

            HtmlAgilityPack.HtmlNodeCollection imgTags = doc.DocumentNode.SelectNodes(strExpression);
            if (imgTags != null)
            {
                foreach (HtmlAgilityPack.HtmlNode tag in imgTags)
                {
                    if (tag.Attributes["src"] != null)
                    {
                        String imgPath = tag.Attributes["src"].Value;
                        tag.Attributes["src"].Value = GetAndSaveImage(imgPath, dirPath);
                    }
                }
            }

            string finalContents = null;
            using (StringWriter sw = new StringWriter())
            {
                doc.Save(sw);
                finalContents = sw.ToString();
            }

            return finalContents;
        }

        private static string GetAndSaveImage(string imgPath, string dirPath)
        {
            string fileName = Path.GetFileName(imgPath);
            if (!imageMap.ContainsKey(imgPath))
            {
                SaveImageFromUrl(imgPath, Path.Combine(Path.GetDirectoryName(dirPath), fileName));
                imageMap.Add(imgPath, fileName);
            }
            return Path.Combine(dirPath, fileName);
        }

        public static void SaveImageFromUrl(string url, string filePath)
        {
            Uri uri;
            if (url.Contains(Program.INFINITE_STORY_URL_BASE))
            {
                uri = new Uri(url);
            }
            else
            {
                if (url.StartsWith("/"))
                {
                    uri = new Uri(Program.INFINITE_STORY_URL_BASE + url.Substring(1));
                }
                else
                {
                    uri = new Uri(Program.INFINITE_STORY_URL_BASE + url);
                }

            }

            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(uri);

            using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                using (Stream stream = httpWebReponse.GetResponseStream())
                {
                    using (FileStream fs = File.OpenWrite(filePath))
                    {
                        stream.CopyTo(fs);
                    }
                }
            }
        }

        private static List<Choice> ParseChoices(string choicesHtml)
        {
            choicesHtml = System.Web.HttpUtility.HtmlDecode(choicesHtml);
            var choices = new List<Choice>();

            /*
             * <div id="room-choices"><h2>You have 4 choices:</h2>
             * <ul class="choices">
             * <li><a href="/story/choice.php?id=184898" title="184898,94419">Years later&hellip;</a></li>
             * <li><a href="/story/choice.php?id=184899" title="184899,94416">The Empire (Lies)</a></li>
             * <li><a href="/story/choice.php?id=184900" title="184900,94417">The Empire (Truth)</a></li>
             * <li><a href="/story/choice.php?id=184901" title="184901,94418">The Eternal Program (Hope)</a></li>
             * </ul>
             * </div> 
             */


            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(new StringReader(choicesHtml));
            var nav = doc.CreateNavigator();
            var strExpression = "//a";

            foreach (HtmlAgilityPack.HtmlNode link in doc.DocumentNode.SelectNodes(strExpression))
            {

                string titleValue = link.GetAttributeValue("title", null);
                String[] titleSplit = titleValue.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                Choice choice = new Choice(titleSplit[0], titleSplit[1], link.InnerHtml);
                choices.Add(choice);
            }

            return choices;
        }
    }
}
