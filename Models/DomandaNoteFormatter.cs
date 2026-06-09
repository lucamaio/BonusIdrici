namespace Models
{
    public static class DomandaNoteFormatter
    {
        private const string NotaTecnicaConfrontoIndirizzo = "Confronto indirizzo tramite IdIndirizzoNormaliz";

        public static string? NascondiNoteTecniche(string? note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return null;
            }

            var noteVisibili = note
                .Replace("\r\n", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(n => !n.StartsWith(NotaTecnicaConfrontoIndirizzo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return noteVisibili.Count == 0
                ? null
                : string.Join("\n", noteVisibili);
        }

        public static bool IsNotaTecnica(string? nota)
        {
            return !string.IsNullOrWhiteSpace(nota)
                && nota.TrimStart().StartsWith(NotaTecnicaConfrontoIndirizzo, StringComparison.OrdinalIgnoreCase);
        }
    }
}
