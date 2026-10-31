# Guia de login, sessão e recuperação de senha

## O que foi implementado

O fluxo segue a ideia do `poo-todo`: o repositório consulta as credenciais, o login bem-sucedido grava o ID e o nome na sessão e a área privada verifica a sessão antes de continuar. No SIVAD, o UID é o valor de `Pessoas.id`; o perfil é `Administrador` ou `Funcionario`. A sessão expira após 60 minutos sem atividade e o cookie é `HttpOnly`.

As senhas não são armazenadas em texto simples como no exemplo de aula. `GerenciadorSenhas` usa PBKDF2-SHA256, salt aleatório por senha e comparação em tempo constante. O acesso ao banco permanece em `RepositorioSqlServer`, usando SQL parametrizado e `Microsoft.Data.SqlClient`; Entity Framework não é usado.

## Etapa 1 — aplicar o schema de autenticação

No SQL Client, conecte ao banco que a aplicação usa e execute `Database/AtualizarBanco.sql`. O script é idempotente e adiciona `CredenciaisAcesso` e `SolicitacoesRecuperacaoSenha`, sem apagar dados existentes. Em banco novo, execute primeiro `Database/Banco.sql` e depois `Database/AtualizarBanco.sql`.

A conta fica ligada a `Pessoas.id`, guarda perfil, e-mail, hash e estado ativo. A solicitação de recuperação fica ligada à conta e tem estado `Pendente`, `Aprovada` ou `Rejeitada`. A carga demonstrativa do projeto fornece credenciais de apresentação com senhas conhecidas apenas para teste; as senhas continuam armazenadas somente como hash.

## Etapa 2 — preparar o primeiro administrador

A configuração inicial exige uma chave local, para que a existência de um registro demonstrativo não permita a qualquer visitante escolher a primeira senha. No terminal do VS Code, dentro do projeto, defina a chave com User Secrets (não crie `.env` nem grave a chave no Git):

```bash
dotnet user-secrets init --project SIVAD.csproj
dotnet user-secrets set "Autenticacao:ChaveConfiguracaoInicial" "<uma-chave-local-longa>" --project SIVAD.csproj
```

O site deve estar em ambiente `Development` para carregar User Secrets. Consulte os dados já cadastrados para identificar o administrador que será configurado:

```sql
SELECT a.pessoa_id AS UID, p.nome, a.cpf, a.email
FROM dbo.Administradores a
INNER JOIN dbo.Pessoas p ON p.id = a.pessoa_id;
```

Com a migração aplicada e o site iniciado, o link **Configurar o primeiro administrador** aparece na tela de login enquanto ainda não houver credencial de administrador. Ele leva à rota `/Autenticacao/ConfigurarPrimeiroAdministrador`. Informe UID, CPF e e-mail exatamente como constam no banco, a chave local e uma senha nova de pelo menos 8 caracteres. A senha é gravada como hash. Depois de configurar a primeira conta de administrador, o link some do login; a verificação no repositório também impede criar uma segunda conta administrativa inicial. Remova a chave dos User Secrets (`dotnet user-secrets remove "Autenticacao:ChaveConfiguracaoInicial" --project SIVAD.csproj`).

Esse cadastro inicial usa uma transação `Serializable` para impedir que duas configurações simultâneas passem pela verificação de “primeiro administrador”. É a única operação de autenticação com esse nível de isolamento; os demais fluxos usam transações simples apenas para manter juntas gravações que formam uma única operação lógica.

## Etapa 3 — cadastrar e provisionar funcionários

No cadastro de funcionário, informe nome, e-mail cadastrado e senha inicial (mínimo de 8 caracteres). O sistema grava Pessoa, Funcionário e credencial na mesma transação. O UID do novo funcionário é o ID que o banco atribuir à Pessoa; esse ID aparece na mensagem de sucesso e no gerenciamento de funcionários.

Para um funcionário que já existia antes da migração, o administrador pode abrir **Funcionários → Gerenciamento de Funcionários → Acesso**, informar o e-mail e definir a senha inicial. O funcionário então entra com o ID de Pessoa exibido na lista e essa senha.

## Etapa 4 — entrar e sair

Na tela de login, informe o ID numérico de Pessoa no campo UID e a senha. O perfil determina o destino: administrador abre o painel e funcionário abre o registro de pedidos. Rotas administrativas e de funcionário verificam sessão e perfil; os atalhos existentes não concedem acesso sem login. O botão **Sair** limpa a sessão.

## Etapa 5 — pedir e aprovar uma recuperação

Na tela **Esqueci minha senha**, informe nome cadastrado, UID e o e-mail já associado à credencial. Se os dados corresponderem a um funcionário ativo, o pedido é gravado como pendente. A mensagem é propositalmente genérica para não revelar se o UID existe.

O pedido aparece na central **Notificações**, junto aos alertas de estoque. **Rejeitar** muda o estado para `Rejeitada` e o pedido desaparece da lista. **Aprovar** encaminha para **Funcionários → Gerenciamento de Funcionários**, com o funcionário e seu ID carregados; o ID continua somente leitura. O administrador digita a nova senha (mínimo de 8 caracteres) no campo de senha e clica **Salvar alterações**. A atualização do nome/senha e a mudança do pedido para `Aprovada` acontecem na mesma transação. A solicitação continua pendente até a gravação terminar com sucesso.

Como o projeto não tem serviço SMTP e a pessoa usuária confirmou que não deseja envio automático, o administrador deve informar a senha aprovada ao funcionário por um canal externo seguro. Não há menu nem página separada de solicitações.

## Etapa 6 — verificação

Execute `dotnet build SIVAD.sln -c Release`. O teste de tela deve conferir `/Autenticacao/Login`, `/Autenticacao/RecuperacaoSenha` e `/Autenticacao/ConfigurarPrimeiroAdministrador`; uma requisição sem sessão para `/Administrador/Dashboard` ou `/Funcionario/RegistrarPedido` deve voltar ao login. O teste do SQL Server deve ser feito no computador que executa o banco: esta sandbox não consegue abrir a instância SQL configurada, portanto o build e o teste HTTP não substituem a validação da migração e das gravações no SQL Client.

Se a configuração mostrar uma falha SQL, rode `Database/DiagnosticoAutenticacao.sql` no SQL Client conectado ao mesmo catálogo de `ConnectionStrings:DefaultConnection`. `banco_atual` precisa corresponder ao banco da aplicação, e as tabelas/colunas verificadas não podem ser `NULL`. O script também conta quantas credenciais administrativas já existem. Se você tentou configurar usando uma versão anterior e essa contagem for maior que zero, o cadastro pode ter sido gravado antes da mensagem de erro; tente entrar com o UID e a senha que escolheu antes de repetir a configuração.

## Etapa 7 — roteiro para testar a recuperação

1. Use um funcionário ativo e informe na tela de recuperação o nome, o UID e o e-mail que estão cadastrados para ele. A resposta é genérica para não revelar se a conta existe.
2. Entre como administrador e abra **Notificações**. Confirme que o card mostra nome e UID do funcionário, com **Aprovar** e **Rejeitar**.
3. Para testar a rejeição, clique **Rejeitar** e confirme que o card sai da lista. O funcionário pode enviar um novo pedido.
4. Envie outro pedido e clique **Aprovar**. Confirme o redirecionamento para o gerenciamento com nome e ID carregados, o ID somente leitura e o campo de nova senha obrigatório.
5. Digite uma senha com pelo menos oito caracteres e clique **Salvar alterações**. A tela deve confirmar a conclusão; a solicitação não deve mais aparecer em **Notificações**. Teste o login do funcionário com a senha nova.
6. Para validar uma falha de gravação, não conclua o salvamento (por exemplo, deixe a senha vazia se desabilitar a validação do navegador): o sistema deve manter a solicitação pendente e não trocar a senha. Nome, senha e status `Aprovada` são gravados na mesma transação.

O build e os testes HTTP deste ambiente não têm acesso à instância SQL Server do computador do usuário. Portanto, o roteiro acima precisa ser executado contra o banco real para confirmar a persistência e o login após a troca.

Na versão atual, quando o site está em `Development`, a página mostra o número e o estado do erro SQL; a exceção completa fica na saída do terminal. Esses dados ajudam a identificar conexão, permissão ou schema. Não compartilhe a connection string, senha, chave local, CPF ou e-mail ao enviar o diagnóstico.
