using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using tesseract;

//res: 2480x3507
namespace WindowsFormsApplication1
{
    enum lineType
    {
        empty, mixed, black
    }
    enum ColorType
    {
        white, black
    }

    static class PageStyle
    {
        static Rectangle FrontPage = new Rectangle(250, 1400, 2140, 1910);
        static Rectangle MidPage = new Rectangle(250, 1155, 2140, 1910);
        static Rectangle LastPage = new Rectangle(250, 1155, 2140, 1640);
        static Point TopPoint = new Point(1250, 990);
        static Point LowPoint = new Point(2360, 3340);

        static public Rectangle getCropArea(Image img)
        {
            Bitmap bm = (Bitmap)img;

            if (bm.GetPixel(TopPoint.X, TopPoint.Y).GetBrightness() >= 0.9f)
            {
                return FrontPage;
            }
            else
            {
                if (bm.GetPixel(LowPoint.X, LowPoint.Y).GetBrightness() >= 0.9f)
                {
                    return LastPage;
                }
                else
                {
                    return MidPage;
                }
            }
        }
    }

    class lineDetect
    {
        public int StartX, StartY, Width;
        public float Margin;
        private float lBound;
        private Bitmap Img;
        public int last;
        public bool end = false;

        public lineDetect(int startX, int startY, int width, float margin, Image img)
        {
            StartX = startX;
            StartY = startY;
            Width = width;
            Margin = margin;
            Img = (Bitmap)img;
            lBound = 1.0f - margin;
            last = startY;
        }

        private bool isWhite(float brightness) {
            if (brightness >= lBound)
            {
                return true;
            }
            return false;
        }

        public Rectangle findArea()
        {
            bool allWhite = true;
            bool rowWhite = true;
            int iy = last;
            int ll = last;

            while ((allWhite || (!allWhite && !rowWhite)) && iy < Img.Height)
            {
                rowWhite = true;
                for (int ix = StartX; ix <= StartX + Width; ix++)
                {
                    float B = Img.GetPixel(ix, iy).GetBrightness();
                    rowWhite = isWhite(B) && rowWhite;
                    if (!rowWhite)
                    {
                        allWhite = false;
                        break;
                    }
                }
                iy++;
            }
            last = iy;
            if (iy >= Img.Height - 2)
            {
                end = true;
            }
            return new Rectangle(StartX, ll, Width, iy-ll);
        }
    }

    class parser
    {
        public Image baseIMG;
        private Form1 caller;
        private TesseractProcessor ocr;
        private float _margin = 0.1f;
        private StreamWriter Writer;

        public parser(Image img, Form1 call)
        {
            baseIMG = img;
            caller = call;
            ocr = new TesseractProcessor();
            ocr.DoMonitor = true;

            //MessageBox.Show(Application.StartupPath+@"\");
            ocr.Init(Application.StartupPath + @"\tessdata", "deu", 0);
        }

        public void cutPage()
        {
            baseIMG = cropImg(PageStyle.getCropArea(baseIMG));
        }

        public Image cropImg(Rectangle rect)
        {
            Bitmap tmp = ((Bitmap)baseIMG).Clone(rect, baseIMG.PixelFormat);
            return (Image)tmp;
        }

        public void DoIt()
        {
            cutPage();
            Writer = new StreamWriter(Application.StartupPath + @"\res.csv", true);
            findRows();
            Writer.Close();
        }

        public void findRows()
        {
            lineDetect lD = new lineDetect(1060, 0, 50, _margin, (Image)baseIMG);

            while (!lD.end)
            {
                Rectangle found = lD.findArea();

                if (lD.end) continue;
                string ResDate = getDate(found);
                //MessageBox.Show(ResDate);
                string ResValue = getValue(found);
                string ResZweck = string.Join("\";\"", getZweck(found));

                writeLine("\"" + ResDate + "\";\"" + ResValue + "\";\"" + ResZweck + "\"");
            }
        }

        private string getOCR(Image img)
        {
            ocr.Clear();
            ocr.ClearAdaptiveClassifier();
            ocr.SetVariable(@"tessedit_pageseg_mode", "3");
            return ocr.Apply(img).Trim();
        }

        private string getDate(Rectangle r)
        {
            string res = "";
            Rectangle v = new Rectangle(990, r.Y, 150, r.Height);
            ocr.SetVariable("tessedit_char_whitelist", "\0");
            res = getOCR(cropImg(v)).Trim();
            return res;
        }

        private string getValue(Rectangle r)
        {
            string res = "";
            Rectangle v = new Rectangle(1150, r.Y, 490, r.Height);
            ocr.SetVariable("tessedit_char_whitelist", ".0123456789+-,");
            res = getOCR(cropImg(v));
            if (res == "")
            {
                v.X = 1650;
                res = "+" + getOCR(cropImg(v));
            }
            else
            {
                res = "-" + res.Replace("-", string.Empty);
            }
            res = res.Replace(" ", string.Empty).Trim() ;
            return res;
        }

        private string[] getZweck(Rectangle r)
        {
            string[] res = new string[14];
            
            Rectangle v = new Rectangle(0, r.Y, 900, r.Height);
            ocr.SetVariable("tessedit_char_whitelist", "\0");
            Queue<String> tmp=new Queue<string>(getOCR(cropImg(v)).Split(Environment.NewLine.ToCharArray()));
            if (tmp.ElementAt(0).StartsWith("Buchungsdatum"))
            {
                tmp.Dequeue();
            }
            if (tmp.Count > 14)
            {
                return res;
            }
            tmp.CopyTo(res, 0);
            return res;
        }

        private void writeLine(string line) {
            //MessageBox.Show(line);
            Writer.WriteLine(line);
        }
    }
}
