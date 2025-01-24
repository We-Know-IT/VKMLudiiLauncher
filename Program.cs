using Microsoft.Playwright;
using System.Diagnostics;


Console.WriteLine("What is the Exhibition Number?");
var exhibitionNumber = Console.ReadLine();

await StartPlaywrightAsync();
return;



async Task StartPlaywrightAsync(){
    var url = $"http://gamescreen.smvk.se/{exhibitionNumber}";
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
        Headless = false,
    });

    var page = await browser.NewPageAsync();
    page.SetDefaultNavigationTimeout(0);
    await page.GotoAsync(url);

    await page.SetViewportSizeAsync(1920, 1080); 
    
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
    var pythonScriptPath = $"/home/pi/Hämtningar/QuitButton.py";

    try {
        // Start the JAR
        var jarStartInfo = new ProcessStartInfo {
            FileName = "java",
            Arguments = $"-jar \"{jarFilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };
        var jarProcess = new Process { StartInfo = jarStartInfo, EnableRaisingEvents = true };

        // Start the Python script
        var pyStartInfo = new ProcessStartInfo {
            FileName = "python",
            Arguments = pythonScriptPath,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };
        var pyProcess = new Process { StartInfo = pyStartInfo, EnableRaisingEvents = true };

        // Handle Python script output
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
                if (e.Data.Contains("Quit")) {
                    try { jarProcess.Kill(); } catch {}
                    try { pyProcess.Kill(); } catch {}
                }
            }
        };
        

        // Start them both
        jarProcess.Start();
        pyProcess.Start();

        // Begin async reads
        pyProcess.BeginOutputReadLine();
        pyProcess.BeginErrorReadLine();

        // Wait for both to exit (but non-blocking)
        _ = Task.Run(() => jarProcess.WaitForExit());
        _ = Task.Run(() => pyProcess.WaitForExit());
    }
    
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }

    return Task.CompletedTask;
}