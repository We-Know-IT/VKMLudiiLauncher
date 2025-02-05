using Microsoft.Playwright;
using System.Diagnostics;


bool isPc = true;
string luddiiGameFolderAddress = "B:/Projects/VKMLudiiLauncher/Ludii/";

bool launchQuitButton = true;

Console.WriteLine("What is the Exhibition Number?");
var exhibitionNumber = Console.ReadLine();

await StartPlaywrightAsync();
return;


async Task StartPlaywrightAsync(){
    var url = $"http://gamescreen.smvk.se/{exhibitionNumber}";
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
        Headless = false,
        Args =["--enable-logging=stderr", "--v=1"]
    });

    var page = await browser.NewPageAsync();
    await page.GotoAsync(url);

    await page.SetViewportSizeAsync(1920, 1000); 
    
    page.Request += async (_, request) =>  {
        Console.WriteLine("Request event: " + request.Url);
        var gameName = request.Url;

        if (gameName.Contains("8080")) {
            gameName = gameName.Substring(gameName.IndexOf("8080/") + 5);
            await LaunchJarAsync(gameName);
        }
    };
    await Task.Delay(-1);
}

Task LaunchJarAsync(string gameName) {
    var jarFilePath = $"/home/pi/Hämtningar/Ludii/{gameName}.jar";
    
    if (isPc){
        jarFilePath = $"{luddiiGameFolderAddress}{gameName}.jar";
        launchQuitButton = false;
    }

    try {
        // Start JAR File
        var jarStartInfo = new ProcessStartInfo {
            FileName = "java",
            Arguments = $"-jar \"{jarFilePath}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };
        var jarProcess = new Process { StartInfo = jarStartInfo, EnableRaisingEvents = true };

        // Start the Python Quit Button
        if (launchQuitButton){
            var pythonScriptPath = "/home/pi/Hämtningar/QuitButton.py";
            var pyStartInfo = new ProcessStartInfo {
                FileName = "python",
                Arguments = pythonScriptPath,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };
            var pyProcess = new Process { StartInfo = pyStartInfo, EnableRaisingEvents = true };

            // Read Python Quit Button Output
            pyProcess.OutputDataReceived += (_, e) =>  {
                if (!string.IsNullOrEmpty(e.Data)) {
                    Console.WriteLine($"[Python STDOUT] {e.Data}");
                    if (e.Data.Contains("Quit")) {
                        try { jarProcess.Kill(); } catch {}
                        try { pyProcess.Kill(); } catch {}
                    }
                }
            };
            
            pyProcess.ErrorDataReceived += (_, e) => {
                if (!string.IsNullOrEmpty(e.Data)) {
                    Console.WriteLine($"[Python STDERR] {e.Data}");
                    // Quits JAR Process
                    if (e.Data.Contains("Quit")) {
                        try { jarProcess.Kill(); } catch {}
                        try { pyProcess.Kill(); } catch {}
                    }
                }
            };
            
            pyProcess.Start();

            pyProcess.BeginOutputReadLine();
            pyProcess.BeginErrorReadLine();
            // Keep Python Process Alive
            _ = Task.Run(() => pyProcess.WaitForExit());
        }
        Console.WriteLine("Starting JAR File:" + jarFilePath);

        jarProcess.Start();
        // Keep JAR Process Alive
        _ = Task.Run(() => jarProcess.WaitForExit());
    }
    
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }

    return Task.CompletedTask;
}