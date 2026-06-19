using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Core.Interfaces;
using Core.Models;
using Core.Exceptions;
using Serilog;

namespace Infrastructure.Data
{
    public class JsonStudentRepository : IStudentRepository
    {
        private readonly string _FilePath;
        private readonly object _FileLock = new();

        public JsonStudentRepository(string filePath)
        {
            _FilePath = string.IsNullOrWhiteSpace(filePath) ? "students.json" : filePath;
            InitializeStorageFile();
        }

        private void InitializeStorageFile()
        {
            lock (_FileLock)
            {
                try
                {
                    var fileInfo = new FileInfo(_FilePath);
                    if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                    }

                    if (!File.Exists(_FilePath) || fileInfo.Length == 0)
                    {
                        File.WriteAllText(_FilePath, "[]");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Fatal(ex, "Critical Permission Error: Cannot write to storage path {Path}", _FilePath);
                    throw new InfrastructureException("Storage access denied by filesystem security policies.", ex);
                }
                catch (IOException ex)
                {
                    Log.Fatal(ex, "Hardware/IO fault during file initialization at {Path}", _FilePath);
                    throw new InfrastructureException("Failed to initialize physical database storage file.", ex);
                }
            }
        }

        private List<Student> LoadAll()
        {
            lock (_FileLock)
            {
                try
                {
                    if (!File.Exists(_FilePath)) return new List<Student>();
                    
                    string json = File.ReadAllText(_FilePath);
                    return JsonSerializer.Deserialize<List<Student>>(json) ?? new List<Student>();
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, "Data corruption identified inside system file: {Path}", _FilePath);
                    throw new InfrastructureException("The database file format is corrupted and cannot be parsed.", ex);
                }
                catch (IOException ex)
                {
                    Log.Warning(ex, "Transient file-lock collision encountered during read phase.");
                    throw new InfrastructureException("Database storage is temporarily locked by another system thread.", ex);
                }
            }
        }

        private void SaveAll(List<Student> students)
        {
            lock (_FileLock)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(students, options);
                    
                    // Defensive writing: Write to a temporary file first, then swap
                    string tempPath = _FilePath + ".tmp";
                    File.WriteAllText(tempPath, json);
                    File.Move(tempPath, _FilePath, overwrite: true);
                }
                catch (IOException ex)
                {
                    Log.Error(ex, "Failed to commit transactional state update onto disk storage.");
                    throw new InfrastructureException("Auto-save failed. Your modifications have not been committed to disk.", ex);
                }
            }
        }

        public void Add(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));
            
            var students = LoadAll();
            if (students.Any(s => s.Id == student.Id))
            {
                throw new StudentValidationException($"Violation of Unique Constraint: A student record with ID '{student.Id}' already exists.");
            }

            students.Add(student);
            SaveAll(students);
            Log.Information("Successfully saved new student record: ID {Id}", student.Id);
        }

        public IEnumerable<Student> GetAll() => LoadAll();

        public Student GetById(int id)
        {
            var student = LoadAll().FirstOrDefault(s => s.Id == id);
            if (student == null)
            {
                throw new KeyNotFoundException($"No record exists matching the specified student ID: {id}");
            }
            return student;
        }

        public void Update(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            var students = LoadAll();
            var target = students.FirstOrDefault(s => s.Id == student.Id);
            
            if (target == null)
            {
                throw new KeyNotFoundException($"Modification aborted: Student record with ID {student.Id} was not found.");
            }

            target.Name = student.Name;
            target.Grade = student.Grade;
            target.ExternalData = student.ExternalData;

            SaveAll(students);
            Log.Information("Successfully modified student profile: ID {Id}", student.Id);
        }

        public void Delete(int id)
        {
            var students = LoadAll();
            var target = students.FirstOrDefault(s => s.Id == id);

            if (target == null)
            {
                throw new KeyNotFoundException($"Deletion aborted: Student record with ID {id} was not found.");
            }

            students.Remove(target);
            SaveAll(students);
            Log.Information("Successfully evicted student profile: ID {Id}", id);
        }
    }
}