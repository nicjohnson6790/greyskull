using LTOCS.Config;
using LTOCS.Data;
using LTOCS.Middleware;
using LTOCS.Models.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

var apiConfig = builder.Configuration.GetSection("APIConfig").Get<APIConfig>() ?? new APIConfig();
builder.Services.AddSingleton(apiConfig);

var jwtConfig = builder.Configuration.GetSection("JwtConfig").Get<JwtConfig>() ?? new JwtConfig();
builder.Services.AddSingleton(jwtConfig);

var connConfig = builder.Configuration.GetSection("ConnConfig").Get<ConnectionConfig>() ?? new ConnectionConfig();
builder.Services.AddSingleton(connConfig);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connConfig.DbConnection));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var key = Encoding.ASCII.GetBytes(jwtConfig.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(gen =>
{
    gen.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "LTOCS API", Version = "v1" });

    gen.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please insert JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    gen.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }});
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LTOCS API V1");
    c.RoutePrefix = string.Empty;
});

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
    }
    await next();
});

app.MapControllers();

app.Run();