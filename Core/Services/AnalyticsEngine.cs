using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;

namespace Core.Services
{
    public class AnalyticsEngine
    {
        private readonly List<Product> _Products;

        public AnalyticsEngine()
        {
            
            _Products = new List<Product>
            {
                new(1, "ThinkPad X1 Carbon", 120000m, "Electronics"),
                new(2, "MacBook Pro M3", 180000m, "Electronics"),
                new(3, "Dell UltraSharp Monitor", 350000m, "Electronics"),
                new(4, "C# in a Nutshell Book", 4500m, "Books"),
                new(5, "Clean Architecture Book", 3200m, "Books"),
                new(6, "Design Patterns (GoF)", 5000m, "Books"),
                new(7, "Leather Jacket", 8500m, "Clothing"),
                new(8, "Slim Fit Denim", 3500m, "Clothing"),
                new(9, "Running Shoes", 6500m, "Clothing"),
                new(10, "Sony WH-1000XM5", 30000m, "Electronics"),
                new(11, "Mechanical Keyboard", 8000m, "Electronics"),
                new(12, "Oversized Hoodie", 2500m, "Clothing")
            };
        }

        public IEnumerable<Product> GetProducts() => _Products;


        // 1. Filtering & Sorting Students
        public IEnumerable<Student> GetStudentsWithGradeSorted(IEnumerable<Student> students, string grade)
        {
            return students
                .Where(s => s.Grade.Equals(grade, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name); // Ascending order
        }

        // 2. Filtering & Sorting Products
        public IEnumerable<Product> GetProductsByCategoryAndPrice(string category, decimal minPrice)
        {
            return _Products
                .Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase) && p.Price > minPrice)
                .OrderByDescending(p => p.Price); // Descending order
        }

        // 3. Aggregation: Calculate Average Grade Scale Value
        public double CalculateAverageGradeValue(IEnumerable<Student> students)
        {
            if (!students.Any()) return 0.0;

            return students.Average(s => s.Grade.ToUpper() switch
            {
                "A" => 4.0,
                "B" => 3.0,
                "C" => 2.0,
                "D" => 1.0,
                _ => 0.0 // 'F' maps to 0
            });
        }

        // 4. Grouping & Counting Students
        public IEnumerable<dynamic> GetStudentCountByGrade(IEnumerable<Student> students)
        {
            return students
                .GroupBy(s => s.Grade.ToUpper())
                .Select(g => new { Grade = g.Key, Count = g.Count() })
                .OrderBy(g => g.Grade);
        }

        // 5. Grouping & Totals for Products
        public IEnumerable<dynamic> GetProductTotalsByCategory()
        {
            return _Products
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, TotalPrice = g.Sum(p => p.Price), MaxProduct = g.MaxBy(p => p.Price) });
        }
    }
}