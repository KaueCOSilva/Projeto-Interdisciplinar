using System.Data;
using Microsoft.Data.SqlClient;
using SIVAD.Models;
using SIVAD.Models.ViewModels;

namespace SIVAD.Repositories;

/// <summary>Operações de produtos, devoluções, funcionários e categorias.</summary>
public sealed partial class RepositorioSqlServer
{
    // ================================================================
    // PRODUTOS — ATUALIZAÇÃO e devolução
    // ================================================================
    // Produto não é apagado fisicamente: relacionamentos históricos precisam continuar válidos.
    public async Task<bool> AtualizarProdutoAsync(Produto produto)
    {
        if (produto.Codigo <= 0 || string.IsNullOrWhiteSpace(produto.CodigoBarras) || string.IsNullOrWhiteSpace(produto.Nome) || produto.PrecoUnit < 0 || produto.Estoque < 0 || produto.CategoriaCodigo <= 0 || (produto.Cor?.Trim().Length ?? 0) > 60)
            throw new InvalidOperationException("Informe código, nome, cor (até 60 caracteres), preço, quantidade e categoria válidos.");
        produto.Cor = string.IsNullOrWhiteSpace(produto.Cor) ? "Não informada" : produto.Cor.Trim();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        const string sql = "UPDATE Produtos SET codigo_barras=@codigoBarras,nome=@nome,cor=@cor,preco_unit=@precoUnitario,estoque=@estoque,categoria_codigo=@categoriaCodigo WHERE codigo=@codigo AND ativo=1";
        await using var comando = new SqlCommand(sql, conexao);
        comando.Parameters.Add("@codigoBarras", SqlDbType.VarChar, 50).Value = produto.CodigoBarras.Trim();
        comando.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = produto.Nome.Trim();
        comando.Parameters.Add("@cor", SqlDbType.VarChar, 60).Value = produto.Cor;
        AdicionarValorMonetario(comando, "@precoUnitario", produto.PrecoUnit); AdicionarInteiro(comando, "@estoque", produto.Estoque);
        AdicionarInteiro(comando, "@categoriaCodigo", produto.CategoriaCodigo); AdicionarInteiro(comando, "@codigo", produto.Codigo);
        try { return await comando.ExecuteNonQueryAsync() == 1; }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        { throw new InvalidOperationException("Já existe outro produto com esse código de barras.", ex); }
    }

    // Devolução: aumenta o estoque e registra o movimento em Devolucoes na mesma transação.
    public async Task<int> RegistrarDevolucaoAsync(int produtoCodigo, int quantidade)
    {
        if (produtoCodigo <= 0 || quantidade <= 0)
            throw new InvalidOperationException("Selecione um produto e informe uma quantidade de devolução maior que zero.");

        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(IsolationLevel.Serializable);
        // Serializable + locks impede duas devoluções de ultrapassarem o limite do estoque.
        decimal precoUnitario;
        int estoqueAtual;
        await using (var comandoProduto = new SqlCommand("SELECT preco_unit,estoque FROM Produtos WITH (UPDLOCK,HOLDLOCK) WHERE codigo=@codigo AND ativo=1", conexao, transacao))
        {
            AdicionarInteiro(comandoProduto, "@codigo", produtoCodigo);
            await using var leitor = await comandoProduto.ExecuteReaderAsync();
            if (!await leitor.ReadAsync())
                throw new InvalidOperationException("O produto não foi encontrado ou está inativo.");
            precoUnitario = leitor.GetDecimal(0);
            estoqueAtual = leitor.GetInt32(1);
        }

        if (quantidade > int.MaxValue - estoqueAtual)
            throw new InvalidOperationException("A quantidade devolvida ultrapassa o limite de estoque permitido.");

        var valorTotal = decimal.Round(precoUnitario * quantidade, 2, MidpointRounding.AwayFromZero);
        if (valorTotal > 99_999_999.99m)
            throw new InvalidOperationException("O valor total da devolução ultrapassa o limite permitido pelo banco.");

        await using (var atualizarEstoque = new SqlCommand("UPDATE Produtos SET estoque=estoque+@quantidade WHERE codigo=@codigo AND ativo=1", conexao, transacao))
        {
            AdicionarInteiro(atualizarEstoque, "@quantidade", quantidade);
            AdicionarInteiro(atualizarEstoque, "@codigo", produtoCodigo);
            if (await atualizarEstoque.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException("Não foi possível atualizar o estoque do produto.");
        }

        int numero;
        await using (var comandoInserirDevolucao = new SqlCommand("INSERT INTO Devolucoes(produto_codigo,quantidade,valor_total) OUTPUT INSERTED.numero VALUES(@produtoCodigo,@quantidade,@valor)", conexao, transacao))
        {
            AdicionarInteiro(comandoInserirDevolucao, "@produtoCodigo", produtoCodigo);
            AdicionarInteiro(comandoInserirDevolucao, "@quantidade", quantidade);
            AdicionarValorMonetario(comandoInserirDevolucao, "@valor", valorTotal);
            numero = Convert.ToInt32(await comandoInserirDevolucao.ExecuteScalarAsync());
        }

        await transacao.CommitAsync();
        return numero;
    }
    // ================================================================
    // FUNCIONÁRIOS — INSERÇÃO (INSERT), ATUALIZAÇÃO (UPDATE) e exclusão lógica
    // ================================================================
    // INSERÇÃO grava Pessoa e Funcionário juntos; a pessoa é a entidade base do relacionamento.
    public async Task<int> CadastrarFuncionarioAsync(string nome, string email, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 150 ||
            string.IsNullOrWhiteSpace(email) || email.Trim().Length > 254 ||
            string.IsNullOrWhiteSpace(senhaHash) || senhaHash.Length > 200)
            throw new InvalidOperationException("Informe nome, e-mail válido e senha de pelo menos 8 caracteres.");
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync();
        int id;
        await using (var comandoInserirPessoa = new SqlCommand("INSERT INTO Pessoas(nome) OUTPUT INSERTED.id VALUES(@nome)", conexao, transacao))
        {
            comandoInserirPessoa.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = nome.Trim();
            id = Convert.ToInt32(await comandoInserirPessoa.ExecuteScalarAsync());
        }
        await using (var comandoInserirFuncionario = new SqlCommand("INSERT INTO Funcionarios(pessoa_id,ativo) VALUES(@codigo,1)", conexao, transacao))
        {
            AdicionarInteiro(comandoInserirFuncionario, "@codigo", id);
            await comandoInserirFuncionario.ExecuteNonQueryAsync();
        }
        await using (var comandoInserirAcesso = new SqlCommand(
            "INSERT INTO dbo.CredenciaisAcesso(pessoa_id,perfil,email,senha_hash,ativo) VALUES(@pessoaId,'Funcionario',@email,@senhaHash,1)", conexao, transacao))
        {
            AdicionarInteiro(comandoInserirAcesso, "@pessoaId", id);
            comandoInserirAcesso.Parameters.Add("@email", SqlDbType.VarChar, 254).Value = email.Trim().ToLowerInvariant();
            comandoInserirAcesso.Parameters.Add("@senhaHash", SqlDbType.VarChar, 200).Value = senhaHash;
            try
            {
                await comandoInserirAcesso.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                throw new InvalidOperationException("Esse e-mail já está associado a outra conta.", ex);
            }
        }
        await transacao.CommitAsync();
        return id;
    }

    // EXCLUSÃO lógica: ativo=0 oculta o funcionário, mas preserva pedidos antigos.
    public async Task<bool> DesativarFuncionarioAsync(int id)
    {
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand("UPDATE Funcionarios SET ativo=0 WHERE pessoa_id=@codigo AND ativo=1", conexao);
        AdicionarInteiro(comando, "@codigo", id);
        return await comando.ExecuteNonQueryAsync() == 1;
    }

    // Atualiza dados do funcionário e finaliza uma recuperação sem separar as gravações.
    public async Task<bool> AtualizarFuncionarioAsync(int id, string nome, string? senhaHash, int? solicitacaoId)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 150)
            throw new InvalidOperationException("Informe um nome de funcionário (até 150 caracteres).");
        if (senhaHash?.Length > 200)
            throw new InvalidOperationException("A senha informada não possui um formato válido.");
        if (solicitacaoId.HasValue && string.IsNullOrWhiteSpace(senhaHash))
            throw new InvalidOperationException("Informe a nova senha para concluir a recuperação.");

        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync();

        if (solicitacaoId.HasValue)
        {
            const string verificarSql = "SELECT COUNT(*) FROM dbo.SolicitacoesRecuperacaoSenha WHERE solicitacao_id=@solicitacaoId AND pessoa_id=@id AND status='Pendente'";
            await using var verificar = new SqlCommand(verificarSql, conexao, transacao);
            AdicionarInteiro(verificar, "@solicitacaoId", solicitacaoId.Value);
            AdicionarInteiro(verificar, "@id", id);
            if (Convert.ToInt32(await verificar.ExecuteScalarAsync()) != 1) return false;
        }

        const string atualizarNomeSql = "UPDATE Pessoas SET nome=@nome FROM Pessoas INNER JOIN Funcionarios ON Funcionarios.pessoa_id=Pessoas.id WHERE Pessoas.id=@id AND Funcionarios.ativo=1";
        await using (var atualizarNome = new SqlCommand(atualizarNomeSql, conexao, transacao))
        {
            atualizarNome.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = nome.Trim();
            AdicionarInteiro(atualizarNome, "@id", id);
            if (await atualizarNome.ExecuteNonQueryAsync() != 1) return false;
        }

        if (!string.IsNullOrWhiteSpace(senhaHash))
        {
            const string atualizarSenhaSql = "UPDATE dbo.CredenciaisAcesso SET senha_hash=@senhaHash WHERE pessoa_id=@id AND perfil='Funcionario' AND ativo=1";
            await using var atualizarSenha = new SqlCommand(atualizarSenhaSql, conexao, transacao);
            AdicionarInteiro(atualizarSenha, "@id", id);
            atualizarSenha.Parameters.Add("@senhaHash", SqlDbType.VarChar, 200).Value = senhaHash;
            if (await atualizarSenha.ExecuteNonQueryAsync() != 1) return false;
        }

        if (solicitacaoId.HasValue)
        {
            const string concluirSql = "UPDATE dbo.SolicitacoesRecuperacaoSenha SET status='Aprovada' WHERE solicitacao_id=@solicitacaoId AND pessoa_id=@id AND status='Pendente'";
            await using var concluir = new SqlCommand(concluirSql, conexao, transacao);
            AdicionarInteiro(concluir, "@solicitacaoId", solicitacaoId.Value);
            AdicionarInteiro(concluir, "@id", id);
            if (await concluir.ExecuteNonQueryAsync() != 1) return false;
        }

        await transacao.CommitAsync();
        return true;
    }

    public async Task<List<Categoria>> ListarCategoriasAsync()
    {
        var lista = new List<Categoria>();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        await using var comando = new SqlCommand("SELECT codigo,nome FROM Categorias ORDER BY nome", conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        while (await leitor.ReadAsync()) lista.Add(new Categoria { Codigo = leitor.GetInt32(0), Nome = leitor.GetString(1) });
        return lista;
    }

    // INSERÇÃO de produto: INSERT parametrizado e OUTPUT do código gerado.
    public async Task<int> CadastrarProdutoAsync(Produto produto)
    {
        if (string.IsNullOrWhiteSpace(produto.CodigoBarras) || string.IsNullOrWhiteSpace(produto.Nome) || produto.PrecoUnit < 0 || produto.Estoque < 0 || produto.CategoriaCodigo <= 0 || (produto.Cor?.Trim().Length ?? 0) > 60)
            throw new InvalidOperationException("Informe código de barras, nome, cor (até 60 caracteres), preço, quantidade e categoria válidos.");
        produto.Cor = string.IsNullOrWhiteSpace(produto.Cor) ? "Não informada" : produto.Cor.Trim();
        await using var conexao = CriarConexao();
        await conexao.OpenAsync();
        const string sql = "INSERT INTO Produtos(codigo_barras,nome,cor,preco_unit,estoque,categoria_codigo) OUTPUT INSERTED.codigo VALUES(@codigoBarras,@nome,@cor,@precoUnitario,@estoque,@categoriaCodigo)";
        await using var comando = new SqlCommand(sql, conexao);
        comando.Parameters.Add("@codigoBarras", SqlDbType.VarChar, 50).Value = produto.CodigoBarras.Trim();
        comando.Parameters.Add("@nome", SqlDbType.VarChar, 150).Value = produto.Nome.Trim();
        comando.Parameters.Add("@cor", SqlDbType.VarChar, 60).Value = produto.Cor;
        AdicionarValorMonetario(comando, "@precoUnitario", produto.PrecoUnit);
        AdicionarInteiro(comando, "@estoque", produto.Estoque);
        AdicionarInteiro(comando, "@categoriaCodigo", produto.CategoriaCodigo);
        try
        {
            return Convert.ToInt32(await comando.ExecuteScalarAsync());
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("Já existe um produto com esse código de barras.", ex);
        }
    }
}
