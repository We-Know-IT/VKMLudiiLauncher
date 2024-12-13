using Microsoft.Playwright;
using System.Diagnostics;

Console.WriteLine("What is the Exhibition Number?");
var exhibitionNumber = Console.ReadLine();

await StartPlaywright();


async Task StartPlaywright(){
	var playwright = await Playwright.CreateAsync();
	await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions{
		Headless = false,
		Args = ["--start-fullscreen"]

	});
	var page = await browser.NewPageAsync();
	await page.GotoAsync("http://localhost:3000/" + exhibitionNumber);

	page.Console += (_, msg) => Console.WriteLine("Console event: " + msg.Text);
	page.Request += (_, request) => {
		Console.WriteLine("Request event: " + request.Url);
		var gameName = request.Url;
		if (gameName.Contains("8080")) {
			playwright.Dispose();
			gameName = gameName.Remove(0, 22);
			
			LaunchJar(gameName);
		}
	};

	Console.WriteLine("Press any key to exit...");
	Console.ReadKey();

	
	void LaunchJar(string gameName){
		string jarFilePath = "C:/Ludii/" + gameName + ".jar";
		string javaPath = "java";
		
		try {
			ProcessStartInfo startInfo = new ProcessStartInfo {
				FileName = javaPath,
				Arguments = $"-jar \"{jarFilePath}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true 
			};

			using Process process = new Process { StartInfo = startInfo };

			process.OutputDataReceived += (sender, e) => {
				if (!string.IsNullOrEmpty(e.Data)){
					Console.WriteLine($"JAR Output: {e.Data}");
				}
			};

			process.ErrorDataReceived += (sender, e) => {
				if (!string.IsNullOrEmpty(e.Data)){
					Console.WriteLine($"JAR Error: {e.Data}");
				}
			};

			process.Start();

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			process.WaitForExit();
			
			if (process.HasExited){
				StartPlaywright();
			}
			
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred: {ex.Message}");
		}
	}
}