using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Core.Interfaces;
using Core.Models;

namespace Infrastructure.Data
{
    public class JsonStudentRepository : IStudentRepository
    {
        private readonly string _filePath;
        private readonly object _fileLock = new();

        public JsonStudentRepository(string filePath)
        {
            _filePath = string.IsNullOrWhiteSpace(filePath) ? "students.json" : filePath;
            lock (_fileLock)
            {
                if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "[]");
            }
        }

        private List<Student> LoadAll()
        {
            lock (_fileLock)
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<Student>>(json) ?? new List<Student>();
            }
        }

        private void SaveAll(List<Student> students)
        {
            lock (_fileLock)
            {
                string json = JsonSerializer.Serialize(students, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }

        public void Add(Student student)
        {
            var students = LoadAll();
            if (students.Any(s => s.Id == student.Id)) throw new InvalidOperationException("ID already exists.");
            students.Add(student);
            SaveAll(students);
        }

        public IEnumerable<Student> GetAll() => LoadAll();

        public Student GetById(int id) => LoadAll().First(s => s.Id == id);

        public void Update(Student student)
        {
            var students = LoadAll();
            var target = students.FirstOrDefault(s => s.Id == student.Id) ?? throw new KeyNotFoundException();
            target.Name = student.Name;
            target.Grade = student.Grade;
            SaveAll(students);
        }

        public void Delete(int id)
        {
            var students = LoadAll();
            var target = students.FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException();
            students.Remove(target);
            SaveAll(students);
        }
    }
}