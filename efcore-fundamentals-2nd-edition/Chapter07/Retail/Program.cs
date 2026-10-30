using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var connection = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Set ConnectionStrings:DefaultConnection.");
var retry = !builder.Environment.IsDevelopment();
CommandLog.On = builder.Configuration.GetValue<bool>("ShowSql");
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(connection, sql =>
        {
            if (retry) sql.EnableRetryOnFailure(3);
        })
        .LogTo(CommandLog.Filter, CommandLog.Write));

using var host = builder.Build();
await Labs.RunAsync(host.Services, builder.Configuration["lab"]);
