using System.Text;
using System.Text.RegularExpressions;

namespace Application.Common;

/// <summary>
/// Slices a timestamped raw transcript (lines prefixed with "[HH:MM:SS]") into
/// the plain text falling within a given time range, with timestamp markers
/// stripped. Used to keep a chunk's transcript in sync when its boundaries
/// are dragged on the timeline.
/// </summary>
public static class TranscriptSlicer
{
    private static readonly Regex TimestampPattern = new(@"^\[(\d{2}):(\d{2}):(\d{2})\]\s*(.*)$", RegexOptions.Compiled);

    /// <summary>Parses "HH:MM:SS" into total seconds. Returns null if the format is invalid.</summary>
    public static int? ParseTimeToSeconds(string? time)
    {
        if (string.IsNullOrWhiteSpace(time)) return null;
        var parts = time.Split(':');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[0], out var h) ||
            !int.TryParse(parts[1], out var m) ||
            !int.TryParse(parts[2], out var s))
            return null;

        return h * 3600 + m * 60 + s;
    }

    public static string FormatSecondsToTime(int totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return ts.ToString(@"hh\:mm\:ss");
    }

    /// <summary>All line timestamps present in the raw transcript, in seconds, ascending.</summary>
    private static List<int> ExtractLineTimestamps(string rawTranscript)
    {
        var times = new List<int>();
        foreach (var line in rawTranscript.Split('\n'))
        {
            var match = TimestampPattern.Match(line.TrimEnd('\r'));
            if (!match.Success) continue;

            var seconds = int.Parse(match.Groups[1].Value) * 3600
                        + int.Parse(match.Groups[2].Value) * 60
                        + int.Parse(match.Groups[3].Value);
            times.Add(seconds);
        }
        return times;
    }

    /// <summary>
    /// Returns the plain text (timestamps stripped) of every line in
    /// <paramref name="rawTranscript"/> whose timestamp falls within
    /// [startSeconds, endSeconds). If endSeconds is null, slices to the end.
    /// </summary>
    public static string Slice(string rawTranscript, int startSeconds, int? endSeconds)
    {
        if (string.IsNullOrWhiteSpace(rawTranscript)) return string.Empty;

        var sb = new StringBuilder();
        foreach (var line in rawTranscript.Split('\n'))
        {
            var match = TimestampPattern.Match(line.TrimEnd('\r'));
            if (!match.Success) continue;

            var lineSeconds = int.Parse(match.Groups[1].Value) * 3600
                             + int.Parse(match.Groups[2].Value) * 60
                             + int.Parse(match.Groups[3].Value);

            if (lineSeconds < startSeconds) continue;
            if (endSeconds.HasValue && lineSeconds >= endSeconds.Value) continue;

            var text = match.Groups[4].Value.Trim();
            if (text.Length == 0) continue;

            if (sb.Length > 0) sb.Append('\n');
            sb.Append(text);
        }

        return sb.ToString();
    }

    /// <summary>Convenience overload accepting "HH:MM:SS" string boundaries.</summary>
    public static string Slice(string rawTranscript, string startTime, string? endTime)
    {
        var start = ParseTimeToSeconds(startTime) ?? 0;
        var end = ParseTimeToSeconds(endTime);
        return Slice(rawTranscript, start, end);
    }

    /// <summary>
    /// Checks whether a given boundary (in seconds) lands exactly on a known
    /// transcript-line timestamp. If not, returns the nearest line timestamps
    /// immediately before and after the boundary so the UI can show the gap
    /// the creator landed in.
    /// </summary>
    public static BoundaryAlignment CheckAlignment(string rawTranscript, int boundarySeconds)
    {
        var times = ExtractLineTimestamps(rawTranscript);
        if (times.Count == 0)
            return new BoundaryAlignment { IsAligned = true }; // nothing to compare against

        if (times.Contains(boundarySeconds))
            return new BoundaryAlignment { IsAligned = true };

        int? before = times.Where(t => t < boundarySeconds).DefaultIfEmpty(-1).Max();
        int? after = times.Where(t => t > boundarySeconds).DefaultIfEmpty(-1).Min();

        return new BoundaryAlignment
        {
            IsAligned = false,
            NearestLineBefore = before is >= 0 ? FormatSecondsToTime(before.Value) : null,
            NearestLineAfter = after is >= 0 ? FormatSecondsToTime(after.Value) : null
        };
    }
}

public class BoundaryAlignment
{
    public bool IsAligned { get; set; }
    public string? NearestLineBefore { get; set; }
    public string? NearestLineAfter { get; set; }
}