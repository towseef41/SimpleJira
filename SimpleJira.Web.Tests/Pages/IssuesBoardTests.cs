using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SimpleJira.Contracts;
using SimpleJira.Web.Components.Pages;
using SimpleJira.Web.Services;
using Xunit;

namespace SimpleJira.Web.Tests.Pages;

public class IssuesBoardTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly FakeIssueHandler _handler;
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public IssuesBoardTests()
    {
        _handler = new FakeIssueHandler(_projectId, _userId);
        var http = new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        _ctx.Services.AddSingleton<AuthService>();
        _ctx.Services.AddSingleton(_handler);
        _ctx.Services.AddScoped(_ => new JiraApiClient(http, _ctx.Services.GetRequiredService<AuthService>()));
    }

    [Fact]
    public void RendersStoryPointsAndAssigneeInitials()
    {
        var cut = RenderIssues();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(">5<", cut.Markup);
            Assert.Contains("Ja", cut.Markup); // initials from Jane
        });
    }

    [Fact]
    public void AssigneeDropdownAssignsUser()
    {
        var cut = RenderIssues();
        var avatarButton = cut.FindAll("button")
            .First(b => b.ClassList.Contains("btn-light") && b.TextContent.Contains("Ja", StringComparison.OrdinalIgnoreCase));

        avatarButton.Click();
        cut.WaitForAssertion(() =>
        {
            var listItem = cut.FindAll("button")
                .First(b => b.TextContent.Contains("Jane Doe", StringComparison.OrdinalIgnoreCase));
            listItem.Click();
        });

        Assert.Equal(_userId, _handler.LastAssignedUserId);
        Assert.Equal(_handler.Issues.First().Id, _handler.LastAssignedIssueId);
    }

    [Fact]
    public void DragDropMovesCard()
    {
        var cut = RenderIssues();
        var card = cut.Find(".issue-card");
        card.TriggerEvent("ondragstart", new DragEventArgs());

        var inProgressColumn = cut.FindAll(".board-column").ElementAt(1);
        inProgressColumn.TriggerEvent("ondrop", new DragEventArgs());

        cut.WaitForAssertion(() =>
        {
            var todoCount = cut.FindAll(".board-column").ElementAt(0).QuerySelectorAll(".issue-card").Length;
            var inProgressCount = cut.FindAll(".board-column").ElementAt(1).QuerySelectorAll(".issue-card").Length;
            Assert.Equal(0, todoCount);
            Assert.Equal(1, inProgressCount);
            Assert.Equal(IssueStatus.InProgress, _handler.Issues.First().Status);
        });
    }

    [Fact]
    public void DragDropShowsErrorOnFailure()
    {
        _handler.FailStatusUpdate = true;
        var cut = RenderIssues();
        var card = cut.Find(".issue-card");
        card.TriggerEvent("ondragstart", new DragEventArgs());

        var doneColumn = cut.FindAll(".board-column").ElementAt(2);
        doneColumn.TriggerEvent("ondrop", new DragEventArgs());

        cut.WaitForAssertion(() =>
            Assert.Contains("status update failed", cut.Markup));
    }

    [Fact]
    public void DetailModalSaveFailureShowsError()
    {
        _handler.FailUpdate = true;
        var cut = RenderIssues();
        cut.Find(".issue-card").Click();

        var saveButton = FindButtonByText(cut, "Save");
        saveButton.Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Failed to update issue", cut.Markup));
    }

    [Fact]
    public void DetailModalSaveSuccessClosesModal()
    {
        var cut = RenderIssues();
        cut.Find(".issue-card").Click();

        var saveButton = FindButtonByText(cut, "Save");
        saveButton.Click();

        cut.WaitForAssertion(() =>
            Assert.Empty(cut.FindAll("h5").Where(h => h.TextContent.Contains("Issue Details"))));
    }

    private IRenderedComponent<Issues> RenderIssues()
    {
        _ctx.Services.GetRequiredService<AuthService>().SetToken("t");
        return _ctx.RenderComponent<Issues>(p => p.Add(c => c.ProjectId, _projectId));
    }

    private static IElement FindButtonByText(IRenderedFragment cut, string text) =>
        cut.FindAll("button").First(b => b.TextContent.Contains(text, StringComparison.OrdinalIgnoreCase));

    public void Dispose() => _ctx.Dispose();

    private class FakeIssueHandler : HttpMessageHandler
    {
        public List<IssueDto> Issues { get; }
        public bool FailStatusUpdate { get; set; }
        public bool FailUpdate { get; set; }
        public Guid? LastAssignedIssueId { get; private set; }
        public Guid? LastAssignedUserId { get; private set; }

        private readonly Guid _projectId;
        private readonly Guid _userId;

        public FakeIssueHandler(Guid projectId, Guid userId)
        {
            _projectId = projectId;
            _userId = userId;

            Issues = new List<IssueDto>
            {
                new IssueDto(
                    Guid.NewGuid(),
                    _projectId,
                    "Test Issue",
                    "Summary",
                    5,
                    IssueStatus.Todo,
                    new UserDto(_userId, "Jane Doe"),
                    null,
                    0,
                    new List<Guid>())
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            if (request.Method == HttpMethod.Get && path == "/projects")
            {
                var payload = new[]
                {
                    new ProjectDto(_projectId, "Demo", "DEMO", "Software", null, null, null)
                };
                return Task.FromResult(JsonResponse(payload));
            }

            if (request.Method == HttpMethod.Get && path.EndsWith("users", StringComparison.OrdinalIgnoreCase))
            {
                var users = new[] { new UserDto(_userId, "Jane Doe") };
                return Task.FromResult(JsonResponse(users));
            }

            if (request.Method == HttpMethod.Get && path.EndsWith($"projects/{_projectId}/issues"))
            {
                return Task.FromResult(JsonResponse(Issues));
            }

            if (request.Method == HttpMethod.Patch && path.Contains("/issues/") && path.EndsWith("/status"))
            {
                if (FailStatusUpdate)
                    throw new HttpRequestException("status update failed");

                var issueId = ExtractIssueId(path);
                var idx = Issues.FindIndex(i => i.Id == issueId);
                if (idx >= 0)
                {
                    var i = Issues[idx];
                    Issues[idx] = new IssueDto(
                        i.Id,
                        i.ProjectId,
                        i.Title,
                        i.Summary,
                        i.StoryPoints,
                        IssueStatus.InProgress,
                        i.Assignee,
                        i.Reporter,
                        i.CommentsCount,
                        i.LinkedIssueIds);
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            if (request.Method == HttpMethod.Patch && path.EndsWith("/assignee"))
            {
                var issueId = ExtractIssueId(path);
                LastAssignedIssueId = issueId;
                LastAssignedUserId = _userId;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            if (request.Method == HttpMethod.Patch && path.StartsWith("/issues/") && !path.EndsWith("/status") && !path.EndsWith("/assignee"))
            {
                if (FailUpdate)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static Guid ExtractIssueId(string path)
        {
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return Guid.Parse(parts[1]);
        }

        private static HttpResponseMessage JsonResponse<T>(T payload) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
    }
}
