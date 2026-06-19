using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Core.Interfaces;
using Core.Models;
using Core.Exceptions;
using Infrastructure.Data;

// 1. Bootstrap Serilog Logging Provider
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/app-log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Initializing Student Analytics Management Engine...");

    var builder = Host.CreateApplicationBuilder(args);
    
    // Register configurations and services
    string filePath = builder.Configuration["StudentRepositorySettings:FilePath"] ?? "students.json";
    builder.Services.AddSingleton<IStudentRepository>(provider => new JsonStudentRepository(filePath));
    builder.Services.AddSingleton<ExternalApiService>();

    using var host = builder.Build();
    var repository = host.Services.GetRequiredService<IStudentRepository>();
    var apiService = host.Services.GetRequiredService<ExternalApiService>();

    await RunMenuLoopAsync(repository, apiService);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during runtime initialization.");
}
finally
{
    Log.CloseAndFlush(); // Safeguard and flush log stream buffers
}

static async Task RunMenuLoopAsync(IStudentRepository repo, ExternalApiService apiService)
{
    bool exit = false;
    while (!exit)
    {
        Console.WriteLine("\n=== SYSTEM CONTROL PANEL ===");
        Console.WriteLine("1. Create Student");
        Console.WriteLine("2. Read All Students");
        Console.WriteLine("3. Update Student");
        Console.WriteLine("4. Delete Student");
        Console.WriteLine("5. Search Students");
        Console.WriteLine("6. Run LINQ Analytics Engine ");
        Console.WriteLine("7. Synchronize External API Data Async");
        Console.WriteLine("8. Exit");
        Console.Write("Select an option (1-8): ");

        string choice = Console.ReadLine() ?? "";
        try
        {
            switch (choice)
            {
                case "1": HandleCreate(repo); break;
                case "2": HandleRead(repo); break;
                case "3": HandleUpdate(repo); break;
                case "4": HandleDelete(repo); break;
                case "5": HandleSearch(repo); break;
                case "6": HandleAnalytics(repo); break;
                case "7": await HandleApiIntegrationAsync(repo, apiService); break;
                case "8": exit = true; break;
                default: Log.Warning("User supplied an invalid input choice: {Choice}", choice); break;
            }
        }
        catch (InvalidGradeException ex)
        {
            Log.Warning("Validation Catch: {Message}", ex.Message);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[BAD INPUT] {ex.Message}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unhandled error occurred during menu processing choice: {Choice}", choice);
        }
    }
}

static void HandleCreate(IStudentRepository repo)
{
    Console.WriteLine("--- Create New Student ---");
    
    Console.Write("Enter unique ID (Integer): ");
    if (!int.TryParse(Console.ReadLine(), out int id))
        throw new ArgumentException("ID must be a valid integer.");

    Console.Write("Enter Name: ");
    string name = Console.ReadLine() ?? "";
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be blank.");

    Console.Write("Enter Grade (A, B, C, D, F): ");
    string grade = Console.ReadLine() ?? "";

    // The domain validation rules handle incorrect grades inside the constructor
    var newStudent = new Student(id, name, grade);
    repo.Add(newStudent);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n[SUCCESS] Student record added and saved successfully!");
    Console.ResetColor();
    Console.ReadKey();
}

static void HandleRead(IStudentRepository repo)
{
    Console.WriteLine("--- Student Directory Listing ---");
    var students = repo.GetAll().ToList();

    if (!students.Any())
    {
        Console.WriteLine("No records present in storage system.");
    }
    else
    {
        PrintTable(students);
    }
    Console.ReadKey();
}

static void HandleUpdate(IStudentRepository repo)
{
    Console.WriteLine("--- Update Existing Student ---");
    Console.Write("Enter Student ID to update: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
        throw new ArgumentException("Invalid ID input format.");

    // Check if user exists first to throw KeyNotFound if missing
    var current = repo.GetById(id);

    Console.Write($"Enter New Name (Current: {current.Name}) [Press Enter to keep unchanged]: ");
    string updatedName = Console.ReadLine() ?? "";
    if (string.IsNullOrWhiteSpace(updatedName)) updatedName = current.Name;

    Console.Write($"Enter New Grade (Current: {current.Grade}) [Press Enter to keep unchanged]: ");
    string updatedGrade = Console.ReadLine() ?? "";
    if (string.IsNullOrWhiteSpace(updatedGrade)) updatedGrade = current.Grade;

    var updatedStudent = new Student(id, updatedName, updatedGrade);
    repo.Update(updatedStudent);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n[SUCCESS] Student profile updated and auto-saved successfully!");
    Console.ResetColor();
    Console.ReadKey();
}

static void HandleDelete(IStudentRepository repo)
{
    Console.WriteLine("--- Delete Student Profile ---");
    Console.Write("Enter Student ID to delete: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
        throw new ArgumentException("Invalid ID sequence.");

    var student = repo.GetById(id);
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"Are you absolutely sure you want to remove {student.Name} (ID: {id})? (Y/N): ");
    Console.ResetColor();
    
    string confirmation = Console.ReadLine()?.ToUpper() ?? "N";
    if (confirmation == "Y" || confirmation == "YES")
    {
        repo.Delete(id);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[SUCCESS] Record eradicated successfully!");
    }
    else
    {
        Console.WriteLine("\nDeletion aborted safely.");
    }
    Console.ResetColor();
    Console.ReadKey();
}

static void HandleSearch(IStudentRepository repo)
{
    Console.WriteLine("--- Search Filter Center ---");
    Console.Write("Search by Name or Grade (Leave blank to show all matches): ");
    string query = (Console.ReadLine() ?? "").ToUpper();

    var results = repo.GetAll().Where(s => 
        s.Name.ToUpper().Contains(query) || 
        s.Grade.ToUpper() == query
    ).ToList();

    if (!results.Any())
    {
        Console.WriteLine("No search outcomes matched your criteria.");
    }
    else
    {
        PrintTable(results);
    }
    Console.ReadKey();
}

static void PrintTable(IEnumerable<Student> students)
{
    Console.WriteLine("-----------------------------------------------------------------");
    Console.WriteLine(string.Format("| {0, -10} | {1, -25} | {2, -10} |", "ID", "Name", "Grade"));
    Console.WriteLine("-----------------------------------------------------------------");
    foreach (var student in students)
    {
        Console.WriteLine(string.Format("| {0, -10} | {1, -25} | {2, -10} |", student.Id, student.Name, student.Grade));
    }
    Console.WriteLine("-----------------------------------------------------------------");
}

static void HandleAnalytics(IStudentRepository repo)
{
    Console.Clear();
    Console.WriteLine("=================================================================");
    Console.WriteLine("                       LINQ ANALYTICS                            ");
    Console.WriteLine("=================================================================");

    var analytics = new Core.Services.AnalyticsEngine();
    var allStudents = repo.GetAll().ToList();

    // 1. Student Grade Filtering & Sorting
    Console.WriteLine("\n[1. Students with Grade 'A' Sorted Ascending By Name]");
    var topStudents = analytics.GetStudentsWithGradeSorted(allStudents, "A");
    foreach (var s in topStudents)
    {
        Console.WriteLine($" -> ID: {s.Id} | Name: {s.Name}");
    }
    if (!topStudents.Any()) Console.WriteLine(" No active students holding an 'A' status.");

    // 2. Product Category and Price Queries
    Console.WriteLine("\n[2. Electronics Priced Above 10,000 Sorted Descending By Price]");
    var expensiveTech = analytics.GetProductsByCategoryAndPrice("Electronics", 10000m);
    foreach (var p in expensiveTech)
    {
        Console.WriteLine($" -> {p.Name} | Price: ₹{p.Price:N2} | Category: {p.Category}");
    }

    // 3. Average Grade Aggregate Value Calculations
    double avgValue = analytics.CalculateAverageGradeValue(allStudents);
    Console.WriteLine($"\n[3. Combined System Student Grade Average GPA Value]: {avgValue:F2} / 4.0");

    // 4. Student Aggregation Groups
    Console.WriteLine("\n[4. Global Student Distribution Count Metrics Per Grade Group]");
    var gradeGroups = analytics.GetStudentCountByGrade(allStudents);
    foreach (var group in gradeGroups)
    {
        Console.WriteLine($" -> Grade Group '{group.Grade}': {group.Count} Student(s)");
    }

    // 5. Product Segment Matrix Categories
    Console.WriteLine("\n[5. Category Structural Valuation Metrics & Most Expensive Products]");
    var catMetrics = analytics.GetProductTotalsByCategory();
    foreach (var item in catMetrics)
    {
        Console.WriteLine($" -> [{item.Category}] Net Valuation Portfolio: ₹{item.TotalPrice:N2}");
        if (item.MaxProduct != null)
        {
            Console.WriteLine($"    Premium Offering Item -> {item.MaxProduct.Name} (₹{item.MaxProduct.Price:N2})");
        }
    }
    Console.WriteLine("=================================================================");
    Console.WriteLine("\nPress any key to jump back to main configuration window...");
    Console.ReadKey();
}

static async Task HandleApiIntegrationAsync(IStudentRepository repo, ExternalApiService apiService)
{
    Console.Clear();
    var students = repo.GetAll().ToList();
    if (!students.Any()) return;

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var totalSystemWatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        var tasks = students.Select(async student =>
        {
            string externalInsight = await apiService.FetchExternalDataAsync(student.Id, cts.Token);
            student.ExternalData = externalInsight;
            repo.Update(student);
        });

        await Task.WhenAll(tasks);
        Log.Information("Successfully synced {Count} records via concurrent API pipelines.", students.Count);
    }
    catch (OperationCanceledException)
    {
        Log.Error("API processing threshold breached! Timeout canceled pending requests.");
    }
}