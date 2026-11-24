# FinTrack API

API REST para uma aplicação de gestão financeira pessoal, desenvolvida com **C# e ASP.NET Core 10**. O backend concentra as regras de negócio e a persistência de contas, categorias, transações e resumos financeiros.

O projeto foi estruturado para demonstrar práticas de desenvolvimento backend: separação de responsabilidades, autenticação, autorização por usuário, validação de entrada, acesso a dados com Entity Framework Core e testes automatizados.

> **Status:** projeto em desenvolvimento. O escopo implementado e os próximos passos estão indicados neste README.

**Frontend relacionado:** [FinTrack Web](https://github.com/Padokazzz/fintrack-web)

## Funcionalidades

- Cadastro e autenticação de usuários com JWT.
- Operações de criação, consulta, atualização e remoção de contas financeiras.
- Gerenciamento de categorias de receitas e despesas.
- Registro e consulta de transações.
- Filtros de transações por mês, ano, tipo e categoria.
- Resumo mensal de receitas, despesas e saldo.
- Documentação interativa dos endpoints com Swagger / OpenAPI.

Os dados financeiros são associados ao usuário autenticado. O identificador do usuário deve ser obtido a partir das claims de autenticação, e não confiado ao conteúdo enviado pelo cliente.

## Tecnologias

| Área | Tecnologias |
| --- | --- |
| Linguagem e plataforma | C# · .NET 10 · ASP.NET Core |
| Persistência | Entity Framework Core · PostgreSQL |
| Autenticação | JWT Bearer |
| Validação | FluentValidation |
| Documentação | Swagger · OpenAPI |
| Testes | xUnit · Moq · FluentAssertions |
| Ambiente | Docker · Docker Compose |

## Arquitetura

A solução utiliza uma organização inspirada em Clean Architecture, mantendo regras de negócio e contratos separados dos detalhes de infraestrutura.

```text
FinTrack.Api
FinTrack.Application
FinTrack.Domain
FinTrack.Infrastructure
FinTrack.Tests
```

| Projeto | Responsabilidade |
| --- | --- |
| `FinTrack.Domain` | Entidades, enums e conceitos centrais do domínio. |
| `FinTrack.Application` | Casos de uso, DTOs, interfaces e validadores. |
| `FinTrack.Infrastructure` | Persistência, Entity Framework Core, repositórios e implementações técnicas. |
| `FinTrack.Api` | Endpoints HTTP, middlewares, Swagger e composição da aplicação. |
| `FinTrack.Tests` | Testes automatizados das regras e componentes da aplicação. |

Visão simplificada das dependências:

```text
Api ───────────────> Application ─────> Domain
 │                       ▲
 └────> Infrastructure ──┘
                 │
                 └───────────────────> Domain
```

A camada de domínio não depende dos projetos externos.

## Estrutura do repositório

```text
src/
├── FinTrack.Api/
│   ├── Controllers/
│   ├── Extensions/
│   ├── Middlewares/
│   └── Program.cs
├── FinTrack.Application/
│   ├── Common/
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Services/
│   └── Validators/
├── FinTrack.Domain/
│   ├── Common/
│   ├── Entities/
│   └── Enums/
└── FinTrack.Infrastructure/
    ├── Authentication/
    ├── Data/
    ├── Extensions/
    └── Repositories/
tests/
└── FinTrack.Tests/
```

## Executar localmente

### Pré-requisitos

- .NET SDK 10 ou superior.
- PostgreSQL local ou Docker.
- Git.

Clone o projeto e restaure as dependências:

```bash
git clone https://github.com/Padokazzz/FinTrack.git
cd FinTrack
dotnet restore
```

Configure a conexão com o banco e as opções JWT em `src/FinTrack.Api/appsettings.Development.json`. Você pode partir do arquivo de exemplo, quando disponível:

```bash
cp src/FinTrack.Api/appsettings.Development.example.json src/FinTrack.Api/appsettings.Development.json
```

Exemplo de configuração:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fintrack_db;Username=YOUR_USER;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Issuer": "FinTrack.Api",
    "Audience": "FinTrack.Client",
    "Secret": "USE_A_LONG_RANDOM_SECRET_OF_AT_LEAST_32_CHARACTERS",
    "ExpirationMinutes": 60
  }
}
```

Não versione credenciais ou segredos reais. Use variáveis de ambiente ou um gerenciador de segredos em ambientes compartilhados e de produção.

Inicie a API:

```bash
dotnet run --project src/FinTrack.Api/FinTrack.Api.csproj
```

A URL local e a porta são exibidas no terminal. A documentação Swagger fica disponível em `/swagger`.

## Executar com Docker

Na raiz do repositório:

```bash
cp .env.example .env
docker compose up --build
```

A configuração Docker disponibiliza a API e o PostgreSQL. No ambiente descrito pelo projeto, a documentação fica em:

```text
http://localhost:8080/swagger
```

Para encerrar:

```bash
docker compose down
```

Para remover também os volumes persistidos:

```bash
docker compose down -v
```

## Testes

Execute a suíte automatizada:

```bash
dotnet test
```

Ou informe o projeto de testes explicitamente:

```bash
dotnet test tests/FinTrack.Tests/FinTrack.Tests.csproj
```

A suíte atual contempla componentes como serviços da aplicação, validadores e regras de cálculo. Testes de integração e pipeline de CI permanecem como evoluções planejadas.

## Principais endpoints

| Recurso | Método e rota |
| --- | --- |
| Cadastro | `POST /api/auth/register` |
| Login | `POST /api/auth/login` |
| Contas | `GET, POST, PUT, DELETE /api/accounts` |
| Categorias | `GET, POST, PUT, DELETE /api/categories` |
| Transações | `GET, POST, PUT, DELETE /api/transactions` |
| Resumo mensal | `GET /api/summaries/monthly?month={month}&year={year}` |

As rotas de consulta, atualização e remoção de recursos individuais também recebem seus respectivos identificadores. Consulte o Swagger para os contratos completos de request e response.

## Segurança considerada no projeto

- Autenticação por JWT para endpoints protegidos.
- Associação dos registros financeiros ao usuário autenticado.
- Validação de entrada antes da execução dos casos de uso.
- Segredos e credenciais mantidos fora do controle de versão.
- Separação entre regras de negócio e detalhes de infraestrutura.

## Próximos passos

- [ ] Configurar CI com GitHub Actions.
- [ ] Ampliar a cobertura com testes de integração.
- [ ] Implementar refresh tokens.
- [ ] Adicionar paginação às listagens.
- [ ] Evoluir relatórios e exportação de dados.
- [ ] Preparar implantação em ambiente cloud.

## Repositórios relacionados

- [FinTrack Web](https://github.com/Padokazzz/fintrack-web): interface React que consome esta API.

---

Desenvolvido por [Leonardo Padilha](https://www.leonardopadilha.com) como projeto de portfólio com foco em engenharia backend e desenvolvimento de APIs.
