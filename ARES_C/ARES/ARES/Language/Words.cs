using ARES.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARES.Language
{
    public class Words
    {
        public static IniFile languageFile { get; set; }

        #region Tabs
        public static string TabNames { get; set; }
        public static string TabMain { get; set; }
        public static string TabSettings { get; set; }
        public static string TabUtility { get; set; }
        public static string TabAbout { get; set; }
        public static string TabAvatarComments { get; set; }
        #endregion
        #region Main Tab
        public static string MainBody { get; set; }
        public static string SearchTerm { get; set; }
        public static string SearchType { get; set; }
        public static string Filter { get; set; }
        public static string Public { get; set; }
        public static string Private { get; set; }
        public static string PC { get; set; }
        public static string Quest { get; set; }
        public static string PinLocked { get; set; }
        public static string SearchAPI { get; set; }
        public static string SearchLocal { get; set; }
        public static string StopSearch { get; set; }
        public static string SearchRipped { get; set; }
        public static string LoadFavorites { get; set; }
        public static string ToggleFavorite { get; set; }
        public static string Count { get; set; }
        public static string PCVersion { get; set; }
        public static string QuestVersion { get; set; }
        public static string ResetScene { get; set; }
        public static string OpenUnity { get; set; }
        public static string HotSwap { get; set; }
        public static string BrowserView { get; set; }
        #endregion       

        public static void SetLanguage(string language)
        {
            if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\Language\"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Language\");
            }
            if (language == "english")
            {
                languageFile = new IniFile(Directory.GetCurrentDirectory() + @"\Language\english.ini");
            }
            else if (language == "spanish")
            {
                languageFile = new IniFile(Directory.GetCurrentDirectory() + @"\Language\spanish.ini");
            }
            else
            {
                languageFile = new IniFile(Directory.GetCurrentDirectory() + @"\Language\english.ini");
            }
        }

        public static string CheckWrite(string checkString, string section, string defaultString)
        {
            string text = languageFile.Read(nameof(checkString), section);
            if (text == "")
            {
                languageFile.Write(checkString, defaultString, section);
                return defaultString;
            }
            else
            {
                return text;
            }
        }

        public static void LoadTabNames(Main main)
        {
            main.mTabMain.Text = CheckWrite(nameof(TabMain), nameof(TabNames), "Main");
            main.mTabSettings.Text = CheckWrite(nameof(TabSettings), nameof(TabNames), "Settings");
            main.metroTabPage3.Text = CheckWrite(nameof(TabUtility), nameof(TabNames), "Utility");
            main.metroTabPage2.Text = CheckWrite(nameof(TabAbout), nameof(TabNames), "About");
            main.mtAvatar.Text = CheckWrite(nameof(TabAvatarComments), nameof(TabNames), "Avatar Comments");
        }

        public static void LoadMainTabLang(Main main)
        {
            main.lblCount.Text = CheckWrite(nameof(Count), nameof(MainBody), "Count");
            main.lblSearchTerm.Text = CheckWrite(nameof(SearchTerm), nameof(MainBody), "Search Term");
            main.lblSearchType.Text = CheckWrite(nameof(SearchType), nameof(MainBody), "Search Type");
            main.lblFilter.Text = CheckWrite(nameof(Filter), nameof(MainBody), "Filter");
            main.lblPCVersion.Text = CheckWrite(nameof(PCVersion), nameof(MainBody), "PC Version");
            main.lblQuestVersion.Text = CheckWrite(nameof(QuestVersion), nameof(MainBody), "Quest Version");
            main.chkPublic.Text = CheckWrite(nameof(Public), nameof(MainBody), "Public");
            main.chkPrivate.Text = CheckWrite(nameof(Private), nameof(MainBody), "Private");
            main.chkPC.Text = CheckWrite(nameof(PC), nameof(MainBody), "PC");
            main.chkQuest.Text = CheckWrite(nameof(Quest), nameof(MainBody), "Quest");
            main.chkPin.Text = CheckWrite(nameof(PinLocked), nameof(MainBody), "Pin Locked");
            main.btnSearch.Text = CheckWrite(nameof(SearchAPI), nameof(MainBody), "Search API");
            main.btnSearchLocal.Text = CheckWrite(nameof(SearchLocal), nameof(MainBody), "Search Local");
            main.btnStopSearch.Text = CheckWrite(nameof(StopSearch), nameof(MainBody), "Stop Search");
            main.btnRipped.Text = CheckWrite(nameof(SearchRipped), nameof(MainBody), "Search Ripped");
            main.btnSearchFavorites.Text = CheckWrite(nameof(LoadFavorites), nameof(MainBody), "Load Favorites");
            main.btnToggleFavorite.Text = CheckWrite(nameof(ToggleFavorite), nameof(MainBody), "Toggle Favorite");
        }
    }
}
