using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace OmniSummitDeviceService.UnitTests
{
    [TestFixture]
    public partial class SummitServerTest
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        private const UInt32 WM_CLOSE          = 0x0010;
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SetForegroundWindow(IntPtr point);

        // Tests Requirement 5.1.1 - Initialization of Services
        [Test]
        public void Startup_PrintsToConsole()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var p = new Process();
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = $"{basePath}/../../../../OmniSummitDeviceService/bin/Debug/net472/OmniSummitDeviceService";
            p.Start();
            var reader = p.StandardOutput;
            Thread.Sleep(1000);

            // Collect standard output, kill process
            p.Kill();

            // Read output to end
            var output = reader.ReadToEnd();
            var regex = new Regex(@".*Version: (?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.\d+.*");
            var match = regex.Match(output);

            TestContext.WriteLine(output);

            Assert.That(match.Groups["major"].Success);
            Assert.That(match.Groups["minor"].Success);
            Assert.That(match.Groups["patch"].Success); 

            regex = new Regex(@".*Supported devices: (?<supportedDevices>[^\r\n]*)");
            match = regex.Match(output);

            Console.Write(output);

            Assert.That(match.Groups["supportedDevices"].Success);
            Assert.AreEqual(match.Groups["supportedDevices"].Value, "Medtronic Summit RC+S");
        }

        // Tests Requirement 4.3 - Command Console
        // Tests Requirement 5.1.3 - Server Shutdown
        [Test]
        public void Startup_CreatesWindow()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            p.StartInfo.FileName = $"{basePath}/../../../../OmniSummitDeviceService/bin/Debug/net472/OmniSummitDeviceService";
            p.Start();
            
            // Wait for window to open
            Thread.Sleep(1000);

            // Confirm that the window exists
            var windowHandle = p.MainWindowHandle;
            Assert.That(windowHandle != null);
            
            // Confirm that the window can be closed
            SendMessage(windowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine(windowHandle);

            // Save the value to see if window closing was successful
            bool hasExited = p.HasExited;
            
            // If window closing failed, kill process to free resources anyway.
            if (p.HasExited == false)
            {
                p.Kill();
            }

            // Confirm that the process was exited in the non-killing fashion
            Assert.That(hasExited);
        }

        // Tests Requirement 5.1.3 - Server Shutdown
        [Test]
        public void Startup_CreatesWindow_qToShutdown()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            p.StartInfo.FileName = $"{basePath}/../../../../OmniSummitDeviceService/bin/Debug/net472/OmniSummitDeviceService";
            p.Start();

            // Wait for window to open
            Thread.Sleep(1000);

            // Confirm that the window exists
            var windowHandle = p.MainWindowHandle;
            Assert.That(windowHandle != null);

            // Confirm that the window can be closed
            SetForegroundWindow(windowHandle);
            SendKeys.SendWait("q");
            Thread.Sleep(1000);

            // Save the value to see if window closing was successful
            bool hasExited = p.HasExited;

            // If window closing failed, kill process to free resources anyway.
            if (p.HasExited == false)
            {
                p.Kill();
            }

            // Confirm that the process was exited in the non-killing fashion
            Assert.That(hasExited);
        }
    }
}
