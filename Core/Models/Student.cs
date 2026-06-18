using System;
using System.Collections.Generic;
using Core.Exceptions;

namespace Core.Models
{
    public class Student
    {
        private static readonly HashSet<string> AllowedGrades = new() { "A", "B", "C", "D", "F" };
        private string _grade;

        public int Id { get; set; }
        public string Name { get; set; }
        public string Grade
        {
            get => _grade;
            set
            {
                string upperGrade = value?.ToUpper() ?? throw new ArgumentNullException(nameof(value));
                if (!AllowedGrades.Contains(upperGrade)) throw new InvalidGradeException(upperGrade);
                _grade = upperGrade;
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