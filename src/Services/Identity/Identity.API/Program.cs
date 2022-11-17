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

builder.Services.AddTransient<IAuthorizationHandler, MinimumRankHandler>();

var policies = Assembly.GetExecutingAssembly().GetTypes()
    .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
    .SelectMany(type => type.GetMethods())
    .Where(method => method.IsDefined(typeof(AuthorizeAttribute)))
    .Select(method => ((AuthorizeAttribute)method.GetCustomAttributes().First(attr => attr.GetType() == typeof(AuthorizeAttribute))).Policy)
    .Where(policy => !policy.IsNullOrEmpty())
    .ToHashSet();

builder.Services.AddAuthorization(options =>
{
    foreach (var policy in policies)
        options.AddPolicy(policy!, p =>
            p.Requirements.Add(new MinimumRankRequirement(policy!)));
});

var app = builder.Build();

using (var client = new IdentityContext(app.Configuration))
{
    client.Database.EnsureDeleted();
    client.Database.EnsureCreated();

    foreach (var policy in client.Policies.ToHashSet())
        if (!policies.Contains(policy.Name))
            client.Policies.Remove(policy);

    foreach (var policy in policies)
        if (client.Policies.FirstOrDefault(p => p.Name == policy) == null)
            client.Policies.Add(
                new Policy()
                {
                    Name = policy!,
                    MinimumRank = 1
                });

    client.SaveChanges();

    int maxRank = 0;
    foreach (var policy in client.Policies)
        if (policy.MinimumRank > maxRank)
            maxRank = policy.MinimumRank;

    using (var scope = app.Services.CreateScope())
    {
        var roleManager = (RoleManager<Role>)scope.ServiceProvider.GetService(typeof(RoleManager<Role>))!;
        string roleName = null!;
        if (roleManager.Roles.Count() == 0)
        {
            roleName = "Supervisor";
            roleManager.CreateAsync(new Role(roleName, maxRank));
        }
        else
        {
            Role maxRole = null!;
            foreach (var role in roleManager.Roles)
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

app.Run();