using Domain.Entities.Content.Calendar;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using static Application.Commands.Calendar.CreateEventHandler;

namespace Application.Queries.Content.Calendar
{
    public class GetCalendarMonthQuery : IRequest<CalendarMonthDto>
    {
        public string UserId { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public class GetCalendarMonthHandler : IRequestHandler<GetCalendarMonthQuery, CalendarMonthDto>
    {
        private readonly IEventRepository _eventRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly ILogger<GetCalendarMonthHandler> _logger;

        public GetCalendarMonthHandler(
            IEventRepository eventRepo,
            IEnrollmentRepository enrollmentRepo,
            ILogger<GetCalendarMonthHandler> logger)
        {
            _eventRepo = eventRepo;
            _enrollmentRepo = enrollmentRepo;
            _logger = logger;
        }

        public async Task<CalendarMonthDto> Handle(GetCalendarMonthQuery request, CancellationToken ct)
        {
            try
            {
                var startOfMonth = new DateTime(request.Year, request.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var enrolledCourseIds = (await _enrollmentRepo.GetAllAsync(
                    e => e.UserId == request.UserId, ct))
                    .Select(e => e.CourseId)
                    .ToHashSet();

                // Get all events for this month that are relevant to this user
                var events = (await _eventRepo.GetAllAsync(
                    e => e.StartDateTime >= startOfMonth && e.StartDateTime <= endOfMonth,
                    ct))
                    .Where(e => CalendarEventScope.IsRelevantToUser(e, request.UserId, enrolledCourseIds))
                    .ToList();

                // Group events by day
                var eventsByDay = events.GroupBy(e => e.StartDateTime.Day)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Build calendar days
                var days = new List<CalendarDayDto>();
                var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
                var today = DateTime.UtcNow.Date;

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var currentDate = new DateTime(request.Year, request.Month, day);
                    var dayEvents = eventsByDay.ContainsKey(day) ? eventsByDay[day] : new List<Event>();

                    days.Add(new CalendarDayDto
                    {
                        Day = day,
                        IsToday = currentDate == today,
                        HasEvents = dayEvents.Any(),
                        EventCount = dayEvents.Count,
                        Events = dayEvents.Select(e => new EventDto
                        {
                            Id = e.Id,
                            Title = e.Title,
                            Type = e.Type.ToString(),
                            StartDateTime = e.StartDateTime,
                            Color = e.Color,
                            AttendeesCount = e.Attendees.Count
                        }).ToList()
                    });
                }

                // Calculate stats for this week
                var startOfWeek = GetStartOfWeek(today);
                var endOfWeek = startOfWeek.AddDays(7);

                var weekEvents = (await _eventRepo.GetAllAsync(
                    e => e.StartDateTime >= startOfWeek && e.StartDateTime < endOfWeek,
                    ct))
                    .Where(e => CalendarEventScope.IsRelevantToUser(e, request.UserId, enrolledCourseIds))
                    .ToList();

                var eventsThisWeek = weekEvents.Count();
                var deadlinesThisWeek = weekEvents.Count(e => e.Type == EventType.Assignment || e.Type == EventType.Deadline);

                return new CalendarMonthDto
                {
                    Year = request.Year,
                    Month = request.Month,
                    MonthName = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(request.Month)} {request.Year}",
                    Days = days,
                    EventsThisWeek = eventsThisWeek,
                    DeadlinesThisWeek = deadlinesThisWeek
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting calendar month");
                return new CalendarMonthDto();
            }
        }

        private static DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }
    public class CalendarMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty; // "March 2026"
        public List<CalendarDayDto> Days { get; set; } = new();
        public int EventsThisWeek { get; set; }
        public int DeadlinesThisWeek { get; set; }
    }

    public class CalendarDayDto
    {
        public int Day { get; set; }
        public bool IsToday { get; set; }
        public bool HasEvents { get; set; }
        public int EventCount { get; set; }
        public List<EventDto> Events { get; set; } = new();
    }

}