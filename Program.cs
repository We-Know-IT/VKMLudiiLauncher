using Microsoft.Playwright;
using System.Diagnostics;


bool isPc = false;
string luddiiGameFolderAddress = "B:/Projects/VKMLudiiLauncher/Ludii/";

var exhibitionNumber = "1D";


await StartPlaywrightAsync();
return;

async Task StartPlaywrightAsync(){
    var url = $"http://gamescreen.smvk.se/{exhibitionNumber}";
    
    var launchOptions = new BrowserTypeLaunchOptions
    {
        Headless = false,
        Args = new List<string> {"--kiosk"}
    };
    
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Firefox.LaunchAsync(launchOptions);
    
    var context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        ViewportSize = ViewportSize.NoViewport
    });
    
    var page = await context.NewPageAsync();
    await page.GotoAsync(url);
    
    page.Request += async (_, request) =>  {
        Console.WriteLine("Request event: " + request.Url);
        var gameName = request.Url;

        if (gameName.Contains("8080")) {
            gameName = gameName.Substring(gameName.IndexOf("8080/") + 5);
            await LaunchJarAsync(gameName);
            await page.GotoAsync(url);
        }
    };
    await Task.Delay(-1);
}

Task LaunchJarAsync(string gameName) {
    var jarFilePath = $"/home/pi/Hämtningar/Ludii/{gameName}.jar";
    
    if (isPc){
        jarFilePath = $"{luddiiGameFolderAddress}{gameName}.jar";
    }

    try {
        var jarStartInfo = new ProcessStartInfo {
            FileName = "java",
            Arguments = $"-jar \"{jarFilePath}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };
        var jarProcess = new Process { StartInfo = jarStartInfo, EnableRaisingEvents = true };
        

        jarProcess.Start();
        

        _ = Task.Run(() => jarProcess.WaitForExit());
    }
    
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }

    return Task.CompletedTask;
}