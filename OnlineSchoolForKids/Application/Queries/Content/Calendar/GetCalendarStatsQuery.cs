using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Content.Calendar
{
    public class GetCalendarStatsQuery : IRequest<CalendarStatsDto>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class GetCalendarStatsHandler : IRequestHandler<GetCalendarStatsQuery, CalendarStatsDto>
    {
        private readonly IEventRepository _eventRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly ILogger<GetCalendarStatsHandler> _logger;

        public GetCalendarStatsHandler(
            IEventRepository eventRepo,
            IEnrollmentRepository enrollmentRepo,
            ILogger<GetCalendarStatsHandler> logger)
        {
            _eventRepo = eventRepo;
            _enrollmentRepo = enrollmentRepo;
            _logger = logger;
        }

        public async Task<CalendarStatsDto> Handle(GetCalendarStatsQuery request, CancellationToken ct)
        {
            try
            {
                var now = DateTime.UtcNow;
                var startOfWeek = GetStartOfWeek(now);
                var endOfWeek = startOfWeek.AddDays(7);

                var enrolledCourseIds = (await _enrollmentRepo.GetAllAsync(
                    e => e.UserId == request.UserId, ct))
                    .Select(e => e.CourseId)
                    .ToHashSet();

                var weekEvents = (await _eventRepo.GetAllAsync(
                    e => e.StartDateTime >= startOfWeek && e.StartDateTime < endOfWeek,
                    ct))
                    .Where(e => CalendarEventScope.IsRelevantToUser(e, request.UserId, enrolledCourseIds))
                    .ToList();

                var upcomingEvents = (await _eventRepo.GetAllAsync(
                    e => e.StartDateTime >= now,
                    ct))
                    .Where(e => CalendarEventScope.IsRelevantToUser(e, request.UserId, enrolledCourseIds))
                    .ToList();

                return new CalendarStatsDto
                {
                    EventsThisWeek = weekEvents.Count(),
                    DeadlinesThisWeek = weekEvents.Count(e => e.Type == EventType.Assignment || e.Type == EventType.Deadline),
                    UpcomingEvents = upcomingEvents.Count()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting calendar stats");
                return new CalendarStatsDto();
            }
        }

        private static DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }
    public class CalendarStatsDto
    {
        public int EventsThisWeek { get; set; }
        public int DeadlinesThisWeek { get; set; }
        public int UpcomingEvents { get; set; }
    }
}