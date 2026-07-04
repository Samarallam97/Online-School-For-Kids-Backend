using Domain.Interfaces.Services.Shared;
using Microsoft.Extensions.Options;


namespace Infrastructure.Services.Shared;

public class GoogleMeetOptions
{
    /// <summary>Absolute path to the service-account JSON key file.</summary>
    public string ServiceAccountKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// The Google Workspace user the service account impersonates.
    /// Must have Calendar API enabled and domain-wide delegation granted.
    /// </summary>
    public string CalendarOwnerEmail { get; set; } = string.Empty;
}

public class GoogleMeetService : IGoogleMeetService
{
    private readonly GoogleMeetOptions _opts;

    public GoogleMeetService(IOptions<GoogleMeetOptions> opts)
        => _opts = opts.Value;

    //public async Task<string> CreateMeetingAsync(
    //    string title,
    //    DateTime startUtc,
    //    DateTime endUtc,
    //    string organizerEmail,
    //    string attendeeEmail,
    //    CancellationToken ct = default)
    //{
    //    var credential = GoogleCredential
    //        .FromFile(_opts.ServiceAccountKeyPath)
    //        .CreateScoped(CalendarService.Scope.Calendar)
    //        .CreateWithUser(_opts.CalendarOwnerEmail);

    //    var service = new CalendarService(new BaseClientService.Initializer
    //    {
    //        HttpClientInitializer = credential,
    //        ApplicationName       = "LearningPlatform",
    //    });

    //    var calEvent = new Event
    //    {
    //        Summary = title,
    //        Start   = new EventDateTime { DateTimeDateTimeOffset = startUtc },
    //        End     = new EventDateTime { DateTimeDateTimeOffset = endUtc },
    //        Attendees = new List<EventAttendee>
    //        {
    //            new() { Email = organizerEmail },
    //            new() { Email = attendeeEmail  },
    //        },
    //        // ConferenceData triggers Meet link generation
    //        ConferenceData = new ConferenceData
    //        {
    //            CreateRequest = new CreateConferenceRequest
    //            {
    //                RequestId             = Guid.NewGuid().ToString(),
    //                ConferenceSolutionKey = new ConferenceSolutionKey
    //                { Type = "hangoutsMeet" },
    //            }
    //        },
    //    };

    //    var insertRequest = service.Events.Insert(calEvent, "primary");
    //    insertRequest.ConferenceDataVersion = 1; // required for Meet link generation
    //    insertRequest.SendUpdates           = EventsResource.InsertRequest.SendUpdatesEnum.All;

    //    var created = await insertRequest.ExecuteAsync(ct);

    //    var meetLink = created.ConferenceData?.EntryPoints?
    //        .FirstOrDefault(e => e.EntryPointType == "video")?.Uri;

    //    if (string.IsNullOrEmpty(meetLink))
    //        throw new InvalidOperationException(
    //            "Google Calendar did not return a Meet link. " +
    //            "Ensure the service account has domain-wide delegation and Calendar API access.");

    //    return meetLink;
    //}

    public async Task<string> CreateMeetingAsync(
    string title,
    DateTime startUtc,
    DateTime endUtc,
    string organizerEmail,
    string attendeeEmail,
    CancellationToken ct = default)
    {
        var meetCode = Guid.NewGuid().ToString("N")[..10];
        return await Task.FromResult(
            $"https://meet.google.com/{meetCode[..3]}-{meetCode[3..7]}-{meetCode[7..]}");
    }
}