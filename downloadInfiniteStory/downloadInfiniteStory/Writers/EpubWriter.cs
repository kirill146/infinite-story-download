using downloadInfiniteStory.Writers;
using EPubFactory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace downloadInfiniteStory
{
    class EpubWriter : IWriter
    {
        private Dictionary<string, ISPage> pageMap;
        private string baseRoom;
        private string filePath;

        public EpubWriter(Dictionary<string, ISPage> pageMap, string baseRoom, string filePath)
        {
            // TODO: Complete member initialization
            this.pageMap = pageMap;
            this.baseRoom = baseRoom;
            this.filePath = filePath;
        }

        public void Write()
        {
           string fileDir = Path.GetDirectoryName(filePath);
           if (fileDir != "") {
               Directory.CreateDirectory(fileDir);
           }
           WriteEPub().Wait();
        }

        private async Task WriteEPub()
        {
            try
            {
                using (var ePubStream = File.Create(filePath))
                {
                    using (var writer = await EPubWriter.CreateWriterAsync(
                        ePubStream,
                        Path.GetFileNameWithoutExtension(filePath),
                        "Generator",
                        baseRoom,
                        leaveOpen: true))
                    {
                        ISPage firstPage = pageMap.Values.Where(x => x.RoomId.Equals(baseRoom)).First();
                        await writer.AddChapterAsync(firstPage.RoomId + ".html", firstPage.RoomId, GetEpubPageContents(firstPage));
                        foreach (ISPage page in pageMap.Values.Where(x => !x.RoomId.Equals(baseRoom)))
                        {
                            await writer.AddChapterAsync(page.RoomId + ".html", page.RoomId, GetEpubPageContents(page));
                        }



                        //  Add a chapter with string content as x-html
                        //await writer.AddChapterAsync(
                        //    "FirstChapter.xhtml",
                        //    "First Chapter Title",
                        //    File.ReadAllText("FirstChapter.xhtml"));


                        //  Streaming mode
                        //var chapterStream = writer.GetChapterStream(
                        //    "SecondChapter.xhtml",
                        //    "Second Chapter - Streaming");

                        //  Fill the chapter-stream
                        //  await chapterStream.WriteAsync(...)

                        //  Add an image from the disk
                        //await writer.AddResourceAsync("MyLogo.png", "image/png", File.ReadAllBytes("MyLogo.png"));

                        //  Streaming mode for images
                        //var imageStream = writer.GetResourceStream("MyBackground.jpg", "image/jpeg");

                        //  Fill the image-stream
                        //  await imageStream.WriteAsync(...)

                        //  Must be called async at the end to write down the TOC of the ePub
                        await writer.WriteEndOfPackageAsync();
                        //ePubStream.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private string GetEpubPageContents(ISPage page)
        {
            return HtmlWriter.GetHtmlContents(page);
        }


        private static async Task TestEPub()
        {
            var ePubStream = File.Create("Test.epub");

            using (var writer = await EPubWriter.CreateWriterAsync(
                ePubStream,
                "My Title",
                "Me, The Author",
                "My Unique Identifier"))
            {
                //  Optional parameter
                writer.Publisher = "Demo App";

                //  Add a chapter with string content as x-html
                await writer.AddChapterAsync(
                    "FirstChapter.xhtml",
                    "First Chapter Title",
                    File.ReadAllText("FirstChapter.xhtml"));

                
                //  Streaming mode
                var chapterStream = writer.GetChapterStream(
                    "SecondChapter.xhtml",
                    "Second Chapter - Streaming");

                //  Fill the chapter-stream
                //  await chapterStream.WriteAsync(...)

                //  Add an image from the disk
                await writer.AddResourceAsync("MyLogo.png", "image/png", File.ReadAllBytes("MyLogo.png"));

                //  Streaming mode for images
                var imageStream = writer.GetResourceStream("MyBackground.jpg", "image/jpeg");

                //  Fill the image-stream
                //  await imageStream.WriteAsync(...)

                //  Must be called async at the end to write down the TOC of the ePub
                await writer.WriteEndOfPackageAsync();
            }
        }
    }
}
