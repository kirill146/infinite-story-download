using downloadInfiniteStory.Writers;
using iTextSharp.text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace downloadInfiniteStory
{
    class PdfWriter : IWriter
    {



        private void BuildPDF(Dictionary<string, ISPage> pageMap, string baseRoom, int test)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, memStream);
                document.Open();
                document.Add(new Paragraph("Hello World"));
                //document.Close();
                //writer.Close();

                //File.WriteAllBytes(Path.Combine(baseFilePath, baseFileName), memStream.GetBuffer());
                using (FileStream file = new FileStream(Path.Combine(filePath, fileName), FileMode.Create, FileAccess.Write))
                {
                    memStream.WriteTo(file);
                }

                document.Close();
                writer.Close();
            }
        }

        private void BuildPDF(Dictionary<string, ISPage> pageMap, string baseRoom)
        {
            Dictionary<string, int> chapterPageMap = new Dictionary<string, int>();
            BuildPDF(pageMap, baseRoom, chapterPageMap, true);
            BuildPDF(pageMap, baseRoom, chapterPageMap, false);
        }

        private void BuildPDF(Dictionary<string, ISPage> pageMap, string baseRoom, Dictionary<string, int> chapterPageMap, bool fakeRun)
        {
            Dictionary<string, int> ISPageToPhysicalPageMap = new Dictionary<string, int>();

            int currentPage = 1;
            int currentChapter = 1;
            Random r = new Random(123456);
            List<string> pagesLeft = new List<string>(pageMap.Count);
            foreach (string x in pageMap.Keys)
            {
                pagesLeft.Add(x);
            }

            using (MemoryStream memStream = new MemoryStream())
            {
                Document pdfDoc = new Document();
                iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(pdfDoc, memStream);
                HeaderFooter evnt = new HeaderFooter();
                if (fakeRun)
                {
                    evnt.pageByTitle = chapterPageMap;
                }
                else
                {
                    evnt.pageByTitle = new Dictionary<string, int>();
                }
                if (!fakeRun)
                {
                    writer.PageEvent = evnt;
                }

                pdfDoc.Open();
                pdfDoc.AddAuthor("test");
                pdfDoc.AddTitle("testTitle");

                while (pagesLeft.Any())
                {

                    string pageToAdd = pagesLeft.First();
                    if (currentPage > 1)
                    {
                        pagesLeft.Skip(r.Next(pagesLeft.Count)).First();
                    }
                    pagesLeft.Remove(pageToAdd);

                    if (fakeRun)
                    {
                        chapterPageMap.Add(pageToAdd, writer.PageNumber + 1);
                    }

                    ISPageToPhysicalPageMap.Add(pageToAdd, currentPage);

                    var chapter = GetPDFPage(pageMap[pageToAdd], int.Parse(pageToAdd), chapterPageMap);
                    pdfDoc.Add(chapter);

                    int actualPageLength = fakeRun ? 1 : chapterPageMap[pageToAdd];

                    currentPage += actualPageLength;
                    currentChapter++;
                }

                pdfDoc.Close();
                writer.Close();

                if (!fakeRun)
                {
                    File.WriteAllBytes(Path.Combine(filePath, fileName), memStream.GetBuffer());
                }
            }
        }

        private Dictionary<string, ISPage> pageMap;
        private string baseRoom;
        private string filePath;
        private string fileName;

        public PdfWriter(Dictionary<string, ISPage> pageMap, string baseRoom, string filePath)
        {
            // TODO: Complete member initialization
            this.pageMap = pageMap;
            this.baseRoom = baseRoom;
            this.filePath = Path.GetDirectoryName(filePath);
            this.fileName = Path.GetFileName(filePath);
        }
        private static Chapter GetPDFPage(ISPage iSPage, int chapterNum, Dictionary<string, int> map)
        {

            Chapter chap = PdfChapterHelper.HtmlToPdfString(iSPage, chapterNum, map);

            return chap;
        }


        public void Write()
        {
            if (filePath != "") {
                Directory.CreateDirectory(filePath);
            }
            BuildPDF(pageMap, baseRoom);
        }
    }
}
