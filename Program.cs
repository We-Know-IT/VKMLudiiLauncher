﻿using Microsoft.Playwright;
using System.Diagnostics;


bool isPc = false;
string luddiiGameFolderAddress = "B:/Projects/VKMLudiiLauncher/Ludii/";

var exhibitionNumber = "1D";

if (isPc){
    Console.WriteLine("What is the Exhibition Number?");
    exhibitionNumber = Console.ReadLine();
}

await StartPlaywrightAsync();
return;


async Task StartPlaywrightAsync(){
    var url = $"http://gamescreen.smvk.se/{exhibitionNumber}";
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
        Headless = false,
        Args =[ "--kiosk", "--enable-logging=stderr", "--v=1"]
    });

    var page = await browser.NewPageAsync();
    await page.GotoAsync(url);
    await page.SetViewportSizeAsync(1920, 1080); 

    await Task.Delay(5000); 
    await page.ClickAsync("body");
    await page.Keyboard.PressAsync("F11");
    
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