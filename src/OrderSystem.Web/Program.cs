using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json.Serialization;
using OrderSystem.Web.Data;
using OrderSystem.Web.Data.Repositories;
using OrderSystem.Web.Models;
using OrderSystem.Web.Dto;
using OrderSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

_ = builder.Configuration.GetConnectionString("MySql")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:MySql in appsettings.json");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<MySqlConnectionFactory>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<OrderPaymentService>();
builder.Services.AddScoped<ImportService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = 400;
        context.Response.ContentType = "text/plain; charset=utf-8";
        if (ex is null)
        {
            await context.Response.WriteAsync("Unknown error.");
            return;
        }
        await context.Response.WriteAsync(ex.Message);
    });
});

app.UseDefaultFiles(); 
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/api/customers", async (ICustomerRepository repo, CancellationToken ct) =>
{
    return Results.Ok(await repo.GetAllAsync(ct));
});

app.MapGet("/api/customers/{id:int}", async (int id, ICustomerRepository repo, CancellationToken ct) =>
{
    var c = await repo.GetByIdAsync(id, ct);
    return c is null ? Results.NotFound() : Results.Ok(c);
});

app.MapPost("/api/customers", async (Customer customer, ICustomerRepository repo, CancellationToken ct) =>
{
    ValidateCustomer(customer);
    var id = await repo.CreateAsync(customer, ct);
    return Results.Ok(id);
});

app.MapPut("/api/customers/{id:int}", async (int id, Customer customer, ICustomerRepository repo, CancellationToken ct) =>
{
    if (id != customer.Id) throw new ArgumentException("Path id does not match body id.");
    ValidateCustomer(customer);
    var ok = await repo.UpdateAsync(customer, ct);
    return ok ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/customers/{id:int}", async (int id, ICustomerRepository repo, CancellationToken ct) =>
{
    var ok = await repo.DeleteAsync(id, ct);
    return ok ? Results.Ok() : Results.NotFound();
});


app.MapGet("/api/products", async (IProductRepository repo, CancellationToken ct) =>
{
    return Results.Ok(await repo.GetAllAsync(ct));
});

app.MapGet("/api/products/{id:int}", async (int id, IProductRepository repo, CancellationToken ct) =>
{
    var p = await repo.GetByIdAsync(id, ct);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

app.MapPost("/api/products", async (Product product, IProductRepository repo, CancellationToken ct) =>
{
    ValidateProduct(product);
    var id = await repo.CreateAsync(product, ct);
    return Results.Ok(id);
});

app.MapPut("/api/products/{id:int}", async (int id, Product product, IProductRepository repo, CancellationToken ct) =>
{
    if (id != product.Id) throw new ArgumentException("Path id does not match body id.");
    ValidateProduct(product);
    var ok = await repo.UpdateAsync(product, ct);
    return ok ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/products/{id:int}", async (int id, IProductRepository repo, CancellationToken ct) =>
{
    var ok = await repo.DeleteAsync(id, ct);
    return ok ? Results.Ok() : Results.NotFound();
});


app.MapGet("/api/orders", async (IOrderRepository repo, CancellationToken ct) =>
{
    return Results.Ok(await repo.GetAllAsync(ct));
});

app.MapGet("/api/orders/{id:int}", async (int id, IOrderRepository repo, CancellationToken ct) =>
{
    var o = await repo.GetByIdAsync(id, ct);
    if (o is null) return Results.NotFound();
    var items = await repo.GetItemsAsync(id, ct);
    return Results.Ok(new { order = o, items });
});

app.MapPost("/api/orders", async (CreateOrderRequest req, OrderService svc, CancellationToken ct) =>
{
    var id = await svc.CreateOrderAsync(req, ct);
    return Results.Ok(id);
});

app.MapPut("/api/orders/{id:int}/paid", async (int id, SetOrderPaidRequest req, OrderPaymentService svc, CancellationToken ct) =>
{
    await svc.SetPaidAsync(id, req, ct);



    return Results.Ok();
});



app.MapDelete("/api/orders/{id:int}", async (int id, IOrderRepository repo, CancellationToken ct) =>
{
    var existing = await repo.GetByIdAsync(id, ct);
    if (existing is null) return Results.NotFound(new { message = "Order not found." });

    await repo.DeleteAsync(id, ct);
    return Results.NoContent();
});


app.MapGet("/api/reports/top-customers", async (int? limit, IReportRepository repo, CancellationToken ct) =>
{
    var lim = (limit is null or <= 0 or > 100) ? 10 : limit.Value;
    return Results.Ok(await repo.GetTopCustomersAsync(lim, ct));
});

app.MapGet("/api/reports/product-sales", async (IReportRepository repo, CancellationToken ct) =>
{
    return Results.Ok(await repo.GetProductSalesAsync(ct));
});

app.MapGet("/api/reports/order-totals", async (IReportRepository repo, CancellationToken ct) =>
{
    return Results.Ok(await repo.GetOrderTotalsAsync(ct));
});


app.MapPost("/api/import/customers", async (HttpRequest request, ImportService svc, CancellationToken ct) =>
{
    if (!request.HasFormContentType) throw new ArgumentException("Expected multipart/form-data with file.");
    var form = await request.ReadFormAsync(ct);
    var file = form.Files["file"] ?? throw new ArgumentException("Missing file field.");
    await using var stream = file.OpenReadStream();
    var inserted = await svc.ImportCustomersCsvAsync(stream, ct);
    return Results.Ok(new { inserted });
});

app.MapPost("/api/import/products", async (HttpRequest request, ImportService svc, CancellationToken ct) =>
{
    if (!request.HasFormContentType) throw new ArgumentException("Expected multipart/form-data with file.");
    var form = await request.ReadFormAsync(ct);
    var file = form.Files["file"] ?? throw new ArgumentException("Missing file field.");
    await using var stream = file.OpenReadStream();
    var inserted = await svc.ImportProductsJsonAsync(stream, ct);
    return Results.Ok(new { inserted });
});

app.Run();

static void ValidateCustomer(Customer c)
{
    if (string.IsNullOrWhiteSpace(c.FirstName)) throw new ArgumentException("FirstName is required.");
    if (string.IsNullOrWhiteSpace(c.LastName)) throw new ArgumentException("LastName is required.");
    if (string.IsNullOrWhiteSpace(c.Email) || !c.Email.Contains('@')) throw new ArgumentException("Email must contain '@'.");
}

static void ValidateProduct(Product p)
{    if (string.IsNullOrWhiteSpace(p.Name)) throw new ArgumentException("Name is required.");
    if (p.Price < 0) throw new ArgumentException("Price must be >= 0.");
    if (p.Stock < 0) throw new ArgumentException("Stock must be >= 0.");
}