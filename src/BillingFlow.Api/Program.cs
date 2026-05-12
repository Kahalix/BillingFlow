//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();


// File: src/BillingFlow.Api/Program.cs
using BillingFlow.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURE SERVICES ---

// Add Layer Dependencies (Thanks to our DependencyInjection classes)
builder.Services.AddApplication();
//builder.Services.AddInfrastructure(builder.Configuration);
//builder.Services.AddBillingMigrations(builder.Configuration);

// Add API Specific Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// --- 2. CONFIGURE PIPELINE ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

// Use authentication/authorization logic provided by the framework
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
