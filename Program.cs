// See https://aka.ms/new-console-template for more information
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ZeroTier;
using ZeroTier.Core;


class ZeroTierManager
{
    static string ZeroTierInstallerURL = "https://download.zerotier.com/dist/ZeroTier%20One.msi";
    static string ZeroTierPath = "C:\\Program Files (x86)\\ZeroTier\\One\\zerotier-cli.exe";
    static string NetID;
    static List<string> moonID= new List<string>();
    public static ulong NetworkID = 0xF;
    public static bool usingMoons;

    static bool IsRunAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    static void Main(string[] args)
    {
        
        if (!IsRunAsAdmin())
        {
            // Relaunch as admin
            var exeName = Process.GetCurrentProcess().MainModule.FileName;
            var startInfo = new ProcessStartInfo(exeName)
            {
                UseShellExecute = true,
                Verb = "runas" // Triggers UAC
            };

            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                Console.WriteLine("This program requires administrator privileges.");
            }
            
            return; // Exit current non-admin instance
        }
        //NamedPipeServer.StartServer();
        Console.WriteLine("Starting ZeroTier Manager...");

        if (!File.Exists(ZeroTierPath)) InstallZeroTier();
        ConfigureZeroTier();

        Console.WriteLine("Press any key to exit ONCE YOU HAVE CLOSED OGAT...");
        Console.ReadKey();
        RunCommand($"zerotier-cli leave {NetID}");
        foreach(string moon in moonID) 
        {
            RunCommand($"zerotier-cli deorbit {moon}");
        }
        RunCommand("sc stop ZeroTierOne");

    }

    static bool IsZeroTierInstalled()
    {
        // Check if the ZeroTier installation files are present
        string installPath = "C:\\Program Files (x86)\\ZeroTier\\One";

        // Check if the ZeroTier folder exists
        return Directory.Exists(installPath);
    }

    static void InstallZeroTier()
    {
        string zeroTierServiceName = "ZeroTierOne"; // Name of the ZeroTier service

        // Check if ZeroTier service is already installed and running
        if (IsZeroTierInstalled())
        {
            Console.WriteLine("ZeroTier is already installed.");
            return; // Skip installation if it's already installed
        }

        string startupPath = System.IO.Directory.GetCurrentDirectory();
        //string installerPath = startupPath + "\\BepInEx\\plugins\\OGAT_ModdedClient\\OGAT_ZeroTierManager\\ZeroTierOne.msi";
        string installerPath = startupPath + "\\ZeroTierOne.msi";
        Console.WriteLine("Downloading ZeroTier...");
        using (var client = new WebClient()) client.DownloadFile(ZeroTierInstallerURL, installerPath);
        Console.WriteLine("Installing ZeroTier...");

        Process installProcess = Process.Start("msiexec.exe", $"/i \"{installerPath}\" /norestart");
        installProcess.WaitForExit();
        Console.WriteLine("ZeroTier installation completed.");
    }

    static void ConfigureZeroTier()
    {
        RunCommand("sc stop ZeroTierOne");
        RunCommand("sc start ZeroTierOne");
        string startupPath = System.IO.Directory.GetCurrentDirectory();
        //string moonPath = startupPath;
        string pathConfig = startupPath + "\\BepInEx\\plugins\\OGAT_ModdedClient\\OGAT_ZeroTierManager\\ZtManagerNetwork.config";
        //string pathConfig = startupPath + "\\ZtManagerNetwork.config";
        Console.WriteLine(pathConfig);
        ReadConfigFile(pathConfig);

        if (usingMoons)
        {
            string moonPath = startupPath + "\\BepInEx\\plugins\\OGAT_ModdedClient\\OGAT_ZeroTierManager\\";
            if (Directory.Exists("C:\\ProgramData\\ZeroTier\\One\\moons.d"))
            {
                string fileExtension = "*.moon"; // Change to your desired extension

                try
                {
                    // Get all files of the specified type in source (recursive)
                    string[] files = Directory.GetFiles(moonPath, fileExtension, SearchOption.AllDirectories);

                    foreach (string filePath in files)
                    {
                        string fileName = Path.GetFileName(filePath);
                        string destFilePath = Path.Combine("C:\\ProgramData\\ZeroTier\\One\\moons.d", fileName);

                        // If file already exists at destination, overwrite it
                        File.Copy(filePath, destFilePath, overwrite: true);

                        Console.WriteLine($"Moved: {fileName}");

                        string nameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                        string trimmed = nameWithoutExtension.TrimStart('0');
                        moonID.Append(trimmed);
                        Console.WriteLine($"try orbit {trimmed}");
                        Console.WriteLine(RunCommand($"zerotier-cli orbit {trimmed} {trimmed}"));
                        Console.WriteLine("orbited, restarting zerotier");
                        RunCommand("net stop ZeroTierOneService");
                        RunCommand("net start ZeroTierOneService");
                    }

                    Console.WriteLine("All files moved successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            string moons = RunCommand($"{ZeroTierPath} listmoons");
            Console.WriteLine(moons);
        }
        else
        {
            RunCommand("net stop ZeroTierOneService");
            RunCommand("net start ZeroTierOneService");
        }

        Console.WriteLine($"Joining ZeroTier network {NetID}...");
        RunCommand($"zerotier-cli join {NetID}");
    }

    public static void ReadConfigFile(string configPath)
    {
        //if(ConfigurationManager.AppSettings["networkID"] == null) { Console.WriteLine("ERROR READING CONFIG"); return; }
        try
        {
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Config file not found: {configPath}");
                return;
            }

            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = configPath;

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;

            if (settings["netID"] == null)
            {
                Console.WriteLine("ERROR: netID not found in config file.");
                return;
            }

            string netID = settings["netID"].Value;
            NetID = netID;
            Console.WriteLine($"Read netID: {netID}");


            NetworkID = UInt64.Parse(netID, System.Globalization.NumberStyles.HexNumber);

            Console.WriteLine($"{NetID}");

            if (settings["usingMoons"] == null)
            {
                Console.WriteLine("ERROR: usingMoons not found in config file.");
                return;
            }
            if (settings["usingMoons"].Value == "true")
            {
                usingMoons = true;
            }
            else
            {
                usingMoons = false;
            }
            Console.WriteLine($"read usingMoons: {settings["usingMoons"].Value}");
            Console.WriteLine($"{usingMoons}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Config Read Error: {ex.Message}");
        }
    }

    static string GetZeroTierIP()
    {
        string output = RunCommand($"{ZeroTierPath} listnetworks");
        foreach (var line in output.Split('\n'))
        {
            if (line.Contains(NetID) && line.Contains("OK"))
            {
                string[] parts = line.Split();
                return parts[parts.Length - 1]; // Last column should be IP
            }
        }
        return "No IP assigned.";
    }


    static string RunCommand(string command)
    {
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result;
    }
}

