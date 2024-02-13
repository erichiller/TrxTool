using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace TrxTool;

[XmlRoot("UnitTestResult", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010", IsNullable = false)]
public record UnitTestResult {
    [XmlIgnore]
    public string ShortTestName =>
        Regex.Match(TestName, @"(?<ClassAndMethod>\w+\.\w+)($|\()").Groups["ClassAndMethod"].Value;

    [XmlAttribute(AttributeName = "executionId")]
    public required string ExecutionId { get; set; } // executionId="fda94305-b507-4079-9196-78b331b1f041" 

    [XmlAttribute(AttributeName = "testId")]
    public required string TestId { get; set; }
    //testId="98e23071-3a96-b37b-c807-6afac95180b5" 

    [XmlAttribute(AttributeName = "testName")]
    public required string TestName { get; set; }
    // testName="namespace.type.method" 

    [XmlAttribute(AttributeName = "computerName")]
    public required string ComputerName { get; set; }
    // computerName="fv-az471-492" 

    private System.TimeSpan _duration;

    [XmlIgnore]
    public TimeSpan Duration {
        get => _duration;
        set => _duration = value;
    }
    [XmlIgnore]
    public DateTimeOffset ComputedStartTime => StartTime - Duration;

    [Browsable(false)]
    [XmlAttribute(DataType = "duration", AttributeName = "duration")]
    public required string DurationString {
        get => XmlConvert.ToString(_duration);
        set => _duration = String.IsNullOrEmpty(value)
            ? TimeSpan.Zero
            : TimeSpan.Parse(value);
    }

    [XmlAttribute(AttributeName = "startTime")]
    public required DateTimeOffset StartTime { get; set; }
    // startTime="2023-07-23T08:13:40.6752240-05:00" 

    [XmlAttribute(AttributeName = "endTime")]
    public required DateTimeOffset EndTime { get; set; }
    // endTime="2023-07-23T08:13:40.6752242-05:00" 

    [XmlAttribute(AttributeName = "testType")]
    public required string TestType { get; set; }
    // testType="13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b" 

    [XmlAttribute(AttributeName = "outcome")]
    public required string Outcome { get; set; }
    // outcome="Passed" 

    [XmlAttribute(AttributeName = "testListId")]
    public required string TestListId { get; set; }
    // testListId="8c84fa94-04c1-424b-9868-57a2d4851a1d" l

    [XmlAttribute(AttributeName = "relativeResultsDirectory")]
    public required string RelativeResultsDirectory { get; set; }
    // relativeResultsDirectory="fda94305-b507-4079-9196-78b331b1f041"

    public required Output Output { get; set; }

    /// <summary>
    /// Gets the highest path and line in the Exception StackTrace.
    /// </summary>
    public MethodPosition? GetErrorHighestLocalSource() {
        if (this.Output is { ErrorInfo.StackTrace: { } stackTrace }) {
            // 
            var match = Regex.Match(
                stackTrace,
                @"^ *at (?<MethodName>[^\)]+\)) in (?<FilePath>[^:]+):line (?<Line>[0-9]+)$",
                RegexOptions.Multiline
            );
            Log.Debug($"[{nameof(GetErrorHighestLocalSource)}] Match? {match.Success}");
            if (match.Groups["MethodName"].Value is { Length : > 0 } methodName
                && match.Groups["FilePath"].Value is { Length: > 0 } filePath
                && match.Groups["Line"].Value is { Length    : > 0 } line
               ) {
                return new MethodPosition(
                    methodName,
                    filePath,
                    Int32.Parse(line)
                );
            }
            Log.Warn($"[{nameof(GetErrorHighestLocalSource)}] Invalid regex result: {match}\n\t"
                     + $"MethodName: '{match.Groups["MethodName"].Value}'\n\t"
                     + $"FilePath: '{match.Groups["FilePath"].Value}'\n\t"
                     + $"Line: '{match.Groups["Line"].Value}'"
            );
        }
        Log.Warn($"[{nameof(GetErrorHighestLocalSource)}] Could not determine Method Position for {this}");
        return null;
    }
}



[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Used for XML Serialization")]
public record Output {
    public required string StdOut { get; set; }

    public ErrorInfo? ErrorInfo { get; set; }
}

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Used for XML Serialization")]
public record ErrorInfo {
    public required string Message    { get; set; }
    public required string StackTrace { get; set; }
}


[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Used for XML Serialization")]
public record MethodPosition(
    string MethodName,
    string FilePath,
    int    Line
);