using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using Newtonsoft.Json.Linq;
using System.IO.Compression;

namespace WindowsGSM.Plugins
{
    public class RCT2 
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.RCT2", // WindowsGSM.XXXX
            author = "Andy",
            description = "WindowsGSM plugin for supporting RCT2",
            version = "1.0",
            url = "https://github.com/Kickbut101/WindowsGSM.RCT2", // Github repository link (Best practice)
            color = "#800080" // Color Hex
        };



        // - Standard Constructor and properties

        public RCT2(ServerConfig serverData) => _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public string StartPath => @"openrct2.exe"; // Game server start path
        public string FullName = "RCT2 Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "11753"; // Default port // Need UDP and TCP
        public string QueryPort = "11753"; // Default query port
        public string Defaultmap = "DefaultWorld"; // Default World Name
        public string Maxplayers = "8"; // Default maxplayers
        public string Additional = ""; // Additional server start parameter

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public async void CreateServerCFG()
        {}

        public async Task<Process> Start()
        {
            string shipWorkingPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID); // c:\windowsgsm\servers\1\serverfiles\
            string rct2EXEPath = Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath)); // c:\windowsgsm\servers\1\serverfiles\openrct2.exe
            //string terrariaServerMapPath = Path.Combine(shipWorkingPath, "Worlds", $"{_serverData.ServerMap}.wld"); // c:\windowsgsm\servers\1\serverfiles\Worlds\DefaultWorld.wld

            //terrariaServerMapPath = terrariaServerMapPath.Replace(@"\","/"); // Flip the backslashes for forwards slashes. Unsure if this was necessary.

            // Does .exe path exist?
            if (!File.Exists(rct2EXEPath))
            {
                Error = $"{Path.GetFileName(rct2EXEPath)} not found in ({_serverData.ServerID})";
                return null;
            }

            // Prepare start parameters
            var param = new StringBuilder();
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $"{_serverData.ServerParam}");

            // Output the startupcommands used. Helpful for troubleshooting server commands and testing them out - leaving this in because it's helpful af.
            var startupCommandsOutputTxtFile = ServerPath.GetServersServerFiles(_serverData.ServerID, "startupCommandsUsed.log");
            File.WriteAllText(startupCommandsOutputTxtFile, $"{param}");


            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    WorkingDirectory = shipWorkingPath,
                    FileName = rct2EXEPath,
                    Arguments = param.ToString()
                },
                EnableRaisingEvents = true
            };


            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;

            }
            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }
        // Stop process with commands for stopping servers (this actually right now doesn't work, server will not stop gracefully. Embedded console option doesn't seem to affect this behavior)
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p.StartInfo.RedirectStandardInput)
                {
                    p.StandardInput.WriteLine("exit");
                }
                else
                {
                    Functions.ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "exit");
                }
            });
        }

        public async Task<Process> Install()
        {
            try
            {
                    // Set webclient object
                    WebClient webClient = new WebClient();

                    // Set the working path again - for this context
                    string shipWorkingPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID); // c:\windowsgsm\servers\1\serverfiles\

                    // Base url for RCT2 info/webpage
                    string html = await webClient.DownloadStringTaskAsync("https://openrct2.io/");

                    // Regex pattern for pulling the download file location/address
                    Regex regexFullString = new Regex(@"href=.(.+?).>.ownload .atest .elease"); // href="https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip">Download Latest Release
                    Match matches1 = regexFullString.Match(html);

                    // Save group 1 as URLFull
                    string URLFull = matches1.Groups[1].Value; // https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip

                    // We need to grab the file name from within the URL with regex
                    Regex regexFileName = new Regex(@".*\/(.*?\.zip)"); // https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip
                    Match matches2 = regexFileName.Match(URLFull);

                    // Save group 1 as newestDownloadFileName
                    string newestDownloadFileName = matches2.Groups[1].Value; // OpenRCT2-0.3.3-windows-portable-x64.zip

                    // We need to grab the file version of what we are downloading (for later)
                    Regex regexVersionNumber = new Regex(@"\/v([\d\.]+)\/"); // https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip
                    Match matches3 = regexVersionNumber.Match(URLFull);

                    // Save group 1 as versionWeJustDownloaded
                    string versionWeJustDownloaded = matches3.Groups[1].Value; // 0.3.3

                    //Set path/directory for downloaded files
                    string downloadDir = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "downloadDIR"); // c:\windowsgsm\servers\1\serverfiles\downloadDIR

                    // Delete all files inside the extractdir (This command kills anything inside and remakes the dir)
                    Directory.CreateDirectory(downloadDir);

                    // Combined path for zip file destination based on build name
                    string zipPath = Path.Combine(downloadDir, newestDownloadFileName); // c:\windowsgsm\servers\1\serverfiles\downloadDIR\OpenRCT2-0.3.3-windows-portable-x64.zip

                    // Download .zip file to specified dir
                    await webClient.DownloadFileTaskAsync($"{URLFull}", zipPath);

                    // Extract files to c:\windowsgsm\servers\1\serverfiles\
                    await Task.Run(async () =>
                    {
                        try
                        {
                            await FileManagement.ExtractZip(zipPath, shipWorkingPath);
                        }
                        catch
                        {
                            Error = "Path too long";
                        }
                    });

                    // Delete zip file downloaded from website
                    await Task.Run(() => File.Delete(zipPath));

                    // Create file that houses the version last downloaded.
                    var versionFile = ServerPath.GetServersServerFiles(_serverData.ServerID, "rct2_version.txt");
                    File.WriteAllText(versionFile, $"{versionWeJustDownloaded}");

                return null;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null;
            }
        }


        // Fully update necessary server files to newest version (this typically happens after the GetRemoteBuild and GetLocalBuild tasks)
        public async Task<Process> Update()
        {
             try
            {
                    // Set webclient object
                    WebClient webClient = new WebClient();

                    // Set the working path again - for this context
                    string shipWorkingPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID); // c:\windowsgsm\servers\1\serverfiles\

                    // Base url for RCT2 info/webpage
                    string html = await webClient.DownloadStringTaskAsync("https://openrct2.io/");

                    // Regex pattern for pulling the download file location/address
                    Regex regexFullString = new Regex(@"href=.(.+?).>.ownload .atest .elease"); // href="https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip">Download Latest Release
                    Match matches1 = regexFullString.Match(html);

                    // Save group 1 as URLFull
                    string URLFull = matches1.Groups[1].Value; // https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip

                    // We need to grab the file name from within the URL with regex
                    Regex regexFileName = new Regex(@".*\/(.*?\.zip)"); // https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip
                    Match matches2 = regexFileName.Match(URLFull);

                    // Save group 1 as newestDownloadFileName
                    string newestDownloadFileName = matches2.Groups[1].Value; // OpenRCT2-0.3.3-windows-portable-x64.zip

                    // We need to grab the file version of what we are downloading (for later)
                    Regex regexVersionNumber = new Regex(@"\/v([\d\.]+)\/"); // https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip
                    Match matches3 = regexVersionNumber.Match(URLFull);

                    // Save group 1 as versionWeJustDownloaded
                    string versionWeJustDownloaded = matches3.Groups[1].Value; // 0.3.3

                    //Set path/directory for downloaded files
                    string downloadDir = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "downloadDIR"); // c:\windowsgsm\servers\1\serverfiles\downloadDIR

                    // Delete all files inside the downloadDir (This command kills anything inside and remakes the dir)
                    Directory.CreateDirectory(downloadDir);

                    // Combined path for zip file destination based on build name
                    string zipPath = Path.Combine(downloadDir, newestDownloadFileName); // c:\windowsgsm\servers\1\serverfiles\downloadDIR\OpenRCT2-0.3.3-windows-portable-x64.zip

                    // Download .zip file to specified dir
                    await webClient.DownloadFileTaskAsync($"{URLFull}", zipPath);

                    //Set path/directory for extraction of zip files ( I was trying ot avoid this, but the extraction method used below wasn't overwriting files as needed)
                    string extractdir = Path.Combine(downloadDir, "extractedDir"); // c:\windowsgsm\servers\1\serverfiles\downloadDIR\extractedDir
                    Directory.CreateDirectory(extractdir); // make path

                    // Extract files to c:\windowsgsm\servers\1\serverfiles\downloadDIR\extractedDir
                    await Task.Run(async () =>
                    {
                        try
                        {
                            await FileManagement.ExtractZip(zipPath, extractdir);
                        }
                        catch
                        {
                            Error = "Path too long";
                        }
                    });

                    // Delete zip file downloaded from website
                    await Task.Run(() => File.Delete(zipPath));

                    // Copy files from the extracted directory to the main file directory within windowsGSM (c:\windowsgsm\servers\1\serverfiles)
                    foreach (var file in Directory.GetFiles(extractdir))
                        File.Copy(file, Path.Combine(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID), Path.GetFileName(file)), true);

                    // Create file that houses the version last downloaded.
                    var versionFile = ServerPath.GetServersServerFiles(_serverData.ServerID, "rct2_version.txt");
                    File.WriteAllText(versionFile, $"{versionWeJustDownloaded}");

                    // Delete the directory for extractedfiles
                    Directory.Delete(extractdir,true);

                return null;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null;
            }
        }


        // Verify that files needed to run server are present after gold/fresh install
        public bool IsInstallValid()
        {
            string exePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath); // c:\windowsgsm\servers\1\serverfiles\openrct2.exe
            return File.Exists(exePath);
        }


        // Verify that files needed to run server are present after import
        public bool IsImportValid(string path)
        {
            string exePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath); // c:\windowsgsm\servers\1\serverfiles\openrct2.exe
            return File.Exists(exePath);
        }


        // Read files to ascertain what version is currently installed
        public string GetLocalBuild()
        {
            // Get local version and build by rct2_version.txt
            const string VERSION_TXT_FILE = "rct2_version.txt"; // should contain something like "0.3.3"
            var versionTxtFile = ServerPath.GetServersServerFiles(_serverData.ServerID, VERSION_TXT_FILE);
            if (!File.Exists(versionTxtFile))
            {
                Error = $"{VERSION_TXT_FILE} does not exist";
                return string.Empty;
            }
            var fileContents = File.ReadAllText(versionTxtFile);
            return $"{fileContents}";
        }


        // Read online sources to determine newest publically available version
        public async Task<string> GetRemoteBuild()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string htmlstring = await webClient.DownloadStringTaskAsync("https://openrct2.io/");

                    // Regex pattern for pulling the download file location/address
                    Regex regexFullString = new Regex(@"href=.(.+?).>.ownload .atest .elease"); // href="https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip">Download Latest Release
                    Match matches1 = regexFullString.Match(htmlstring);

                    // Save group 1 as URLFull
                    string URLFull = matches1.Groups[1].Value; // https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip

                    // We need to grab the file name from within the URL with regex
                    Regex regexFileName = new Regex(@".*\/(.*?\.zip)"); // https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip
                    Match matches2 = regexFileName.Match(URLFull);

                    // Save group 1 as newestDownloadFileName
                    string newestDownloadFileName = matches2.Groups[1].Value; // OpenRCT2-0.3.3-windows-portable-x64.zip

                    // We need to grab the file version of what we are downloading (for later)
                    Regex regexVersionNumber = new Regex(@"\/v([\d\.]+)\/"); // https://github.com/OpenRCT2/OpenRCT2/releases/download/v0.3.3/OpenRCT2-0.3.3-windows-portable-x64.zip
                    Match matches3 = regexVersionNumber.Match(URLFull);

                    // Save group 1 as versionWeJustDownloaded
                    string versionWeJustDownloaded = matches3.Groups[1].Value; // 0.3.3

                    return versionWeJustDownloaded;
                }
            }
            catch
            {
                Error = $"Fail to get remote version";
                return string.Empty;
            }

        }
    }
}