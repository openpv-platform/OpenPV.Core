using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.Utility;

namespace WebApp
{
    public class Program
    {
        static bool includeRazorPages = true;
        static bool includeWebAPI = false;
        static string startPage = "http://google.com";

        /// <summary>
        ///  This Demo Application shows how to create a simple HTML based web application
        ///  with c#.  Its uses both WebAPI (For inteacting with services) as well as
        ///  hosting Razor pages for HTML Content
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            // Add services to the container.
            if (includeRazorPages) 
                builder.Services.AddRazorPages();

            var app = builder.Build();


            // Add WebAPI Handlers
            if (includeWebAPI)
                startPage = AddWebApi(app); // Start WebAPI Page

            // Add Razor Page Support
            if (includeRazorPages)
                startPage = AddRazorPages(app);


            // Send Release Notification
            AhsokaRuntime.ReleaseStartup();
            var systemClient = new SystemServiceClient();
            systemClient.Connect(); // Start Service for use in Apps
            
            // Start Our Application - Will Block until Ready
            app.Start();

            CancellationTokenSource tokenSource = new();
            var task = StartEmbeddedBrowser(startPage, tokenSource.Token);
            
            // Wait for the WebApp to Shutdown.   
            // Note that this will 
            app.WaitForShutdown();

            /// Wait for Browser Shutdown.
            tokenSource.Cancel();
            task.Wait();
        }

        /// <summary>
        /// This Function Adds Razor Page support for handling 
        /// pages defined in the "Pages" Folder
        /// Note: To disable razor completely you shoudl comment out the AddRazorPages
        /// </summary>
        /// <param name="app"></param>
        private static string  AddRazorPages(WebApplication app)
        {
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapRazorPages();
            app.UseAuthorization();

            return "http://localhost:5000/"; // Start Index Page

        }

        public static Task StartEmbeddedBrowser(string url, CancellationToken token)
        {
            // Startup Browser in an Async Thread
            return Task.Run(() =>
            {
                IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, info.Data);
                });

                if (!OperatingSystem.IsWindows())
                    ProcessUtility.RunProcessAsync("web-browser", url, null, progressText, token);
            });
        }

        /// <summary>
        /// This function adds a simple WebAPI set of endpoints.  This is useful if you app can be interacted with 
        /// using a REST Client 
        /// </summary>
        /// <param name="app"></param>
        private static string AddWebApi(WebApplication app)
        {
            // This section shows how you might 
            var group = app.MapGroup("/WebAPI");
            group.Map("/", () =>
            {
                return "Hello World";
            });

            return "http://localhost:5000/WebAPI"; // Start Index Page

        }
    }
}
