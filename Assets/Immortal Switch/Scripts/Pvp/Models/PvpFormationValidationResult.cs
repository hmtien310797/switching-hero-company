using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Kết quả validate formation (DOCX §17, Markdown screen 02 — Validation).
    /// IsValid = true khi Errors rỗng. Warnings không block save.
    /// </summary>
    [Serializable]
    public sealed class PvpFormationValidationResult
    {
        public bool IsValid;
        public List<string> Errors = new();
        public List<string> Warnings = new();

        public override string ToString()
        {
            return IsValid ? "Valid" : $"Invalid: {string.Join("; ", Errors)}";
        }
    }
}
