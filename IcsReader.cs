using Ical.Net;
using Ical.Net.DataTypes;
using Mathilda.Extensions;
using Mathilda.Models;

namespace Mathilda
{
    public interface IIcsReader
    {
        public Task<List<CalendarEvent>> ReadIcsFileManually(string filePath, CalendarFilters f);
        public Task<List<CalendarEvent>> ReadIcsFile(string filePath, CalendarFilters f);
    }
    public class IcsReader : IIcsReader
    {
        public async Task<List<CalendarEvent>> ReadIcsFileManually(string filePath, CalendarFilters f)
        {
            var events = new List<CalendarEvent>();
            var lines = await File.ReadAllLinesAsync(filePath);
            CalendarEvent currentEvent = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("BEGIN:VEVENT"))
                {
                    currentEvent = new CalendarEvent();
                    Console.WriteLine("BEGIN:VEVENT");
                }
                else if (line.StartsWith("SUMMARY:") && currentEvent != null)
                {
                    currentEvent.Summary = line.Substring("SUMMARY:".Length);
                    Console.WriteLine($"SUMMARY: {currentEvent.Summary}");
                }
                else if (line.StartsWith("DTSTART:") && currentEvent != null)
                {
                    currentEvent.Start = DateTimeExtensions.ParseDateTime(line.Substring("DTSTART:".Length));
                    Console.WriteLine($"DTSTART: {currentEvent.Start}");
                }
                else if (line.StartsWith("DTEND:") && currentEvent != null)
                {
                    currentEvent.End = DateTimeExtensions.ParseDateTime(line.Substring("DTEND:".Length));
                    Console.WriteLine($"DTEND: {currentEvent.End}");
                }
                else if (line.StartsWith("END:VEVENT") && currentEvent != null)
                {
                    events.Add(currentEvent);
                    Console.WriteLine("END:VEVENT");
                    currentEvent = null; // Reset for the next event
                }
            }

            if (f.EnableFilters)
                return events
                    .Where(e => e.Start >= f.Start && e.End <= f.End)
                    .OrderBy(e => e.Start).ToList();
            return events;
        }

        /// <summary>
        /// Used package to read the ics file, some events were being skipped e.g Google meet
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public async Task<List<CalendarEvent>> ReadIcsFile(string filePath, CalendarFilters f)
        {
            var events = new List<CalendarEvent>();
            var calendarData = await File.ReadAllTextAsync(filePath);
            var calendar = Calendar.Load(calendarData);

            var startDate = f.Start;
            var endDate = f.End;

            foreach (var calendarEvent in calendar.Events)
            {
                if (calendarEvent.Organizer == null)
                {
                    continue;
                }

                if (calendarEvent.RecurrenceRules != null && calendarEvent.RecurrenceRules.Count > 0)
                {
                    var occurrences = calendarEvent.GetOccurrences(startDate, endDate);
                    foreach (var occurrence in occurrences)
                    {
                        var occurrenceStart = occurrence.Period.StartTime.AsSystemLocal;
                        var occurrenceEnd = occurrence.Period.EndTime.AsSystemLocal;

                        if (occurrenceStart >= startDate && occurrenceEnd <= endDate)
                        {
                            events.Add(new CalendarEvent
                            {
                                Start = occurrenceStart,
                                End = occurrenceEnd,
                                Summary = calendarEvent.Summary
                            });
                        }
                    }
                }
                else
                {
                    var eventStart = calendarEvent.Start.AsSystemLocal;
                    var eventEnd = calendarEvent.End.AsSystemLocal;

                    if (eventStart >= startDate && eventEnd <= endDate)
                    {
                        events.Add(new CalendarEvent
                        {
                            Start = eventStart,
                            End = eventEnd,
                            Summary = calendarEvent.Summary
                        });
                    }
                }
            }
            return events.OrderBy(e => e.Start).DistinctBy(x => x.Start).ToList();
        }


    }
}