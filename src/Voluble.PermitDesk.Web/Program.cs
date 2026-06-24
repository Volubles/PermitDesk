using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Voluble.PermitDesk.Web.Cases;
using Voluble.PermitDesk.Web.Components;
using Voluble.PermitDesk.Web.Data;
using Voluble.PermitDesk.Web.Files;
using Voluble.PermitDesk.Web.Mail;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 5L * 1024 * 1024 * 1024);

builder.Services.AddMudServices();

var connectionString = builder.Configuration.GetConnectionString("PermitDesk")
    ?? throw new InvalidOperationException(
        "Missing connection string 'PermitDesk'. Add it to appsettings.Local.json.");

builder.Services.AddDbContextFactory<PermitDeskDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<CaseNumberGenerator>();

builder.Services.Configure<LocalStorageOptions>(
    builder.Configuration.GetSection(LocalStorageOptions.SectionName));
builder.Services.AddScoped<ICaseStorage, LocalCaseStorage>();

builder.Services.Configure<FakeMailInboxOptions>(
    builder.Configuration.GetSection(FakeMailInboxOptions.SectionName));
builder.Services.AddScoped<IMailInbox, FakeMailInbox>();
builder.Services.AddSingleton<IMailClassifier, RuleBasedMailClassifier>();
builder.Services.AddScoped<ICaseMatcher, CaseMatcher>();

var app = builder.Build();

if (args.Length > 0 && args[0].Equals("seed", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PermitDeskDbContext>>();
    var numberGenerator = scope.ServiceProvider.GetRequiredService<CaseNumberGenerator>();
    var storage = scope.ServiceProvider.GetRequiredService<ICaseStorage>();
    await SeedData.RunAsync(dbFactory, numberGenerator, storage);
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
