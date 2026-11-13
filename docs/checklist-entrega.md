# Critérios de .NET da Global Solution 2026/2

| Critério do enunciado | Evidência no projeto |
|---|---|
| Minimal API, validação e Swagger (20 pts) | Rotas `/api`, validação de entrada, códigos HTTP e contrato OpenAPI extraído da aplicação. |
| JWT e controle de acesso (20 pts) | Cadastro, login, validação do token e permissões de `ADMIN`, `PRODUCER` e `READER`. |
| Idempotência (15 pts) | `Idempotency-Key` nos POSTs de negócio, replay da resposta persistida e 409 quando o conteúdo muda. Concorrência testada em duas instâncias com PostgreSQL. |
| Rate limiting (15 pts) | Limites por usuário ou IP e limite no login. Teste do retorno 429 com `Retry-After`. |
| xUnit (20 pts) | 28 testes aprovados em Release. A suíte cobre regras de risco, rotas e permissões. |
| HealthChecks (10 pts) | `/health/live` para o processo e `/health/ready` para o banco. A queda do PostgreSQL foi exercitada. |

Os valores entre parênteses indicam o peso dos critérios no enunciado. O backend CitizenTech atende pequenos produtores e técnicos de cooperativas. A identificação dos quatro integrantes e o link do GitHub constam em [IDENTIFICACAO.txt](../IDENTIFICACAO.txt).

A versão AgroConecta precisa ser publicada no repositório do grupo antes do envio. O teste com o app Mobile depende do projeto do aplicativo. O [roteiro](roteiro-video-dotnet.md) reúne as etapas da apresentação.
