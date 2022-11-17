using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Data.Common;
using System.Diagnostics.Metrics;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddEntityFrameworkSqlite().AddDbContext<IdentityContext>();
builder.Services
    .AddIdentity<User, Role>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<IdentityContext>();

builder.Services.AddScoped<ITokenCreationService, JwtService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            )
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, MinimumRankHandler>();

builder.Services.AddAuthorization(options =>
{
    for (int i = RoleSettings.MAX_RANK; i >= RoleSettings.MIN_RANK; i--)
        options.AddPolicy($"Rank{i}", policy =>
            policy.Requirements.Add(new MinimumRankRequirement(i)));
});

var app = builder.Build();

using (var client = new IdentityContext(app.Configuration))
{
    client.Database.EnsureDeleted();
    client.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseCors(x => x
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

var policies = Assembly.GetExecutingAssembly().GetTypes()
    .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
    .SelectMany(type => type.GetMethods())
    .Where(method => method.IsDefined(typeof(AuthorizeAttribute)))
    .Select(method => ((AuthorizeAttribute)method.GetCustomAttributes().First(attr => attr.GetType() == typeof(AuthorizeAttribute))).Policy)
    .Where(policy => !policy.IsNullOrEmpty())
    .ToArray();

var p = string.Join(", ", policies);


using (var scope = app.Services.CreateScope())
{
    var roleManager = (RoleManager<Role>)scope.ServiceProvider.GetService(typeof(RoleManager<Role>))!;
    string roleName = "";

    if (roleManager.Roles.Count() == 0)
        for (int i = RoleSettings.MIN_RANK; i <= RoleSettings.MAX_RANK; i++)
        {
            roleName = $"{RoleSettings.DEFAULT_ROLE_NAME} with rank {i}";
            roleManager.CreateAsync(new Role(roleName, i));
        }
    else
    {
        Role maxRole = null!;
        foreach (var role in roleManager.Roles.ToArray())
            if (maxRole == null || role.Rank > maxRole.Rank)
                maxRole = role;

        roleName = maxRole.Name;
    }

    var userManager = (UserManager<User>)scope.ServiceProvider.GetService(typeof(UserManager<User>))!;
    if (userManager.Users.Count() == 0)
    {
        var user = new User()
        {
            UserName = "supervisor@candleshop.com",
            Email = "supervisor@candleshop.com"
        };

        userManager.CreateAsync(user, "supervisor");
        userManager.AddToRolesAsync(user, new[] { roleName });
    }
}

app.Run();