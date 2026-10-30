# SIVAD

Para preparar a apresentação sobre a organização e os comandos SQL dos repositórios, consulte o [Guia de explicação dos repositórios](GUIA_EXPLICACAO_REPOSITORIOS.md).

Para configurar o primeiro administrador, o login com sessão e a recuperação de senha aprovada pelo administrador, consulte o [Guia de login, sessão e recuperação](GUIA_LOGIN_SESSAO_RECUPERACAO.md).

Aplicação acadêmica em **ASP.NET Core MVC + padrão Repositório + SQL Server**. O acesso a dados usa `Microsoft.Data.SqlClient` e está organizado por responsabilidade dentro de `Repositories/`. O CSS das telas existentes foi mantido; o login agora cria sessão real e a recuperação registra pedidos para análise administrativa.

## Padrão de arquitetura

Seguindo a separação do projeto de referência: `Program.cs` registra `IRepositorioSIVAD` com a implementação `RepositorioSqlServer`; os controladores dependem apenas da interface; o repositório executa as operações SQL; e `DataConnection` lê `DefaultConnection` de `appsettings.json` e cria conexões sob demanda. Cada operação abre e fecha sua própria conexão; a implementação é registrada com escopo `Scoped`. A sessão MVC é mantida por até 60 minutos de inatividade.

Para facilitar a manutenção sem mexer no comportamento que já está funcionando, `RepositorioSqlServer` é uma classe parcial (`partial`). A mesma classe e a mesma interface foram organizadas nestes arquivos, de acordo com cada área:

- `RepositorioSqlServer.Consultas.cs`: consultas de produtos, funcionários, administradores, fornecedores e compras.
- `RepositorioSqlServer.Painel.cs`: indicadores, gráficos, atividades, alertas e dados dos relatórios.
- `RepositorioSqlServer.Pedidos.cs`: gravação e consulta de pedidos, atualização de status e leitura de itens.
- `RepositorioSqlServer.ComprasEstoque.cs`: registro, edição, cancelamento e consulta de compras de estoque.
- `RepositorioSqlServer.Cadastros.cs`: operações de produtos, devoluções, funcionários e categorias.
- `RepositorioSqlServer.Diagnostico.cs`: consulta somente leitura para diagnóstico da conexão.
- `RepositorioSqlServer.Autenticacao.cs`: login, provisionamento de credenciais e solicitações de recuperação.
- `RepositorioSqlServer.cs`: construtor e métodos auxiliares SQL compartilhados.

As consultas de vendas, compras, gráficos e estoque permanecem organizadas nos mesmos arquivos. O fluxo de autenticação foi acrescentado em arquivo parcial próprio, com novos métodos parametrizados e tabelas de credenciais/recuperação; essas tabelas são instaladas pelo script de migração, não pelo Entity Framework.

## 1. Preparar o SQL Server

O SQL Server precisa estar iniciado e acessível pelo computador que executa o site, normalmente em `127.0.0.1:1433`. Se vocês usam um contêiner SQL Server no Docker do sistema operacional, mantenham o mapeamento da porta do banco para `1433` no computador. **O projeto web não usa Docker, Compose ou `.env`.**

Conectem ao servidor pelo SQL Client.

- **Banco novo:** executem `Database/Banco.sql` uma vez e, em seguida, `Database/AtualizarBanco.sql` no banco criado. O primeiro cria `SQLVendas2026` e as tabelas-base; o segundo adiciona as atualizações da aplicação, incluindo as tabelas de acesso.
- **Banco existente:** abram `Database/AtualizarBanco.sql` no SQL Client conectado ao servidor de vocês e executem. O script adiciona `data_pedido`, flags `ativo`, `fornecedor_nome`, as colunas `cor` de produto/itens, `Devolucoes`, `CredenciaisAcesso` e `SolicitacoesRecuperacaoSenha` se estiverem ausentes; também permite `fornecedor_id` e `Pedidos.cliente_id` nulos. Inclui registros demonstrativos se ainda não existirem. É uma migração aditiva; não apaga nem substitui registros. O site MVC não altera o schema automaticamente.

Antes de executar a migração, confirme que o catálogo `SQLVendas2026` corresponde ao `Initial Catalog` de `ConnectionStrings:DefaultConnection`; o script seleciona esse banco explicitamente. Ele mostra o `banco_alvo`, recusa bancos sem as tabelas-base do SIVAD e, ao final, retorna o banco e as colunas verificadas. O campo textual `fornecedor_nome` é o nome digitado do **fornecedor**; não existe `funcionario_nome`: o nome do funcionário é armazenado em `Pessoas.nome` e ligado por `Funcionarios.pessoa_id`.

Os scripts criam um administrador e um funcionário fictícios, mas não criam contas nem senhas padrão. A primeira credencial administrativa é configurada uma vez usando UID, CPF e e-mail já cadastrados e uma chave local. Também garantem a categoria **Vestuário** para testes; nas demais categorias e produtos, cadastrem o que for necessário antes de registrar operações. Fornecedores são informados por texto no lançamento de compra. O sistema não solicita nem processa CPF de cliente ao registrar pedidos, que são salvos sem associação a cliente.

## 2. Configurar e executar no VS Code

1. Abra a pasta que contém `SIVAD.sln` e `SIVAD.csproj`; abra `SIVAD.sln` no C# Dev Kit.
2. Confira `ConnectionStrings:DefaultConnection` em `appsettings.json`. Ajustem servidor, banco, usuário e senha para a instância SQL de vocês. Para o site executado no computador, usem `127.0.0.1,1433` se essa for a porta publicada pelo SQL Server. Não compartilhem nem versionem credenciais reais; substituam qualquer senha de demonstração antes de distribuir o projeto.
3. No terminal do VS Code, executem:

```bash
dotnet restore SIVAD.sln
dotnet build SIVAD.sln -c Release
dotnet run --project SIVAD.csproj
```

4. Abram a URL local mostrada pelo `dotnet run` (por exemplo, `http://localhost:5129`).

Se o VS Code mostrar erros em cascata como `Microsoft.AspNetCore`, `Controller` ou `WebApplication` não encontrados, confirme que o SDK .NET 10 está instalado (`dotnet --list-sdks`), que o C# Dev Kit abriu `SIVAD.sln` e que o `dotnet build SIVAD.sln -c Release` conclui. Depois, use **Developer: Reload Window** no VS Code para recarregar o serviço de linguagem C#; os diagnósticos do editor não substituem o resultado do build.

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
5. Depois da contabilização, o sistema volta ao formulário de registro com um modelo novo; pedidos contabilizados permanecem no histórico, mas não voltam a aparecer como resumos pendentes. A rota do resumo também impede reabrir um pedido concluído pelo botão Voltar do navegador.

### Administrador — estoque, funcionários e compras

1. Estoque, categorias, administradores e compras recentes vêm do banco. O fornecedor é texto livre e fica salvo na compra; na tabela de estoque aparece o fornecedor da compra recebida mais recente daquele produto.
2. Cadastro/edição de produtos persistem cor, código, preço e quantidade no SQL Server. Cada linha de compra também recebe uma cor, salva no histórico do item e atualizada no produto do estoque. Na tela de estoque, a lixeira abre o diálogo **Registrar devolução**; ela não exclui nem desativa o produto.
3. Cadastro, edição do nome e remoção de funcionários persistem no SQL Server. Remover desativa o funcionário e preserva pedidos antigos.
4. Remover item ou clicar em **Cancelar Compra** no formulário descarta apenas o rascunho. Todas as compras já gravadas aparecem no painel **Histórico de Compras** e podem ser editadas ou canceladas. A edição aplica ao estoque a diferença entre as quantidades antigas e novas e atualiza data/fornecedor/total; o cancelamento retira do estoque as quantidades daquela compra e muda o status para cancelado, tudo em transação. Se o saldo atual não comportar a retirada, a operação é recusada sem alterar os dados.

### Dashboard e relatórios

O Dashboard e os gráficos Google Charts consultam o banco: o gráfico mensal mantém vendas e compras recebidas do ano corrente, e um gráfico anual separado exibe compras recebidas de **todos os anos** (inclusive datas históricas como 2005). Atividades recentes combinam pedidos e compras recebidas. Edição de compra atualiza o total/data exibidos nos gráficos e relatórios; cancelamento reverte as unidades e exclui esse movimento dos totais (o registro permanece marcado como cancelado para auditoria). Ao registrar uma devolução, a operação guarda produto, quantidade, data e valor calculado pelo preço unitário vigente, além de somar as unidades ao estoque, em uma transação. A aba **Devoluções** consulta esses registros e mostra tanto o valor quanto o total de unidades devolvidas. **Fluxo de Caixa** considera compras de estoque e devoluções como saídas financeiras. O badge e a central de notificações usam produtos ativos com estoque igual ou inferior a 10 unidades, então as movimentações atualizam também os alertas.

## 5. Login, sessão e recuperação de senha

O UID de login é o ID de `Pessoas`. As credenciais ficam separadas dos dados de negócio; senhas são armazenadas como hash PBKDF2-SHA256 com salt aleatório, e não em texto simples. O login grava ID, nome e perfil na sessão; filtros impedem acesso direto a áreas sem sessão ou pelo perfil errado. O cadastro de funcionário cria as credenciais na mesma transação do registro da pessoa e do funcionário. Funcionários antigos podem receber acesso pela opção **Acesso** na tela de gerenciamento.

**Esqueci minha senha** grava uma solicitação pendente depois de conferir nome, UID e e-mail do funcionário. Ela aparece junto aos demais alertas em **Notificações**. **Aprovar** abre o funcionário no gerenciamento; a solicitação só é finalizada quando o administrador informa a nova senha e salva. **Rejeitar** encerra o pedido e o remove da lista. Não há envio automático de e-mail ou configuração SMTP. Para o primeiro administrador, use `/Autenticacao/ConfigurarPrimeiroAdministrador` diretamente; a opção não aparece mais na tela de login.

Se a configuração inicial falhar, execute `Database/DiagnosticoAutenticacao.sql` no SQL Client conectado ao mesmo banco da aplicação. O script é somente de leitura e não mostra dados pessoais.

A coluna `Clientes.cpf` permanece no schema por compatibilidade, mas o fluxo atual de pedidos não a solicita, consulta nem grava.

## 6. Testes realizados

O build Release passou após a implementação. Foram verificados por HTTP o login, a recuperação e a configuração inicial (resposta 200 e token antifalsificação presente); as áreas administrativa e de funcionário redirecionam para o login sem sessão. O teste isolado de `GerenciadorSenhas` aceitou a senha correta, rejeitou senha errada e hash malformado e confirmou salts diferentes. Os 16 arquivos CSS foram comparados byte a byte com o ZIP-base e não mudaram. Também foi confirmado que o projeto não contém Docker, Compose ou `.env`.

**Não foi possível testar gravação, login válido, migração nem recuperação ponta a ponta com SQL Server nesta sandbox**, pois não há instância acessível em `127.0.0.1:1433`. Para essa verificação, executem `Database/AtualizarBanco.sql` no computador conectado ao banco, configurem a chave local para o primeiro administrador e façam o roteiro descrito em `GUIA_LOGIN_SESSAO_RECUPERACAO.md`.
