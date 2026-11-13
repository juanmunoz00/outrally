using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Outrally.Infrastructure;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
        builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("OutrallyDev"));
    else
        builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    var jwtKey = builder.Configuration["Jwt:Key"]!;
    var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
    var jwtAudience = builder.Configuration["Jwt:Audience"]!;

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("http://localhost:4200", "https://outrally.juanmunoz.dev", "https://jamunozz-001-site3.ltempurl.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    builder.Services.AddScoped<IEncryptionService, EncryptionService>();

    var app = builder.Build();

    // Seed default organization
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Organizations.Any())
        {
            db.Organizations.Add(new Outrally.Core.Organization { Name = "Outrally" });
            db.SaveChanges();
        }
    }

    // Seed test users (dev only)
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        async Task SeedUser(string email, string firstName, string lastName, UserRole role)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    OrganizationId = 1,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "Test1234!");
            }
        }

        await SeedUser("rafa@outrally.dev",  "Rafa",   "Admin",  UserRole.AD);
        await SeedUser("javi@outrally.dev", "Javi",  "Leader", UserRole.TL);
        await SeedUser("joe@outrally.dev", "Joe",  "Leader", UserRole.TL);

        await SeedUser("vivi@outrally.dev", "Vivi", "Member", UserRole.OE);
        await SeedUser("jimmy@outrally.dev", "Jimmy", "Member", UserRole.OE);
        await SeedUser("juanis@outrally.dev", "Juanis", "Member", UserRole.OE);
        await SeedUser("eli@outrally.dev", "Eli", "Member", UserRole.OE);                
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL STARTUP ERROR: {ex}");
}

public partial class Program { }