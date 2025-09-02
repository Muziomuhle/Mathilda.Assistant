using Mathilda.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mathilda.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TimeEntryController : ControllerBase
    {
        private readonly ILogger<TimeEntryController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IIcsReader _reader;
        private readonly IClockifyService _clockifyService;
        private readonly ITicketService _ticketService;
        private readonly string _icsFilePath;

        public TimeEntryController(ILogger<TimeEntryController> logger, IConfiguration configuration, 
            IIcsReader reader, IClockifyService clockifyService, ITicketService ticketService)
        {
            _logger = logger;
            _configuration = configuration;
            _reader = reader;
            _clockifyService = clockifyService;
            _ticketService = ticketService;
            _icsFilePath = _configuration.GetSection("Paths:Ics").Value;
        }

        [HttpGet("GetCalendarEvents")]
        public async Task<IActionResult> GetCalendarEvents([FromQuery] DateTime Start, [FromQuery] DateTime End)
        {
            // Not given we use current month.
            if (Start == default || End == default)
            {
                Start = new(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                End = Start.AddMonths(1).AddDays(-1);
            }

            var filter = new CalendarFilters()
            {
                EnableFilters = true,
                Start = Start,
                End = End
            };

            var events = await _reader.ReadIcsFile(_icsFilePath, filter);

            return Ok(events);
        }

        [HttpPost("CreateMeetingFromCalendarEvents")]
        public async Task<IActionResult> CreateMeetingFromCalendarEvents([FromQuery] DateTime Start, [FromQuery] DateTime End)
        {
            // Not given we use current month.
            if (Start == default || End == default)
            {
                Start = new(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                End = Start.AddMonths(1).AddDays(-1);
            }

            var filter = new CalendarFilters()
            {
                EnableFilters = true,
                Start = Start,
                End = End
            };

            var events = await _reader.ReadIcsFile(_icsFilePath, filter);
            var result = await _clockifyService.CreateMeetingTimeEntries(events);
            return Ok(result);
        }


        [HttpPost("CreateProductiveEvent")]
        public async Task<IActionResult> CreateProductiveEvent([FromBody] List<TimeEntryRequest> requests)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var filter = new CalendarFilters()
            {
                EnableFilters = true,
                Start = requests.Min(a=> a.Start),
                End = requests.Max(a=> a.End)
            };

            var events = 
                await _reader.ReadIcsFile(_icsFilePath, filter);
            var result = 
                await _clockifyService.CreateProductiveTimeEntries(events, requests);
            return Ok(result);
        }

        [HttpPost("CreateReccurringMeetingEvent")]
        public async Task<IActionResult> CreateReccurringMeetingEvent([FromBody] List<ReccurringTimeEntryRequest> requests)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = 
                await _clockifyService.CreateReccurringMeetingTimeEntries(requests);
            return Ok(result);
        }
        
        [HttpGet("Tickets")]
        public async Task<IActionResult> GetTickets([FromQuery] DateTime Start, [FromQuery] DateTime End)
        {
            // Not given we use current month.

            var tickets = await _ticketService.GetInProgressTickets(Start, End);

            return Ok(tickets);
        }
        
        [HttpPost("JiraTimeEntries")]
        public async Task<IActionResult> ProcessTimeEntry([FromQuery] DateTime Start, [FromQuery] DateTime End)
        {
            // Not given we use current month.

            var tickets = await _ticketService.GetInProgressTickets(Start, End);
            
            var filter = new CalendarFilters()
            {
                EnableFilters = true,
                Start = Start,
                End = End
            };

            var requests = new List<TimeEntryRequest>();
            foreach (var ticketsOnDay in tickets.TicketsOnDays)
            {
                if (!ticketsOnDay.Tickets.Any())
                    continue;

               requests.Add(new TimeEntryRequest()
               {
                   Description = $"{ticketsOnDay.Tickets[0].TicketKey} | {ticketsOnDay.Tickets[0].Summary}",
                   Start = ticketsOnDay.Date,
                   End = ticketsOnDay.Date,
                   TaskName = "Development"
               }); 
            }

            var events = 
                await _reader.ReadIcsFile(_icsFilePath, filter);
            
            var timeEntryResult = await _clockifyService.CreateMeetingTimeEntries(events);
            
            if (requests.Any())
            {
                var productiveEntryResutls = 
                    await _clockifyService.CreateProductiveTimeEntries(events, requests);
            }
          

            return Ok(tickets);
        }
    }
}
