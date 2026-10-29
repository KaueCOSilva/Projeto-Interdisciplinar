# Guia para explicar os repositórios do SIVAD

Este guia acompanha a organização atual do projeto e pode ser usado como roteiro de apresentação. O acesso ao banco continua no padrão MVC + Repositório, com SQL explícito em C# e `Microsoft.Data.SqlClient`.

## 1. Caminho de uma operação

1. O usuário envia uma ação pela tela.
2. O controlador recebe a solicitação e chama um método da interface `IRepositorioSIVAD`.
3. `Program.cs` configura a implementação concreta `RepositorioSqlServer` para essa interface.
4. O método do repositório prepara o comando SQL, envia os parâmetros e converte o resultado em modelos do projeto.
5. O controlador entrega esses modelos para a tela, ou retorna uma confirmação da gravação.

`DataConnection` lê a chave `ConnectionStrings:DefaultConnection` do `appsettings.json` e fornece o método `CriarConexao()`. Cada operação abre e descarta a própria conexão. Os arquivos `RepositorioSqlServer.*.cs` são partes (`partial`) da mesma classe: a divisão é apenas organizacional, não cria repositories adicionais nem muda como o sistema funciona.

## 2. Estrutura básica de consulta

O padrão de leitura usado nos métodos é o mesmo ensinado no projeto de referência:

```csharp
const string sql = "SELECT codigo, nome FROM Categorias ORDER BY nome";
var categorias = new List<Categoria>();

await using var conexao = CriarConexao();
await conexao.OpenAsync();
await using var comando = new SqlCommand(sql, conexao);
await using var leitor = await comando.ExecuteReaderAsync();

while (await leitor.ReadAsync())
{
    categorias.Add(new Categoria
    {
        Codigo = leitor.GetInt32(0),
        Nome = leitor.GetString(1)
    });
}

return categorias;
```

`SELECT` pede dados ao SQL Server. `ExecuteReaderAsync()` permite percorrer as linhas retornadas; cada linha é transformada em um objeto e adicionada à lista. Ao final, a lista volta ao controlador.

## 3. Para que servem os principais comandos

- **`SELECT`**: consulta dados. O código usa `ExecuteReaderAsync()` quando pode haver várias colunas ou linhas; para um único resultado, como o código recém-gerado, usa `ExecuteScalarAsync()`.
- **`INSERT`**: inclui um registro. `OUTPUT INSERTED.codigo` ou `OUTPUT INSERTED.numero` pede ao SQL Server o identificador que acabou de gerar.
- **`UPDATE`**: altera um registro existente, por exemplo, preço, nome, quantidade em estoque ou status.
- **`DELETE`**: apaga linhas. O sistema evita apagar produtos e funcionários que podem aparecer em registros históricos; nesses casos usa exclusão lógica com `UPDATE ... SET ativo = 0`.
- **`ExecuteNonQueryAsync()`**: executa `INSERT`, `UPDATE` ou `DELETE` quando não é necessário ler um valor de retorno. O resultado informa quantas linhas foram afetadas.

### Parâmetros

Valores do usuário são enviados como parâmetros, por exemplo `@codigo`, `@nome` e `@preco`, adicionados ao `SqlCommand`. Isso evita montar SQL concatenando texto e trata os valores com seus tipos. `AdicionarInteiro` e `AdicionarValorMonetario` são pequenos auxiliares para os parâmetros numéricos, não uma camada extra de acesso a dados.

## 4. Como o padrão aparece nas telas

- **Catálogo e listas** (`RepositorioSqlServer.Consultas.cs`): `SELECT` consulta produtos, funcionários, fornecedores e compras; o leitor converte as linhas em modelos.
- **Painel e relatórios** (`RepositorioSqlServer.Painel.cs`): `SELECT`, `SUM`, `COUNT`, `GROUP BY` e `JOIN` calculam totais e agrupam vendas/compras para os gráficos.
- **Pedidos** (`RepositorioSqlServer.Pedidos.cs`): `INSERT` grava cabeçalho e itens; `SELECT` recupera um pedido; `UPDATE` muda o status e o estoque.
- **Compras de estoque** (`RepositorioSqlServer.ComprasEstoque.cs`): `INSERT` grava a compra e seus itens; `UPDATE` ajusta o estoque; a edição e o cancelamento preservam o histórico.
- **Cadastros** (`RepositorioSqlServer.Cadastros.cs`): `INSERT` cadastra produto/funcionário, `UPDATE` edita dados ou desativa registros, e uma devolução registra o movimento e aumenta o estoque.
- **Diagnóstico** (`RepositorioSqlServer.Diagnostico.cs`): somente `SELECT`; mostra qual servidor e banco estão conectados, sem alterar registros.

## 5. Por que algumas gravações usam transação

Uma transação agrupa comandos que formam uma única operação de negócio. Se um deles falhar, o banco não deve ficar com somente metade da operação gravada.

**Venda:** o repositório confere o preço e o saldo no banco, grava o pedido e depois os itens. Ao contabilizar, atualiza o estoque e o status do pedido. Esses passos pertencem à mesma operação: não se deve salvar o pedido sem seus itens nem marcar a venda sem baixar o estoque.

**Compra de estoque:** grava cabeçalho e itens, atualiza quantidade/cor dos produtos e confirma a transação. Na edição, aplica somente a diferença entre as quantidades antigas e novas; no cancelamento, reverte o estoque e marca a compra como cancelada. Isso impede que uma edição ou cancelamento deixe o estoque inconsistente ou apague o histórico.

Essas etapas são mais longas que um CRUD de uma tabela, mas são necessárias porque uma venda ou compra altera mais de uma tabela e o estoque. Removê-las para encurtar o arquivo poderia permitir gravações incompletas.

## 6. Resumo para apresentar

> “O controlador não conversa diretamente com o SQL Server. Ele chama a interface `IRepositorioSIVAD`, que é implementada por `RepositorioSqlServer`. Os métodos usam `SqlCommand` com parâmetros. Para ler, usamos `SELECT` e `SqlDataReader`; para gravar, `INSERT`; para alterar, `UPDATE`; e usamos `OUTPUT INSERTED` quando precisamos do identificador gerado. Operações de estoque que gravam em várias tabelas usam transação para manter os dados consistentes. Os arquivos separados são partes da mesma classe, organizadas para facilitar a leitura.”

## 7. O que esta etapa não muda

Não há Entity Framework, camada genérica ou procedimentos armazenados nesta etapa. O SQL continua visível no repositório, como no material de referência. A interface, os controladores, as telas, os estilos e as regras de negócio foram preservados; a organização dos arquivos e os nomes internos em português ajudam a leitura. A criação de procedures pode ser feita depois, como etapa separada e testada.


## 8. Relação com o material da disciplina

O ZIP da matéria traz exemplos de `SELECT`, filtros, `INSERT`, `UPDATE`, `DELETE`, parâmetros, agregações, `JOIN`, views, funções, procedures, triggers e transações. O SIVAD usa diretamente a base já estudada:

| Conteúdo da aula | Onde aparece no SIVAD |
|---|---|
| `SELECT`, `WHERE`, `ORDER BY`, `JOIN` | Consultas de cadastro, pedidos, estoque, painel e relatórios. |
| `INSERT`, `UPDATE`, `DELETE` | Cadastros e movimentos de estoque. A remoção de produtos/funcionários é lógica (`UPDATE ativo = 0`) para preservar históricos. |
| `SUM`, `COUNT`, `GROUP BY` | Totais e agrupamentos dos gráficos e relatórios, como nos exercícios de Views e funções do tipo tabela. |
| Parâmetros `@nome` em procedures | Parâmetros `@...` dos `SqlCommand`: o programa fornece os valores separadamente do texto SQL. |
| `BEGIN TRAN`, `COMMIT`, `ROLLBACK` e propriedades ACID | `BeginTransactionAsync`, `CommitAsync` e `RollbackAsync` do `SqlTransaction`; a chamada C# controla a mesma transação do SQL Server. |
| Níveis de isolamento, inclusive `SERIALIZABLE` | `IsolationLevel.Serializable` protege a edição/cancelamento de compras que consultam e alteram mais de uma tabela. |

O programa também precisa de recursos de integração C# que não são comandos SQL da matéria: `SqlConnection`, `SqlCommand`, `SqlDataReader`, parâmetros tipados e os métodos `ExecuteReaderAsync`, `ExecuteScalarAsync` e `ExecuteNonQueryAsync`. Eles são a forma de enviar os comandos estudados e receber o resultado no MVC.

## 9. Comandos adicionais que precisam de uma explicação curta

- **`OUTPUT INSERTED`** devolve, no mesmo `INSERT`, o código que o SQL Server acabou de gerar. A matéria demonstra `@@IDENTITY`; o `OUTPUT` deixa explícito de qual tabela veio o código e continua previsível se um trigger inserir registros auxiliares no futuro. É uma exceção intencional para manter os relacionamentos de cabeçalho e itens corretos.
- **`UPDLOCK`** reserva para atualização a linha de status que acabou de ser consultada. Evita que duas requisições finalizem ou cancelem o mesmo pedido com base no mesmo status antigo. A dica `ROWLOCK` foi retirada: ela controlava o tamanho físico do bloqueio, não a regra de negócio, e não era necessária.
- **`HOLDLOCK`** mantém os bloqueios até o fim da transação nas edições/cancelamentos de compra; a aula também mostra essa família de dicas de bloqueio. Junto de `SERIALIZABLE`, protege a leitura dos itens enquanto o estoque é ajustado.
- **`COALESCE`**, `CASE` e `OUTPUT` são expressões do SQL Server visíveis nas consultas. Podem ser explicadas como valor padrão, condição e retorno do registro inserido.

As transações e as verificações de estoque não foram removidas para encurtar o código. Uma venda ou compra grava mais de uma tabela; sem a transação, um erro entre os comandos poderia deixar o cabeçalho, os itens e o saldo divergentes. O princípio é o mesmo de atomicidade discutido no PDF de transações.

## 10. Etapas recomendadas para Views e Procedures

Neste momento, não criamos views, funções, procedures ou triggers no banco: o pedido indica que essa migração será uma etapa posterior. O SQL permanece explícito no repositório, que permite acompanhar cada comando enquanto vocês aprendem o fluxo.

Quando a turma estiver pronta, a mudança pode ser feita sem uma reescrita geral:

1. Criar primeiro uma **view somente de leitura** para um relatório, conferir os mesmos dados no SQL Server e na tela e comparar os resultados.
2. Migrar uma **consulta do repositório** para essa view, mantendo a interface MVC e os filtros/parâmetros necessários.
3. Escolher uma gravação pequena e criar uma **procedure** com parâmetros; testar valores válidos e inválidos antes de trocar a chamada C#.
4. Migrar as gravações transacionais maiores uma por vez, testando `COMMIT`, `ROLLBACK`, chaves estrangeiras e o saldo de estoque.
5. Avaliar **triggers** somente depois de decidir se a baixa/entrada de estoque pertence ao repositório ou ao banco. Hoje o repositório já atualiza o estoque; adicionar trigger com a mesma regra agora executaria a movimentação duas vezes.

Em cada etapa, manter uma só implementação ativa da regra, aplicar o script de banco, compilar o projeto e comparar a tela com consultas `SELECT` feitas diretamente no SQL Server. Assim o código continua explicável e cada mudança tem uma verificação objetiva.


## 11. Simplificações aplicadas com base no material enviado

- A busca do último fornecedor de um produto usava `OUTER APPLY`. Foi substituída por uma subconsulta correlacionada `SELECT TOP (1)` com `INNER JOIN` e `ORDER BY`, conceitos mais próximos das consultas e subconsultas das aulas. Quando não há compra, `COALESCE` mantém o texto vazio usado pela tela.
- O gráfico de vendas/compras mensais usava uma CTE e uma tabela de meses construída no SQL. Agora o SQL faz dois `SELECT` com `GROUP BY`, reunidos por `UNION ALL`, que retorna os totais de cada movimento. O C# cria os 12 meses e começa os totais em zero; por isso os meses sem registros continuam aparecendo no gráfico como zero, sem precisar da CTE.
- Foi retirada a dica `ROWLOCK`, que não alterava a regra de negócio. O `UPDLOCK` foi mantido no pedido porque protege contra duas finalizações concorrentes. A parte restante da gravação e as transações não foram reescritas.

Essas mudanças simplificam construções da consulta sem alterar os dados que alimentam as telas. Views, procedures e triggers seguem reservados para uma etapa posterior, com testes e sem duplicar as regras de estoque.


## 12. Comparação direta com o SistemaTarefas do professor

O SIVAD segue o mesmo caminho estrutural do exemplo: o controlador recebe uma **interface de repositório**; `Program.cs` registra uma **implementação concreta**; o repositório SQL **herda da classe-base de conexão**; e a implementação usa `Microsoft.Data.SqlClient` para enviar comandos ao SQL Server.

| SistemaTarefas do professor | SIVAD |
|---|---|
| `ITarefaRepository` | `IRepositorioSIVAD` |
| `TarefaSqlRepository : DataConnection, ITarefaRepository` | `RepositorioSqlServer : DataConnection, IRepositorioSIVAD` |
| `TarefaController` recebe `ITarefaRepository` no construtor | Os controladores, como `AdministradorController`, recebem `IRepositorioSIVAD` no construtor. |
| `Program.cs` registra a implementação da interface | `Program.cs` também registra interface e implementação; usa `AddScoped` por requisição web. |
| `Read`, `Create`, `Update`, `Delete` exemplificam CRUD de uma tabela | Os métodos têm nomes de domínio em português e cobrem cadastro, pedidos, compras e estoque. Uma venda não é um `INSERT` isolado: envolve cabeçalho, itens, status e saldo. |
| `TarefaSqlRepository.cs` contém os métodos de uma classe | `RepositorioSqlServer` continua sendo uma só classe, separada em arquivos `partial` para localizar com facilidade consultas, painel, pedidos, compras, cadastros e diagnóstico. |

Algumas escolhas do exemplo foram adaptadas de propósito, sem mudar o padrão ensinado. A cadeia de conexão do SIVAD fica em `appsettings.json`, em vez de fixa no código. `DataConnection` guarda a configuração e `CriarConexao()` abre uma conexão por operação; assim uma conexão não permanece aberta enquanto a aplicação web recebe várias requisições. A implementação é `Scoped`, e não uma instância compartilhada única. Além disso, as consultas selecionam as colunas necessárias e usam parâmetros tipados.

Essas diferenças não substituem MVC + Repository; elas adaptam o mesmo desenho ao SIVAD e ao ciclo de vida de uma aplicação web. Para a apresentação, descrevam primeiro o padrão como está no exemplo do professor e depois expliquem essas adaptações como cuidados com configuração, conexão e operações de negócio que gravam várias tabelas.
