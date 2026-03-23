using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

List<Expense> expenses = 
[
    new Expense 
    { 
    Id = 1,
    Amount = 50,
    Category = "Indkøb",
    Description = "Aftensmad i Netto",
    Date = new DateOnly(2026, 3, 22)
    },
    new Expense
    { 
    Id = 2,
    Amount = 100,
    Category = "Øl",
    Description = "Sjov",
    Date = new DateOnly(2026, 3, 22)
    },
    new Expense
    { 
    Id = 3,
    Amount = 10000,
    Category = "Husleje",
    Description = "Månedlig Husleje",
    Date = new DateOnly(2026, 3, 22)
    }
];

app.MapGet("/", () => "Welcome to the Expense Tracker!");

app.MapGet("/expenses", () =>
{
    return expenses;
})
.WithName("GetExpenses");

app.MapGet("/expenses/{id}", (int id) =>
{
    foreach (Expense expense in expenses)
    {
        if (id == expense.Id) 
        {
            return Results.Ok(expense);
        } 
    }
    return Results.NotFound();
})
.WithName("GetExpenseById");

app.MapPost("/expenses", (Expense expense) =>
{   
    int newId = 1;
    foreach (Expense existing_expense in expenses)
    {
        if (existing_expense.Id >= newId) {
            newId = existing_expense.Id + 1;
        }
    }
    expense.Id = newId;
    expenses.Add(expense);
    return Results.Created($"/expenses/{expense.Id}", expense);
})
.WithName("CreateExpense");

app.MapDelete("/expenses/{id}", (int id) =>
{   
    Expense? expenseToDelete = null;
    foreach (Expense expense in expenses)
    {
        if (expense.Id == id)
        {
            expenseToDelete = expense;
        }
    }
    if (expenseToDelete == null)
    {
        return Results.NotFound();
    }
    expenses.Remove(expenseToDelete);
    return Results.Ok(expenseToDelete);
})
.WithName("DeleteExpense");

app.MapPut("/expenses/{id}", (int id, Expense expense) =>
{
    Expense? expenseToUpdate = null;
    foreach (Expense existing_expense in expenses)
    {
        if (existing_expense.Id == id)
        {
            expenseToUpdate = existing_expense;
        }
    }
    if (expenseToUpdate == null)
    {
        return Results.NotFound();
    }
    expenseToUpdate.Amount = expense.Amount;
    expenseToUpdate.Category = expense.Category;
    expenseToUpdate.Description = expense.Description;
    expenseToUpdate.Date = expense.Date;
    return Results.Ok(expenseToUpdate);
})
.WithName("UpdateExpense");

app.Run();
