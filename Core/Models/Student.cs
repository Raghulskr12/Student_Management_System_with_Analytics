using System;
using System.Collections.Generic;
using Core.Exceptions;

namespace Core.Models
{
    public class Student
    {
        private static readonly HashSet<string> _AllowedGrades = new() { "A", "B", "C", "D", "F" };
        private string _Grade = "F"; // Default grade

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Grade
        {
            get => _Grade;
            set
            {
                string upperGrade = value?.ToUpper() ?? throw new ArgumentNullException(nameof(value));
                if (!_AllowedGrades.Contains(upperGrade)) throw new InvalidGradeException(upperGrade);
                _Grade = upperGrade;
            }
        }
        public string ExternalData { get; set; } = string.Empty;

        public Student() { }
        public Student(int id, string name, string grade)
        {
            Id = id;
            Name = name;
            Grade = grade;
        }
    }
}