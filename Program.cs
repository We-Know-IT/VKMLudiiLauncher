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
    });

    var page = await browser.NewPageAsync();
    await page.GotoAsync(url);

    await page.SetViewportSizeAsync(1900, 1000); 
    
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
    
    string jarFilePath = $"..\\..\\..\\Ludii/{gameName}.jar"; // Needed to go back from \bin\Debug\net8.0 to be able to find "Ludii" folder - ..\\..\\..\\ is not the best solution
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
        Console.WriteLine($"The Full Path: {Path.GetFullPath(jarFilePath)}");
        
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
            StartPlaywrightAsync();
            await Task.Delay(4000);
            await browser.CloseAsync();
        }
        
        jarProcess.WaitForExit();
    }
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}
