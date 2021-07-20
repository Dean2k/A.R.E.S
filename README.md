# Avatar Logger GUI

Thx to [KeafyIsHere](https://github.com/KeafyIsHere) for the original avatar logging script!

And special thanks to [cassell1337](https://github.com/cassell1337) for willingly being mindfucked with me while making the semi-auto hotswap!

If you have any feedback for us or would like to suggest a feature please use our [FeedbackForm](https://forms.gle/QifnS6ZSa8fse9yF7).

I have gone about validating the latest V6 package with Microsoft, if a false positive is detected please follow the insructions under GUI>Dist>Submission Details. <<This also works as proof that all files are safe as they were manually reviewed by a Microsoft employee!

Allows for easy searching of the VRChat cache folder for a particular avatar that has been previously encountered via the use of a simple GUI:
![IMAGE !](https://i.imgur.com/SxgNGkv.png)
![IMAGE !](https://i.imgur.com/XLhPj6n.png)
![IMAGE !](https://i.imgur.com/8bgq6Rm.png)
![IMAGE !](https://i.imgur.com/43VhZ38.png)
![IMAGE !](https://i.imgur.com/6SaIDA8.png)
![IMAGE !](https://i.imgur.com/GNYVwaE.png)
Features:

	-Logs ALL avatars seen in game regardless of them being private or public!
	
	-An easy to navigate and in my opinion extremely sexy GUI
	
	-Ability to search avatar logs by:
	
		-Avatar Name
		
		-Avatar ID
		
		-Avatar Author
		
		-Uploader Tags
		
	-Ability to delete logs and parses from within the GUI
	
	-Browse the logs with image previews of avatars
	
	-Semi-Automatic Hotswap
	
	-HTML Viewer for easy browsing of larger logs!
	
	-Search by tags: These are the tags given during the upload of the avatar
	
	-Version detection: IF your copy is out of dste your application will now propmt you to upgrade!

    -API support! If you are willing to share your logs with us in return you will gain access to a database containing avatars logged by other users!

Installation:
	
	1. Install Melon Loader(https://github.com/HerpDerpinstine/MelonLoader/releases/latest/download/MelonLoader.Installer.exe).
	
	2. Run VRchat once, this will allow melon to build the necessary mod files
	
	3. Download "Avatar Logger GUI V6.rar" from releases and extract the "Mods" folder into your game directory
	
	4. Place the "GUI" folder wherever it is most convenient
	
	5. Run VRChat and enter some public worlds to create the needed paths/files
	
	6. Run the "Avatar Logger V6.exe" and enjoy!
	
Usage:

	1. On first load you will have to select your log folder within your setting tab, this is stored in your VRChat folder, it will look somthin like this: "F:\SteamLibrary\steamapps\common\VRChat\AvatarLog"
	{The GUI will restart upon setting this!}
	
	2. Press the "Load Avatars" button to scan your log files
	{Every time you log new avatars you will need to press "Load Avatars" to update the GUI}
	
	3. Select your log type and search type, you are now ready to search your logs!
	
	4. If you want to just browse your logs it is as simple as selecting your log types and navigating the logs using the "Previous" and "Next" buttons
	
	5. If you want to take advantage of HTML browsing its as simple as checking the HTML box and whatever you search/load will be generated into a HTML file that will be opened by your default browser! This view will display avatars preview images, avatar ID and status (Blue=Public, Red=Private). Once you find an avatr you like its as simple as unchecking HTML, and searching the avatar ID in the GUI to bring up all its specific information!
	
	5. If the GUI begins to run slightly sluggish,slow or your logs have gotten larger than you'd like them to be it is as simple as pressing the "Delete Logs" button in settings, once deleted logs cannot be recovered!
	
	6. To utilise the Hotswap button first open a blank unity project and import your avatar SDK
	
	7. Login to the VRChat SDK, create a new game object and attach a blank avatar descriptor to the object
	
	8. Build & publish the blank avatar, give it a name, desc and tags before uploading the avatar
	
	9. You may then navigate to the avatar you would like to Hotswap within the GUI
	
	10. Enter the "Content Manager" within the VRChat SDK and copy the ID of your blank avatar and paste it into the "New Avatar ID" box and press Hotswap
	
	11. The avatars VRCA will be automatically downloaded, unpacked, have its avatar ID swapped out with yours and repacked creating a "custom.vrca" file in the "HOTSWAP" folder
	
	12. In Unity press the build and publish button again and when you reach the screen where you can set your avatars name fill out the fields as you would like (Be sure to tick the box at the bottom agreeing to T&Cs!) then navigate to your %temp% folder
	
	13. In your temp folder locate the folder called "DefaultCompany", enter it and then open the folder with the same name as your Unity project
	
	14. Replace the "custom.vrca" file in this folder with the one generated by the GUI and press "Upload"
	Complete! You may now log on to VRChat and switch into the avatar you had just uploaded!
	
Extras:

	-The "ALLOW API" option is disabled by default, if enabled this option will upload your logged avatars to a central database! This will allow us to create a public search of all avatars uploaded to the API!
	
	-The "LogOwnAvatars", "LogFriendsAvatars" and "LogToConsole" all do exactly what they say they do! These setting effect the mod and will reqire a restart of VRChat to take effect!
	

Issues? Open an issue in the "Issues" tab, We will do our best to resolve your issue!
