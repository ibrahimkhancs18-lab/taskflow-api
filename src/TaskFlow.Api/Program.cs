var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Temporary in-memory storage. PostgreSQL replaces this later.
var tasks = new List<TaskItem>();
var nextId = 1;

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/tasks", () => tasks);

app.MapGet("/api/tasks/{id:int}", (int id) =>
    tasks.FirstOrDefault(t => t.Id == id) is { } task
        ? Results.Ok(task)
        : Results.NotFound());

app.MapPost("/api/tasks", (CreateTaskRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { error = "Title is required" });

    var task = new TaskItem(nextId++, request.Title.Trim(), false);
    tasks.Add(task);
    return Results.Created($"/api/tasks/{task.Id}", task);
});

app.MapPut("/api/tasks/{id:int}", (int id, UpdateTaskRequest request) =>
{
    var index = tasks.FindIndex(t => t.Id == id);
    if (index == -1) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { error = "Title is required" });

    tasks[index] = tasks[index] with { Title = request.Title.Trim(), IsDone = request.IsDone };
    return Results.Ok(tasks[index]);
});

app.MapDelete("/api/tasks/{id:int}", (int id) =>
    tasks.RemoveAll(t => t.Id == id) > 0 ? Results.NoContent() : Results.NotFound());

app.Run();

record TaskItem(int Id, string Title, bool IsDone);
record CreateTaskRequest(string Title);
record UpdateTaskRequest(string Title, bool IsDone);
