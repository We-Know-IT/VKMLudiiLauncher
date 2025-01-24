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
        
        var jarProcess = new Process { StartInfo = startInfo };
        
        if (jarProcess.Start()){
            // Show Button
        }
        
        jarProcess.WaitForExit();
    }
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
    return Task.CompletedTask;
}
