using Mathilda.Models;
using static Mathilda.Models.ClockifyConstants;
using System.Reflection;
using Mathilda.Extensions;
using System.Globalization;

namespace Mathilda
{
    public interface IClockifyService
    {
        public Task<ExecutionSummary> CreateMeetingTimeEntries(List<CalendarEvent> calendarEvents);
        public Task<ExecutionSummary> CreateReccurringMeetingTimeEntries(List<ReccurringTimeEntryRequest> timeEntryRequests);
        public Task<ExecutionSummary> CreateProductiveTimeEntries(List<CalendarEvent> calendarEvents, List<TimeEntryRequest> timeEntryRequests);
    }
    public class ClockifyService : IClockifyService
    {
        private readonly IClockifyClient _clockifyClient;
        public ClockifyService(IClockifyClient clockifyClient) 
        { 
            _clockifyClient = clockifyClient;
        }
        public async Task<ExecutionSummary> CreateMeetingTimeEntries(List<CalendarEvent> calendarEvents)
        {
            // We want to take all calendar events and make them time entries.
            // Calendar events are meetings. The tasks under meeting dont make much sense.
            // How do we separate a Fix from Improvement.
            var projectId = ProjectConstants.Meetings;

            // Build the request objects. These will be executed with a time delay.
            var timeEntries = calendarEvents.Select(aT => new TimeEntryEvent()
            {
                Start = aT.Start,
                End = aT.End,
                Description = aT.Summary,
                ProjectId = projectId,
                TaskId = GetTaskIdByDescription(aT.Summary)
            }).ToList();

            return await SendTimeEntries(timeEntries);

            #region Local Funcs
            static string GetTaskIdByDescription(string description)
                => description.Contains("Voda") ? 
                TaskConstants.VodaMeeting :
                description.Contains("FFC") || description.Contains("First Friday Connect") ?
                TaskConstants.Other :
                TaskConstants.Improvement;

            #endregion 
        }

        public async Task<ExecutionSummary> CreateReccurringMeetingTimeEntries(List<ReccurringTimeEntryRequest> timeEntryRequests)
        {
            var projectId = ProjectConstants.Meetings;

            var events = new List<TimeEntryEvent>();

            foreach (var ter in timeEntryRequests)
            {
                for (var date = ter.Start; date <= ter.End; date = date.AddDays(1))
                {
                    if (ter.daysOfWeek.Contains(date.DayOfWeek))
                    {
                        var startDateTime = DateTimeExtensions.ParseDateTime($"{date:yyyy-MM-dd}T{ter.StartTime}", DateTimeStyles.AssumeLocal);
                        var endDateTime = DateTimeExtensions.ParseDateTime($"{date:yyyy-MM-dd}T{ter.EndTime}", DateTimeStyles.AssumeLocal);
                        events.Add(new TimeEntryEvent
                        {
                            Description = ter.Description,
                            Start = DateTimeExtensions.ParseIso8601ToDate(DateTimeExtensions.FormatDateToIso8601(startDateTime)),
                            End = DateTimeExtensions.ParseIso8601ToDate(DateTimeExtensions.FormatDateToIso8601(endDateTime)),
                            ProjectId = projectId,
                            TaskId = GetTaskConstantValue(ter.TaskName) ?? TaskConstants.Improvement
                        });

                        date = date.AddDays(ter.Interval - 1);
                    }
                }
            }

            return await SendTimeEntries(events);
        }

        public async Task<ExecutionSummary> CreateProductiveTimeEntries(List<CalendarEvent> calendarEvents, List<TimeEntryRequest> timeEntryRequests)
        {
            var timeEntries = new List<TimeEntryEvent>();
            var groupedEvents = calendarEvents.GroupBy(e => e.Start.Date);
            var groupedRequests = timeEntryRequests.GroupBy(r => r.Start.Date);

            var allDates = calendarEvents.Select(e => e.Start.Date)
                                         .Union(timeEntryRequests.Select(r => r.Start.Date))
                                         .Distinct();

            foreach (var date in allDates)
            {
                // Skip weekends
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                var events = groupedEvents.FirstOrDefault(g => g.Key == date)?.OrderBy(e => e.Start).ToList() ?? new List<CalendarEvent>();
                var requestForDate = groupedRequests.FirstOrDefault(g => g.Key == date)?.FirstOrDefault();

                if (requestForDate == null)
                {
                    continue;
                }

                DateTime workDayStart = new(date.Year, date.Month, date.Day, 8, 0, 0);
                DateTime workDayEnd = new(date.Year, date.Month, date.Day, 17, 0, 0);
                DateTime lunchStart = new(date.Year, date.Month, date.Day, 12, 0, 0);
                DateTime lunchEnd = new(date.Year, date.Month, date.Day, 13, 0, 0);
                DateTime currentTime = workDayStart;

                foreach (var calendarEvent in events)
                {
                    if (currentTime < calendarEvent.Start)
                    {
                        if (currentTime < lunchStart && calendarEvent.Start > lunchEnd)
                        {
                            var productiveTime = CreateProductiveTimeEntry(currentTime, lunchStart, requestForDate);
                            timeEntries.Add(productiveTime);
                            currentTime = lunchEnd;
                        }

                        if (currentTime < calendarEvent.Start)
                        {
                            var productiveTime = CreateProductiveTimeEntry(currentTime, calendarEvent.Start, requestForDate);
                            timeEntries.Add(productiveTime);
                        }
                    }

                    currentTime = calendarEvent.End > currentTime ? calendarEvent.End : currentTime;
                }

                if (currentTime < lunchStart)
                {
                    var productiveTime = CreateProductiveTimeEntry(currentTime, lunchStart, requestForDate);
                    timeEntries.Add(productiveTime);
                    currentTime = lunchEnd;
                }
                else if (currentTime < lunchEnd)
                {
                    currentTime = lunchEnd;
                }

                if (currentTime < workDayEnd)
                {
                    var productiveTime = CreateProductiveTimeEntry(currentTime, workDayEnd, requestForDate);
                    timeEntries.Add(productiveTime);
                }
            }

            return await SendTimeEntries(timeEntries);
        }

        /// <summary>
        /// Will probably use this later 
        /// </summary>
        /// <param name="calendarEvents"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private static void AddRecurringMeetings(List<CalendarEvent> calendarEvents, DateTime startDate, DateTime endDate)
        {
            // Define the recurring meetings
            var dailyStandup = GenerateRecurringMeetings(
                startDate, endDate, "09:15", "09:30",
                new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                "Titans - Daily Standup", 1);

            var biweeklyMeeting = GenerateRecurringMeetings(
                startDate, endDate, "10:00", "11:00",
                new List<DayOfWeek> { DayOfWeek.Monday },
                "Biweekly Meeting", 14);

            calendarEvents.AddRange(dailyStandup);
            calendarEvents.AddRange(biweeklyMeeting);

            calendarEvents.Sort((e1, e2) => e1.Start.CompareTo(e2.Start));

            static List<CalendarEvent> GenerateRecurringMeetings(DateTime startDate, DateTime endDate, 
                string startTime, string endTime, List<DayOfWeek> daysOfWeek, string description, int interval)
            {

                var events = new List<CalendarEvent>();

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (daysOfWeek.Contains(date.DayOfWeek))
                    {
                        var startDateTime = DateTime.Parse($"{date:yyyy-MM-dd}T{startTime}");
                        var endDateTime = DateTime.Parse($"{date:yyyy-MM-dd}T{endTime}");

                        events.Add(new CalendarEvent
                        {
                            Summary = description,
                            Start = startDateTime,
                            End = endDateTime
                        });

                        // Skip to the same day after the specified interval
                        date = date.AddDays(interval - 1);
                    }
                }

                return events;
            }
        }
        private static TimeEntryEvent CreateProductiveTimeEntry(DateTime start, DateTime end, TimeEntryRequest requestForDate)
        {
            var projectId = ProjectConstants.Productive;
            return new TimeEntryEvent
            {
                Description = requestForDate.Description,
                Start = DateTimeExtensions.ParseIso8601ToDate(DateTimeExtensions.FormatDateToIso8601(start)),
                End = DateTimeExtensions.ParseIso8601ToDate(DateTimeExtensions.FormatDateToIso8601(end)),
                ProjectId = projectId,
                TaskId = GetTaskConstantValue(requestForDate.TaskName) ?? TaskConstants.Development
            };
        }
        private static string? GetTaskConstantValue(string input)
        {
            Type type = typeof(TaskConstants);
            FieldInfo field = type.GetField(input, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public);

            if (field != null)
            {
                return field.GetValue(null) as string;
            }

            return null;
        }

        private async Task<ExecutionSummary> SendTimeEntries(List<TimeEntryEvent> timeEntries)
        {
            var summary = new ExecutionSummary()
            {
                TotalRequested = timeEntries.Count
            };

            var uri = string.Format(PathConstants.CreateTimeEntry, WorkspaceConstants.Id);

            foreach (var entry in timeEntries)
            {
                var res = await _clockifyClient.Post<TimeEntryEvent, TimeEntryResponse>(entry, uri);

                if (res.IsSuccess && res.Result != null)
                {
                    summary.TimeEntryResponses.Add(res.Result);
                }
                else
                {
                    summary.TimeEntryFailures.Add(entry);
                }

                await Task.Delay(10); // Rate limiting
            }

            summary.TotalSuccess = summary.TimeEntryResponses.Count;
            summary.TotalFailed = summary.TimeEntryFailures.Count;

            return summary;
        } 
    }
}
