using CsvExporterLibrary.Models;


namespace CsvExporterLibrary.Interfaces
{
    /// <summary>
    /// Interface for type detection and conversion
    /// </summary>
    public interface ITypeDetector
    {
        object DetectAndConvert(string? value, CsvImportOptions options);
        Type DetectType(string value, CsvImportOptions options);
        bool TryParseValue<T>(string value, out T result);
    }

}
