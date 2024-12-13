using Microsoft.Playwright;
using System.Diagnostics;

string exhibitionNumber;

Console.WriteLine("What is the Exhibition Number?");
exhibitionNumber = Console.ReadLine();

Process jarProcess;
await StartPlaywrightAsync();

async Task StartPlaywrightAsync()
{
    // Initialize Playwright and launch the browser
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = false,
        Args = new[] { "--start-fullscreen" }
    });

    var page = await browser.NewPageAsync();
    await page.GotoAsync($"http://localhost:3000/{exhibitionNumber}");

    // Handle request events
    page.Request += async (_, request) =>
    {
        Console.WriteLine("Request event: " + request.Url);
        var gameName = request.Url;
        if (gameName.Contains("8080"))
        {
            // Extract the game name
            gameName = gameName.Substring(gameName.IndexOf("8080/") + 5);

            // Close the browser before proceeding
            await browser.CloseAsync();

            // Launch the JAR process
            await LaunchJarAsync(gameName);
        }
    };

    // Keep the initial browser running until the event is triggered
    Console.WriteLine("Browser running. Waiting for a matching request...");
    await Task.Delay(-1); // Keeps the browser open until the process completes
}

async Task LaunchJarAsync(string gameName)
{
    string jarFilePath = $"C:/Ludii/{gameName}.jar";
    string javaPath = "java";

    try
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = javaPath,
            Arguments = $"-jar \"{jarFilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        jarProcess = new Process { StartInfo = startInfo };
        jarProcess.Start();

        // Start the quit button browser
        await StartQuitButtonAsync();

        // Wait for the JAR process to exit
        jarProcess.WaitForExit();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}

async Task StartQuitButtonAsync()
{
    // Create a separate Playwright instance for the quit button
    using var quitPlaywright = await Playwright.CreateAsync();
    await using var browser = await quitPlaywright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = false
    });

    var page = await browser.NewPageAsync();
    string filePath = @"B:\Projects\VKMLudiiLauncher\QuitButton\index.html";
    string fileUrl = new Uri(filePath).AbsoluteUri;

    await page.GotoAsync(fileUrl);

    // Handle "Quit" button interaction
    page.Console += async (_, message) =>
    {
        Console.WriteLine("Message event: " + message.Text);
        if (message.Text == "Quit" && !jarProcess.HasExited)
        {
            jarProcess.Kill(true);
            jarProcess.WaitForExit();
            await browser.CloseAsync();
            await StartPlaywrightAsync();
        }
    };

    // Keep the quit button page open until explicitly closed
    Console.WriteLine("Quit button browser open. Press CTRL+C to exit if needed.");
    await Task.Delay(-1); // Keeps the browser open
}
