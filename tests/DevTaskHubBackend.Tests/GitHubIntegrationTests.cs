using System.Net;
using System.Text;
using DevTaskHubBackend.Data;
using Microsoft.AspNetCore.Http;
using Xunit;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Services;
using Microsoft.EntityFrameworkCore;
using TaskStatus = DevTaskHubBackend.Data.TaskStatus;

namespace DevTaskHubBackend.Tests;

public class GitHubPullRequestStatusTests
{
    [Theory]
    [InlineData("open", false, null, "Open")]
    [InlineData("open", true, null, "Draft")]
    [InlineData("closed", false, "2026-11-01T00:00:00Z", "Merged")]
    [InlineData("closed", false, null, "Closed")]
    public void MapStatus_UsesMergedDraftAndClosedSeparately(string state, bool draft, string? mergedAt, string expected)
    {
        DateTime? merged = mergedAt == null ? null : DateTime.Parse(mergedAt).ToUniversalTime();

        var status = GitHubPullRequestRules.MapStatus(state, draft, merged);

        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData("http://github.com/acme/app/pull/15")]
    [InlineData("https://evil.example/acme/app/pull/15")]
    [InlineData("https://github.com.evil/acme/app/pull/15")]
    [InlineData("https://github.com/acme/app/issues/15")]
    [InlineData("not a url")]
    public void InvalidPullRequestUrl_IsRejected(string url)
    {
        var parsed = GitHubPullRequestRules.TryParsePullRequestUrl(url, out _, out _, out _);

        Assert.False(parsed);
    }
}

public class GitHubIntegrationTests
{
    [Fact]
    public async Task Sync_DoesNotCreateTasks_OrChangeTaskStatus()
    {
        await using var fixture = await GitHubFixture.CreateAsync();
        var task = fixture.Task;
        fixture.Handler.Responder = request =>
        {
            Assert.Contains("/pulls/7", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            Assert.DoesNotContain("per_page", request.RequestUri.Query, StringComparison.Ordinal);
            return GitHubFixture.Json(HttpStatusCode.OK, GitHubFixture.Pull(7, "closed", draft: false, mergedAt: "2026-11-02T00:00:00Z"));
        };

        var result = await fixture.Service.SyncPullRequestsAsync(fixture.Developer.UserID, fixture.Project.ProjectId, CancellationToken.None);

        var saved = await fixture.Context.Tasks.SingleAsync(item => item.TaskId == task.TaskId);
        Assert.Equal(0, result.Created);
        Assert.Equal(1, result.Updated);
        Assert.Equal(2, await fixture.Context.Tasks.CountAsync());
        Assert.Equal(nameof(TaskStatus.ToDo), saved.Status);
        Assert.Equal(nameof(TaskPriority.High), saved.Priority);
        Assert.Equal("Keep this title", saved.Title);
        Assert.Equal(fixture.Developer.UserID, saved.AssignedToUserId);
        Assert.Equal(nameof(PullRequestStatus.Merged), saved.GitHubPRStatus);
        Assert.Null((await fixture.Context.Tasks.SingleAsync(item => item.Title == "Unlinked task")).GitHubPRNumber);
    }

    [Fact]
    public async Task ListingUnlinkedPullRequests_DoesNotChangeTasks()
    {
        await using var fixture = await GitHubFixture.CreateAsync();
        var before = await fixture.Context.Tasks.AsNoTracking().Select(task => new { task.TaskId, task.Status, task.GitHubPRNumber }).ToListAsync();
        fixture.Handler.Responder = _ => GitHubFixture.Json(HttpStatusCode.OK, "[" + GitHubFixture.Pull(99, "open", draft: false, mergedAt: null) + "]");

        var page = await fixture.Service.GetPullRequestsAsync(fixture.Developer.UserID, fixture.Project.ProjectId, 1, 30, null, CancellationToken.None);

        var after = await fixture.Context.Tasks.AsNoTracking().Select(task => new { task.TaskId, task.Status, task.GitHubPRNumber }).ToListAsync();
        Assert.Contains(page.Items, item => item.Number == 99 && item.Linked == false && item.TaskId == null);
        Assert.Equal(before.Select(item => (item.TaskId, item.Status, item.GitHubPRNumber)), after.Select(item => (item.TaskId, item.Status, item.GitHubPRNumber)));
    }

    [Fact]
    public async Task Developer_CanLinkOwnTask_AndCannotLinkAnotherDevelopersTask()
    {
        await using var fixture = await GitHubFixture.CreateAsync();
        fixture.Task.GitHubPRNumber = null;
        fixture.Task.GitHubPRStatus = null;
        fixture.Task.GitHubPRUrl = null;
        await fixture.Context.SaveChangesAsync();
        fixture.Handler.Responder = request =>
            request.RequestUri!.AbsolutePath.EndsWith("/pulls/15", StringComparison.Ordinal)
                ? GitHubFixture.Json(HttpStatusCode.OK, GitHubFixture.Pull(15, "open", draft: true, mergedAt: null))
                : GitHubFixture.Json(HttpStatusCode.NotFound, "{}");

        var linked = await fixture.Service.LinkPullRequestAsync(
            fixture.Developer.UserID,
            fixture.Task.TaskId,
            "https://github.com/acme/app/pull/15",
            CancellationToken.None);

        Assert.Equal(nameof(PullRequestStatus.Draft), linked.Status);
        Assert.True(linked.Draft);
        var saved = await fixture.Context.Tasks.SingleAsync(task => task.TaskId == fixture.Task.TaskId);
        Assert.Equal(nameof(TaskStatus.ToDo), saved.Status);
        Assert.Equal(15, saved.GitHubPRNumber);

        var error = await Assert.ThrowsAsync<AppException>(() => fixture.Service.LinkPullRequestAsync(
            fixture.OtherDeveloper.UserID,
            fixture.Task.TaskId,
            "https://github.com/acme/app/pull/15",
            CancellationToken.None));
        Assert.Equal(StatusCodes.Status403Forbidden, error.StatusCode);
    }

    [Fact]
    public async Task Manager_CannotLinkTaskInAnotherManagersProject()
    {
        await using var fixture = await GitHubFixture.CreateAsync();
        fixture.Handler.Responder = _ => throw new InvalidOperationException("GitHub should not be called.");

        var error = await Assert.ThrowsAsync<AppException>(() => fixture.Service.LinkPullRequestAsync(
            fixture.OtherManager.UserID,
            fixture.Task.TaskId,
            "https://github.com/acme/app/pull/15",
            CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, error.StatusCode);
    }

    [Fact]
    public async Task PullRequestFromAnotherRepository_IsRejected()
    {
        await using var fixture = await GitHubFixture.CreateAsync();
        fixture.Handler.Responder = _ => throw new InvalidOperationException("GitHub should not be called.");

        var error = await Assert.ThrowsAsync<AppException>(() => fixture.Service.LinkPullRequestAsync(
            fixture.Developer.UserID,
            fixture.Task.TaskId,
            "https://github.com/other/repo/pull/15",
            CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, error.StatusCode);
    }

    [Fact]
    public async Task DuplicatePullRequestLink_IsRejected()
    {
        await using var fixture = await GitHubFixture.CreateAsync();
        fixture.Task.GitHubPRNumber = null;
        fixture.OtherTask.GitHubPRNumber = 15;
        await fixture.Context.SaveChangesAsync();
        fixture.Handler.Responder = _ => GitHubFixture.Json(HttpStatusCode.OK, GitHubFixture.Pull(15, "open", false, null));

        var error = await Assert.ThrowsAsync<AppException>(() => fixture.Service.LinkPullRequestAsync(
            fixture.Developer.UserID,
            fixture.Task.TaskId,
            "https://github.com/acme/app/pull/15",
            CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, error.StatusCode);
        Assert.Null((await fixture.Context.Tasks.SingleAsync(task => task.TaskId == fixture.Task.TaskId)).GitHubPRNumber);
    }

    [Fact]
    public async Task InvalidUrl_AndGitHubFailures_AreHandled()
    {
        await using var fixture = await GitHubFixture.CreateAsync();
        fixture.Handler.Responder = _ => throw new InvalidOperationException("GitHub should not be called.");
        var invalid = await Assert.ThrowsAsync<AppException>(() => fixture.Service.LinkPullRequestAsync(
            fixture.Developer.UserID,
            fixture.Task.TaskId,
            "https://example.com/acme/app/pull/15",
            CancellationToken.None));
        Assert.Equal(StatusCodes.Status400BadRequest, invalid.StatusCode);

        fixture.Handler.Responder = _ => GitHubFixture.Json(HttpStatusCode.NotFound, "{}");
        var missing = await Assert.ThrowsAsync<AppException>(() => fixture.Service.LinkPullRequestAsync(
            fixture.Developer.UserID,
            fixture.Task.TaskId,
            "https://github.com/acme/app/pull/15",
            CancellationToken.None));
        Assert.Equal(StatusCodes.Status404NotFound, missing.StatusCode);

        fixture.Handler.Responder = _ => GitHubFixture.Json(HttpStatusCode.TooManyRequests, "{}");
        var limited = await Assert.ThrowsAsync<AppException>(() => fixture.Service.LinkPullRequestAsync(
            fixture.Developer.UserID,
            fixture.Task.TaskId,
            "https://github.com/acme/app/pull/15",
            CancellationToken.None));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, limited.StatusCode);
    }

    [Fact]
    public async Task PublicRepositoryList_HidesPrivateRepositories()
    {
        await using var fixture = await GitHubFixture.CreateAsync();
        fixture.Handler.Responder = _ => GitHubFixture.Json(HttpStatusCode.OK, """
            [
              {"name":"app","full_name":"acme/app","private":false,"visibility":"public","html_url":"https://github.com/acme/app","description":"Public","default_branch":"main","owner":{"login":"acme"}},
              {"name":"secret","full_name":"acme/secret","private":true,"visibility":"private","html_url":"https://github.com/acme/secret","description":"Hidden","default_branch":"main","owner":{"login":"acme"}}
            ]
            """);

        var page = await fixture.Service.GetPublicRepositoriesAsync("acme", 1, 30, CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Equal("app", page.Items[0].Name);
        Assert.Equal("public", page.Items[0].Visibility);
    }
}

internal sealed class GitHubFixture : IAsyncDisposable
{
    public required MySqlDbContext Context { get; init; }
    public required GitHubService Service { get; init; }
    public required RecordingHandler Handler { get; init; }
    public required User Developer { get; init; }
    public required User OtherDeveloper { get; init; }
    public required User OtherManager { get; init; }
    public required Projects Project { get; init; }
    public required Tasks Task { get; init; }
    public required Tasks OtherTask { get; init; }

    public static async Task<GitHubFixture> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<MySqlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new MySqlDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var manager = User(UserRole.Manager, "Manager", "manager@example.com", "9000000001");
        var otherManager = User(UserRole.Manager, "Other Manager", "other-manager@example.com", "9000000002");
        var developer = User(UserRole.Developer, "Developer", "developer@example.com", "9000000003");
        var otherDeveloper = User(UserRole.Developer, "Other Developer", "other-developer@example.com", "9000000004");
        context.Users.AddRange(manager, otherManager, developer, otherDeveloper);

        var project = new Projects
        {
            ProjectId = Guid.CreateVersion7(),
            ProjectName = "App",
            Description = "Project",
            Status = nameof(ProjectStatus.Active),
            CreatedByUserId = manager.UserID,
            AssignedToUserId = manager.UserID,
            GitHubOwner = "acme",
            GitHubRepoName = "app",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Projects.Add(project);
        context.ProjectMembers.Add(new ProjectMember
        {
            ProjectMemberId = Guid.CreateVersion7(),
            ProjectId = project.ProjectId,
            UserId = developer.UserID,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var task = new Tasks
        {
            TaskId = Guid.CreateVersion7(),
            ProjectId = project.ProjectId,
            CreatedByUserId = manager.UserID,
            AssignedToUserId = developer.UserID,
            Title = "Keep this title",
            TaskSummery = "Keep this description",
            Status = nameof(TaskStatus.ToDo),
            Priority = nameof(TaskPriority.High),
            GitHubPRNumber = 7,
            GitHubPRStatus = nameof(PullRequestStatus.Open),
            GitHubPRUrl = "https://github.com/acme/app/pull/7",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var otherTask = new Tasks
        {
            TaskId = Guid.CreateVersion7(),
            ProjectId = project.ProjectId,
            CreatedByUserId = manager.UserID,
            AssignedToUserId = otherDeveloper.UserID,
            Title = "Unlinked task",
            Status = nameof(TaskStatus.InProgress),
            Priority = nameof(TaskPriority.Low),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Tasks.AddRange(task, otherTask);
        await context.SaveChangesAsync();

        var handler = new RecordingHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        var service = new GitHubService(client, context, new ActivityLogService(context), new PublicProfileGitHubOwnershipVerifier());
        return new GitHubFixture
        {
            Context = context,
            Service = service,
            Handler = handler,
            Developer = developer,
            OtherDeveloper = otherDeveloper,
            OtherManager = otherManager,
            Project = project,
            Task = task,
            OtherTask = otherTask
        };
    }

    public static string Pull(int number, string state, bool draft, string? mergedAt)
    {
        var merged = mergedAt == null ? "null" : $"\"{mergedAt}\"";
        var draftJson = draft ? "true" : "false";
        return "{\"number\":" + number
            + ",\"title\":\"GitHub title\",\"html_url\":\"https://github.com/acme/app/pull/" + number
            + "\",\"state\":\"" + state
            + "\",\"draft\":" + draftJson
            + ",\"merged_at\":" + merged
            + ",\"created_at\":\"2026-10-01T00:00:00Z\",\"updated_at\":\"2026-11-01T00:00:00Z\",\"user\":{\"login\":\"octocat\"},\"base\":{\"repo\":{\"name\":\"app\",\"owner\":{\"login\":\"acme\"}}}}";
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    private static User User(UserRole role, string name, string email, string phone)
    {
        return new User
        {
            UserID = Guid.CreateVersion7(),
            RoleId = role.ToId(),
            FullName = name,
            Email = email,
            PhoneNumber = phone,
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RefreshToken = "token",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
        };
    }
}

internal sealed class RecordingHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } = _ =>
        new HttpResponseMessage(HttpStatusCode.NotFound);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Responder(request));
    }
}
