// See https://aka.ms/new-console-template for more information
using System;
using System.Net;
using System.Runtime.CompilerServices;

List<WinStoreVersion> clientVersions = new();

string[] winJsFileNames =
{
    "base.js",
    "ui-light.css",
    "ui.js",
};

string[] mainFolderFileNames =
{
    "WinStore.js",
    "WinStore.css",
    "frame.htm",
    "PCSFrame.htm",
    "PCSRedirect.htm",
    "Upgrade.htm",
    "Installs.htm",
    "Home.htm",
    "Results.htm",
    "PDP.htm",
    "Topic.htm",
    "Reacquire.htm",
    "Updates.htm",
    "Settings.htm",
    "ReportProblem.htm",
    "Review.htm",
    // also BI
};

string[] miscFileNames =
{
    "jquery-1.5.min.js",
    "wol.contentinstrumentation.logging.js",
};

string win80BaseUrl = "https://wscont.apps.microsoft.com/winstore/6.2/";
string win81BaseUrl = "https://wscont.apps.microsoft.com/winstore/a43f8337-2b31-4735-a006-9328167c3098/6.3/";
HttpClient hc = new();

#region Main part of script
Console.WriteLine("Finding valid client versions... [472-670 - later Win8]");

// a wider scope was tested. this contains everything that exists and can be accessed
FindUrls(win80BaseUrl, 472, 614, 32); // originally 14

FindUrls(win80BaseUrl, 615, 615, 87);
FindUrls(win80BaseUrl, 616, 670, 32);

Console.WriteLine("Finding valid client versions... [726-788 - Win8.1]");

FindUrls(win81BaseUrl, 726, 754, 31);
FindUrls(win81BaseUrl, 755, 769, 31);
FindUrls(win81BaseUrl, 770, 775, 31);
FindUrls(win81BaseUrl, 776, 776, 191);
FindUrls(win81BaseUrl, 777, 787, 31);
FindUrls(win81BaseUrl, 788, 788, 315);

Console.WriteLine("Downloading WinJS content...");
DownloadWinJS(win80BaseUrl, 472, 670);
DownloadWinJS(win81BaseUrl, 726, 788);

Console.WriteLine("Downloading static content...");
DownloadHtml(win80BaseUrl, 472, 670);
DownloadHtml(win81BaseUrl, 726, 788);

Console.WriteLine("Downloading BI/Instrumentation content...");
DownloadMisc(win80BaseUrl, 472, 670);
DownloadMisc(win81BaseUrl, 726, 788);



DownloadMisc(win80BaseUrl, 472, 670);

Console.WriteLine("Done.");

void FindUrls(string baseUrl, int initialClientVersion, int finalClientVersion, int maximumPageVersion)
{
    for (int clientVersion = initialClientVersion; clientVersion <= finalClientVersion; clientVersion++)
    {
        HttpResponseMessage msg = hc.Send(new HttpRequestMessage(HttpMethod.Head, $"{baseUrl}/{clientVersion}"));

        Console.WriteLine($"Scanning for client version {clientVersion}...");

        // forbidden = we know it exists
        if (msg.StatusCode == HttpStatusCode.Forbidden)
        {
            Console.WriteLine($"Client version {clientVersion} exists, scanning page versions 1-{maximumPageVersion} (this may take a while)...");

            // we will create a folder that doesnt exist if the url format for page versions is unknown for a client version if we don't check
            // see: v100
            Directory.CreateDirectory($"{clientVersion}");

            for (int pageVersion = 1; pageVersion <= maximumPageVersion; pageVersion++)
            {
                HttpResponseMessage getPageVersionMsg = hc.Send(new HttpRequestMessage(HttpMethod.Head, $"{baseUrl}/{clientVersion}/WW/en-us/0/{pageVersion}"));

                if (getPageVersionMsg.StatusCode == HttpStatusCode.Forbidden)
                {
                    Console.WriteLine($"Found client version {clientVersion}.{pageVersion}!");
                    clientVersions.Add(new(clientVersion, pageVersion));
                }
            }
        }
    }
}

void DownloadWinJS(string baseUrl, int minimumClientVersion, int maximumClientVersion)
{
    foreach (WinStoreVersion version in clientVersions)
    {
        if (version.ClientVersion < minimumClientVersion
            || version.ClientVersion > maximumClientVersion) continue;

        string finalOutFolder = $"{version.ClientVersion}\\WinJS\\";
        if (version.ClientVersion >= 479) finalOutFolder = $"{version.ClientVersion}\\WinJS\\{version.PageVersion}\\";

        Directory.CreateDirectory(finalOutFolder);

        foreach (string winJsFileName in winJsFileNames)
        {
            string finalUrl = $"{baseUrl}/{version.ClientVersion}/WinJS/{winJsFileName}";
            if (version.ClientVersion > 479) finalUrl = $"{baseUrl}/{version.ClientVersion}/WinJS/{version.PageVersion}/{winJsFileName}";

            Console.WriteLine($"Downloading {finalUrl}...");

            var winJsStream = hc.GetByteArrayAsync(finalUrl);

            while (!winJsStream.IsCompleted) { };

            if (winJsStream.IsCompletedSuccessfully)
            {
                File.WriteAllBytes($"{finalOutFolder}\\{winJsFileName}", winJsStream.Result);
            }
            else
            {
                Console.WriteLine($"Failed ({winJsStream.Exception}). Skipping...");
            }
        }
    }
}

void DownloadHtml(string baseUrl, int minimumClientVersion, int maximumClientVersion)
{
    foreach (WinStoreVersion version in clientVersions)
    {
        if (version.ClientVersion < minimumClientVersion
            || version.ClientVersion > maximumClientVersion) continue;

        string finalOutFolder = $"{version.ClientVersion}\\WW\\en-US\\0\\{version.PageVersion}\\";

        Directory.CreateDirectory(finalOutFolder);

        foreach (string mainFolderFileName in mainFolderFileNames)
        {
            string finalUrl = $"{baseUrl}/{version.ClientVersion}/WW/en-US/0/{version.PageVersion}/{mainFolderFileName}";
            Console.WriteLine($"Downloading {finalUrl}...");

            var winJsStream = hc.GetByteArrayAsync(finalUrl);

            while (!winJsStream.IsCompleted) { };

            if (winJsStream.IsCompletedSuccessfully)
            {
                File.WriteAllBytes($"{finalOutFolder}\\{mainFolderFileName}", winJsStream.Result);
            }
            else
            {
                Console.WriteLine($"Failed ({winJsStream.Exception}). Skipping...");
            }
        }
    }
}

void DownloadMisc(string baseUrl, int minimumClientVersion, int maximumClientVersion)
{
    foreach (WinStoreVersion version in clientVersions)
    {
        if (version.ClientVersion < minimumClientVersion
            || version.ClientVersion > maximumClientVersion) continue;

        string finalOutFolder = $"{version.ClientVersion}\\BI\\{version.PageVersion}\\";

        if (version.ClientVersion < 479
            || (version.ClientVersion == 479) && version.ClientVersion < 3) finalOutFolder = $"{version.ClientVersion}\\BI\\";

        Directory.CreateDirectory(finalOutFolder);

        foreach (string miscFileName in miscFileNames)
        {
            string finalUrl = $"{baseUrl}/{version.ClientVersion}/BI/{version.PageVersion}/{miscFileName}";

            if (version.ClientVersion < 479
                || (version.ClientVersion == 479) && version.ClientVersion < 3) finalUrl = $"{baseUrl}/{version.ClientVersion}/BI/{miscFileName}";

            Console.WriteLine($"Downloading {finalUrl}...");

            var winJsStream = hc.GetByteArrayAsync(finalUrl);

            while (!winJsStream.IsCompleted) { };

            if (winJsStream.IsCompletedSuccessfully)
            {
                File.WriteAllBytes($"{finalOutFolder}\\{miscFileName}", winJsStream.Result);
            }
            else
            {
                Console.WriteLine($"Failed ({winJsStream.Exception}). Skipping...");
            }
        }
    }
}

struct WinStoreVersion
{
    public int ClientVersion;
    public int PageVersion;

    public WinStoreVersion(int clientVersion, int pageVersion)
    {
        ClientVersion = clientVersion;
        PageVersion = pageVersion;
    }
};
#endregion