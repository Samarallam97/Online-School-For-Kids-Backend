using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Content.Calendar
{
    public class GetUpcomingEventsQuery : IRequest<IEnumerable<UpcomingEventDto>>
    {
        public string UserId { get; set; } = string.Empty;
        public int Limit { get; set; } = 10;
    }

    public class GetUpcomingEventsHandler : IRequestHandler<GetUpcomingEventsQuery, IEnumerable<UpcomingEventDto>>
    {
        private readonly IEventRepository _eventRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly ILogger<GetUpcomingEventsHandler> _logger;

        public GetUpcomingEventsHandler(
            IEventRepository eventRepo,
            IEnrollmentRepository enrollmentRepo,
            ILogger<GetUpcomingEventsHandler> logger)
        {
            _eventRepo = eventRepo;
            _enrollmentRepo = enrollmentRepo;
            _logger = logger;
        }

        public async Task<IEnumerable<UpcomingEventDto>> Handle(GetUpcomingEventsQuery request, CancellationToken ct)
        {
            try
            {
                var now = DateTime.UtcNow;

                var events = await _eventRepo.GetAllAsync(
                    e => e.StartDateTime >= now,
                    ct);

                var enrolledCourseIds = (await _enrollmentRepo.GetAllAsync(
                    e => e.UserId == request.UserId, ct))
                    .Select(e => e.CourseId)
                    .ToHashSet();

                return events
                    .Where(e => CalendarEventScope.IsRelevantToUser(e, request.UserId, enrolledCourseIds))
                    .OrderBy(e => e.StartDateTime)
                    .Take(request.Limit)
                    .Select(e => new UpcomingEventDto
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Type = e.Type.ToString(),
                        TypeIcon = GetIconForEventType(e.Type),
                        Date = e.StartDateTime.ToString("ddd, MMM dd"),
                        Time = e.StartDateTime.ToString("h:mm tt"),
                        InstructorName = e.InstructorName,
                        AttendeesCount = e.Attendees.Count,
                        Color = e.Color,
                        CanJoin = e.Type == EventType.LiveSession || e.Type == EventType.Webinar,
                        CanView = e.Type == EventType.Assignment
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming events");
                return Enumerable.Empty<UpcomingEventDto>();
            }
        }
        private static string GetIconForEventType(EventType type)
        {
            return type switch
            {
                EventType.LiveSession => "📹",
                EventType.Assignment => "📝",
                EventType.StudyGroup => "👥",
                EventType.Webinar => "🎥",
                EventType.Exam => "📋",
                _ => "📅"
            };
        }
    }
    public class UpcomingEventDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string TypeIcon { get; set; } = string.Empty; // "📹", "📝", "👥", "🎥"
        public string Date { get; set; } = string.Empty; // "Tue, Jan 27"
        public string Time { get; set; } = string.Empty; // "10:00 AM"
        public string? InstructorName { get; set; }
        public int AttendeesCount { get; set; }
        public string Color { get; set; } = string.Empty; // Background color
        public bool CanJoin { get; set; } // If it's a live session
        public bool CanView { get; set; } // If it's an assignment
    }
}