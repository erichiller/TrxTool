using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TrxTool;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Used for JSON deserialization")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Used for JSON deserialization")]
public record WorkflowRun {
    [JsonPropertyName("id")]                 public required long   Id               { get; init; }
    [JsonPropertyName("repository_id")]      public required int    RepositoryId     { get; init; }
    [JsonPropertyName("head_repository_id")] public required int    HeadRepositoryId { get; init; }
    [JsonPropertyName("head_branch")]        public required string HeadBranch       { get; init; }
    [JsonPropertyName("head_sha")]           public required string HeadSha          { get; init; }
}

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Used for JSON deserialization")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Used for JSON deserialization")]
public record GitHubArtifactResponseContainer(
    [property: JsonPropertyName("total_count")] int                  TotalCount,
    [property: JsonPropertyName("artifacts")]   List<GitHubArtifact> Artifacts
);

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Used for JSON deserialization")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Used for JSON deserialization")]
public record GitHubArtifact(
    [property: JsonPropertyName("id")]                   long        Id,
    [property: JsonPropertyName("node_id")]              string      NodeId,
    [property: JsonPropertyName("name")]                 string      Name,
    [property: JsonPropertyName("size_in_bytes")]        int         SizeInBytes,
    [property: JsonPropertyName("url")]                  string      Url,
    [property: JsonPropertyName("archive_download_url")] string      ArchiveDownloadUrl,
    [property: JsonPropertyName("expired")]              bool        Expired,
    [property: JsonPropertyName("created_at")]           DateTime    CreatedAt,
    [property: JsonPropertyName("expires_at")]           DateTime    ExpiresAt,
    [property: JsonPropertyName("updated_at")]           DateTime    UpdatedAt,
    [property: JsonPropertyName("workflow_run")]         WorkflowRun WorkflowRun
);