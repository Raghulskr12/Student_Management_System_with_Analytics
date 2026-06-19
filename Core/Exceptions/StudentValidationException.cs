using System;

namespace Core.Exceptions
{
    public class StudentValidationException : Exception
    {
        public StudentValidationException(string message) : base(message) { }
    }
}