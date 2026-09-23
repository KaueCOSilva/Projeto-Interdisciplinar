using Microsoft.EntityFrameworkCore;
using SIVAD.Models;

namespace SIVAD.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Pessoa> Pessoas { get; set; }
        public DbSet<Administrador> Administradores { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Fornecedor> Fornecedores { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<CompraEstoque> CompraEstoque { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemCompraEstoque> Itens_CompraEstoque { get; set; }
        public DbSet<ItemPedido> Itens_Pedidos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------------------------------------------------
            // COMPRA ESTOQUE E ITENS
            // -------------------------------------------------------------------
            modelBuilder.Entity<CompraEstoque>(entity =>
            {
                entity.ToTable("CompraEstoque");
                entity.HasKey(c => c.Numero);

                entity.Property(c => c.Numero).HasColumnName("numero");
                entity.Property(c => c.DataCompra).HasColumnName("data_compra");
                entity.Property(c => c.Total).HasColumnName("total");
                entity.Property(c => c.Status).HasColumnName("status");
                entity.Property(c => c.AdministradorId).HasColumnName("administrador_id");
                entity.Property(c => c.FornecedorId).HasColumnName("fornecedor_id");
            });

            modelBuilder.Entity<ItemCompraEstoque>(entity =>
            {
                entity.ToTable("Itens_CompraEstoque");
                entity.HasKey(ic => new { ic.CompraNumero, ic.ProdutoCodigo });

                entity.Property(ic => ic.CompraNumero).HasColumnName("compra_numero");
                entity.Property(ic => ic.ProdutoCodigo).HasColumnName("produto_codigo");
                entity.Property(ic => ic.Quantidade).HasColumnName("quantidade");
                entity.Property(ic => ic.Valor).HasColumnName("valor");

                // Mapeamento explícito da chave estrangeira
                entity.HasOne(ic => ic.CompraEstoque)
                      .WithMany(c => c.Itens)
                      .HasForeignKey(ic => ic.CompraNumero);
            });

            // -------------------------------------------------------------------
            // PEDIDOS E ITENS
            // -------------------------------------------------------------------
            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.ToTable("Pedidos");
                entity.HasKey(p => p.Codigo);

                entity.Property(p => p.Codigo).HasColumnName("codigo");
                entity.Property(p => p.Status).HasColumnName("status");
                entity.Property(p => p.ValorTotal).HasColumnName("valor_total");
                entity.Property(p => p.ClienteId).HasColumnName("cliente_id");
                entity.Property(p => p.FuncionarioId).HasColumnName("funcionario_id");
            });

            modelBuilder.Entity<ItemPedido>(entity =>
            {
                entity.ToTable("Itens_Pedidos");
                entity.HasKey(ip => new { ip.PedidoCodigo, ip.ProdutoCodigo });

                entity.Property(ip => ip.PedidoCodigo).HasColumnName("pedido_codigo");
                entity.Property(ip => ip.ProdutoCodigo).HasColumnName("produto_codigo");
                entity.Property(ip => ip.Qtd).HasColumnName("qtd");
                entity.Property(ip => ip.PrecoTotal).HasColumnName("preco_total");

                // Mapeamento explícito da chave estrangeira
                entity.HasOne(ip => ip.Pedido)
                      .WithMany(p => p.Itens)
                      .HasForeignKey(ip => ip.PedidoCodigo);
            });

            // -------------------------------------------------------------------
            // PRODUTOS E CATEGORIAS
            // -------------------------------------------------------------------
            modelBuilder.Entity<Produto>(entity =>
            {
                entity.ToTable("Produtos");
                entity.HasKey(p => p.Codigo);

                entity.Property(p => p.Codigo).HasColumnName("codigo");
                entity.Property(p => p.CodigoBarras).HasColumnName("codigo_barras");
                entity.Property(p => p.Nome).HasColumnName("nome");
                entity.Property(p => p.PrecoUnit).HasColumnName("preco_unit");
                entity.Property(p => p.Estoque).HasColumnName("estoque");
                entity.Property(p => p.CategoriaCodigo).HasColumnName("categoria_codigo");
            });

            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("Categorias");
                entity.HasKey(c => c.Codigo);

                entity.Property(c => c.Codigo).HasColumnName("codigo");
                entity.Property(c => c.Nome).HasColumnName("nome");
            });
        }
    }
}