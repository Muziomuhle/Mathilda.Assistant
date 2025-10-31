using Atlassian.Jira;
using Mathilda.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Mathilda;

public interface ITicketService
{
    public Task<TicketsResponse> GetInProgressTickets(DateTime start, DateTime end);
}

public class JiraService : ITicketService
{
    private const string jqlCurrentUserActiveTickets = "assignee = currentuser() AND status WAS \"In Progress\" DURING (\"2024-10-01\", \"2024-11-05\")";
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    public JiraService(IConfiguration configuration)
    {
        _configuration = configuration;
        // Create and configure a single HttpClient
        _httpClient = new HttpClient();
        var jiraUrl = _configuration.GetSection("Jira:BaseUrl").Value;
        var user = _configuration.GetSection("Jira:User").Value;
        var token = _configuration.GetSection("Jira:Token").Value;

        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{token}"));
        _httpClient.BaseAddress = new Uri(jiraUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    public async Task<TicketsResponse> GetInProgressTicketsV2(DateTime start, DateTime end)
    {
        // create a connection to JIRA using the Rest client
        var jira = Jira.CreateRestClient(
            _configuration.GetSection("Jira:BaseUrl").Value,
            _configuration.GetSection("Jira:User").Value,
            _configuration.GetSection("Jira:Token").Value);

        var response = new TicketsResponse()
        {
            TicketsOnDays = new List<TicketsOnDay>()
        };
        
        for (DateTime day = start; day <= end; day = day.AddDays(1))
        {
            var issuesOnDay = await jira.Issues.GetIssuesFromJqlAsync(
                new IssueSearchOptions(
                    $"assignee = currentuser() AND status WAS \"In Progress\" ON (\"{day:yyyy-MM-dd}\")"));

            var ticketsOnDay = issuesOnDay.Select(x => new TicketInfo()
            {
                Summary = x.Summary,
                TicketKey = x.Key.Value,
                Type = x.Type.Name,
            }).ToList();
            
            response.TicketsOnDays.Add(new TicketsOnDay()
            {
                Date = day,
                Tickets = ticketsOnDay
            });
        }
        
        return response;
    }

    public async Task<TicketsResponse> GetInProgressTickets(DateTime start, DateTime end)
    {
        var response = new TicketsResponse()
        {
            TicketsOnDays = new List<TicketsOnDay>()
        };

        for (DateTime day = start; day <= end; day = day.AddDays(1))
        {
            // This is the correct v3 API endpoint
            var url = "rest/api/3/search/jql";

            var jql = $"assignee = currentuser() AND status WAS \"In Progress\" ON (\"{day:yyyy-MM-dd}\")";

            // The JQL must be sent in the request BODY
            var requestBody = new
            {
                jql = jql,
                fields = new[] { "summary", "issuetype" }, // Ask only for what you need
                maxResults = 250 // Get up to 250 issues per day
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                // Make the POST request
                var httpResponse = await _httpClient.PostAsync(url, content);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    // This will show you the *real* error from Jira
                    var error = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Jira API failed on {day:yyyy-MM-dd} with status {httpResponse.StatusCode}: {error}");
                }

                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                // --- Manually parse the JSON response ---
                var ticketsOnDay = new List<TicketInfo>();
                using (var doc = JsonDocument.Parse(jsonResponse))
                {
                    if (doc.RootElement.TryGetProperty("issues", out var issues))
                    {
                        foreach (var issue in issues.EnumerateArray())
                        {
                            ticketsOnDay.Add(new TicketInfo
                            {
                                TicketKey = issue.GetProperty("key").GetString(),
                                Summary = issue.GetProperty("fields").GetProperty("summary").GetString(),
                                Type = issue.GetProperty("fields").GetProperty("issuetype").GetProperty("name").GetString()
                            });
                        }
                    }
                }

                response.TicketsOnDays.Add(new TicketsOnDay()
                {
                    Date = day,
                    Tickets = ticketsOnDay
                });
            }
            catch (Exception ex)
            {
                // Log the error but continue the loop
                Console.WriteLine(ex.Message);

                // Add an empty entry for the day that failed
                response.TicketsOnDays.Add(new TicketsOnDay()
                {
                    Date = day,
                    Tickets = new List<TicketInfo>() // Empty list
                });
            }
        }
        return response;
    }
}