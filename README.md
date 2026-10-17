# SIVAD

Aplicação acadêmica em **ASP.NET Core MVC + Repository + SQL Server**. O acesso a dados está concentrado em `Repositories/SqlServerRepository.cs`, usando `Microsoft.Data.SqlClient`. As views e estilos originais foram preservados; os indicadores e gráficos são alimentados por consultas ao banco.

## Padrão de arquitetura

Seguindo a separação do projeto de referência: `Program.cs` registra `ISIVADRepository` para `SqlServerRepository`; os controllers dependem apenas da interface; `SqlServerRepository` implementa as operações SQL; e `DataConnection` lê `DefaultConnection` de `appsettings.json` e cria conexões sob demanda. Diferente do exemplo do professor, cada operação abre e fecha sua própria conexão e a implementação é registrada como `Scoped`, em vez de manter uma conexão aberta compartilhada como `Singleton`.

## 1. Preparar o SQL Server

O SQL Server precisa estar iniciado e acessível pelo computador que executa o site, normalmente em `127.0.0.1:1433`. Se vocês usam um contêiner SQL Server no Docker do sistema operacional, mantenham o mapeamento da porta do banco para `1433` no computador. **O projeto web não usa Docker, Compose ou `.env`.**

Conectem ao servidor pelo SQL Client.

- **Banco novo:** executem `Database/Banco.sql` uma vez. Ele cria `SQLVendas2026` e as tabelas, além de incluir um administrador e um funcionário fictícios para demonstração. Esses registros não são contas de login.
- **Banco existente:** abram `Database/AtualizarBanco.sql` no SQL Client conectado ao servidor de vocês e executem. O script adiciona `data_pedido`, flags `ativo`, `fornecedor_nome` e as colunas `cor` de produto/itens se estiverem ausentes; também permite `fornecedor_id` e `Pedidos.cliente_id` nulos. Inclui os registros demonstrativos se ainda não existirem. É uma migração aditiva; não apaga nem substitui registros. O site MVC não altera o schema automaticamente.

Antes de executar a migração, selecione no SQL Client o mesmo banco definido em `ConnectionStrings:DefaultConnection`. O script mostra o `banco_alvo`, recusa bancos sem as tabelas-base do SIVAD e, ao final, retorna o banco e as colunas verificadas. O campo textual `fornecedor_nome` é o nome digitado do **fornecedor**; não existe `funcionario_nome`: o nome do funcionário é armazenado em `Pessoas.nome` e ligado por `Funcionarios.pessoa_id`.

Os scripts criam um administrador e um funcionário fictícios, mas não criam contas nem senhas. Também garantem a categoria **Vestuário** para testes; nas demais categorias e produtos, cadastrem o que for necessário antes de registrar operações. Fornecedores são informados por texto no lançamento de compra. O sistema não solicita nem processa CPF de cliente ao registrar pedidos, que são salvos sem associação a cliente.

## 2. Configurar e executar no VS Code

1. Abra a pasta do projeto que contém `SIVAD.csproj`.
2. Confira `ConnectionStrings:DefaultConnection` em `appsettings.json`. Ajustem servidor, banco, usuário e senha para a instância SQL de vocês. Para o site executado no computador, usem `127.0.0.1,1433` se essa for a porta publicada pelo SQL Server. Não compartilhem nem versionem credenciais reais; substituam qualquer senha de demonstração antes de distribuir o projeto.
3. No terminal do VS Code, executem:

```bash
dotnet restore
dotnet run
```

4. Abram a URL local mostrada pelo `dotnet run` (por exemplo, `http://localhost:5129`).

Se o VS Code mostrar erros em cascata como `Microsoft.AspNetCore` ou `Controller` não encontrados, abra a pasta raiz que contém `SIVAD.csproj` (não apenas `Controllers`) e confirme que o SDK .NET 10 está instalado (`dotnet --list-sdks`). No terminal dessa pasta, execute `dotnet restore SIVAD.csproj` e `dotnet build SIVAD.csproj -c Release`. Depois, use **Developer: Reload Window** no VS Code para recarregar o serviço de linguagem C#.

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

A aplicação não apaga registros antigos automaticamente. Se a base já continha dados de teste de versões anteriores, eles continuam nela até serem removidos deliberadamente pelo SQL Client. Os scripts atuais inserem os registros fictícios de administrador e funcionário e garantem a categoria **Vestuário**; não inserem amostras de clientes ou produtos.

## 4. Funcionalidades conectadas

### Funcionário — pedidos

1. Produtos disponíveis, preços e funcionários são consultados no SQL Server. O pedido não solicita nem grava CPF ou associação com cliente.
2. A entrada manual do produto (seleção do produto do banco + quantidade) e a leitura/digitação do código de barras são controles separados; ambos adicionam ao mesmo pedido e respeitam o estoque disponível.
3. O Repository relê os preços no banco e grava pedido e itens em transação; o resumo carrega os itens pelo código salvo.
4. Ao clicar em **Contabilizar Compra**, a aplicação valida o estoque, baixa as quantidades e atualiza o status na mesma transação. A venda passa a compor os gráficos depois de finalizada.

### Administrador — estoque, funcionários e compras

1. Estoque, categorias, administradores e compras recentes vêm do banco. O fornecedor é texto livre e fica salvo na compra; na tabela de estoque aparece o fornecedor da compra recebida mais recente daquele produto.
2. Cadastro/edição de produtos persistem cor, código, preço e quantidade no SQL Server. Cada linha de compra também recebe uma cor, salva no histórico do item e atualizada no produto do estoque. Remover desativa o produto, preservando referências históricas.
3. Cadastro, edição do nome e remoção de funcionários persistem no SQL Server. Remover desativa o funcionário e preserva pedidos antigos.
4. Remover item ou clicar em **Cancelar Compra** no formulário descarta apenas o rascunho. Todas as compras já gravadas aparecem no painel **Histórico de Compras** e podem ser editadas ou canceladas. A edição aplica ao estoque a diferença entre as quantidades antigas e novas e atualiza data/fornecedor/total; o cancelamento retira do estoque as quantidades daquela compra e muda o status para cancelado, tudo em transação. Se o saldo atual não comportar a retirada, a operação é recusada sem alterar os dados.

### Dashboard e relatórios

O Dashboard e os gráficos Google Charts consultam o banco: o gráfico mensal mantém vendas e compras recebidas do ano corrente, e um gráfico anual separado exibe compras recebidas de **todos os anos** (inclusive datas históricas como 2005). Atividades recentes combinam pedidos e compras recebidas. Edição de compra atualiza o total/data exibidos nos gráficos e relatórios; cancelamento reverte as unidades e exclui esse movimento dos totais (o registro permanece marcado como cancelado para auditoria). A tela de relatórios agrupa os movimentos lidos do SQL Server; **Fluxo de Caixa** considera compras de estoque como saídas. O badge e a central de notificações usam produtos ativos com estoque igual ou inferior a 10 unidades, então a edição/cancelamento recalcula também os alertas. O schema não tem tabela de devoluções, por isso essa aba não inventa lançamentos.

## 5. Limites do schema atual

Não há tabela de credenciais; portanto, login por senha, recuperação/alteração de senha e autenticação real não podem ser persistidos pelo schema fornecido. O código não implementa hash de senha porque não existe fluxo de autenticação com senha armazenada. Os registros demonstrativos de administrador e funcionário são apenas entidades de negócio, não usuários autenticáveis. O cadastro de funcionário grava em `Pessoas`/`Funcionarios`, mas não cria uma conta de acesso. A coluna `Clientes.cpf` permanece no schema para compatibilidade com dados existentes, mas o fluxo atual de pedidos não a solicita, consulta nem grava. Também não há tabela de devoluções.

## 6. Testes realizados

O projeto foi compilado em Release após as alterações (**0 avisos e 0 erros**); a formatação C# foi verificada, os scripts JavaScript externos e o script embutido do Dashboard passaram na verificação de sintaxe, e a página inicial respondeu HTTP 200. **Não foi possível executar os scripts SQL nem testes de integração com SQL Server nesta sandbox**, pois não há instância acessível em `127.0.0.1:1433`. Para validar leitura/gravação, executem `Database/AtualizarBanco.sql` no SQL Client, iniciem o site com a conexão de vocês, registrem uma compra e testem editar/cancelar uma compra recebida. Depois confiram estoque, gráfico anual do Dashboard e **Fluxo de Caixa** nos relatórios. Se aparecer `Invalid column name 'data_pedido'` ou `fornecedor_nome`, a migração não foi executada na base que o site está usando.
