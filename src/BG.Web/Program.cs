using BG.Infrastructure;
using BG.Infrastructure.Persistence;
using BG.Web.Configuration;

var runOperationalSeed = args.Any(argument => string.Equals(argument, "--seed-operational-demo", StringComparison.OrdinalIgnoreCase));
var builder = WebApplication.CreateBuilder(args);

// Modular Service Registration [A-ARCH-02]
builder.Services.AddBgSecurity(builder.Configuration, builder.Environment)
               .AddBgProjectServices(builder.Configuration)
               .AddBgWebDefaults(builder.Configuration);

var app = builder.Build();

ProductionReadinessValidator.Validate(app.Configuration, app.Environment);
await app.Services.InitializeInfrastructureAsync();

if (runOperationalSeed)
{
    if (app.Environment.IsProduction())
    {
        throw new InvalidOperationException("Operational demo seeding cannot run when ASPNETCORE_ENVIRONMENT is Production.");
    }

    await app.Services.RunOperationalSeedAsync();
    return;
}

// Modular Middleware Orchestration [A-ARCH-02]
app.UseBgWebDefaults();
app.UseBgSecurity(app.Environment);

var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapBgEndpoints();

app.Run();

public partial class Program
{
}
