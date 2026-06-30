using System;
using System.Collections.Generic;

namespace Domain.Entities.Content;

public class VideoProcessingJob : BaseEntity
{
    public string InstructorId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;

    /// <summary>"upload" or "youtube"</summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>YouTube URL or uploaded file name</summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>pending | processing | awaiting_review | completed | failed</summary>
    public string Status { get; set; } = "pending";

    public string? ErrorMessage { get; set; }
    public string? RawTranscript { get; set; }

    /// <summary>
    /// Course-level metadata returned by the pipeline's "description" object.
    /// Stored here so the instructor can review / edit it before applying it.
    /// </summary>
    public PipelineDescription? Description { get; set; }

    public List<VideoChunk> Chunks { get; set; } = new();
}

/// <summary>
/// Mirrors the pipeline API's DescribeResponse object.
/// All fields are editable by the instructor on the review page.
/// </summary>
public class PipelineDescription
{
    public string Summary { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string ToneAndStyle { get; set; } = string.Empty;
    public List<string> SeoTags { get; set; } = new();
}

public class VideoChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Chunk index from the AI segmentation (0-based)</summary>
    public int Index { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;

    /// <summary>Set by instructor during review</summary>
    public string? SectionId { get; set; }
    public string? LessonTitle { get; set; }

    /// <summary>Whether this chunk has been saved as a lesson</summary>
    public bool IsSaved { get; set; } = false;
    public string? LessonId { get; set; }
}