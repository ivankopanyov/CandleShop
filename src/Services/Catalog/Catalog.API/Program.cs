var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddEntityFrameworkSqlite().AddDbContext<CatalogContext>();
builder.Services.AddMvc(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

var app = builder.Build();

using (var client = new CatalogContext(app.Configuration))
{
    // client.Database.EnsureDeleted();
    client.Database.EnsureCreated();

    if (client.CatalogCategories.SingleOrDefault(cc => cc.ParentCategoryId == null) == null)
    {
        var baseCategory = new CatalogCategory()
        {
            Name = "All",
            ParentCategoryId = null
        };

        client.CatalogCategories.Add(baseCategory);
        client.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
