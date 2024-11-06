using Atlassian.Jira;

namespace Mathilda;

public interface ITicketService
{
    public Task<List<Issue>> GetInProgressTickets(DateTime start, DateTime end);
}

public class JiraService : ITicketService
{
    private const string jqlCurrentUserActiveTickets = "assignee = currentuser() AND status WAS \"In Progress\" DURING (\"2024-10-01\", \"2024-11-05\")";
    public async Task<List<Issue>> GetInProgressTickets(DateTime start, DateTime end)
    {
        // create a connection to JIRA using the Rest client
        var jira = Jira.CreateRestClient("https://company.atlassian.net/", "EMAIL/USERNAMR", "API-TOKEN");
        
        var issues = jira.Issues.GetIssuesFromJqlAsync(
            new IssueSearchOptions(
                $"assignee = currentuser() AND status WAS \"In Progress\" DURING (\"{start:yyyy-MM-dd}\", \"{end:yyyy-MM-dd}\")"));
        
        return issues.Result.ToList();
    }
}