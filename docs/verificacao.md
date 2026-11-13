# Verificação da AgroConecta

Em 12/11/2026, a solução compilou em Release e os 28 testes xUnit passaram. A suíte usa SQLite para testar as regras de risco e o fluxo HTTP, incluindo JWT, perfis, CRUD, idempotência e o retorno 429.

A API publicada também foi executada com PostgreSQL 16.15 em um banco novo. A inicialização aplicou as migrations. O teste HTTP cadastrou um produtor, autenticou a conta, criou uma fazenda e um talhão, enviou uma observação orbital e conferiu o alerta e o dashboard.

Oito POSTs simultâneos com a mesma chave foram enviados a duas instâncias da API. O banco gravou uma fazenda e os sete reenvios retornaram o mesmo ID. Uma requisição com a mesma chave e outro corpo recebeu 409.

Com o PostgreSQL desligado, `/health/ready` respondeu 503 e `/health/live` permaneceu em 200. O arquivo [openapi.json](openapi.json) foi extraído da API em execução.

O arquivo do Docker Compose foi validado separadamente. A execução em containers e o teste com o aplicativo Mobile ainda estão pendentes.
