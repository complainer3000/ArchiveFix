using RageLib.GTA5.Archives;
using RageLib.GTA5.ArchiveWrappers;
using RageLib.GTA5.Cryptography;
using RageLib.GTA5.Cryptography.Helpers;
using RageLib.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

#nullable disable
namespace ArchiveFix
{
    internal class Program
    {
        private static bool IsInvokedFromConsole => Program.GetConsoleProcessList(new uint[2], 2U) > 1U;

        private static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.Unicode;
                Func<int, string> getArg = (Func<int, string>)(idx => args.Length > idx ? args[idx] : string.Empty);
                Dictionary<string, Action> dictionary = new Dictionary<string, Action>()
        {
          {
            "fix",
            (Action) (() => Program.FixArchive(getArg(1)))
          },
          {
            "fetch",
            new Action(Program.FetchKeys)
          },
          {
            "buildHashTables",
            new Action(Program.BuildHashTables)
          }
        };
                string key = getArg(0).ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(key) && File.Exists(args[0]))
                {
                    Console.WriteLine("Oh, you're one of these people who... drags files to executables. What heresy. Just for you, we manipulated the code to handle that, though!");
                    Console.WriteLine("That's how Affluent Fix makes defense from communism accessible to everyone, even dimwits!");
                    Console.WriteLine();
                    if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "gtav_ng_encrypt_luts.dat")))
                    {
                        getArg = (Func<int, string>)(idx => args[0]);
                        key = "fix";
                    }
                    else
                    {
                        Console.WriteLine("... you'll have to run the `fetch' command first, though. You have 0 cryptokeys!");
                        Console.WriteLine();
                    }
                }
                Action action;
                if (!dictionary.TryGetValue(key, out action))
                {
                    int foregroundColor = (int)Console.ForegroundColor;
                    Console.Write(string.Format("Invalid command '{0}'. Try one of these instead: {1}", (object)key, (object)string.Join(" .:. ", dictionary.Keys.Select<string, string>((Func<string, string>)(a => string.Format("[{0}]", (object)a))))));
                    Console.WriteLine();
                }
                else
                    action();
            }
            finally
            {
                if (!Program.IsInvokedFromConsole)
                {
                    Console.WriteLine();
                    Console.WriteLine("Press the `any' key to exit.");
                    Console.ReadKey();
                }
            }
        }

        private static void FetchKeys()
        {
            Process process = ((IEnumerable<Process>)Process.GetProcessesByName("GTA5")).FirstOrDefault<Process>();
            bool flag = false;
            if (process == null)
            {
                process = ((IEnumerable<Process>)Process.GetProcessesByName("FiveM")).FirstOrDefault<Process>((Func<Process, bool>)(a => !string.IsNullOrWhiteSpace(a.MainWindowTitle) && a.MainWindowTitle.Contains("Auto V")));
                flag = true;
                if (process == null)
                {
                    process = ((IEnumerable<Process>)Process.GetProcessesByName("FiveReborn")).FirstOrDefault<Process>((Func<Process, bool>)(a => !string.IsNullOrWhiteSpace(a.MainWindowTitle) && a.MainWindowTitle.Contains("Auto V")));
                    if (process == null)
                    {
                        Console.WriteLine("Hey, mate, listen up. There's a time for reason, and a time for stupidity.");
                        Console.WriteLine("Now is neither. Run Grand Theft Auto V. Then try doing whatever it is you were doing again.");
                        return;
                    }
                    Console.WriteLine("mumble, mumble, you're using a FiveM ripoff, good for you I guess, you better own a license to the game, or you should be executed by firing squad...");
                }
                else
                    Console.WriteLine("FiveM, eh? didn't that get shut down?");
            }
            string path = process.MainModule.FileName;
            if (flag)
            {
                path = Path.Combine(((IEnumerable<string>)File.ReadAllLines(Path.Combine(Path.GetDirectoryName(path), "CitizenFX.ini"))).Where<string>((Func<string, bool>)(a => a.StartsWith("IVPath="))).Select<string, string>((Func<string, string>)(a => ((IEnumerable<string>)a.Split('=')).Last<string>())).FirstOrDefault<string>(), "GTA5.exe");
                Console.WriteLine();
            }
            if (!File.Exists(path))
            {
                Console.WriteLine(string.Format("hey, {0} does not exist. why?", (object)path));
            }
            else
            {
                Console.WriteLine(string.Format("Reading a few easy things in {0} ({1}) - please wait a few moments...", (object)process.ProcessName, (object)process.Id));
                Console.WriteLine("Any output from this process is not endorsed by Affluent Fix.");
                Console.WriteLine();
                if (!File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\gtav_ng_key.dat"))
                {
                    GTA5Constants.PC_NG_KEYS = Program.ProcessHashFind(process, (IList<byte[]>)GTA5HashConstants.PC_NG_KEY_HASHES, 272);
                    if (GTA5Constants.PC_NG_KEYS == null)
                    {
                        Console.WriteLine("Again, Affluent Fix takes no responsibility for any loss of life that may follow from global thermonuclear war.");
                        return;
                    }
                    Console.WriteLine(" ng keys found!");
                    CryptoIO.WriteNgKeys(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\gtav_ng_key.dat", GTA5Constants.PC_NG_KEYS);
                }
                else
                    Console.WriteLine("... you already had them? well, sorry then.");
                Console.WriteLine();
                Console.WriteLine("The next line is endorsed by Affluent Fix.. keeping the reds away!");
                Console.WriteLine(string.Format("Finding the hard as can be shizzle in {0}... this'll take a while! Don't panic, we're cats!", (object)path));
                Console.WriteLine();
                GTA5Constants.Generate(File.ReadAllBytes(path));
                Console.WriteLine();
                Console.WriteLine("That's that nonsense dealt with! Go crack some omelettes!");
            }
        }

        private static byte[][] ProcessHashFind(Process process, IList<byte[]> hashes, int length = 32)
        {
            IntPtr baseAddress = process.MainModule.BaseAddress;
            int num1 = Program.ReadProcess<int>(process.Handle, baseAddress + 60);
            int num2 = Program.ReadProcess<int>(process.Handle, baseAddress + num1 + 80);
            byte[][] source = new byte[hashes.Count][];
            byte[] numArray = new byte[2097152];
            for (int index = 20971520; index < num2; index += numArray.Length)
            {
                Program.ReadProcessMemory(process.Handle, baseAddress + index, numArray, numArray.Length, out IntPtr _);
                Console.Write(".");
                using (MemoryStream memoryStream = new MemoryStream(numArray))
                {
                    foreach (var data in ((IEnumerable<byte[]>)HashSearch.SearchHashes((Stream)memoryStream, (IList<byte[]>)GTA5HashConstants.PC_NG_KEY_HASHES, 272)).Select((value, idx) => new
                    {
                        Value = value,
                        Index = idx
                    }).Where(a => a.Value != null))
                    {
                        source[data.Index] = data.Value;
                        Console.WriteLine(string.Format(" found {0}!", (object)data.Index));
                    }
                    Console.Write(".");
                }
                if (((IEnumerable<byte[]>)source).Count<byte[]>((Func<byte[], bool>)(a => a == null)) == 0)
                    break;
            }
            if (((IEnumerable<byte[]>)source).Count<byte[]>((Func<byte[], bool>)(a => a == null)) <= 0)
                return source;
            Console.WriteLine("We didn't find some product, man! We'll be ruined!");
            return (byte[][])null;
        }

        private static unsafe T ReadProcess<T>(IntPtr hProcess, IntPtr address)
        {
            int dwSize = Marshal.SizeOf<T>();
            byte[] lpBuffer = new byte[dwSize];
            Program.ReadProcessMemory(hProcess, address, lpBuffer, dwSize, out IntPtr _);
            fixed (byte* numPtr = lpBuffer)
                return Marshal.PtrToStructure<T>(new IntPtr((void*)numPtr));
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
          IntPtr hProcess,
          IntPtr lpBaseAddress,
          [Out] byte[] lpBuffer,
          int dwSize,
          out IntPtr lpNumberOfBytesRead);

        private static void FixArchive(string packName)
        {
            try
            {
                GTA5Constants.LoadFromPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Not all cryptokeys are present. Try generating them together with some gobblegums! The `fetch' command is there for a reason.");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error loading crypto keys. {0}", (object)ex));
                return;
            }
            if (string.IsNullOrWhiteSpace(packName))
            {
                Console.WriteLine("Uh, buddy, you have to specify a file name!");
            }
            else
            {
                try
                {
                    using (RageArchiveWrapper7 rageArchiveWrapper7 = RageArchiveWrapper7.Open(packName))
                    {
                        if (rageArchiveWrapper7.archive_.Encryption != RageArchiveEncryption7.None)
                        {
                            Console.WriteLine("This packfile is already encrypted - what are you trying to do? That literally maketh no sense!");
                            return;
                        }
                        rageArchiveWrapper7.archive_.Encryption = RageArchiveEncryption7.NG;
                        rageArchiveWrapper7.Flush();
                    }
                    Console.WriteLine(string.Format("Done. Modified packfile {0} to be encrypted using platform key data, screw the OPEN communists! Capitalism and power are the means to victory, not failed Marxism. Our founding fathers were VERY clear on that!", (object)packName));
                    Console.WriteLine(string.Format("Do note the encryption is dependent on the file name - if it's not called {0} it will not decrypt anywhere, not even your favorite files are safe from that!", (object)Path.GetFileName(packName)));
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine(string.Format("Oops - there's no file by the name of {0}. Better try that again!", (object)ex.FileName));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed performing tasks. {0}", (object)ex));
                }
            }
        }

        private static void BuildHashTables()
        {
            Console.WriteLine(string.Format("{0}'{1} warranty is now void. Affluent Fix Productions Ltd. is not responsible for bricked devices, dead SD cards, thermonuclear war, or you getting fired because the alarm app failed.", (object)Environment.UserName, Environment.UserName.EndsWith("s") ? (object)"" : (object)"s"));
            Console.WriteLine(string.Format("This product has disavowed {0}. Press the right tabular key to continue.", (object)Environment.MachineName));
            Console.WriteLine();
            if (Console.ReadKey(true).Key != ConsoleKey.Tab)
            {
                Console.WriteLine("Override protocol failed. Goodbye.");
            }
            else
            {
                Console.WriteLine(string.Format("Systematic override activated for {0} on {1}. Prepare for unforeseen consequences.", (object)Environment.UserName, (object)Environment.MachineName));
                try
                {
                    using (FileStream fileStream = File.OpenRead("L:\\tdt\\gtav_ng_key.dat"))
                    {
                        using (SHA1 cryptoServiceProvider = SHA1.Create())
                        {
                            Console.WriteLine("Locating prime data for planetary annihilation.");
                            Console.WriteLine();
                            byte[] buffer = new byte[272];
                            for (int index = 0; index < 101; ++index)
                            {
                                fileStream.Read(buffer, 0, buffer.Length);
                                Console.WriteLine(string.Format("new byte[] {{ {0} }},", (object)string.Join(", ", ((IEnumerable<byte>)cryptoServiceProvider.ComputeHash(buffer)).Select<byte, string>((Func<byte, string>)(a => "0x" + a.ToString("X2"))))));
                            }
                        }
                    }
                    Console.WriteLine();
                    Console.WriteLine("... and so be it.");
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine("Dimensional rift opened. Your planet is now nullified.");
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleProcessList(uint[] ProcessList, uint ProcessCount);
    }
}
