//Importing reqired modules
using ComfyUtils;
using MelonLoader;
using ReMod.Core.Managers;
using ReMod.Core.UI.QuickMenu;
using ReMod.Core.UI.Wings;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.UI;
using static AvatarLogger.Main;
using static BaseFuncs.BaseFuncs;

//Contains all code responsible for creating buttons/in-game Ui elements
namespace Buttons
{
    internal static class Buttons
    {
        private static GameObject SocialMenuInstance;

        public static GameObject GetSocialMenuInstance()
        {
            if (SocialMenuInstance == null)
            {
                SocialMenuInstance = GameObject.Find("UserInterface/MenuContent/Screens");
            }
            return SocialMenuInstance;
        }

        public static Sprite ButtonImage = LoadSpriteFromDisk((Environment.CurrentDirectory + "\\ARESLogo.png"));
        private static ConfigHelper<AvatarLogger.Config> Helper => AvatarLogger.Main.Helper;

        //Creation of buttons within the Ui
        public static void OnUiManagerInit()
        {
            MelonLogger.Msg("Ui initiating...");

            MelonLogger.Msg("ARES Favorites Go BRRR");

            ReMirroredWingMenu WingMenu = ReMirroredWingMenu.Create("ARES", "Open the ARES menu", ButtonImage);
            ReMirroredWingMenu LSMP = WingMenu.AddSubMenu("Log Settings", "Allows you to configure your ARES settings!");
            LSMP.AddToggle("Log Worlds", "Toggles the logging of worlds", delegate (bool b) { Config.LogWorlds = b; }, Config.LogWorlds);
            LSMP.AddToggle("Log Avatars", "Toggles the logging of avatars", delegate (bool b) { Config.LogAvatars = b; }, Config.LogAvatars);
            LSMP.AddToggle("Log Public Avatars", "Toggles the logging of public avatars", delegate (bool b) { Config.LogPublicAvatars = b; }, Config.LogPublicAvatars);
            LSMP.AddToggle("Log Private Avatars", "Toggles the logging of private avatars", delegate (bool b) { Config.LogPrivateAvatars = b; }, Config.LogPrivateAvatars);
            LSMP.AddToggle("Log Own Avatars", "Toggles the logging of own avatars", delegate (bool b) { Config.LogOwnAvatars = b; }, Config.LogOwnAvatars);
            LSMP.AddToggle("Log Friends Avatars", "Toggles the ability to log avatars uploaded to your friends accounts!", delegate (bool b) { Config.LogFriendsAvatars = b; }, Config.LogFriendsAvatars);
            LSMP.AddToggle("Log To Console", "Toggles the ability display logged avatars in console!", delegate (bool b) { Config.LogToConsole = b; }, Config.LogToConsole);
            LSMP.AddToggle("Log Errors To Console", "Toggles the ability display why avatars weren't logged in console!", delegate (bool b) { Config.ConsoleError = b; }, Config.ConsoleError);

            ReMirroredWingMenu FPage = WingMenu.AddSubMenu("Functions", "Use the other features within ARES");
            FPage.AddButton("Open ARES GUI", "Opens the ARES GUI on your desktop!", delegate { OpenGUI(); });
            FPage.AddButton("Copy Instance ID", "Copies the current instance ID to your clipboard!", delegate { Clipboard.SetText(WorldInstanceID); });
            FPage.AddButton("Join Instance By ID", "Joins the instance currently within your clipboard!", delegate { JoinInstanceByID(); });
            FPage.AddButton("Wear Avatar ID", "Changes into avatar ID that is currently in clipboard!", delegate { ChangeAvatar(); });
            FPage.AddButton("Show Logging Statistics", "Displays session statistics within the console", delegate { ShowSessionStats(); });

            ReMirroredWingMenu otherToggles = WingMenu.AddSubMenu("Other", "Use the other features within ARES");
            otherToggles.AddToggle("Stealth Mode", "Hides all in-game indicators that you are running ARES (Reiqires restart!)", delegate (bool b) { Config.Stealth = b; }, Config.Stealth);
            otherToggles.AddToggle("HWID Spoof", "Spoof your HWID incase you've been banned etc!", delegate (bool b) { Config.HWIDSpoof = b; }, Config.HWIDSpoof);
            otherToggles.AddToggle("Auto Update", "Allow the plugin to auto update!", delegate (bool b) { Config.AutoUpdate = b; }, Config.AutoUpdate);
            FPage.AddButton("Restart VRC", "Restarts VRChat!", delegate { RVRC(false); });
            FPage.AddButton("Restart VRC (Persistent)", "Restarts VRChat and re-joins the room you were in!", delegate { RVRC(true); });

            UiManager uiManager = new UiManager("ARES Logger", ButtonImage);

            ReMenuPage LSMPT = null;
            try
            {
                LSMPT = uiManager.MainMenu.AddMenuPage("Logging Settings", "Allows you to configure your ARES settings!");
            }
            catch { }

            LSMPT.AddToggle("Log Worlds", "Toggles the logging of worlds", delegate (bool b) { Config.LogWorlds = b; }, Config.LogWorlds);
            LSMPT.AddToggle("Log Avatars", "Toggles the logging of avatars", delegate (bool b) { Config.LogAvatars = b; }, Config.LogAvatars);
            LSMPT.AddToggle("Log Public Avatars", "Toggles the logging of public avatars", delegate (bool b) { Config.LogPublicAvatars = b; }, Config.LogPublicAvatars);
            LSMPT.AddToggle("Log Private Avatars", "Toggles the logging of private avatars", delegate (bool b) { Config.LogPrivateAvatars = b; }, Config.LogPrivateAvatars);
            LSMPT.AddToggle("Log Own Avatars", "Toggles the logging of own avatars", delegate (bool b) { Config.LogOwnAvatars = b; }, Config.LogOwnAvatars);
            LSMPT.AddToggle("Log Friends Avatars", "Toggles the ability to log avatars uploaded to your friends accounts!", delegate (bool b) { Config.LogFriendsAvatars = b; }, Config.LogFriendsAvatars);
            LSMPT.AddToggle("Log To Console", "Toggles the ability display logged avatars in console!", delegate (bool b) { Config.LogToConsole = b; }, Config.LogToConsole);
            LSMPT.AddToggle("Log Errors To Console", "Toggles the ability display why avatars weren't logged in console!", delegate (bool b) { Config.ConsoleError = b; }, Config.ConsoleError);

            ReMenuPage FPageT = null;
            try
            {
                FPageT = uiManager.MainMenu.AddMenuPage("ARES Functions", "Use the other features within ARES");
            }
            catch { }

            FPageT.AddButton("Open ARES GUI", "Opens the ARES GUI on your desktop!", delegate { OpenGUI(); });
            FPageT.AddButton("Copy Instance ID", "Copies the current instance ID to your clipboard!", delegate { Clipboard.SetText(WorldInstanceID); });
            FPageT.AddButton("Join Instance By ID", "Joins the instance currently within your clipboard!", delegate { JoinInstanceByID(); });
            FPageT.AddButton("Wear Avatar ID", "Changes into avatar ID that is currently in clipboard!", delegate { ChangeAvatar(); });
            FPageT.AddButton("Restart VRC", "Restarts VRChat!", delegate { RVRC(false); });
            FPageT.AddButton("Restart VRC (Persistent)", "Restarts VRChat and re-joins the room you were in!", delegate { RVRC(true); });
            FPageT.AddButton("Show Logging Statistics", "Displays session statistics within the console", delegate { ShowSessionStats(); });
            FPageT.AddToggle("Custom Nameplates", "Shows Custom Nameplates (reload world to fully unload)", delegate (bool b) { Config.CustomNameplates = b; CustomNamePlate(b); }, Config.CustomNameplates);
            FPageT.AddToggle("Stealth Mode", "Hides all in-game indicators that you are running ARES (Reiqires restart!)", delegate (bool b) { Config.Stealth = b; RVRC(true); }, Config.Stealth);
            FPageT.AddToggle("HWID Spoof", "Spoof your HWID incase you've been banned etc!", delegate (bool b) { Config.HWIDSpoof = b; }, Config.HWIDSpoof);
            FPageT.AddToggle("Auto Update", "Allow the plugin to auto update!", delegate (bool b) { Config.AutoUpdate = b; }, Config.AutoUpdate);
            MelonLogger.Msg("Ui ready!");
        }

        //Restarts VRChat
        public static void RVRC(bool persistence)
        {
            new Thread(() =>
            {
                //Kills VRChat
                UnityEngine.Application.Quit();
                Thread.Sleep(2500);
                try
                {
                    //Opens VRChat
                    string cl = Environment.CommandLine;
                    if (cl.Contains("vrchat://launch"))
                    {
                        string launch = cl.Substring(cl.IndexOf("vrchat://launch"));
                        cl = cl.Remove(cl.IndexOf("vrchat://launch"), launch.Contains(" ") ? launch.IndexOf(" ") : launch.Length);
                    }
                    if (persistence) { cl = $"{cl} vrchat://launch?id={RoomManager.field_Internal_Static_ApiWorld_0.id}:{RoomManager.field_Internal_Static_ApiWorldInstance_0.instanceId}"; }
                    Process.Start($"{Environment.CurrentDirectory}\\VRChat.exe", cl);
                }
                catch (Exception) { new Exception(); }
                Process.GetCurrentProcess().Kill();
            })
            {
                IsBackground = true,
                Name = "RestartVRC Thread"
            }.Start();
        }

        public static void CustomNamePlate(bool enable)
        {
            if (enable)
            {
                try
                {
                    foreach (Player player in UnityEngine.Object.FindObjectsOfType<Player>())
                    {

                        AvatarLogger.CustomNameplate nameplate = player.transform.Find("Player Nameplate/Canvas/Nameplate").gameObject.AddComponent<AvatarLogger.CustomNameplate>();
                        nameplate.player = player;

                    }
                }
                catch { }
            }
            else
            {
                foreach (Player player in UnityEngine.Object.FindObjectsOfType<Player>())
                {
                    AvatarLogger.CustomNameplate disabler = player.transform.Find("Player Nameplate/Canvas/Nameplate").gameObject.GetComponent<AvatarLogger.CustomNameplate>();
                    disabler.Dispose();
                }
            }
        }

        //Shows the current logging session statistics
        private static void ShowSessionStats()
        {
            MelonLogger.Msg("-------------------------------------");
            MelonLogger.Msg($"Current Logging Session Stats:");
            MelonLogger.Msg("-------------------------------------");
            MelonLogger.Msg($"Total Logged Avatars: {Pub + Pri}");
            MelonLogger.Msg($"Logged PC Avatars: {PC}");
            MelonLogger.Msg($"Logged Quest Avatars: {Q}");
            MelonLogger.Msg($"Logged Private Avatars: {Pri}");
            MelonLogger.Msg($"Logged Public Avatars: {Pub}");
            MelonLogger.Msg("-------------------------------------");
        }

        //Takes the insatnce ID attached to the clipboard and joins the world!
        private static void JoinInstanceByID()
        {
            string[] ID = Clipboard.GetText().Split(':');
            if (Clipboard.GetText().Contains("wrld"))
            {
                JoinInstance(ID[0], ID[1]);
                MelonLogger.Msg($"Instance joined: {Clipboard.GetText()}");
            }
            else
            {
                MelonLogger.Msg($"Invalid instance ID!");
            }
        }

        //Changes into the avatar based on an avatar ID within the clipboard
        public static void ChangeAvatar()
        {
            Regex Avatar = new Regex("avtr_[0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{12}");
            if (Avatar.IsMatch(Clipboard.GetText()))
            {
                new ApiAvatar { id = Clipboard.GetText() }.Get(new Action<ApiContainer>(x =>
                {
                    GetSocialMenuInstance().transform.Find("Avatar").GetComponent<PageAvatar>().field_Public_SimpleAvatarPedestal_0.field_Internal_ApiAvatar_0 = x.Model.Cast<ApiAvatar>();
                    GetSocialMenuInstance().transform.Find("Avatar").GetComponent<PageAvatar>().ChangeToSelectedAvatar();
                }), new Action<ApiContainer>(x =>
                {
                    MelonLogger.Msg($"Failed to change to avatar: {Clipboard.GetText()} | Error Message: {x.Error}");
                }));
            }
            else
            {
                MelonLogger.Msg($"Invalid Avatar ID!");
            }
        }

        //Launches the ARES GUI from VRChat
        public static void OpenGUI()
        {
            //Kills confliciting instances/applications
            try
            {
                foreach (Process proc in Process.GetProcessesByName("ARES"))
                {
                    proc.Kill();
                    MelonLogger.Msg("Pre-existant ARES closed!");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg("Error closing ARES :\n" + ex.Message);
            }
            try
            {
                foreach (Process proc in Process.GetProcessesByName("HOTSWAP"))
                {
                    proc.Kill();
                    MelonLogger.Msg("Pre-existant HOTSWAP closed!");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg("Error closing HOTSWAP :\n" + ex.Message);
            }
            try
            {
                foreach (Process proc in Process.GetProcessesByName("Unity"))
                {
                    proc.Kill();
                    MelonLogger.Msg("Pre-existant Unity closed!");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg("Error closing Unity :\n" + ex.Message);
            }
            try
            {
                foreach (Process proc in Process.GetProcessesByName("Unity Hub"))
                {
                    proc.Kill();
                    MelonLogger.Msg("Pre-existant Unity Hub closed!");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg("Error closing Unity Hub :\n" + ex.Message);
            }

            try
            {
                foreach (Process proc in Process.GetProcessesByName("AssetRipperConsole"))
                {
                    proc.Kill();
                    MelonLogger.Msg("Pre-existant AssetRipperConsole closed!");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg("Error closing AssetRipperConsole :\n" + ex.Message);
            }
            //Enters the GUI folder and runs it!
            Directory.SetCurrentDirectory(MelonUtils.GameDirectory + "\\GUI\\");
            Process.Start("ARES.exe");
            Directory.SetCurrentDirectory(MelonUtils.GameDirectory);
            MelonLogger.Msg("ARES GUI Launched!");
        }
    }
}