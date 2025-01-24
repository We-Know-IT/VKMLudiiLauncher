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

    try{
        var startInfo = new ProcessStartInfo {
            FileName = "java",
            Arguments = $"-jar \"{jarFilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        var jarProcess = Process.Start(startInfo);
        
        jarProcess.WaitForExit();
        
        var pyStartInfo = new ProcessStartInfo{
            FileName = "python",
            Arguments = pythonScriptPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        var pyProcess = Process.Start(pyStartInfo);
        
        pyProcess.OutputDataReceived += (sender, eventArgs) => {
            if (eventArgs.Data.Contains("Quit")){
                jarProcess.Kill();
                pyProcess.Kill();
            }
            Console.WriteLine(eventArgs.Data);
        };
        
        pyProcess.ErrorDataReceived += (sender, eventArgs) => {
            if (eventArgs.Data.Contains("Quit")){
                jarProcess.Kill();
                pyProcess.Kill();
            }
            Console.WriteLine(eventArgs.Data);
        };

        pyProcess.BeginOutputReadLine();
        pyProcess.BeginErrorReadLine();

        pyProcess.WaitForExit();
    }
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
    return Task.CompletedTask;
}
