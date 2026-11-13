# AgroConecta

API da Global Solution 2026/2 de **Advanced Business Development with .NET**, no tema CitizenTech. A AgroConecta ajuda pequenos produtores e técnicos de cooperativas a decidir quais talhões inspecionar. A API cruza observações da vegetação, temperatura, cobertura de nuvens e focos de calor para sinalizar risco de seca e incêndio.

## Integrantes

| Nome | RM |
|---|---|
| Felipe Wiclif Leal da Silva | 563901 |
| Deryk de Souza Queiroz | 563412 |
| Lucas Gonçalves Viana | 563254 |
| Vinicius Paschoeto da Silva | 563089 |

## Monitoramento dos talhões

O produtor cadastra a fazenda e seus talhões. Ao receber uma observação orbital, a API calcula o risco e, quando necessário, registra um alerta com recomendação de inspeção. Para demonstrar o fluxo, envie uma observação com `source` igual a `SIMULATED`. Os dados orbitais e as leituras de sensores entram pela API.

Os dados agrícolas são compartilhados dentro de uma cooperativa. `PRODUCER` opera os cadastros comuns, `READER` consulta e `ADMIN` também administra contas. O cadastro público cria contas `PRODUCER`. O campo `UserId` vincula o produtor à fazenda para fins cadastrais.

## Requisitos e implementação

| Critério .NET 2026/2 | Implementação |
|---|---|
| Minimal API, validação e Swagger | `Endpoints/`, DTOs, `ValidationFilter` e Swagger/OpenAPI |
| JWT e autorização | Cadastro/login, senha com hash, perfis e políticas `Write`/`Admin` |
| Idempotência | `Idempotency-Key` obrigatório nos POSTs de negócio; respostas persistidas no mesmo commit da operação |
| Rate limiting | Fixed window por usuário/IP e limite adicional no login; HTTP 429 e `Retry-After` |
| xUnit | Testes de regras e integração HTTP com banco SQLite relacional |
| HealthChecks | `/health`, `/health/ready` com conectividade ao banco e `/health/live` |

## Execução rápida com SQLite

Pré-requisito: SDK .NET 8 ou SDK posterior capaz de compilar net8.0, com runtime ASP.NET Core 8 instalado. Restauração exige acesso ao NuGet.

Na raiz do projeto, com PowerShell 7, execute o script abaixo.

```powershell
./scripts/run-local.ps1
```

O script gera uma chave JWT temporária, usa o arquivo local `agroconecta.db` e abre a API em `http://localhost:5080`. O banco SQLite é criado com `EnsureCreated`; não recebe migrations de PostgreSQL. Não alterne um mesmo arquivo SQLite entre esquemas de versões diferentes.

1. Acesse `http://localhost:5080/swagger`.
2. Faça `POST /api/auth/register` com nome, e-mail e senha de ao menos 10 caracteres.
3. Faça login em `/api/auth/login` e copie `accessToken`.
4. Clique em **Authorize** no Swagger e informe o token.
5. Execute o CRUD e informe um `Idempotency-Key` diferente para cada POST novo.

Para criar um administrador local, execute `./scripts/run-local.ps1 -SeedAdmin`; será solicitada uma senha sem exibição na tela. A chave gerada muda ao reiniciar o script, invalidando os tokens antigos; em um servidor configure um segredo estável por variável de ambiente.

## Execução com PostgreSQL e Docker

Pré-requisitos: Docker Engine/Desktop em execução e Compose.

```powershell
./scripts/initialize-environment.ps1
docker compose up -d --build
```

O primeiro comando cria `.env` com segredos aleatórios; ele não é versionado. As credenciais do administrador ficam nesse arquivo local. Acesse `http://localhost:8080/swagger`. O PostgreSQL fica apenas na rede interna do Compose, e a API executa as migrations existentes na inicialização.

Para usar PostgreSQL sem Docker, configure `ConnectionStrings__DefaultConnection` no formato Npgsql (`Host=...;Database=...;Username=...;Password=...`) e `Jwt__Key` com ao menos 32 bytes. `DATABASE_URL`, se fornecida, também deve ser uma connection string Npgsql, não uma URI `postgres://`. `Jwt__Issuer` e `Jwt__Audience` podem sobrescrever os valores do appsettings.

## Testes e compilação

```powershell
dotnet restore AgroConecta.sln
dotnet build AgroConecta.sln --no-restore
dotnet test AgroConecta.sln --no-build --logger "trx;LogFileName=tests.trx"
dotnet list AgroConecta.sln package --vulnerable --include-transitive
```

Os testes criam bancos SQLite temporários e não precisam de Docker, chave JWT real ou contas externas. Veja a evidência e os limites em [verificação](docs/verificacao.md).

## Contrato e integração Mobile

Todas as rotas de negócio exigem `Authorization: Bearer <token>`. Cadastro/login são públicos e limitados. JWT dura 60 minutos e valida assinatura, emissor, público e expiração. O cliente deve limpar sua sessão no logout; não há refresh token nem revogação imediata de JWT.

| Método | Rota | Uso |
|---|---|---|
| POST | `/api/auth/register`, `/api/auth/login` | Cadastro e autenticação |
| GET | `/api/auth/me` | Conta autenticada |
| GET, POST | `/api/users` | Administração de contas; somente ADMIN |
| GET | `/api/users/{id}` | Conta por ID; somente ADMIN |
| GET, POST | `/api/farms`, `/api/crop-areas` | Listagem e criação |
| GET, PUT, DELETE | `/api/farms/{id}`, `/api/crop-areas/{id}` | CRUD completo |
| GET, POST | `/api/sensors`, `/api/sensor-readings`, `/api/satellite-data` | Monitoramento |
| GET, POST | `/api/climate-alerts`, `/api/recommendations` | Alertas e orientações |
| GET | `/{recurso}/{id}` | Consulta dos recursos acima, com prefixo `/api` |
| PUT | `/api/climate-alerts/{id}/resolve` | Resolver alerta |
| GET | `/api/dashboard` | Resumo |
| GET | `/health`, `/health/ready`, `/health/live` | Monitoramento sem JWT |

As listas são arrays JSON. Status e severidades são strings como `CRITICAL` e `OPEN`. O POST administrativo `/api/users` exige `password`, armazenado como hash e omitido das respostas.

Os retornos são 200 para consulta, 201 com `Location` para criação, 204 para alteração ou exclusão, 400 para dados inválidos, 401 para autenticação inválida, 403 para permissão insuficiente, 404 para recurso ausente, 409 para conflito e 429 para limite de requisições. Fazendas com talhões e talhões com registros retornam 409 na tentativa de exclusão.

### Repetir requisições com segurança

Crie uma chave por ação do usuário, por exemplo um UUID, e conserve a mesma chave **e o mesmo JSON** em cada tentativa de retransmissão. Os POSTs de negócio exigem esse cabeçalho; cadastro/login não o exigem. A chave é vinculada à conta e rota. Uma operação concluída é reproduzida com o mesmo corpo, status e `Location`; o cabeçalho `Idempotency-Replayed: true` identifica a repetição. Um conteúdo diferente com a mesma chave retorna 409. Requisições rejeitadas não consomem a chave.

No PostgreSQL, uma transação e um advisory lock serializam a mesma chave entre instâncias, e o registro da resposta é confirmado junto com as alterações agrícolas. Os registros são mantidos sem expiração automática nesta entrega; planeje retenção e exclusão segura antes de operar em escala. SQLite é uma alternativa de demonstração e testes, sem a garantia de serialização distribuída do PostgreSQL. Os limites de requisições são mantidos por instância; implantações com várias réplicas precisam também de um limite no gateway.

### Exemplo de fluxo

Envie o JSON abaixo para `POST /api/farms` com os cabeçalhos `Authorization` e `Idempotency-Key`.

```json
{
  "name": "Fazenda Esperança",
  "ownerName": "Cooperativa local",
  "city": "Itu",
  "state": "SP",
  "totalArea": 20,
  "userId": null
}
```

Use o ID retornado para criar um talhão e enviar uma observação. O cadastro de dado orbital atualiza o status e pode gerar alerta e recomendação na mesma transação. Uma captura antiga entra no histórico sem alterar o status atual. [Exemplos de requisições](docs/exemplos.http)

## Organização e migrations

`Endpoints` recebe HTTP; `DTOs` descreve entradas e saídas; `Services` aplica as regras de risco e emite JWT; `Data` mapeia o EF; `Domain` preserva o domínio; `Middlewares` trata falhas e idempotência; `Infrastructure` contém validação, documentação e health check. Ver [arquitetura](docs/arquitetura.md).

As migrations incluídas criam o esquema e acrescentam credenciais e idempotência. Para gerar o SQL, configure a conexão PostgreSQL e `Jwt__Key` antes de executar os comandos.

```powershell
dotnet tool restore
dotnet ef migrations script --idempotent --project src/AgroConecta.Api --output scripts/migrations-postgres.sql
```

Antes de atualizar um banco antigo, faça backup, revise o SQL e trate e-mails repetidos/normalização. O índice único bloqueia duplicatas; o projeto não apaga contas para forçar a migração. Contas antigas sem senha precisam ser ativadas; uma conta de administrador existente sem hash pode ser ativada explicitamente por `Seed__AdminEmail`/`Seed__AdminPassword` na primeira execução. Essas variáveis não redefinem uma senha já cadastrada.

## Entrega

- [Identificação do grupo](IDENTIFICACAO.txt)
- [Conferência dos critérios](docs/checklist-entrega.md)
- [Roteiro da apresentação](docs/roteiro-video-dotnet.md)
- [Repositório do grupo](https://github.com/Wiclif06/GS-DotNet)
