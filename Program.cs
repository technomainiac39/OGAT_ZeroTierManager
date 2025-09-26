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

/// my custom moons netid 6a0ba7a49d9f0931
/// 

/*
class NamedPipeServer
{
    public static bool NoIPAssigned = true;
    public static NamedPipeServerStream server;
    public static bool running = true;
    public static bool wasConnected = false;

    public static void StartServer()
    {
        try
        {
            server = new NamedPipeServerStream("ZeroTierPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            Console.WriteLine("Waiting for client connection...");
            server.WaitForConnection();
            Console.WriteLine("Client connected.");
            wasConnected = true;

           _ = Task.Run(() => PipeListener()); // Start listening without blocking
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pipe Error: {ex.Message}");
        }
    }//000000fd946e5bed

    public static void PipeListener()
    { 
        try
        {
            while (running && server.IsConnected)
            {
               
                string receivedData = ReadFromPipe();
                if (!string.IsNullOrEmpty(receivedData))
                {
                    manageCommands(receivedData);
                }
               
            }
            if(wasConnected == true) { OGAT_ZeroTierManager.node.Stop(); }
            running = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pipe Listener Error: {ex.Message}");
        }
    }

    public static void manageCommands(string command)
    {
        if (command == "quit")
        {
            WriteToPipe("quitting");
            StopServer();
        }
        else if (command == "hello")
        {
            WriteToPipe("hello client");
        }
        else if (command == "ip")
        {
            string ip = OGAT_ZeroTierManager.GetZeroTierIP(OGAT_ZeroTierManager.node);
            //Console.WriteLine("sent ip");
            WriteToPipe(ip);
        }
    }

    public static void StopServer()
    {
        running = false;
        if (server.IsConnected)
        {
            server.Disconnect();
            server.Close();
        }
        Console.WriteLine("Server stopped.");
    }

    static string ReadFromPipe()
    {
        try
        {
            if (!server.IsConnected) return null;
            byte[] buffer = new byte[256];
            int bytesRead = server.Read(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine("Error reading from pipe: " + ex.Message);
        }
        return null;
    }

    public static void WriteToPipe(string message)
    {
        try
        {
            if (!server.IsConnected) return;

            byte[] buffer = Encoding.UTF8.GetBytes(message);
            server.Write(buffer, 0, buffer.Length);
            server.Flush();
            Console.WriteLine($"Sent: {message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine("Error writing to pipe: " + ex.Message);
        }
    }
}

class OGAT_ZeroTierManager
{
    public static ZeroTier.Core.Node node;
    public static ulong NetworkID = 0xF;
    public static string NetID;
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


            NetworkID = UInt64.Parse( netID, System.Globalization.NumberStyles.HexNumber);
            
            Console.WriteLine($"{NetworkID}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Config Read Error: {ex.Message}");
        }
    }

    static async Task Main(string[] args)
    {
        try
        {
            string startupPath = System.IO.Directory.GetCurrentDirectory();
            string pathConfig = startupPath + "\\BepInEx\\plugins\\OGAT_ModdedClient\\OGAT_ZeroTierManager\\ZtManagerNetwork.config";
            Console.WriteLine(pathConfig);
            ReadConfigFile(pathConfig);
            NamedPipeServer.StartServer();
            Console.WriteLine("Starting ZeroTier Proxy...");

            // Initialize the ZeroTier Node
            node = new ZeroTier.Core.Node();
            node.InitAllowNetworkCaching(false);
            node.InitAllowPeerCaching(true);
            node.InitSetEventHandler(OnZeroTierEvent);
            node.InitSetRandomPortRange(40000, 50000);

            // Load Moon file
            //string moonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "0000006a0ba7a49d.moon");
            string moonFilePath = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.moon", SearchOption.TopDirectoryOnly)[0];
            if (!File.Exists(moonFilePath))
            {
                Console.WriteLine("Moon file not found.");
                return;
            }

            byte[] moonData = File.ReadAllBytes(moonFilePath);
            int result = node.InitSetRoots(moonData, moonData.Length);
            Console.WriteLine($"Set Roots Result: {result}");

            node.Start();

            // Wait for ZeroTier connection
            while (!node.Online)
            {
              // Console.WriteLine("Waiting for ZeroTier connection...");          
            }

            Console.WriteLine("Connected to ZeroTier Moon.");
           ulong test = 0x6a0ba7a49d43f63cU;
            if(test == 0x6a0ba7a49d43f63c) { Console.WriteLine("fuck my life"); }
           Console.WriteLine($"test {test} {NetworkID}");
            node.Join(NetworkID);

            while (!node.IsNetworkTransportReady(NetworkID))
            {
                Console.WriteLine("waiting on IP");
                Thread.Sleep(100);
                
            }

            Thread PipeHandler = new Thread(NamedPipeServer.PipeListener);
            PipeHandler.Start();
            while (NamedPipeServer.running)
            {
                Task.Delay(100).Wait();
                Console.WriteLine("Event listening");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static public void OnZeroTierEvent(ZeroTier.Core.Event e)
    {
        Console.WriteLine($"Event: {e.Name}");

        if (e.Code == ZeroTier.Constants.EVENT_NODE_ONLINE)
        {
            Console.WriteLine($"Node is online with ID: {node.Id:x16}");
        }
        if (e.Code == ZeroTier.Constants.EVENT_NETWORK_READY_IP4)
        {
            string zeroTierIP = GetZeroTierIP(node);
            Console.WriteLine($"IP address is: {zeroTierIP}");
            SetupZeroTierNetworking(zeroTierIP);
        }
        if(e.Code == ZeroTier.Constants.EVENT_NETWORK_NOT_FOUND)
        {
            Console.WriteLine($"BROKEN");
        }
    }

    public static string GetZeroTierIP(ZeroTier.Core.Node node)
    {
        foreach (var net in node.Networks)
        {
            foreach (var addr in net.Addresses)
            {
                if (!string.IsNullOrEmpty(addr.ToString()))
                {
                    return addr.ToString();
                }
            }
        }
        return "No IP assigned.";
    }
    /*
    public static string GetZeroTierSubnet(ZeroTier.Core.Node node)
    {
        foreach (var net in node.Networks)
        {
            foreach (var addr in net.Addresses)
            {
                if (addr.Address.ToString().Contains("/")) // Check for CIDR notation
                {
                    string[] parts = addr.Split('/');
                    if (parts.Length == 2)
                    {
                        int cidr = int.Parse(parts[1]);
                        return CIDRToSubnet(cidr);
                    }
                }
            }
        }
        return "255.255.255.0"; // Default if not found
    } /

    /// <summary>
    // trying to assing node ip as AbandonedMutexException local one for the computer
    /// </summary>


    public static void SetupZeroTierNetworking(string ip)
    {
        string subnetMask = "255.255.255.0";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SetupWindowsNetwork(ip, subnetMask);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            SetupLinuxNetwork(ip, subnetMask);
        }
    }

    private static void SetupWindowsNetwork(string ip, string subnet)
    {
        string adapterName = "TAP-Windows Adapter V9";
        if (!IsTAPInstalled()) InstallTAPAdapter();
        RunCommand($"netsh interface ip set address name=\"{adapterName}\" static {ip} {subnet}");
        RunCommand($"route add 192.168.192.0 mask {subnet} {ip} metric 1");
    }

    private static void SetupLinuxNetwork(string ip, string subnet)
    {
        RunCommand($"sudo ip addr add {ip}/{subnet} dev zt0");
        RunCommand($"sudo ip link set zt0 up");
        RunCommand($"sudo ip route add 192.168.192.0/24 dev zt0");
    }

    private static bool IsTAPInstalled()
    {
        return RunCommand("netsh interface show interface").Contains("TAP-Windows Adapter V9");
    }

    static string DownloadTAPInstaller()
    {
        //
        string url = "https://github.com/OpenVPN/tap-windows6/releases/download/9.27.0/tap-windows-9.27.0-I0-amd64.msm"; // Update if needed
        string installerPath = Path.Combine(Path.GetTempPath(), "tap-windows-installer.exe");

        try
        {
            Console.WriteLine($"Downloading TAP installer from {url}...");
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, installerPath);
            }
            Console.WriteLine("Download complete.");
            return installerPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download failed: {ex.Message}");
            return null;
        }
    }
    static void InstallTAPAdapter(string installerPath)
    {
        if (!File.Exists(installerPath))
        {
            Console.WriteLine("Installer file not found!");
            return;
        }

        Console.WriteLine("Running TAP installer...");
        RunCommand($"powershell -Command \"Start-Process -Verb RunAs -FilePath '{installerPath}' -ArgumentList '/S' -Wait\"");
    }

    private static void InstallTAPAdapter()
    {
        Console.WriteLine("TAP adapter not found. Installing...");
        string installerPath = DownloadTAPInstaller();
        if (!string.IsNullOrEmpty(installerPath))
        {
            InstallTAPAdapter(installerPath);
            Thread.Sleep(5000); // Wait for installation to complete
        }
    }

    private static string RunCommand(string command)
    {
        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/c {command}") { RedirectStandardOutput = true, UseShellExecute = false };
        Process process = Process.Start(psi);
        return process.StandardOutput.ReadToEnd();
    }
}
*/
class NamedPipeServer
{
    public static NamedPipeServerStream server;
    public static bool running = true;
    public static bool wasConnected = false;

    public static void StartServer()
    {
        try
        {
            server = new NamedPipeServerStream("ZeroTierPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            Console.WriteLine("Waiting for client connection...");
            server.WaitForConnection();
            Console.WriteLine("Client connected.");
            wasConnected = true;
            _ = Task.Run(() => PipeListener());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pipe Error: {ex.Message}");
        }
    }

    public static void PipeListener()
    {
        try
        {
            while (running && server.IsConnected)
            {
                string receivedData = ReadFromPipe();
                if (!string.IsNullOrEmpty(receivedData))
                {
                    manageCommands(receivedData);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pipe Listener Error: {ex.Message}");
        }
    }

    public static void manageCommands(string command)
    {
        if (command == "quit")
        {
            WriteToPipe("quitting");
            StopServer();
        }
        else if (command == "hello")
        {
            WriteToPipe("hello client");
        }
    }

    public static void StopServer()
    {
        running = false;
        if (server.IsConnected)
        {
            server.Disconnect();
            server.Close();
        }
        Console.WriteLine("Server stopped.");
    }

    static string ReadFromPipe()
    {
        try
        {
            if (!server.IsConnected) return null;
            byte[] buffer = new byte[256];
            int bytesRead = server.Read(buffer, 0, buffer.Length);
            return bytesRead > 0 ? Encoding.UTF8.GetString(buffer, 0, bytesRead) : null;
        }
        catch (IOException ex)
        {
            Console.WriteLine("Error reading from pipe: " + ex.Message);
            return null;
        }
    }

    public static void WriteToPipe(string message)
    {
        try
        {
            if (!server.IsConnected) return;
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            server.Write(buffer, 0, buffer.Length);
            server.Flush();
            Console.WriteLine($"Sent: {message}");
        }
        catch (IOException ex)
        {
            //Console.WriteLine("Error writing to pipe: " + ex.Message);
        }
    }
}

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

