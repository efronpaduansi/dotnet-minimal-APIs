using Microsoft.EntityFrameworkCore;
using TodoApi.Entities;
using TodoApi.Models;
using TodoApi.Models.DTOs;

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

todoItems.MapGet("/", GetAllTodos);
todoItems.MapGet("/complete", GetCompleteTodos);
todoItems.MapGet("/{id}", GetTodo);
todoItems.MapPost("/", CreateTodo);
todoItems.MapPut("/{id}", UpdateTodo);
todoItems.MapDelete("/{id}", DeleteTodo);

app.Run();

//Get All Todos
static async Task<IResult> GetAllTodos(TodoDbContext db)
{
    return TypedResults.Ok(await db.Todos.Select(x => new TodoItemDTO(x)).ToArrayAsync());
}

//Get completed todo items
static async Task<IResult> GetCompleteTodos (TodoDbContext db)
{
    var result = await db.Todos.Where(t => t.IsComplete).Select(x => new TodoItemDTO(x)).ToListAsync();
    return TypedResults.Ok(result);
}

//Get todo item by id
static async Task<IResult> GetTodo (int id, TodoDbContext db)
{
    return await db.Todos.FindAsync(id)
    is Todo todo
    ? TypedResults.Ok(new TodoItemDTO(todo))
    : TypedResults.NotFound();
}

//Create new todo item
static async Task<IResult> CreateTodo(TodoItemDTO dto, TodoDbContext db)
{
    var todoItem = new Todo
    {
        Name = dto.Name,
        IsComplete = dto.IsComplete
    };

    db.Todos.Add(todoItem);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/todoitems/{todoItem.Id}", dto);

}

static async Task<IResult> UpdateTodo(int id, TodoItemDTO todoItemDTO, TodoDbContext db)
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return TypedResults.NotFound();

    todo.Name = todoItemDTO.Name;
    todo.IsComplete = todoItemDTO.IsComplete;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodo(int id, TodoDbContext db)
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}