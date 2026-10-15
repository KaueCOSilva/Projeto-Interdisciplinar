# SIVAD

Aplicação acadêmica em **ASP.NET Core MVC + Repository + SQL Server**. O acesso a dados está concentrado em `Repositories/SqlServerRepository.cs`, usando `Microsoft.Data.SqlClient`. As views e estilos originais foram preservados; os indicadores e gráficos são alimentados por consultas ao banco.

## Padrão de arquitetura

Seguindo a separação do projeto de referência: `Program.cs` registra `ISIVADRepository` para `SqlServerRepository`; os controllers dependem apenas da interface; `SqlServerRepository` implementa as operações SQL; e `DataConnection` lê `DefaultConnection` de `appsettings.json` e cria conexões sob demanda. Diferente do exemplo do professor, cada operação abre e fecha sua própria conexão e a implementação é registrada como `Scoped`, em vez de manter uma conexão aberta compartilhada como `Singleton`.

## 1. Preparar o SQL Server

O SQL Server precisa estar iniciado e acessível pelo computador que executa o site, normalmente em `127.0.0.1:1433`. Se vocês usam um contêiner SQL Server no Docker do sistema operacional, mantenham o mapeamento da porta do banco para `1433` no computador. **O projeto web não usa Docker, Compose ou `.env`.**

Conectem ao servidor pelo SQL Client.

- **Banco novo:** executem `Database/Banco.sql` uma vez. Ele cria `SQLVendas2026` e as tabelas sem inserir dados de demonstração.
- **Banco existente:** abram `Database/AtualizarBanco.sql` no SQL Client conectado ao servidor de vocês e executem. O script adiciona `data_pedido`, flags `ativo`, `fornecedor_nome` e as colunas `cor` de produto/itens se estiverem ausentes; também permite `fornecedor_id` nulo. É uma migração aditiva; não apaga nem substitui registros. O site MVC não altera o schema automaticamente.

Antes de executar a migração, selecione no SQL Client o mesmo banco definido em `ConnectionStrings:DefaultConnection`. O script mostra o `banco_alvo`, recusa bancos sem as tabelas-base do SIVAD e, ao final, retorna o banco e as colunas verificadas. O campo textual `fornecedor_nome` é o nome digitado do **fornecedor**; não existe `funcionario_nome`: o nome do funcionário é armazenado em `Pessoas.nome` e ligado por `Funcionarios.pessoa_id`.

O banco novo começa sem registros. Cadastrem pelo SQL Client as categorias, clientes e administradores necessários; fornecedores são informados por texto no lançamento de compra. Funcionários e produtos também podem ser cadastrados nas telas correspondentes.

## 2. Configurar e executar no VS Code

1. Abra a pasta do projeto que contém `SIVAD.csproj`.
2. Confira `ConnectionStrings:DefaultConnection` em `appsettings.json`. Ajustem servidor, banco, usuário e senha para a instância SQL de vocês. Para o site executado no computador, usem `127.0.0.1,1433` se essa for a porta publicada pelo SQL Server.
3. No terminal do VS Code, executem:

```bash
dotnet restore
dotnet run
```

4. Abram a URL local mostrada pelo `dotnet run` (por exemplo, `http://localhost:5129`).

Antes de iniciar o site com um banco existente, confirmem que a migração SQL foi executada. O dashboard falha com `Invalid column name 'data_pedido'` enquanto essa coluna não existir.

Se o log mostrar `Invalid column name 'fornecedor_nome'` ou `Invalid column name 'cor'`, a migração não foi aplicada ao mesmo banco que o site está usando. No SQL Client, execute `SELECT DB_NAME() AS banco_atual, COL_LENGTH('dbo.CompraEstoque', 'fornecedor_nome') AS fornecedor_nome, COL_LENGTH('dbo.Produtos', 'cor') AS produto_cor, COL_LENGTH('dbo.Itens_CompraEstoque', 'cor') AS item_compra_cor;`. `banco_atual` deve corresponder ao `Database` da connection string e as três colunas devem ser diferentes de `NULL`. Se não forem, selecione a base correta e execute `Database/AtualizarBanco.sql` inteiro. A lista de produtos usada pelo funcionário e pelo estoque precisa das colunas de fornecedor e cor.

Para confirmar no SQL Client, executem após a migração:

```sql
SELECT COL_LENGTH('dbo.Pedidos', 'data_pedido') AS data_pedido,
       COL_LENGTH('dbo.Produtos', 'ativo') AS produto_ativo,
       COL_LENGTH('dbo.Funcionarios', 'ativo') AS funcionario_ativo,
       COL_LENGTH('dbo.CompraEstoque', 'fornecedor_nome') AS fornecedor_nome,
       COL_LENGTH('dbo.Produtos', 'cor') AS produto_cor,
       COL_LENGTH('dbo.Itens_CompraEstoque', 'cor') AS item_compra_cor;
```

Os seis resultados devem ser diferentes de `NULL`. Pedidos antigos sem data recebem a data/hora da execução como valor padrão; para datas históricas exatas, preencham `data_pedido` no SQL Client com as datas conhecidas.

## 3. Confirmar o banco em uso

Em ambiente `Development`, abram `/diagnostico/banco` na mesma origem do site. A resposta mostra o nome do servidor, o banco conectado e contagens de produtos, funcionários ativos, pedidos e compras, sem mostrar a senha. Confiram que o banco e os registros são os esperados.

A aplicação não apaga registros antigos automaticamente. Se a base já continha dados de teste de versões anteriores, eles continuam nela até serem removidos deliberadamente pelo SQL Client. A versão atual do script para banco novo não insere mais amostras.

## 4. Funcionalidades conectadas

### Funcionário — pedidos

1. Produtos disponíveis, preços e funcionários são consultados no SQL Server. O funcionário digita o CPF do cliente; a aplicação localiza o cliente cadastrado no banco.
2. O CPF do cliente é digitado manualmente. A entrada manual do produto (seleção do produto do banco + quantidade) e a leitura/digitação do código de barras são controles separados; ambos adicionam ao mesmo pedido e respeitam o estoque disponível.
3. O Repository relê os preços no banco e grava pedido e itens em transação; o resumo carrega os itens pelo código salvo.
4. Ao clicar em **Contabilizar Compra**, a aplicação valida o estoque, baixa as quantidades e atualiza o status na mesma transação. A venda passa a compor os gráficos depois de finalizada.

### Administrador — estoque, funcionários e compras

1. Estoque, categorias, administradores e compras recentes vêm do banco. O fornecedor é texto livre e fica salvo na compra; na tabela de estoque aparece o fornecedor da compra recebida mais recente daquele produto.
2. Cadastro/edição de produtos persistem cor, código, preço e quantidade no SQL Server. Cada linha de compra também recebe uma cor, salva no histórico do item e atualizada no produto do estoque. Remover desativa o produto, preservando referências históricas.
3. Cadastro, edição do nome e remoção de funcionários persistem no SQL Server. Remover desativa o funcionário e preserva pedidos antigos.
4. Os botões de remover item e cancelar compra operam sobre o formulário de compra; registrar compra atualiza estoque em transação.

### Dashboard e relatórios

O dashboard e os gráficos Google Charts usam agregações consultadas no banco: vendas finalizadas e compras de estoque do ano corrente; atividades recentes combinam pedidos e compras recebidas. A tela de relatórios agrupa todos os movimentos lidos do SQL Server; **Fluxo de Caixa** considera compras de estoque como saídas. O badge e a central de notificações usam produtos ativos com estoque igual ou inferior a 10 unidades. O schema não tem tabela de devoluções, por isso essa aba não inventa lançamentos.

## 5. Limites do schema atual

Não há tabela de credenciais; portanto, login por senha, recuperação/alteração de senha e autenticação real não podem ser persistidos pelo schema fornecido. O cadastro de funcionário grava em `Pessoas`/`Funcionarios`, mas não cria uma conta de acesso. Também não há tabela de devoluções.

## 6. Testes realizados

O projeto foi compilado em Release; o formulário MVC de login respondeu por HTTP, os arquivos estáticos alterados responderam `200`, os scripts JavaScript passaram no `node --check` e os comandos SQL foram analisados estaticamente. **Não foi possível executar testes de integração com SQL Server nesta sandbox**, pois não há instância acessível em `127.0.0.1:1433`. Para validar leitura/gravação, executem `Database/AtualizarBanco.sql` no SQL Client, iniciem o site com a conexão de vocês e façam um pedido de teste até clicar em **Contabilizar Compra**; depois confiram dashboard e relatório. Se aparecer `Invalid column name 'data_pedido'` ou `fornecedor_nome`, a migração não foi executada na base que o site está usando.
