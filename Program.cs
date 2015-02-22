using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KanjiScreenSaver
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            LoadConfig();
            if (args.Length > 0)
            {
                if (args[0].ToLower().Trim().Substring(0, 2) == "/s") //show
                {
                    //run the screen saver
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    ShowScreensaver();
                    Application.Run();
                }
                else if (args[0].ToLower().Trim().Substring(0, 2) == "/p") //preview
                {
                    //show the screen saver preview
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Main(new IntPtr(long.Parse(args[1])))); //args[1] is the handle to the preview window
                }
                else if (args[0].ToLower().Trim().Substring(0, 2) == "/c") //configure
                {
                    //display config dialog
                    Config configDialog = new Config();
                    configDialog.Show();
                    Application.Run();
                }
                else //an argument was passed, but it wasn't /s, /p, or /c, so we don't care wtf it was
                {
                    //show the screen saver anyway
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    ShowScreensaver();
                    Application.Run();
                }
            }
            else //no arguments were passed
            {
                //run the screen saver
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ShowScreensaver();
                Application.Run();
            }
        }

        static string LoadRegString(RegistryKey regKey, string key, string def)
        {
            object obj = regKey.GetValue(key, def);
            return (obj!=null) ? (string)obj : "";
        }

        static int LoadRegInt(RegistryKey regKey, string key, int def)
        {
            object obj = regKey.GetValue(key);
            return (obj!=null) ? (int)obj : def;
        }

        static void LoadConfig()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("KanjiScreenSaver\\Settings");
            if (regKey != null)
            {
                string levelSetting = LoadRegString(regKey, "Levels", "100");
                levels = levelSetting.Split(',');
                includeKanjis = LoadRegString(regKey, "IncludeKanjis", "");
                excludeKanjis = LoadRegString(regKey, "ExcludeKanjis", "");
                fontName = LoadRegString(regKey, "FontName", "ＭＳ ゴシック");
                fontFaceName = LoadRegString(regKey, "FontFaceName", "ＭＳ ゴシック");
                duration = LoadRegInt(regKey, "Duration", 5000);
            }
        }

        //will show the screen saver
        static void ShowScreensaver()
        {
            //creates a form just for the primary screen and passes it the bounds of that screen
            Main screensaver = new Main(Screen.PrimaryScreen.Bounds);
            screensaver.Show();
        }

        public static string[] levels;
        public static string includeKanjis;
        public static string excludeKanjis;
        public static string fontName;
        public static string fontFaceName;
        public static int duration;
    }
}
