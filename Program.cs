using Microsoft.Playwright;
using System.Diagnostics;

public class Program
{
    // Global/shared variables:
    static bool isPc = false;
    static string luddiiGameFolderAddress = "B:/Projects/VKMLudiiLauncher/Ludii/";
    static string originalUrl;
    static Process? currentJarProcess = null;
    static IPage? mainPage = null;
    
    // Default values
    static int popupDelaySeconds = 180;  // Delay before showing popup
    static int popupTimeoutSeconds = 10; // How long popup is shown
    public static async Task Main(string[] args)
    {
        // 1st argument: exhibitionNumber (default: "1A")
        string exhibitionNumber = args.Length > 0 ? args[0] : "1A";
        originalUrl = $"http://gamescreen.smvk.se/{exhibitionNumber}";

        // 2nd argument: popup delay in seconds (default: 180)
        if (args.Length > 1 && int.TryParse(args[1], out int delay))
        {
            popupDelaySeconds = delay;
        }

        // 3rd argument: popup timeout in seconds (default: 10)
        if (args.Length > 2 && int.TryParse(args[2], out int timeout))
        {
            popupTimeoutSeconds = timeout;
        }

        // Start both the Playwright browser and the popup loop concurrently.
        Task playwrightTask = StartPlaywrightAsync();
        Task popupTask = PopupLoopAsync();

        await Task.WhenAll(playwrightTask, popupTask);
    }

    static async Task StartPlaywrightAsync()
    {
        // Launch the browser with kiosk mode and no viewport (full screen)
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = false,
            Args = new List<string> { "--kiosk" }
        };

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Firefox.LaunchAsync(launchOptions);

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = ViewportSize.NoViewport
        });

        var page = await context.NewPageAsync();
        mainPage = page; // Save a reference so the popup loop can navigate back.
        await page.GotoAsync(originalUrl);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "body { cursor: none; }" });

        // Listen to requests that indicate a jar file should be launched.
        page.Request += async (_, request) =>
        {
            Console.WriteLine("Request event: " + request.Url);
            var gameName = request.Url;
            if (gameName.Contains("8080"))
            {
                // Extract the game name from the URL.
                gameName = gameName.Substring(gameName.IndexOf("8080/") + 5);
                await LaunchJarAsync(gameName);
                await page.GotoAsync(originalUrl);
                await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "body { cursor: none; }" });
            }
        };

        // Keep the browser running indefinitely.
        await Task.Delay(-1);
    }

    static Task LaunchJarAsync(string gameName)
    {
        // Determine the jar file path based on the platform.
        var jarFilePath = isPc
            ? $"{luddiiGameFolderAddress}{gameName}.jar"
            : $"/home/pi/Hämtningar/Ludii/{gameName}.jar";

        try
        {
            var jarStartInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{jarFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var jarProcess = new Process { StartInfo = jarStartInfo, EnableRaisingEvents = true };
            jarProcess.Start();
            // Save the process reference so it can be killed on timeout.
            currentJarProcess = jarProcess;
            _ = Task.Run(() => jarProcess.WaitForExit());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Runs a loop that waits 180 seconds before showing a popup.
    /// If the popup times out (after 10 seconds), it kills the jar process and returns the page to the original URL.
    /// </summary>
    static async Task PopupLoopAsync()
    {
        while (true)
        {
            // Wait for 180 seconds between popups.
            await Task.Delay(TimeSpan.FromSeconds(popupDelaySeconds));

            int exitCode = await ShowPopupAsync();
            Console.WriteLine("Popup exit code: " + exitCode);

            // If exit code is 5, the dialog timed out (i.e. button not clicked in 10 seconds).
            if (exitCode == 5)
            {
                Console.WriteLine("Popup timed out: closing jar process and returning to the original URL.");

                // Kill the jar process if it's running.
                if (currentJarProcess != null && !currentJarProcess.HasExited)
                {
                    try
                    {
                        currentJarProcess.Kill();
                        currentJarProcess = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error killing jar process: " + ex.Message);
                    }
                }

                // Navigate back to the original URL in the Playwright page.
                if (mainPage != null)
                {
                    await mainPage.GotoAsync(originalUrl);
                }
            }
            // If the user clicked the button (exit code 0), simply restart the timer.
        }
    }

    /// <summary>
    /// Shows a Zenity info dialog with a 10-second timeout and one "OK" button.
    /// Returns the exit code (exit code 5 indicates a timeout).
    /// </summary>
    static async Task<int> ShowPopupAsync()
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "zenity",
            // Use double quotes for the text argument.
            Arguments = $"--info --timeout={popupTimeoutSeconds} --text=\"Are you still here? Press OK to continue.\nÄr du fortfarande här? Tryck på OK för att fortsätta.\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Ensure the dialog shows on the active display.
        processInfo.Environment["DISPLAY"] = ":0";

        using var process = Process.Start(processInfo);
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
