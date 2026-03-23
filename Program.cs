using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;

var builder = WebApplication.CreateBuilder(args);

/**************************************/

// Add services to the container.
builder.Services.AddOpenApi();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string"
        + "'DefaultConnection' not found.");

builder.Services.AddDbContext<ExpenseContext>(options =>
    options.UseSqlite(connectionString));

/**************************************/

var app = builder.Build();

/**************************************/

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

/**************************************/

List<Expense> expenses = 
[
    new Expense 
    { 
    Id = 1,
    Amount = 50,
    Category = "Indkøb",
    Description = "Aftensmad i Netto",
    Date = new DateOnly(2026, 3, 22)
    }
];

/**************************************/

app.MapGet("/", () => "Welcome to the Expense Tracker!");

/**************************************/

app.MapGet("/expenses", async (ExpenseContext db) =>
{
    List <Expense> result = await db.Expenses.ToListAsync();
    return Results.Ok(result);
})
.WithName("GetExpenses");

/**************************************/

app.MapGet("/expenses/{id}", async (ExpenseContext db, int id) =>
{
    Expense? expense = await db.Expenses.FindAsync(id);
    if (expense == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(expense);
})
.WithName("GetExpenseById");

/**************************************/

app.MapPost("/expenses", async (ExpenseContext db, Expense expense) =>
{
    await db.Expenses.AddAsync(expense);
    await db.SaveChangesAsync();

    return Results.Created($"/expenses/{expense.Id}", expense);
})
.WithName("CreateExpense");

/**************************************/

app.MapDelete("/expenses/{id}", async (ExpenseContext db, int id) =>
{
    Expense? expense = await db.Expenses.FindAsync(id);
    if (expense == null)
    {
        return Results.NotFound();
    }
    db.Expenses.Remove(expense);
    await db.SaveChangesAsync();
    return Results.Ok(expense);
})
.WithName("DeleteExpense");

/**************************************/

app.MapPut("/expenses/{id}", async (ExpenseContext db, int id, Expense expense) =>
{
    Expense? expenseToUpdate = await db.Expenses.FindAsync(id);
    if (expenseToUpdate == null)
    {
        return Results.NotFound();
    }
    expenseToUpdate.Amount = expense.Amount;
    expenseToUpdate.Category = expense.Category;
    expenseToUpdate.Description = expense.Description;
    expenseToUpdate.Date = expense.Date;
    await db.SaveChangesAsync();
    return Results.Ok(expenseToUpdate);
})
.WithName("UpdateExpense");

/**************************************/

app.Run();
