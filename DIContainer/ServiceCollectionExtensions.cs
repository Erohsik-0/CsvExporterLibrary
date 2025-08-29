using CsvExporterLibrary.DataTransformers;
using CsvExporterLibrary.FileGeneration;
using CsvExporterLibrary.Interfaces;
using CsvExporterLibrary.Services;
using CsvExporterLibrary.TypeDetectors;
using CsvExporterLibrary.Validators;
using Microsoft.Extensions.DependencyInjection;



namespace CsvExporterLibrary.DIContainer
{

    /// <summary>
    /// Extension methods for configuring dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the CSV Exporter service to the service collection.
        /// </summary>
        
        public static IServiceCollection AddCsvExporterServices(this IServiceCollection services)
        {

            //Register the CsvExportService with scoped lifetime
            
            services.AddScoped<ICsvExportService, CsvExportService>();
            services.AddScoped<ITypeDetector , TypeDetector>();
            services.AddScoped<ICsvDataTransformer , CsvDataTransformer>();
            services.AddScoped<ICsvValidator , CsvValidator>();
            services.AddScoped<ICsvDataRetriever , CsvDataRetriever>();
            services.AddScoped<ICsvFileGenerator , CsvFileGenerator>();

            //Making sure logging is available for injection
            services.AddLogging();

            return services;
        }

    }
}
