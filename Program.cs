using Microsoft.Playwright;
using System.Diagnostics;

string exhibitionNumber;

Console.WriteLine("What is the Exhibition Number?");
exhibitionNumber = Console.ReadLine();

Process jarProcess;
await StartPlaywrightAsync();

async Task StartPlaywrightAsync()
{
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = false,
    });

    var page = await browser.NewPageAsync();
    await page.SetViewportSizeAsync(1024, 768); 
    await page.GotoAsync($"http://localhost:3000/{exhibitionNumber}");

    page.Request += async (_, request) =>
    {
        Console.WriteLine("Request event: " + request.Url);
        var gameName = request.Url;
        if (gameName.Contains("8080"))
        {
            gameName = gameName.Substring(gameName.IndexOf("8080/") + 5);
            
            await LaunchJarAsync(gameName, browser);
        }
    };

    Console.WriteLine("Browser running.");
    await Task.Delay(-1);
}

async Task LaunchJarAsync(string gameName, IBrowser browser) {
    string jarFilePath = $"C:/Ludii/{gameName}.jar";
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
        
        jarProcess.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Console.WriteLine($"[JAR Output]: {args.Data}");
                // You can add additional logic here based on the output
            }
        };

        jarProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Console.WriteLine($"[JAR Error]: {args.Data}");
                // You can handle errors from the process here
            }
        };
        if (jarProcess.Start()){
            jarProcess.BeginOutputReadLine();
            jarProcess.BeginErrorReadLine();
            await Task.Delay(3000);
            await browser.CloseAsync();
        }
        
        await StartQuitButtonAsync();
        jarProcess.WaitForExit();
    }
    catch (Exception ex) {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}

async Task StartQuitButtonAsync() {
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
        if (message.Text == "Quit" && !jarProcess.HasExited)
        {
            await browser.CloseAsync();
            StartPlaywrightAsync();
            await Task.Delay(3000);
            jarProcess.Kill(true);
        }
    };

    Console.WriteLine("Close quit button. Press CTRL+C to exit if needed.");
    await Task.Delay(-1);
}
