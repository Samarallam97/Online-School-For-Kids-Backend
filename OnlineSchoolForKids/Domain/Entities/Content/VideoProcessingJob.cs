using System;
using System.Collections.Generic;

namespace Domain.Entities.Content;

public class VideoProcessingJob : BaseEntity
{
    public string InstructorId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;

    /// <summary>"upload" or "youtube"</summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>"chunked" (long video/youtube → many lessons) or "single" (short video/youtube → one lesson)</summary>
    public string Mode { get; set; } = "chunked";

    /// <summary>YouTube URL or uploaded file name</summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL of the uploaded video, stored via the file storage service so the
    /// creator can scrub/preview it later without re-uploading. Null for
    /// YouTube-sourced jobs (those use the YouTube URL directly for embedding).
    /// </summary>
    public string? VideoUrl { get; set; }

    /// <summary>
    /// pending | processing | awaiting_correction | awaiting_review |
    /// awaiting_quiz | completed | failed | expired
    /// </summary>
    public string Status { get; set; } = "pending";

    public string? ErrorMessage { get; set; }

    /// <summary>Raw transcript exactly as returned by the AI pipeline.</summary>
    public string? RawTranscript { get; set; }

    /// <summary>Transcript after the creator runs "Check accuracy" and the correction API returns a result.</summary>
    public string? CorrectedTranscript { get; set; }

    /// <summary>Accuracy score (0-100) from the last correction check. Null = never checked.</summary>
    public double? AccuracyScore { get; set; }

    /// <summary>Detected language from the correction API.</summary>
    public string? DetectedLanguage { get; set; }

    /// <summary>
    /// True once the creator has accepted a transcript version (corrected or original)
    /// as final for this job. Any subsequent manual edit clears this flag, requiring
    /// a fresh "Check accuracy" before quiz generation can proceed with confidence.
    /// </summary>
    public bool IsTranscriptApproved { get; set; } = false;

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

    /// <summary>
    /// Draft quiz sets (one per difficulty) generated for this chunk after the
    /// creator finishes editing its boundaries/transcript and clicks "Generate Quiz".
    /// Not persisted as a real Lesson until the creator saves the chunk.
    /// </summary>
    public List<DraftQuizSet> DraftQuizzes { get; set; } = new();

    /// <summary>Whether this chunk has been saved as a real Lesson.</summary>
    public bool IsSaved { get; set; } = false;
    public string? LessonId { get; set; }
}

/// <summary>A draft quiz set for one difficulty level, held on a chunk before final save.</summary>
public class DraftQuizSet
{
    /// <summary>"easy" | "medium" | "hard"</summary>
    public string Difficulty { get; set; } = string.Empty;
    public List<DraftQuizQuestion> Questions { get; set; } = new();
}

public class DraftQuizQuestion
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswer { get; set; }
    public string Explanation { get; set; } = string.Empty;
}