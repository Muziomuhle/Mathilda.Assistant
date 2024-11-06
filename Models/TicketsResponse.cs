namespace Mathilda.Models;

public class TicketsResponse
{
    public List<TicketsOnDay> TicketsOnDays { get; set; }
}

public class TicketInfo
{
    public string Type { get; set; }
    public string TicketKey { get; set; }
    public string Summary { get; set; }
}

public class TicketsOnDay
{
    public DateTime Date { get; set; }
    public List<TicketInfo> Tickets { get; set; }
}