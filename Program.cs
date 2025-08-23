using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Entities;
using TodoApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDbContext>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});

var app = builder.Build();

//Swagger config
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

//Show welcome message
app.MapGet("/", () => "Hello World!");

//Initial a map groups
var todoItems = app.MapGroup("/todoitems");

//Get All Todos
todoItems.MapGet("/", async ([FromServices] TodoDbContext db) =>
        await db.Todos.ToListAsync());

//Get completed todo items
todoItems.MapGet("/complete", async ([FromServices] TodoDbContext db) =>
        await db.Todos.Where(t => t.IsComplete).ToListAsync()
);

//Get todo by Id
todoItems.MapGet("/{id}", async (int id, [FromServices] TodoDbContext db) =>
    await db.Todos.FindAsync(id)
    is Todo todo ? Results.Ok(todo) : Results.NotFound()
);

//Store new todo
todoItems.MapPost("/", async (Todo todo, TodoDbContext db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/{todo.Id}", todo);
});

//Update todo item
todoItems.MapPut("/{id}", async (int id, Todo inputTodo, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

//Delete todo item
todoItems.MapDelete("/{id}", async (int id, TodoDbContext db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();
