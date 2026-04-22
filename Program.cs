using Microsoft.AspNetCore.Mvc;
using Netwise_Rekrutacja_D_Jamrozy.Infrastructure.Security;
using Netwise_Rekrutacja_D_Jamrozy.Models.Options;
using Netwise_Rekrutacja_D_Jamrozy.Services;
using Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options => {options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); });

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddOptions<CatFactApiOptions>()
    .Bind(builder.Configuration.GetSection(CatFactApiOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.BaseUrl) && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
        "CatFactApi:BaseUrl must be a valid absolute URL.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.FactEndpoint),
        "CatFactApi:FactEndpoint is required.")
    .Validate(
        options => options.TimeoutSeconds > 0 && options.TimeoutSeconds <= 60,
        "CatFactApi:TimeoutSeconds must be between 1 and 60.");

builder.Services
    .AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.DataDirectory),
        "Storage:DataDirectory is required.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.FactsFileName),
        "Storage:FactsFileName is required.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.CrudLogFileName),
        "Storage:CrudLogFileName is required.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ErrorLogFileName),
        "Storage:ErrorLogFileName is required.")
    .Validate(
        options => options.MaxFactLength > 0 && options.MaxManualEntryLength > 0,
        "Storage max lengths must be greater than 0.");

builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddScoped<ICrudAuditService, CrudAuditService>();
builder.Services.AddScoped<IClientIpResolver, ClientIpResolver>();

builder.Services.AddHttpClient<ICatFactService, CatFactService>((serviceProvider, httpClient) =>
{
    var apiOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CatFactApiOptions>>().Value;

    httpClient.BaseAddress = new Uri(new Uri(apiOptions.BaseUrl), apiOptions.FactEndpoint);
    httpClient.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
    httpClient.DefaultRequestHeaders.Accept.Clear();
    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Netwise_Rekrutacja_D_Jamrozy/1.0");
});

var app = builder.Build();

await EnsureStorageAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "api",
    pattern: "api/{action=Index}/{id?}",
    defaults: new { controller = "Api" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();

static async Task EnsureStorageAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
    await fileStorageService.EnsureStorageReadyAsync();
}