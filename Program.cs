using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// DB setup
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string"
        + "'DefaultConnection' not found.");

builder.Services.AddDbContext<ExpenseContext>(options =>
    options.UseSqlite(connectionString));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

ImmutableArray<string> permittedCategories = [
    "Groceries",
    "Food",
    "Transport",
    "Rent",
    "Bills",
    "Other"
];

// Root endpoint

app.MapGet("/", () => "Welcome to the Expense Tracker!");

// Expense endpoints

app.MapGet("/expenses", async (ExpenseContext db, string? category, DateOnly? startDate, DateOnly? endDate) =>
{
    var query = db.Expenses.AsQueryable();

    // Validating input:
    if (category != null && string.IsNullOrWhiteSpace(category))
    {
        return Results.BadRequest("Category must not be blank.");
    }
    if (category != null && !permittedCategories.Contains(category))
    {
        return Results.BadRequest($"Category must be set to one of the following: {string.Join(", ", permittedCategories)}");
    }
    if (startDate != null && endDate != null && startDate > endDate)
    {
        return Results.BadRequest("Start-date must not be greater than End-date");
    }

    // Querying:
    if (category != null)
    {
        query = query.Where(expense => expense.Category == category);
    }
    if (startDate != null)
    {
        query = query.Where(expense => expense.Date >= startDate);
    }
    if (endDate != null)
    {
        query = query.Where(expense => expense.Date <= endDate);
    }

    List<Expense> expenses = await query.ToListAsync();

    return Results.Ok(expenses);
})
.WithName("GetExpenses");

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

app.MapPost("/expenses", async (ExpenseContext db, Expense expense) =>
{
    var validationError = ValidateExpense(expense);
    if (validationError != null)
    {
        return validationError;
    }

    await db.Expenses.AddAsync(expense);
    await db.SaveChangesAsync();

    return Results.Created($"/expenses/{expense.Id}", expense);
})
.WithName("CreateExpense");

app.MapDelete("/expenses/{id}", async (ExpenseContext db, int id) =>
{
    Expense? expense = await db.Expenses.FindAsync(id);

    if (expense == null)
    {
        return Results.NotFound("Expense not found.");
    }

    db.Expenses.Remove(expense);
    await db.SaveChangesAsync();

    return Results.Ok(expense);
})
.WithName("DeleteExpense");

app.MapPut("/expenses/{id}", async (ExpenseContext db, int id, Expense expense) =>
{
    Expense? expenseToUpdate = await db.Expenses.FindAsync(id);
    if (expenseToUpdate == null)
    {
        return Results.NotFound("Expense not found.");
    }

    var validationError = ValidateExpense(expense);
    if (validationError != null)
    {
        return validationError;
    }

    expenseToUpdate.Amount = expense.Amount;
    expenseToUpdate.Category = expense.Category;
    expenseToUpdate.Description = expense.Description;
    expenseToUpdate.Date = expense.Date;
    await db.SaveChangesAsync();

    return Results.Ok(expenseToUpdate);
})
.WithName("UpdateExpense");


// Summary endpoint

app.MapGet("/expenses/summary", async (ExpenseContext db, int year, int month) =>
{
    var validationError = ValidateSummaryInput(year, month);
    if (validationError != null)
    {
        return validationError;
    }

    List<Expense> summaryExpenses = await db.Expenses.Where(
        expense => expense.Date.Year == year && expense.Date.Month == month).ToListAsync();

    int countMonth = summaryExpenses.Count;

    decimal amountMonth = 0;
    foreach (Expense expense in summaryExpenses)
    {
        amountMonth += expense.Amount;
    }

    var summary = new { Year = year, 
                            Month = month, 
                            ExpenseCount = countMonth, 
                            TotalAmount = amountMonth };

    return Results.Ok(summary);
})
.WithName("GetSummaryYearMonth");


app.Run();


// Auxiliary functions:

IResult? ValidateExpense (Expense expense)
{
    if (expense.Amount <= 0)
    {
        return Results.BadRequest("Amount must be greater than 0.");
    }
    if (string.IsNullOrWhiteSpace(expense.Category))
    {
        return Results.BadRequest("Category must not be blank.");
    }
    if (!permittedCategories.Contains(expense.Category))
    {
        return Results.BadRequest($"Category must be set to one of the following: {string.Join(", ", permittedCategories)}");    
    }
    if (expense.Description.Length > 200)
    {
        return Results.BadRequest("Description must not be longer than 200 characters.");
    }
    if (expense.Date == default)
    {
        return Results.BadRequest("Date must be set.");
    }
    return null;
}

static IResult? ValidateSummaryInput (int year, int month)
{
    if (year <= 0)
    {
        return Results.BadRequest("Year must be a positive integer.");
    }
    if (month < 1 || month > 12)
    {
        return Results.BadRequest("Month must be a valid integer from 1 to 12.");
    }
    return null;
}
