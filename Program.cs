using Microsoft.Playwright;
using System.Diagnostics;


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
        Args = [
            "--disable-gpu",             // Disable GPU acceleration
            "--no-sandbox",              // Bypass sandboxing issues
            "--disable-dev-shm-usage",   // Prevent shared memory overflow
            "--disable-software-rasterizer",
            "--enable-logging=stderr",   // Log Chromium errors
            "--v=1",                      // Verbose logging
            "-disable-dev-shm-usage",
            "--ignore-certificate-errors",
            "--disable-ipv6",
            "--disable-site-isolation-trials",
            
        ],
    });

    var page = await browser.NewPageAsync();
    await page.GotoAsync(url);

    await page.SetViewportSizeAsync(1920, 1080); 
    
    page.Request += async (_, request) =>  {
        Console.WriteLine("Request event: " + request.Url);
        var gameName = request.Url;

        if (gameName.Contains("8080")) {
            gameName = gameName.Substring(gameName.IndexOf("8080/") + 5);
            await page.GotoAsync("http://gamescreen.smvk.se/exhibition-home");
            await LaunchJarAsync(gameName);
        }
    };
    await page.EvaluateAsync(@"setInterval(() => {
    console.log('Keeping session alive');
    fetch(window.location.href, { method: 'HEAD' });
}, 10000);"); // Sends a request every 10 seconds
    await Task.Delay(-1);
}

void LaunchQuitButton(){ 
    var pythonScriptPath = "/home/pi/Hämtningar/QuitButton.py";
    var pythonExecutable = "python";

    try {
        ProcessStartInfo start = new ProcessStartInfo {
            FileName = pythonExecutable,
            Arguments = pythonScriptPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        var pyProcess = Process.Start(start);
        pyProcess.OutputDataReceived += (sender, eventArgs) => {
            Console.WriteLine(eventArgs.Data);
            if (eventArgs.Data != null && eventArgs.Data.Contains("Quit")){
                jarProcess.Kill();
                pyProcess.Kill();
            }
        };
        pyProcess.ErrorDataReceived += (sender, eventArgs) => {
            Console.WriteLine(eventArgs.Data);
            if (eventArgs.Data != null && eventArgs.Data.Contains("Quit")){
                jarProcess.Kill();
                pyProcess.Kill();
            }
        };
        
        
        pyProcess.BeginOutputReadLine();
        pyProcess.BeginErrorReadLine();
        
        pyProcess.WaitForExit();
    }
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}

async Task LaunchJarAsync(string gameName) {
    var jarFilePath = $"/home/pi/Hämtningar/Ludii/{gameName}.jar";
    
    // var jarFilePath = $"..\\..\\..\\Ludii/{gameName}.jar"; // Needed to go back from \bin\Debug\net8.0 to be able to find "Ludii" folder - ..\\..\\..\\ is not the best solution
    var javaPath = "java";

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

        if (jarProcess.Start()){
            LaunchQuitButton();
        }
        jarProcess.WaitForExit();
        
    }
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}
