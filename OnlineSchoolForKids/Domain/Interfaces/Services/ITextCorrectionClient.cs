using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces.Services;

public interface ITextCorrectionClient
{
    Task<TextCorrectionResult> CorrectAsync(string transcript, CancellationToken ct);
}

public class TextCorrectionResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Language { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public string CorrectedText { get; set; } = string.Empty;
    public double Accuracy { get; set; }
}