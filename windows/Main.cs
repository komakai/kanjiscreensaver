using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Xml;

namespace KanjiScreenSaver
{

    public partial class Main : Form
    {
        #region Preview API's

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

        #endregion

        bool IsPreviewMode = false;

        List<Kanji> kanjis = new List<Kanji>();

        int currentKanji = -1;
        int xOffset;
        int yOffset;
        float heightKanji;
        float widthKanji;
        float heightMeaning;
        float widthMeaning;
        float height;
        float width;

        Font fontKanji;
        Font fontMeaning;
        Font fontKeywordNormal;
        Font fontKeywordBold;

        Random rnd = new Random();

        const int keywordParts = 5;
        const int maxKeywords = 10;
        const int kanjiFontHeight = 80;
        const int meaningFontHeight = 20;
        const int keywordFontHeight = 14;
        Label[] labels = new Label[keywordParts * maxKeywords];
        int fontFactor = 1;

        #region Constructors

        public Main()
        {
            InitializeComponent();
            LoadKanjisAndStartTimer();
        }

        //This constructor is passed the bounds this form is to show in
        //It is used when in normal mode
        public Main(Rectangle Bounds)
        {
            InitializeComponent();
            this.Bounds = Bounds;
            //hide the cursor
            Cursor.Hide();
            LoadKanjisAndStartTimer();
        }

        //This constructor is the handle to the select screensaver dialog preview window
        //It is used when in preview mode (/p)
        public Main(IntPtr PreviewHandle)
        {
            InitializeComponent();

            //set the preview window as the parent of this window
            SetParent(this.Handle, PreviewHandle);

            //make this a child window, so when the select screensaver dialog closes, this will also close
            SetWindowLong(this.Handle, -16, new IntPtr(GetWindowLong(this.Handle, -16) | 0x40000000));

            //set our window's size to the size of our window's new parent
            Rectangle ParentRect;
            GetClientRect(PreviewHandle, out ParentRect);
            this.Size = ParentRect.Size;

            //set our location at (0, 0)
            this.Location = new Point(0, 0);

            IsPreviewMode = true;
            fontFactor = 2;
            LoadKanjisAndStartTimer();
        }

        #endregion

        #region Kanji Data Loading

        protected string GetAttribString(XmlNode node, string attrName)
        {
            XmlAttribute attr = node.Attributes[attrName];
            return (attr!=null)?attr.Value:"";
        }

        protected int GetAttribInt(XmlNode node, string attrName)
        {
            XmlAttribute attr = node.Attributes[attrName];
            return (attr != null) ? Convert.ToInt32(attr.Value) : -1;
        }

        public void LoadKanjisAndStartTimer()
        {
            int fontFactor = IsPreviewMode ? 2 : 1;
            fontKanji = new Font(Program.fontFaceName, kanjiFontHeight/fontFactor);
            fontMeaning = new Font(Program.fontName, meaningFontHeight / fontFactor, FontStyle.Bold);
            fontKeywordNormal = new Font(Program.fontName, keywordFontHeight / fontFactor, FontStyle.Regular);
            fontKeywordBold = new Font(Program.fontName, keywordFontHeight / fontFactor, FontStyle.Bold);

            labelKanji.Font = fontKanji;
            labelMeaning.Font = fontMeaning;

            int iControl = 0;
            foreach (Control control in Controls)
            {
                if ( (control.GetType() == typeof(Label)) && control.Tag != null && control.Tag.Equals("keyword") )
                {
                    labels[iControl] = (Label)control;
                    if (((iControl % 5) == 1) || ((iControl % 5 )== 3)) {
                        labels[iControl].Font = fontKeywordBold;
                    } else {
                        labels[iControl].Font = fontKeywordNormal;
                    }
                    iControl++;
                }
            }
            XmlDocument kanjiData = new XmlDocument();
            kanjiData.LoadXml(Properties.Resources.Kanji);
            foreach (XmlNode kanjiNode in kanjiData.DocumentElement.SelectNodes("kanji")) {
                string level = GetAttribString(kanjiNode, "level");
                char kchar = GetAttribString(kanjiNode, "char")[0];
                if ( ( Program.levels.Contains(level) || Program.includeKanjis.Contains(kchar) ) && ! Program.excludeKanjis.Contains(kchar) ) {
                    Kanji kanji = new Kanji();
                    kanji.kanji = kchar;
                    kanji.level = Convert.ToString(Convert.ToInt32(level)/10.0);
                    kanji.meaning = GetAttribString(kanjiNode, "meaning");
                    foreach (XmlNode keywordNode in kanjiNode.SelectNodes("keyword")) {
                        KeyWord keyword = new KeyWord();
                        keyword.word = GetAttribString(keywordNode, "word");
                        keyword.kana = GetAttribString(keywordNode, "kana");
                        keyword.kanaHighlightStart = GetAttribInt(keywordNode, "kanaHighlightStart");
                        keyword.kanaHighlightLength = GetAttribInt(keywordNode, "kanaHighlightLength");
                        if ( keyword.kanaHighlightStart == -1 && keyword.kanaHighlightLength == -1) {
                            keyword.kanaHighlightStart = 0;
                            keyword.kanaHighlightLength = keyword.kana.Length;
                        }
                        keyword.romaji = GetAttribString(keywordNode, "romaji");
                        keyword.romajiHighlightStart = GetAttribInt(keywordNode, "romajiHighlightStart");
                        keyword.romajiHighlightLength = GetAttribInt(keywordNode, "romajiHighlightLength");
                        if (keyword.romajiHighlightStart == -1 && keyword.romajiHighlightLength == -1) {
                            keyword.romajiHighlightStart = 0;
                            keyword.romajiHighlightLength = keyword.romaji.Length;
                        }
                        keyword.meaning = GetAttribString(keywordNode, "meaning");
                        kanji.keywords.Add(keyword);
                    }
                    kanjis.Add(kanji);
                }
            }

            timer1_Tick(null, null);
            timer1.Interval = Program.duration;
            timer1.Enabled = true;
        }

        #endregion

        #region User Input

        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (!IsPreviewMode) //disable exit functions for preview
            {
                Application.Exit();
            }
        }

        private void Main_Click(object sender, EventArgs e)
        {
            if (!IsPreviewMode) //disable exit functions for preview
            {
                Application.Exit();
            }
        }

        //start off OriginalLoction with an X and Y of int.MaxValue, because
        //it is impossible for the cursor to be at that position. That way, we
        //know if this variable has been set yet.
        Point OriginalLocation = new Point(int.MaxValue, int.MaxValue);

        private void Main_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsPreviewMode) //disable exit functions for preview
            {
                //see if originallocat5ion has been set
                if (OriginalLocation.X == int.MaxValue & OriginalLocation.Y == int.MaxValue)
                {
                    OriginalLocation = e.Location;
                }
                //see if the mouse has moved more than 20 pixels in any direction. If it has, close the application.
                if (Math.Abs(e.X - OriginalLocation.X) > 20 | Math.Abs(e.Y - OriginalLocation.Y) > 20)
                {
                    Application.Exit();
                }
            }
        }

        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            currentKanji = rnd.Next(kanjis.Count);
            Kanji kanji = kanjis[currentKanji];
            height = 0;
            width = 0;
            labelKanji.Text = kanji.kanji.ToString();
            heightKanji = height = labelKanji.Height + 4.0f;
            widthKanji = width = labelKanji.Width;
            labelMeaning.Text = kanji.meaning;
            heightMeaning = labelMeaning.Height + 4.0f;
            widthMeaning = labelMeaning.Width;
            height += heightMeaning;
            width = Math.Max(width, widthMeaning);

            Dictionary<KeyWord, float> mapKeywordDrawData = new Dictionary<KeyWord, float>();

            int iControl = 0;
            float keywordHeight = 0.0f;
            foreach (KeyWord keyword in kanji.keywords) {
                labels[iControl].Text = keyword.word + "・" + keyword.kana.Substring(0, keyword.kanaHighlightStart);
                labels[iControl + 1].Text = keyword.kana.Substring(keyword.kanaHighlightStart, keyword.kanaHighlightLength);
                labels[iControl + 2].Text = keyword.kana.Substring(keyword.kanaHighlightStart + keyword.kanaHighlightLength) + "・" + keyword.romaji.Substring(0, keyword.romajiHighlightStart);
                labels[iControl + 3].Text = keyword.romaji.Substring(keyword.romajiHighlightStart, keyword.romajiHighlightLength);
                labels[iControl + 4].Text = keyword.romaji.Substring(keyword.romajiHighlightStart + keyword.romajiHighlightLength) + "・" + keyword.meaning;
                float keywordWidth = 0.0f;
                for (int nPart = 0; nPart <keywordParts ; nPart++ ) {
                    keywordWidth += labels[iControl + nPart].Width;
                    keywordHeight = Math.Max(keywordHeight, labels[iControl + nPart].Height);
                }
                iControl += 5;
                keywordWidth -= 28.0f;
                mapKeywordDrawData[keyword] = keywordWidth;
                width = Math.Max(width, keywordWidth);
            }
            keywordHeight += 4.0f;
            height += keywordHeight * kanji.keywords.Count;
            for (int nIndx = 0; nIndx < labels.Count(); nIndx++) {
                labels[nIndx].Visible = nIndx < iControl;
            }
            if (!IsPreviewMode) {
                xOffset = 20 + rnd.Next(Screen.PrimaryScreen.Bounds.Width - 40 - (int)width);
                yOffset = 20 + rnd.Next(Screen.PrimaryScreen.Bounds.Height - 40 - (int)height);
            } else {
                xOffset = (Size.Width-(int)width)/2;
                yOffset = 0;
            }
            iControl = 0;
            float drawYOffset = yOffset;
            labelKanji.Top = (int)drawYOffset;
            labelKanji.Left = (int)(xOffset + (width - widthKanji) / 2) + 5;
            drawYOffset += heightKanji;
            labelMeaning.Top = (int)drawYOffset;
            labelMeaning.Left = (int)(xOffset + (width - widthMeaning) / 2);
            drawYOffset += heightMeaning;
            foreach (KeyWord keyword in kanji.keywords)
            {
                float keywordWidth = mapKeywordDrawData[keyword];
                float keywordPartOffset = xOffset + (width - keywordWidth) / 2;
                for (int nIndx = 0; nIndx < keywordParts; nIndx++) {
                    labels[iControl].Top = (int)(drawYOffset);
                    labels[iControl].Left = (int)(keywordPartOffset);
                    labels[iControl].BringToFront();
                    keywordPartOffset += labels[iControl].Width - (6.0f/fontFactor);
                    iControl++;
                }
                drawYOffset += keywordHeight;
            }
        }

        private static bool[] bBold = new bool[] { false, true, false, true, false };

        public class KeywordDrawData
        {
            public string[] s = new string[Main.keywordParts];
            public CharacterRange[] characterRanges = new CharacterRange[Main.keywordParts];
            public RectangleF[] boundRects = new RectangleF[Main.keywordParts];
            public float boldYOffset, normalYOffset, width;
        }

    }

    public class KeyWord
    {
        public string word;
        public string kana;
        public int kanaHighlightStart;
        public int kanaHighlightLength;
        public string romaji;
        public int romajiHighlightStart;
        public int romajiHighlightLength;
        public string meaning;
    }

    public class Kanji
    {
        public char kanji;
        public string level;
        public string meaning;
        public List<KeyWord> keywords = new List<KeyWord>();
    }

}
