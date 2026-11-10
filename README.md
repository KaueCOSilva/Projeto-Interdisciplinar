# SIVAD

<p align="center">
  <img src="assets/dashboard-preview.png" alt="Prévia do painel administrativo do SIVAD" width="100%">
</p>

<p align="center">
  Sistema web de gestão comercial para pedidos, compras, estoque, usuários e relatórios.
</p>

## Sobre o projeto

O SIVAD organiza operações de vendas e administração em uma aplicação web. Administradores gerenciam funcionários, produtos, compras de estoque e devoluções. Funcionários registram pedidos, consultam a disponibilidade de produtos e finalizam pedidos pelo sistema.

Os totais dos pedidos são calculados no servidor com base nos preços armazenados no banco de dados e nas quantidades informadas. A aplicação não realiza pagamentos: o pagamento é tratado fora do sistema.

## Funcionalidades

### Autenticação e usuários

- Login de administradores e funcionários por UID numérico e senha.
- Sessão com controle de acesso por perfil.
- Cadastro, edição e desativação de funcionários por administrador.
- Solicitação de recuperação de senha para análise administrativa.
- Senhas armazenadas como hash com salt; não são persistidas em texto simples.

### Pedidos

- Inclusão de produtos por seleção ou por código de barras.
- Consulta de disponibilidade de estoque durante o registro do pedido.
- Cálculo do total no servidor usando os preços vigentes no banco e as quantidades.
- Resumo do pedido antes da finalização.
- Baixa do estoque ao finalizar o pedido.
- Reposição de estoque ao cancelar um pedido finalizado.

### Produtos e estoque

- Cadastro e edição de produtos, incluindo código de barras.
- Consulta de quantidades disponíveis.
- Registro de compras de estoque com fornecedor, produtos e quantidades.
- Registro de devoluções e atualização do estoque.
- Alertas para produtos com estoque baixo.

### Dashboard e relatórios

- Indicadores e gráficos de vendas e compras.
- Relatórios diários, mensais e anuais.
- Consulta de devoluções e fluxo de caixa.
- Central de notificações com alertas de estoque e solicitações pendentes de recuperação de senha.

## Tecnologias

- C# e .NET 10
- ASP.NET Core MVC e Razor Views
- SQL Server
- `Microsoft.Data.SqlClient` para acesso ao banco de dados
- HTML, CSS e JavaScript
- Bootstrap 5 e Bootstrap Icons
- Google Charts para gráficos
- Padrões de arquitetura MVC e Repository
- Padrão Strategy para organização de cálculos

## Guia de execução local

### Pré-requisitos

- .NET 10 SDK instalado (`dotnet --version` deve indicar uma versão 10.x).
- Uma instância do SQL Server ativa e acessível pela máquina que executará a aplicação.
- SQL Server Management Studio (SSMS), Azure Data Studio ou outro cliente que execute scripts T-SQL e reconheça o separador `GO`.
- Git, caso o projeto seja clonado do repositório.

### 1. Obter o projeto

Clone o repositório e entre na pasta que contém `SIVAD.sln` e `SIVAD.csproj`:

```bash
git clone <URL_DO_REPOSITORIO>
cd Projeto-Interdisciplinar
```

Se recebeu o projeto em um arquivo ZIP, extraia-o e abra um terminal nessa mesma pasta.

### 2. Preparar o banco de dados

Conecte o cliente SQL à instância de destino. Para uma instalação nova, abra e execute os arquivos abaixo, **um de cada vez e nesta ordem**:

1. `Database/Banco.sql` — cria o banco `SQLVendas2026` e o schema inicial. Execute somente em uma instalação nova, pois as tabelas não são recriadas de forma idempotente.
2. `Database/AtualizarBanco.sql` — aplica os campos e tabelas complementares necessários pela versão atual do sistema.
3. `Database/Insercoes.sql` — opcional; carrega registros demonstrativos (produtos, funcionários, pedidos e compras). Pode ser executado depois do schema e antes das views.
4. `Database/Views.sql` — cria as views SQL consultadas pelos repositórios da aplicação. É necessário para o funcionamento normal.
5. `Database/Procedures.sql` — instala as procedures documentadas no projeto. A aplicação MVC realiza suas operações pelo Repository e não depende dessas procedures para iniciar.

O `Banco.sql` e os demais scripts usam o catálogo `SQLVendas2026`. Se o banco já existe, **não execute novamente `Banco.sql`**; faça backup e aplique apenas a migração adequada ao estado desse banco. Não execute scripts de demonstração em um banco com dados reais.

### 3. Configurar a conexão

A aplicação lê a string `ConnectionStrings:DefaultConnection`. Configure-a para apontar para a sua instância SQL Server, usando credenciais válidas e mantendo o catálogo `SQLVendas2026` quando executar os scripts sem alterações. O projeto contém um valor de exemplo em `appsettings.json`; não use uma senha de exemplo em um ambiente real e não grave credenciais reais em commits.

É possível sobrescrever a configuração com a variável de ambiente `ConnectionStrings__DefaultConnection`. Exemplo no PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=SQLVendas2026;User Id=SEU_USUARIO;Password=SUA_SENHA;TrustServerCertificate=True;"
```

Exemplo no Bash (Linux/macOS):

```bash
export ConnectionStrings__DefaultConnection='Server=localhost,1433;Database=SQLVendas2026;User Id=SEU_USUARIO;Password=SUA_SENHA;TrustServerCertificate=True;'
```

Troque servidor, porta, usuário e senha conforme sua instalação. Se usar autenticação integrada do Windows ou uma instância nomeada, ajuste a string para o formato aceito pelo seu SQL Server e pelo ambiente em que a aplicação será executada. Não publique a string com senha.

### 4. Restaurar, compilar e iniciar

Na pasta do projeto, execute:

```bash
dotnet restore
dotnet build
dotnet run --launch-profile http
```

O perfil `http` está configurado para abrir `http://localhost:5129`. Também há um perfil HTTPS em `https://localhost:7095`; em desenvolvimento local, o certificado HTTPS pode precisar ser confiado com `dotnet dev-certs https --trust` (quando suportado pelo sistema operacional).

Para encerrar a aplicação, pressione `Ctrl+C` no terminal. O SQL Server deve continuar ativo enquanto a aplicação estiver em uso.

### 5. Primeiro acesso

Há duas opções para preparar as credenciais:

- **Configuração manual do primeiro administrador:** defina a chave inicial em `Autenticacao__ChaveConfiguracaoInicial` e use o link de configuração apresentado na tela de Login quando ainda não houver administrador configurado. Exemplo PowerShell: `$env:Autenticacao__ChaveConfiguracaoInicial = "UMA_CHAVE_LOCAL_FORTE"`. Bash: `export Autenticacao__ChaveConfiguracaoInicial='UMA_CHAVE_LOCAL_FORTE'`. Essa chave é uma proteção da configuração inicial; não a compartilhe nem a use como senha de login.
- **Demonstração local:** depois de executar `Insercoes.sql`, execute `Database/RedefinirCredenciaisDemonstracao.sql`. O script mostra os UIDs no resultado. As credenciais documentadas nele são `admin1@example.invalid` / senha `Sivad@2026!` para o administrador e `funcionario1@example.invalid` / senha `Funcionario@2026!` para o funcionário; o login da aplicação usa o UID numérico retornado pelo script, não o e-mail. Use esses dados apenas em banco local de demonstração e nunca em produção.

### Solução rápida de problemas

- **Erro de conexão/SQL:** confirme que o serviço SQL Server está ativo, que servidor e porta correspondem à string de conexão, e que o usuário tem acesso ao banco `SQLVendas2026`.
- **Erro de tabela ou coluna ausente:** em um banco novo, confirme a ordem de execução de `Banco.sql` e `AtualizarBanco.sql`. Faça backup antes de aplicar uma migração em um banco existente.
- **Erro de view SQL inexistente:** execute `Database/Views.sql` no catálogo correto.
- **Login não funciona:** confirme que há credencial ativa e use o UID numérico, não o e-mail. Para dados demonstrativos, confira o resultado final do script de redefinição.
- **Comandos `dotnet` não reconhecidos:** instale o .NET 10 SDK e abra um terminal novo.

## Arquitetura

A aplicação segue a separação MVC:

- **Models**: entidades e modelos usados pelas telas.
- **Views**: páginas Razor apresentadas ao usuário.
- **Controllers**: recebem as solicitações, aplicam o fluxo da aplicação e retornam as telas ou redirecionamentos.
- **Repositories**: concentram as consultas e operações SQL.

A interface `IRepositorioSIVAD` separa os controladores da implementação SQL. `DataConnection` centraliza a criação de conexões com o SQL Server. A pasta `Strategies` contém estratégias de cálculo para pedidos e compras.

## Estrutura do projeto

```text
SIVAD/
├── Controllers/       # Controladores MVC
├── Database/          # Scripts de criação, atualização e objetos SQL
├── Filtros/           # Filtros de sessão e autorização
├── Infrastructure/    # Componentes de infraestrutura da aplicação
├── Models/            # Entidades e ViewModels
├── Repositories/      # Interface e implementação de acesso a dados
├── Seguranca/         # Recursos relacionados à proteção de senhas
├── Strategies/        # Estratégias de cálculo
├── Views/             # Telas Razor
├── wwwroot/           # CSS e JavaScript
├── assets/            # Imagens do projeto
├── SIVAD.csproj
└── SIVAD.sln
