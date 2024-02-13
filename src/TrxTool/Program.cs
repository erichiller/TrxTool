using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace TrxTool;

public static class Program {
    public static async Task Main(string[] args) {
        const string methodFilterLong    = "--methodFilter", methodFilterShort    = "-m";
        const string artifactLong        = "--artifact",     artifactShort        = "-a";
        const string configLong          = "--config",       configShort          = "-c";
        const string commitShaLong       = "--commitSha",    commitShaShort       = "-s"; // if unset, download from the most recent run
        const string fileNamePatternLong = "--file",         fileNamePatternShort = "-f"; // either the file within the zip file or the direct file on the path if no artifact is specified

        string? methodFilter    = null;
        string? artifactName    = null;
        string  configFilePath  = "config.json";
        string? commitSha       = null;
        string? fileNamePattern = null;

        for (int i = 0; i < args.Length; i++) {
            // Log.Debug($"Checking arg[{i}] = '{args[i]}'");
            switch (args[i]) {
                case methodFilterLong or methodFilterShort when args.Length > i + 1 && args[i + 1] is not ['-', '-']:
                    i++;
                    methodFilter = args[i];
                    Log.Debug($"{nameof(methodFilter)} = {methodFilter}");
                    break;
                case methodFilterLong or methodFilterShort:
                    throw new ArgumentException($"{methodFilterLong} requires an argument");

                case artifactLong or artifactShort when args.Length >= i + 1 && args[i + 1] is not ['-', '-']:
                    i++;
                    artifactName = args[i];
                    Log.Debug($"{nameof(artifactName)} = {artifactName}");
                    break;
                case artifactLong or artifactShort:
                    throw new ArgumentException($"{artifactLong} requires an argument");

                case configLong or configShort when args.Length > i + 1 && args[i + 1] is not ['-', '-']:
                    i++;
                    configFilePath = args[i];
                    Log.Debug($"{nameof(configFilePath)} = {configFilePath}");
                    break;
                case configLong or configShort:
                    throw new ArgumentException($"{configLong} requires an argument");

                case commitShaLong or commitShaShort when args.Length > i + 1 && args[i + 1] is not ['-', '-']:
                    i++;
                    commitSha = args[i];
                    Log.Debug($"{nameof(commitSha)} = {commitSha}");
                    break;
                case commitShaLong or commitShaShort:
                    throw new ArgumentException($"{commitShaLong} requires an argument");

                case fileNamePatternLong or fileNamePatternShort when args.Length > i + 1 && args[i + 1] is not ['-', '-']:
                    i++;
                    fileNamePattern = args[i];
                    Log.Debug($"{nameof(fileNamePattern)} = {fileNamePattern}");
                    break;
                case fileNamePatternLong or fileNamePatternShort:
                    throw new ArgumentException($"{fileNamePatternLong} requires an argument");
            }
        }

        // {
        //     ArgumentException.ThrowIfNullOrWhiteSpace(methodFilter);
        //     new TestResultAnalyzer(fileNamePattern).TestDetails(methodFilter);
        // }

        // // extract trx matching fileNamePattern from zip file
        // {
        //     var extracted = TestResultAnalyzer.ScanZipArchiveForTrx(
        //         zipPath: "artifact_1238862347.zip",
        //         outputDir: "data",
        //         fileNamePattern: fileNamePattern
        //     );
        //     ArgumentException.ThrowIfNullOrWhiteSpace(methodFilter);
        //     extracted.Single().TestDetails(methodFilter);
        //     return;
        // }

        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions() {
            ReadCommentHandling  = JsonCommentHandling.Skip,
            AllowTrailingCommas  = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        ProgramConfiguration config = JsonSerializer.Deserialize<ProgramConfiguration>(await File.ReadAllTextAsync(configFilePath), jsonSerializerOptions) ?? throw new ArgumentException($"Invalid configuration file at {configFilePath}");
        Log.Debug($"config={config}");

        TestResultAnalyzer trx;

        ArgumentException.ThrowIfNullOrWhiteSpace(artifactName);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodFilter);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNamePattern);


        var artifacts = await TestResultAnalyzer.GetMatchingArtifacts(config, artifactName, commitSha);
        Log.Debug($"Found {artifacts.Count} matching artifacts");
        foreach (var artifact in artifacts) {
            // Log.Debug(artifact);
            var artifactFile = await TestResultAnalyzer.GetArtifact(config, artifact.Id);

            Log.Info($"{artifact}\n with file downloaded to {artifactFile.FullName}");

            // extract trx matching fileNamePattern from zip file
            var extracted = TestResultAnalyzer.ScanZipArchiveForTrx(
                zipPath: artifactFile.FullName,
                outputDir: "data",
                fileNamePattern: fileNamePattern
            );
            extracted.Single().TestDetails(methodFilter);
            // return;

            break; // KILL
        }

        return;
        string trxFilePath =
                "data/xxxxxxx.trx"
            ;
        /*
        Log.Debug(
            String.Join( "\n",
                         File.ReadAllLines(trxFilePath)
                             .Where( l => l.Contains("<UnitTestResult"))
                             .Skip(5).Take(50)
                             .Select( l => Regex.Match(l, @"duration=.*endTime="".*?""").Groups[0].Value + "\n" )
                       )
        );
        return;
        */
        StringBuilder sb = new ();
        trx = new TestResultAnalyzer(trxFilePath);
        // trx.ListAll( sb );
        // Log.Info( sb.ToString() );
        trx.TestDetails(methodFilter);
    }
}

/*
 *
 */
/* end XML Supporting Types */

public class TestResultAnalyzer {
    private readonly string               _trxFilePath;
    private readonly string?              _outputDir;
    private readonly string?              _testOutputPath;
    private readonly List<UnitTestResult> _testResults = new ();
    private readonly List<UnitTestResult> _failedTests = new ();


    // public StringBuilder Sb { get; } = new StringBuilder();

    public TestResultAnalyzer(string trxFilePath) {
        this._trxFilePath = trxFilePath;
        _outputDir        = null;

        if (_outputDir is { }) {
            _testOutputPath = System.IO.Path.Combine(_outputDir, "Test_Output.log");
        }
        Log.Info(
            $"==== {nameof(TestResultAnalyzer)} ====\n" +
            $"  Input File   : {_trxFilePath}\n"        +
            $"  OutputDir    : {_outputDir}\n");
        this.extractTrx();
    }

    public static string MinimizeFileName(string thisString, string[] strings) {
        //
        var s = DiffStrings(thisString, strings);
        //Regex.Replace()
        s = Regex.Replace(s, @"[^a-zA-Z0-9_\-]", "_");
        return s.Trim('_');
        //
    }

    public static string DiffStrings(string thisString, string[] strings) {
        /*
        string[] ss = [
            "Hello Dave, and World",
            "Hello World"
        ];
        */
        var ss              = strings;
        int stripFirstChars = Int32.MaxValue;
        int stripLastChars  = Int32.MaxValue;
        for (int i = 1; i < ss.Length; i++) {
            string a         = ss[i - 1];
            string b         = ss[i];
            int    minLength = Int32.Min(a.Length, b.Length);
            int    maxCheck  = Int32.Min(minLength, stripFirstChars);
            // Console.WriteLine( $"minLength={minLength} ; maxCheck={maxCheck}");
            // int startMatchLength = 
            for (int c = maxCheck; c > 0; c--) {
                if (a[..c] == b[..c]) {
                    // Console.WriteLine( $"stripFirstChars={stripFirstChars} ; c={c}");
                    stripFirstChars = Int32.Min(c, stripFirstChars);
                    break;
                }
            }
            maxCheck = Int32.Min(minLength, stripLastChars);
            for (int c = maxCheck; c > 0; c--) {
                if (a[^c..] == b[^c..]) {
                    // Console.WriteLine( $"stripLastChars={stripLastChars} ; c={c}");
                    stripLastChars = Int32.Min(c, stripLastChars);
                    break;
                }
            }
        }
        // Console.WriteLine( $"stripFirstChars={stripFirstChars} ; '{ss[0][..stripFirstChars]}'");
        // Console.WriteLine( $"stripLastChars={stripLastChars} ; '{ss[0][^stripLastChars..]}'");
        // Console.WriteLine( String.Join("\n", ss));
        if (thisString.Length < (stripFirstChars + stripLastChars)) {
            return thisString;
        }
        return thisString[stripFirstChars..^stripLastChars];
    }

    public static List<TestResultAnalyzer> ScanZipArchiveForTrx(string zipPath, string outputDir = ".", string fileNamePattern = ".*") {
        // 
        List<string> trxEntryFullNames = new ();
        Log.Debug($"Scanning zip archive '{zipPath}' and all entries for trx files.");
        int                      trxFileCount = 0;
        List<TestResultAnalyzer> extracted    = new ();
        //return extracted;
        string extractPath = Path.GetFullPath(outputDir);
        if (!Directory.Exists(outputDir)) {
            Directory.CreateDirectory(outputDir);
        }
        using (ZipArchive archive = ZipFile.OpenRead(zipPath)) {
            foreach (ZipArchiveEntry entry in archive.Entries) {
                if (!entry.FullName.EndsWith(".trx", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                trxFileCount++;
                trxEntryFullNames.Add(entry.FullName);
                if (Regex.IsMatch(entry.FullName, fileNamePattern)) {
                    Log.Debug($"trx: {entry.FullName}");
                    // Gets the full path to ensure that relative segments are removed.
                    string nameWithDir     = System.IO.Path.GetDirectoryName(entry.FullName) ?? throw new DirectoryNotFoundException($"Unable to find Directory: '{entry.FullName}'");
                    string newEntryName    = MinimizeFileName(entry.FullName, trxEntryFullNames.ToArray()) + ".trx";
                    string destinationPath = Path.GetFullPath(Path.Combine(extractPath, newEntryName)); // maintainHierarchy ? entry.FullName : entry.Name));

                    Log.Debug($"nameWithDir={nameWithDir}"
                              + "\n\t\t==>> " + newEntryName
                              + "\n\t\t==>> " + destinationPath);
                    // continue;

                    // Ordinal match is safest, case-sensitive volumes can be mounted within volumes that
                    // are case-insensitive.
                    if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal)) {
                        entry.ExtractToFile(destinationPath, overwrite: true);
                        Log.Debug($"Extracted : {entry.FullName} => {extractPath}");
                        extracted.Add(new TestResultAnalyzer(destinationPath));
                    }
                    else {
                        Log.Warn($"'{destinationPath}' !StartsWith '{extractPath}'");
                    }
                }
                else {
                    Log.Debug($"Skipping non-matching file '{entry.FullName}' ");
                }
            }
        }
        Log.Debug($"Found {trxFileCount} trx files, extracted {extracted.Count}, from {zipPath}");
        return extracted;
    }


    private void extractTrx() {
        XDocument     xd = XDocument.Load(_trxFilePath);
        XmlSerializer sx = new XmlSerializer(typeof(UnitTestResult));

        var results = xd.Elements().First().Elements()
                        .Single(static el => el.Name.LocalName == "Results")
                        .Elements().ToArray();
        /*
        string[] nonFailedResultOutcomes = { "Passed", "NotExecuted", Skipped };
        var failedResults = results.Where( el => !nonFailedResultOutcomes.Contains(
                                               el.Attributes()
                                                 .Single( attr => attr.Name == "outcome" )
                                                 .Value )
        );
        */
        _testResults.AddRange(results.Select(r => (sx.Deserialize(r.CreateReader()) as UnitTestResult)!)
                                     .OrderBy(tr => tr.ComputedStartTime));
        // 
    }

    public void ListAll(in StringBuilder sb) {
        int nameColWidth = _testResults.Select(tr => tr.ShortTestName.Length).Max();
        int timeColWidth = DateTime.Now.ToString("o").Length;
        sb.AppendLine();
        foreach (var result in _testResults) {
            sb.AppendLine((string?)$"{result.ShortTestName.PadRight(nameColWidth)} | {(result.ComputedStartTime).ToString("o").PadLeft(timeColWidth)} | {result.EndTime.ToString("o").PadLeft(timeColWidth)} | {result.Duration} | {result.Outcome}");
        }
    }

    public void TestDetails(string regexFilter) {
        foreach (var result in _testResults.Where(r => Regex.IsMatch(r.TestName, regexFilter ?? "*"))) {
            var str = $"{result.ShortTestName}\n"
                      + String.Empty.PadLeft(result.ShortTestName.Length, '=') + "\n\n"
                      + String.Join(
                          "\n",
                          JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(result))?
                                        .Where(kv => kv.Key != "Output")
                                        .Select(kv => $"{kv.Key,-25} | {kv.Value}") ?? Array.Empty<string>())
                      + "\n"
                      + (result.Output is { ErrorInfo.Message: { } errMsg }
                          ? $"\n\nError Message\n{String.Empty.PadLeft(40, '-')}\n{errMsg}\n"
                          : String.Empty)
                      + (result.Output is { ErrorInfo.StackTrace: { } stackTrace }
                          ? $"\n\nStack Trace\n{String.Empty.PadLeft(40, '-')}\n{stackTrace}\n"
                          : String.Empty)
                      + (result.Output is { StdOut: { } stdOut }
                          ? $"\n\nStandard Output\n{String.Empty.PadLeft(40, '-')}\n{stdOut}\n"
                          : String.Empty);
            Log.Debug(str + "\n");
        }
    }
    // 

    public static async Task<List<GitHubArtifact>> GetMatchingArtifacts(ProgramConfiguration config, string? artifactName, string? commitSha) {
        // docs: https://docs.github.com/en/rest/actions/artifacts?apiVersion=2022-11-28
        using CancellationTokenSource cts     = new (60_000);
        int                           perPage = 30; // TODO: should iterate pages until found
        var                           uri     = new UriBuilder($"https://api.github.com/repos/{config.GitHubRepo}/actions/artifacts") { Query = $"per_page={perPage}&page=1" + (artifactName is { } ? $"&name={artifactName}" : String.Empty) }.Uri;
        Log.Debug($"Retrieving list of artifacts from '{uri}'."
                  + (artifactName is { } ? $" Matching name '{artifactName}'" : String.Empty)
                  + (commitSha is { } ? $" Will filter on commitSha '{commitSha}'" : String.Empty)
        );

        using HttpClient httpClient = new ();
        // httpClient.DefaultRequestVersion = HttpVersion.Version11;
        httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.GitHubApiKey);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("TrxTool")));

        //
        // Log.Debug("DefaultRequestHeaders=" + String.Join(", ", httpClient.DefaultRequestHeaders.Select(kv => $"{kv.Key}='" + String.Join("' ++ '", kv.Value) + "'")));
        //
        //
        // // throw new Exception();
        //
        // var result = await httpClient.GetAsync(uri, cts.Token);
        // Log.Debug("Request Headers:"  +displayHeaders(result.RequestMessage?.Headers));
        // Log.Debug(kvDisplay(result.RequestMessage?.RequestUri));
        // Log.Debug(kvDisplay(result.RequestMessage?.Method));
        // Log.Debug(kvDisplay(result.RequestMessage?.Version));
        //
        // Log.Debug("===end request===\n");
        //
        // Log.Debug("==============Response Headers==========\n" + displayHeaders(result.Headers));
        // Log.Debug( kvDisplay(result.Version));
        // Log.Debug("Contents=" + await result.Content.ReadAsStringAsync(cts.Token));
        // Log.Debug(kvDisplay(result.ReasonPhrase));
        // throw new Exception();
        var response = await httpClient.GetFromJsonAsync<GitHubArtifactResponseContainer>(uri, cts.Token)
                       ?? throw new JsonException("Failed to deserialize GitHubArtifactResponseContainer");
        if (commitSha is { }) {
            return response.Artifacts.Where(a => a.WorkflowRun.HeadSha.Contains(commitSha)).ToList();
        }
        return response.Artifacts;
    }

    private static string displayHeaders(HttpHeaders? headers) {
        return headers is { } ? String.Join(", ", headers.Select(kv => $"{kv.Key}='" + String.Join("' ++ '", kv.Value) + "'")) : "null";
    }

    private static string kvDisplay(object? value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => $"{valueName}={value}";

    public static async Task<FileInfo> GetArtifact(ProgramConfiguration config, long id) {
        using CancellationTokenSource cts        = new (60_000);
        var                           uri        = new UriBuilder($"https://api.github.com/repos/{config.GitHubRepo}/actions/artifacts/{id}/zip").Uri;
        using HttpClient              httpClient = new ();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.GitHubApiKey);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("TrxTool")));

        var      outFile  = $"artifact_{id}.zip";
        FileInfo fileInfo = new FileInfo(outFile);

        await File.WriteAllBytesAsync(outFile, await httpClient.GetByteArrayAsync(uri, cts.Token), cts.Token);

        return fileInfo;
        // var response = await httpClient.GetAsync(uri, cts.Token);
        // using (StreamReader sr = new StreamReader(await response.Content.ReadAsStreamAsync(cts.Token), System.Text.Encoding.Default)) {
        //     StreamWriter oWriter = new StreamWriter(outFile);
        //     oWriter.Write(sr.ReadToEnd(), cts.Token);
        //     oWriter.Close();
        //     // oWriter.
        // }
        // Log.Debug($"Download for artifact {id} is Complete");
        //
        // return fileInfo;
    }
}

public record ProgramConfiguration(
    string GitHubRepo,
    string GitHubApiKey
);