using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;
using SIVAD.Repositories;
using SIVAD.Strategies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Acesso ao SQL Server pelo Microsoft.Data.SqlClient, concentrado no Repository.
builder.Services.AddScoped<ISIVADRepository, SqlServerRepository>();

// Estratégias de cálculo usadas pelos fluxos de pedidos e compras.
builder.Services.AddScoped<TotalPedidoStrategy>();
builder.Services.AddScoped<TotalCompraEstoqueStrategy>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Autenticacao}/{action=Login}/{id?}");

app.Run();
