using Asp.Versioning.ApiExplorer;
using ProjectBase.Application;
using ProjectBase.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. API Versioning (Swagger buna ihtiyaç duyuyor, önce eklenmeli)
builder.Services.AddApiVersion();

// 2. Application & Infrastructure services (DbContext, Repositories, MediatR, FluentValidation)
builder.Services.AddApplication(builder.Configuration);

// 3. Controllers
builder.Services.AddControllers();

// 4. Swagger with JWT Authentication (API Versioning ve Controller'lara ihtiyaç duyuyor)
builder.Services.AddSwaggerGenWithAuth();

var app = builder.Build();

// Configure Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", 
                $"API {description.GroupName}");
        }
    });
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Must be before UseAuthorization if authentication is configured
app.UseAuthorization();

app.MapControllers();

app.Run();
