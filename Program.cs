using Microsoft.Playwright;
using System.Diagnostics;
using System.Runtime.InteropServices;

[DllImport("user32.dll", SetLastError = true)]
static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

[DllImport("user32.dll", SetLastError = true)]
static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

[DllImport("user32.dll")]
static extern bool SetForegroundWindow(IntPtr hWnd);

const uint SWP_NOMOVE = 0x0002;
const uint SWP_NOSIZE = 0x0001;
const uint SWP_SHOWWINDOW = 0x0040;

IntPtr HWND_TOPMOST = new IntPtr(-1);
IntPtr HWND_NOTOPMOST = new IntPtr(-2);


string exhibitionNumber;

Console.WriteLine("What is the Exhibition Number?");
exhibitionNumber = Console.ReadLine();

Process jarProcess;
await StartPlaywrightAsync();

async Task StartPlaywrightAsync(){
    var url = $"http://gamescreen.smvk.se/{exhibitionNumber}";
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
        Headless = false,
    });

    var page = await browser.NewPageAsync();
    await page.GotoAsync(url);
    await page.EvaluateAsync("() => document.documentElement.requestFullscreen()");

    await page.SetViewportSizeAsync(1600, 900); 
    
    page.Request += async (_, request) =>  {
        Console.WriteLine("Request event: " + request.Url);
        var gameName = request.Url;

        if (gameName.Contains("8080")) {
            gameName = gameName.Substring(gameName.IndexOf("8080/") + 5);
            
            await LaunchJarAsync(gameName, browser);
        }
    };
    await Task.Delay(-1);
}

async Task LaunchJarAsync(string gameName, IBrowser browser) {
    // string jarFilePath = $"/home/pi/Hämtningar/publish/Ludii/{gameName}.jar";
    
    string jarFilePath = $"Ludii/{gameName}.jar";
    string javaPath = "java";

    try {
        var startInfo = new ProcessStartInfo {
            FileName = javaPath,
            Arguments = $"-jar \"{jarFilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        

        jarProcess = new Process { StartInfo = startInfo };
        
        jarProcess.OutputDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data)) {
                Console.WriteLine($"[JAR Output]: {args.Data}");
            }
        };

        jarProcess.ErrorDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data)) {
                Console.WriteLine($"[JAR Error]: {args.Data}");
            }
        };
        if (jarProcess.Start()){
            jarProcess.BeginOutputReadLine();
            jarProcess.BeginErrorReadLine();
            await Task.Delay(10000);
            IntPtr hWnd = jarProcess.MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                SetForegroundWindow(hWnd);
                Console.WriteLine("JAR window brought to the front.");
            }
            else
            {
                Console.WriteLine("Could not find the JAR window.");
            }
            // await browser.CloseAsync();
        }
        
        await StartQuitButtonAsync();
        jarProcess.WaitForExit();
    }
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}

async Task StartQuitButtonAsync(){
    var exit = false;
    using var quitPlaywright = await Playwright.CreateAsync();
    await using var browser = await quitPlaywright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
        Headless = false,
    });
    
    var page = await browser.NewPageAsync();
    await page.SetViewportSizeAsync(100, 100);
    string filePath = @"B:\Projects\VKMLudiiLauncher\QuitButton\index.html";
    string fileUrl = new Uri(filePath).AbsoluteUri;

    await page.GotoAsync(fileUrl);
    page.Console += async (_, message) =>  {
        Console.WriteLine("Message event: " + message.Text);
        if (message.Text == "Quit" && !jarProcess.HasExited) {
            await browser.CloseAsync();
            // StartPlaywrightAsync();
            await Task.Delay(3000);
            jarProcess.Kill(true);
            exit = true;
        }
    };
    await Task.Delay(3000);
    IntPtr hWnd = FindWindow(null, "Index.html - Chromium");
    if (hWnd != IntPtr.Zero) {
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        Console.WriteLine("Browser is now always on top.");
    } else {
        Console.WriteLine("Browser window not found.");
    }
    await Task.Delay(-1);
}
