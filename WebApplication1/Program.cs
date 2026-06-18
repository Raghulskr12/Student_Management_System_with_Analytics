using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Core.Interfaces;
using Core.Models;
using Core.Exceptions;
using Infrastructure.Data;

var builder = Host.CreateApplicationBuilder(args);

// 1. Dependency Injection Registration
// Pull configuration parameters dynamically from appsettings.json
string filePath = builder.Configuration["StudentRepositorySettings:FilePath"] ?? "students.json";

// Register our implementation as a Singleton service
builder.Services.AddSingleton<IStudentRepository>(provider => new JsonStudentRepository(filePath));

using var host = builder.Build();

// 2. Resolve the repository from our Container
var repository = host.Services.GetRequiredService<IStudentRepository>();

RunMenuLoop(repository);

static void RunMenuLoop(IStudentRepository repo)
{
    bool exit = false;
    while (!exit)
    {
        Console.Clear();
        Console.WriteLine("=========================================");
        Console.WriteLine("    STUDENT MANAGEMENT SYSTEM (MODULE 1) ");
        Console.WriteLine("=========================================");
        Console.WriteLine("1. Create Student");
        Console.WriteLine("2. Read All Students");
        Console.WriteLine("3. Update Student");
        Console.WriteLine("4. Delete Student");
        Console.WriteLine("5. Search Students");
        Console.WriteLine("6. Run LINQ Analytics Engine ");
        Console.WriteLine("7. Exit");
        Console.WriteLine("=========================================");
        Console.Write("Select an option (1-7): ");

        string choice = Console.ReadLine() ?? "";
        Console.WriteLine();

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
                case "7": exit = true; break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid option selection. Press any key to retry...");
                    Console.ResetColor();
                    Console.ReadKey();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR] Operation failed: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("Press any key to return to menu...");
            Console.ReadKey();
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