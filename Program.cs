using SIVAD.Repositories;
using SIVAD.Strategies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Acesso direto ao SQL Server usando ADO.NET.
builder.Services.AddScoped<SqlServerRepository>();

// Estratégias de cálculo usadas pelos fluxos de pedidos e compras.
builder.Services.AddScoped<TotalPedidoStrategy>();
builder.Services.AddScoped<TotalCompraEstoqueStrategy>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Autenticacao}/{action=Login}/{id?}");

app.Run();
