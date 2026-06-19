
using System;

namespace Core.Exceptions
{
    public class InvalidGradeException(string grade)
        : Exception($"Invalid grade: '{grade}'. Allowed values are A, B, C, D, or F.");
}
