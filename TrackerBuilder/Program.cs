using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerBuilder
{
    class Program
    {
        const string BuildDir = @"OzBargainTracker\bin\Release";
        static void Main(string[] args)
        {//Major, Minor, Build, Revision
            if (args.Length != 0)
            {
                string CurrentVersion = "";
                StreamReader SR = new StreamReader("Version.txt");
                CurrentVersion = SR.ReadLine();
                SR.Close();
                if (args[0] == "pre-build")
                {
                    string NewVersion = "";
                    Console.WriteLine("Saving the current build of OzBargainTracker to the Releases version...");

                    Console.WriteLine("Update importance [1-4] (1 -- Major, 2 -- Minor, 3 -- Build, 4 -- Revision)");
                    int BuildType = int.Parse(Console.ReadLine());
                    Console.WriteLine("Current Version: " + CurrentVersion);
                    Console.WriteLine("Incrementing the version...");
                    string[] BuildVersions = CurrentVersion.Split('.');
                    int MajorVersion = 0, MinorVersion = 0, BuildVersion = 0, RevisionVersion = 0;
                    MajorVersion = int.Parse(BuildVersions[0]);
                    MinorVersion = int.Parse(BuildVersions[1]);
                    BuildVersion = int.Parse(BuildVersions[2]);
                    RevisionVersion = int.Parse(BuildVersions[3]);
                    switch (BuildType)
                    {
                        case 1://Major
                            MajorVersion++;
                            break;
                        case 2://Minor
                            MinorVersion++;
                            break;
                        case 3://Build
                            BuildVersion++;
                            break;
                        case 4://Revision
                            RevisionVersion++;
                            break;
                    }
                    NewVersion = string.Format("{0}.{1}.{2}.{3}", MajorVersion, MinorVersion, BuildVersion, RevisionVersion);

                    string TrackerMainCode = Encoding.UTF8.GetString(
                            File.ReadAllBytes(@"OzBargainTracker\Forms\TrackerMain.xaml.cs"));

                    TrackerMainCode = TrackerMainCode.Replace(CurrentVersion, NewVersion);

                    StreamWriter SW = new StreamWriter(
                        File.OpenWrite(@"OzBargainTracker\Forms\TrackerMain.xaml.cs"));
                    SW.Write(TrackerMainCode);
                    SW.Close();

                    SW = new StreamWriter(File.OpenWrite("Version.txt"));
                    SW.Write(NewVersion);
                    SW.Close();
                    Console.WriteLine("New Version: " + NewVersion);
                    Console.WriteLine("Version Updated, proceeding with build...");
                    Console.ReadLine();
                }
                if (args[0] == "post-build")
                {

                    Console.WriteLine("Saving the current build to a .zip in Releases");
                    Directory.CreateDirectory("Temp");
                    File.Copy(BuildDir + @"\System.Windows.Interactivity.dll", @"Temp\System.Windows.Interactivity.dll");
                    File.Copy(BuildDir + @"\MahApps.Metro.dll", @"Temp\MahApps.Metro.dll");
                    File.Copy(BuildDir + @"\Icon.png", @"Temp\Icon.png");
                    File.Copy(BuildDir + @"\Icon2.png", @"Temp\Icon2.png");
                    File.Copy(BuildDir + @"\OzBargainTracker.exe", @"Temp\OzBargainTracker.exe");
                    ZipFile.CreateFromDirectory("Temp", CurrentVersion + ".zip");
                    Directory.Delete("Temp", true);
                    File.Move(CurrentVersion + ".zip", @"Releases\" + CurrentVersion + ".zip");
                    Console.WriteLine("Saved build: " + CurrentVersion + ".zip");
                    Console.ReadLine();
                }
            }
        }
    }
}
