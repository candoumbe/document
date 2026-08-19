using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.Conventions;
using Candoumbe.Types.Numerics;
using Documents.API;
using Documents.API.TypeMappers;
using Documents.DataStores;
using Documents.Ids;
using FastEndpoints;
using FastEndpoints.AspVersioning;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Scalar.AspNetCore;
using Serilog;
using SystemTextJsonPatch.Operations;
using static Microsoft.AspNetCore.Http.StatusCodes;

Action<JsonSerializerOptions> optionsSerializerSettings = s =>
{
    //s.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    s.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    s.AllowTrailingCommas = true;
    s.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    s.Converters.Add(new JsonStringEnumConverter<OperationType>());
    s.Converters.Add(new NonNegativeLongJsonConverter());
};

// Api versions that are currently suported
VersionSets.CreateApi("documents",
    v =>
    {
        v.HasApiVersion(1.0);
        v.HasApiVersion(2.0);
    });


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddCustomizedDependencyInjection();
builder.Services.AddCustomOptions(builder.Configuration);
builder.AddNpgsqlDbContext<DocumentsStore>("postgres",
    configureDbContextOptions: optionsBuilder =>
    {
        optionsBuilder.UseNpgsql(o => o.UseNodaTime()
            .MigrationsAssembly("Documents.DataStores.Postgres"));
    });
builder.Services.AddDataStores();
builder.Services.AddSerilog((serviceProvider, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(serviceProvider));
builder.Services.Configure<JsonOptions>(c => optionsSerializerSettings.Invoke(c.SerializerOptions));
builder.Services
    .SwaggerDocument(options =>
    {
        options.ShortSchemaNames = true;
        options.ShowDeprecatedOps = true;
        options.MaxEndpointVersion = 1;
        options.DocumentSettings = docSettings =>
        {
            docSettings.ApiVersion(new(1.0));
            docSettings.SchemaSettings.AllowReferencesWithProperties = true;
            docSettings.SchemaSettings.TypeMappers.Add(new NumberTypeMapper<PositiveInteger, int>());
            docSettings.SchemaSettings.TypeMappers.Add(new NumberTypeMapper<NonNegativeInteger, int>());
        };
        options.SerializerSettings = optionsSerializerSettings;
        options.AutoTagPathSegmentIndex = 0;
    });

builder.Services.AddFastEndpoints(options => options.IncludeAbstractValidators = false)
    .AddVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        HeaderApiVersionReader optionsApiVersionReader = new HeaderApiVersionReader("api-version");
        optionsApiVersionReader.VersionsByHeader();
        optionsApiVersionReader.VersionsByMediaType();
        options.ApiVersionReader = optionsApiVersionReader;
        options.UnsupportedApiVersionStatusCode = Status400BadRequest;
    });

WebApplication app = builder.Build();

//app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) => diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier));
app.UseFastEndpoints(config =>
{
    config.Binding.ValueParserFor<DocumentId>(values => new ParseResult(DocumentId.TryParse(values.ToString(), CultureInfo.InvariantCulture, out DocumentId id), id));
    config.Binding.ValueParserFor<NonNegativeLong>(values => new ParseResult(long.TryParse(values.ToString(), out long value)
                                                                             && NonNegativeLong.MinValue <= value && value <= NonNegativeLong.MaxValue, NonNegativeLong.From(value)));
    config.Binding.ValueParserFor<NonNegativeInteger>(values => new ParseResult(int.TryParse(values.ToString(), out int value)
                                                                                && NonNegativeInteger.MinValue <= value && value <= NonNegativeInteger.MaxValue, NonNegativeInteger.From(value)));
    config.Binding.ValueParserFor<PositiveInteger>(values => new ParseResult(int.TryParse(values.ToString(), out int value)
                                                                             && PositiveInteger.MinValue <= value
                                                                             && value <= PositiveInteger.MaxValue,
        PositiveInteger.From(value)));

    config.Errors.UseProblemDetails(detailsConfig =>
    {
        detailsConfig.AllowDuplicateErrors = true;
        detailsConfig.IndicateErrorCode = true;
        detailsConfig.TypeTransformer = problemDetails => problemDetails.Status switch
        {
            Status200OK => "https://www.rfc-editor.org/rfc/rfc7231#section-6.3.1",
            Status404NotFound => "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.4",
            Status409Conflict => "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.8",
            Status429TooManyRequests => "https://www.rfc-editor.org/rfc/rfc6585#section-4",
            _ => "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1"
        };
    });

    optionsSerializerSettings.Invoke(config.Serializer.Options);
});

app.UseOpenApi(opts => opts.Path = "/openapi/{documentName}.json");
app.MapScalarApiReference(opts =>
{
    opts.AddDocuments(
    [
        new ScalarDocument("v1", "Documents API v1", IsDefault: true)
    ]);
    opts.ForceDarkMode();
});

app.MapGet("/scalar/{documentName}/{**asset}", (string asset) => Results.LocalRedirect($"/scalar/{asset}"))
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);

return;


/// <summary>
/// Application entry point
/// </summary>
public partial class Program;