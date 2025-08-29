using CsvExporterLibrary.Interfaces;
using CsvExporterLibrary.Models;
using CsvExporterLibrary.Exceptions;
using Newtonsoft.Json;



namespace CsvExporterLibrary.Services
{

    public class CsvExportService : ICsvExportService
    {

        private readonly ICsvDataRetriever _dataRetriever;
        private readonly ICsvFileGenerator _fileGenerator;
        private readonly ICsvDataTransformer _dataTransformer;
        private readonly ICsvValidator _validator;

        public CsvExportService(ICsvDataRetriever dataRetriever, ICsvFileGenerator fileGenerator, ICsvDataTransformer dataTransformer, ICsvValidator validator)
        {
            _dataRetriever = dataRetriever ?? throw new ArgumentNullException(nameof(dataRetriever));
            _fileGenerator = fileGenerator ?? throw new ArgumentNullException(nameof(fileGenerator));
            _dataTransformer = dataTransformer ?? throw new ArgumentNullException(nameof(dataTransformer));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }


        public async Task<CsvExportResult> ExportToCsvAsync(CsvExportRequest request)
        {
            if (request == null)
            {
                throw new CsvExportException("Export request cannot be null", "Invalid export request", "Export");
            }

            if (request.JsonData == null || request.JsonData.Count == 0)
            {
                throw new CsvExportException("No data provided for export", "No data available for export", "Export");
            }

            try
            {
                await _validator.ValidateExportDataAsync(request.JsonData, request.Options);
                return await _fileGenerator.GenerateExportResultAsync(request.JsonData, request.Options, request.FileName);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Export operation failed : {ex.Message}", "Unable to complete the export operation", ex, "Export");
            }
        }

        public async Task<CsvExportResult> ExportFromJsonAsync(string jsonString, CsvExportOptions? options = null, string? fileName = null)
        {
            try
            {
                var data = await _dataRetriever.RetrieveFromJsonAsync(jsonString);
                var exportOptions = options ?? new CsvExportOptions();

                var request = new CsvExportRequest
                {
                    JsonData = data,
                    Options = exportOptions,
                    FileName = fileName
                };

                return await ExportToCsvAsync(request);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Json export failed : {ex.Message}", "Unable to export Json data to CSV", ex, "JsonExport");
            }
        }


        public async Task<CsvExportResult> ExportFromObjectsAsync<T>(List<T> objects, CsvExportOptions? options = null, string? fileName = null)
        {

            if (objects == null)
            {
                throw new CsvExportException("Objects list cannot be null", "No data provided for export", "ObjectExport");
            }

            try
            {
                var jsonData = objects.Select(obj =>
                {
                    if (obj == null) return new Dictionary<string, object>();

                    var json = JsonConvert.SerializeObject(obj, options?.JsonSettings ?? new JsonSerializerSettings());
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                }).ToList();

                var exportOptions = options ?? new CsvExportOptions();

                var request = new CsvExportRequest
                {
                    JsonData = jsonData,
                    Options = exportOptions,
                    FileName = fileName
                };

                return await ExportToCsvAsync(request);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Object export failed : {ex.Message}", "Unable to export objects to CSV", ex, "ObjectExport");
            }

        }


        public async Task<CsvImportResult> ImportFromCsvAsync(CsvImportRequest request)
        {

            if (request == null)
            {
                throw new CsvExportException("Import request cannot be null", "Invalid import request", "Import");
            }

            if (request.CsvData == null || request.CsvData.Length == 0)
            {
                throw new CsvExportException("No CSV data provided for import", "No data available for import", "Import");
            }

            try
            {
                await _validator.ValidateImportDataAsync(request.CsvData, request.Options);

                var rawData = await _dataRetriever.RetrieveFromCsvAsync(request.CsvData, request.Options);
                var transformedData = _dataTransformer.TransformFromImport(rawData, request.Options);
                var headers = _dataTransformer.ExtractHeaders(transformedData, new CsvExportOptions());

                return new CsvImportResult
                {
                    Data = transformedData,
                    Headers = headers,
                    RecordCount = transformedData.Count,
                    ImportedAt = DateTime.UtcNow,
                    Warnings = new List<string>()
                };
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Import operation failed : {ex.Message}", "Unable to complete the import operation", ex, "Import");
            }
        }


        public async Task<CsvImportResult> ImportFromCsvBytesAsync(byte[] csvBytes, CsvImportOptions? options = null)
        {
            var importOptions = options ?? new CsvImportOptions();

            var request = new CsvImportRequest
            {
                CsvData = csvBytes,
                Options = importOptions
            };

            return await ImportFromCsvAsync(request);
        }

        public async Task<CsvImportResult> ImportFromCsvStreamAsync(Stream csvStream, CsvImportOptions? options = null)
        {

            if (csvStream == null)
            {
                throw new CsvExportException("CSV stream cannot be null", "No Csv Stream provided", "StreamImport");
            }

            try
            {

                var importOptions = options ?? new CsvImportOptions();
                var data = await _dataRetriever.RetrieveFromCsvStreamAsync(csvStream, importOptions);

                var transformedData = _dataTransformer.TransformFromImport(data, importOptions);
                var headers = _dataTransformer.ExtractHeaders(transformedData, new CsvExportOptions());

                return new CsvImportResult
                {
                    Data = transformedData,
                    Headers = headers,
                    RecordCount = transformedData.Count,
                    ImportedAt = DateTime.UtcNow,
                    Warnings = new List<string>()
                };
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"Stream import failed : {ex.Message}", "Unable to import CSV from stream", ex, "StreamImport");
            }
        }


        public async Task<string> ConvertToJsonAsync(byte[] csvBytes, CsvImportOptions? options = null)
        {

            try
            {
                var importResult = await ImportFromCsvBytesAsync(csvBytes, options);
                var jsonSettings = options?.JsonSettings ?? new JsonSerializerSettings { Formatting = Formatting.Indented };
                return JsonConvert.SerializeObject(importResult.Data, jsonSettings);
            }
            catch (Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"CSV to JSON conversion failed : {ex.Message}", "Unable to convert CSV to JSON", ex, "CsvToJsonConversion");
            }
        }


        public async Task<byte[]> ConvertJsonToCsvAsync(string jsonString , CsvExportOptions? options =  null)
        {

            try
            {
                var exportResult = await ExportFromJsonAsync(jsonString , options);
                return exportResult.Data;
            }
            catch(Exception ex) when (!(ex is CsvExportException))
            {
                throw new CsvExportException($"JSON to CSV conversion failed : {ex.Message}", "Unable to convert JSON to CSV", ex, "JsonToCsvConversion");
            }

        }
    }

}