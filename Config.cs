using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace KanjiScreenSaver
{
    public partial class Config : Form
    {
        public Config()
        {
            InitializeComponent();
        }

        private void Config_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Config_Load(object sender, EventArgs e)
        {
            XmlDocument kanjiData = new XmlDocument();
            kanjiData.LoadXml(Properties.Resources.Kanji);
            HashSet<string> availableLevels = new HashSet<string>();
            foreach (XmlNode kanjiNode in kanjiData.DocumentElement.SelectNodes("kanji"))
            {
                availableLevels.Add(kanjiNode.Attributes["level"].Value);
            }
            GetUsableFonts();
            if (fontCombo.Items.Count == 0) {
                label4.Text = "No suitable fonts available";
            }
            bool bFontSet = false;
            foreach (Control control in Controls)
            {
                if (control.GetType() == typeof(CheckBox) ) {
                    if ( Program.levels.Contains(control.Tag) ) {
                        ((CheckBox)control).CheckState = CheckState.Checked;
                    }
                    if ( availableLevels.Contains(control.Tag) ) {
                        control.Enabled = true;
                    }
                }
            }
            includeKanjis.Text = Program.includeKanjis;
            excludeKanjis.Text = Program.excludeKanjis;
            for ( int i=0; i<fontCombo.Items.Count; i++ ) {
                if (fontCombo.Items[i].ToString() == Program.fontFaceName) {
                    fontCombo.SelectedIndex = i;
                    bFontSet = true;
                    break;
                }
            }
            if ( fontCombo.Items.Count > 0 && !bFontSet) {
                fontCombo.SelectedIndex = 0;
            }
            trackBar1.Value = Program.duration;
            trackBar1_Scroll(trackBar1, null);
        }

        #region Font Enumeraion
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        static extern int EnumFontFamiliesEx(IntPtr hdc,
                                        [In] IntPtr pLogfont,
                                        EnumFontExDelegate lpEnumFontFamExProc,
                                        IntPtr lParam,
                                        uint dwFlags);

        public class FontComboBoxItem
        {
            public string fontName;
            public string fontFaceName;
            public int fontWeight;
            public FontComboBoxItem(string fontName, string fontFaceName, int fontWeight)
            {
                this.fontName = fontName;
                this.fontFaceName = fontFaceName;
                this.fontWeight = fontWeight;
            }

            // override ToString() function
            public override string ToString()
            {
                return this.fontFaceName;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class LOGFONT
        {

            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public FontWeight lfWeight;
            [MarshalAs(UnmanagedType.U1)]
            public bool lfItalic;
            [MarshalAs(UnmanagedType.U1)]
            public bool lfUnderline;
            [MarshalAs(UnmanagedType.U1)]
            public bool lfStrikeOut;
            public FontCharSet lfCharSet;
            public FontPrecision lfOutPrecision;
            public FontClipPrecision lfClipPrecision;
            public FontQuality lfQuality;
            public FontPitchAndFamily lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string lfFaceName;
        }

        public enum FontWeight : int
        {
            FW_DONTCARE = 0,
            FW_THIN = 100,
            FW_EXTRALIGHT = 200,
            FW_LIGHT = 300,
            FW_NORMAL = 400,
            FW_MEDIUM = 500,
            FW_SEMIBOLD = 600,
            FW_BOLD = 700,
            FW_EXTRABOLD = 800,
            FW_HEAVY = 900
        }

        public enum FontCharSet : byte
        {
            SHIFTJIS_CHARSET = 128
        }

        public enum FontPrecision : byte
        {
            OUT_DEFAULT_PRECIS = 0
        }

        public enum FontClipPrecision : byte
        {
            CLIP_DEFAULT_PRECIS = 0
        }

        public enum FontQuality : byte
        {
            DEFAULT_QUALITY = 0
        }

        [Flags]
        public enum FontPitchAndFamily : byte
        {
            FF_DONTCARE = (0 << 4)
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct NEWTEXTMETRIC
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
            int ntmFlags;
            int ntmSizeEM;
            int ntmCellHeight;
            int ntmAvgWidth;
        }

        public struct FONTSIGNATURE
        {
            [MarshalAs(UnmanagedType.ByValArray)]
            int[] fsUsb;
            [MarshalAs(UnmanagedType.ByValArray)]
            int[] fsCsb;
        }

        public struct NEWTEXTMETRICEX
        {
            NEWTEXTMETRIC ntmTm;
            FONTSIGNATURE ntmFontSig;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct ENUMLOGFONTEX
        {
            public LOGFONT elfLogFont;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string elfFullName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string elfStyle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string elfScript;
        }

        private void GetUsableFonts()
        {
            LOGFONT lf = CreateLogFont("");

            IntPtr plogFont = Marshal.AllocHGlobal(Marshal.SizeOf(lf));
            Marshal.StructureToPtr(lf, plogFont, true);

            int ret = 0;
            try
            {
                Graphics G = CreateGraphics();
                IntPtr P = G.GetHdc();

                del = new EnumFontExDelegate(cb);
                ret = EnumFontFamiliesEx(P, plogFont, del, IntPtr.Zero, 0);

                G.ReleaseHdc(P);
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine("Error!");
            }
            finally
            {
                Marshal.DestroyStructure(plogFont, typeof(LOGFONT));

            }
        }

        public delegate int EnumFontExDelegate(ref ENUMLOGFONTEX lpelfe, ref NEWTEXTMETRICEX lpntme, int FontType, int lParam);
        public EnumFontExDelegate del;
        private static readonly string[] sysFonts = { "System", "Terminal", "FixedSys", "Small Fonts" };

        public int cb(ref ENUMLOGFONTEX lpelfe, ref NEWTEXTMETRICEX lpntme, int FontType, int lParam)
        {
            int cnt = 0;
            try
            {
                if (lpelfe.elfFullName[0] != '@' && !sysFonts.Contains(lpelfe.elfFullName) && lpelfe.elfLogFont.lfFaceName[0] != '@' )
                {
                    FontComboBoxItem fontItem = new FontComboBoxItem(lpelfe.elfFullName, lpelfe.elfLogFont.lfFaceName, (int)lpelfe.elfLogFont.lfWeight);
                    fontCombo.Items.Add(fontItem);
                }
                cnt = 1;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e.ToString());
            }
            return cnt;
        }

        public static LOGFONT CreateLogFont(string fontname)
        {
            LOGFONT lf = new LOGFONT();
            lf.lfHeight = 0;
            lf.lfWidth = 0;
            lf.lfEscapement = 0;
            lf.lfOrientation = 0;
            lf.lfWeight = 0;
            lf.lfItalic = false;
            lf.lfUnderline = false;
            lf.lfStrikeOut = false;
            lf.lfCharSet = FontCharSet.SHIFTJIS_CHARSET;
            lf.lfOutPrecision = 0;
            lf.lfClipPrecision = 0;
            lf.lfQuality = 0;
            lf.lfPitchAndFamily =  FontPitchAndFamily.FF_DONTCARE;
            lf.lfFaceName = "";
            return lf;
        }
        #endregion

        private void fontCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox) sender;
            FontComboBoxItem fontItem = (FontComboBoxItem)comboBox.SelectedItem;
            label4.Font = new Font(fontItem.fontFaceName, label4.Font.Size);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("KanjiScreenSaver\\Settings", true);
            if (regKey==null)
            {
                regKey = Registry.CurrentUser.CreateSubKey("KanjiScreenSaver\\Settings");
            }
            string levelSetting = "";
            foreach (Control control in Controls)
            {
                if (control.GetType() == typeof(System.Windows.Forms.CheckBox) && ((CheckBox)control).CheckState == CheckState.Checked)
                {
                    if (levelSetting.Length > 0)
                    {
                        levelSetting += ",";
                    }
                    levelSetting += control.Tag;
                }
            }
            regKey.SetValue("Levels", levelSetting, RegistryValueKind.String);
            regKey.SetValue("IncludeKanjis", includeKanjis.Text, RegistryValueKind.String);
            regKey.SetValue("ExcludeKanjis", excludeKanjis.Text, RegistryValueKind.String);
            regKey.SetValue("FontName", ((FontComboBoxItem)(fontCombo.SelectedItem)).fontName, RegistryValueKind.String);
            regKey.SetValue("FontFaceName", ((FontComboBoxItem)(fontCombo.SelectedItem)).fontFaceName, RegistryValueKind.String);
            regKey.SetValue("Duration", trackBar1.Value, RegistryValueKind.DWord);
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label5.Text = (((TrackBar)sender).Value/1000.0).ToString("F1") + "s";
        }
    }
}
