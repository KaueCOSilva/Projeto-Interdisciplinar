using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;
using SIVAD.Infrastructure;
using SIVAD.Repositories;
using SIVAD.Strategies;

// Program.cs configura a aplicação; regras de negócio ficam nos controladores e repositórios.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opcoes =>
{
    opcoes.IdleTimeout = TimeSpan.FromMinutes(60);
    opcoes.Cookie.Name = ".SIVAD.Sessao";
    opcoes.Cookie.HttpOnly = true;
    opcoes.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews(opcoes =>
{
    // O navegador envia type="number" com ponto, enquanto o usuário brasileiro
    // também pode digitar vírgula. Normalize antes do model binding decimal.
    opcoes.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
});

// Acesso ao SQL Server pelo Microsoft.Data.SqlClient, concentrado no repositório.
builder.Services.AddScoped<IRepositorioSIVAD, RepositorioSqlServer>();

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
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Autenticacao}/{action=Login}/{id?}");

app.Run();
