# Roteiro de apresentação — AgroConecta 2026/2

1. Apresente os integrantes e o cidadão beneficiado: pequeno produtor e técnicos de cooperativas.
2. Explique como o alerta ajuda o produtor a escolher qual talhão inspecionar primeiro.
3. Mostre `AgroConecta.sln`, a separação Endpoints/DTOs/Services/Data/Domain/Middlewares e a preservação do banco relacional.
4. Abra o Swagger. Consulte fazendas sem token e mostre 401. Faça cadastro/login e use Authorize.
5. Crie uma fazenda com Idempotency-Key, consulte o Location, atualize-a e demonstre o CRUD.
6. Repita um POST com mesma chave e corpo. Comprove o mesmo ID e Idempotency-Replayed. Altere o corpo com a mesma chave para obter 409.
7. Crie um talhão e envie dado orbital SIMULATED de risco. Mostre status, alerta, recomendação e dashboard. Resolva o alerta.
8. Demonstre que READER consulta, mas não altera, e que contas administrativas ficam restritas a ADMIN.
9. Mostre os testes xUnit e a verificação de concorrência com duas instâncias da API no PostgreSQL.
10. Mostre `/health/live` e `/health/ready`, depois a resposta 429 em ambiente de demonstração com um limite reduzido.
11. Explique que a demonstração usa observações simuladas e que as contas acessam os dados de uma cooperativa compartilhada.

Perguntas de preparação: por que Minimal API? Como ocorre a validação no .NET 8? O que torna a criação atômica? Como o advisory lock impede concorrência no PostgreSQL? Por que liveness e readiness são diferentes? Que teste comprova a regra de negócio, e qual comprova o HTTP? O que precisaria mudar para várias cooperativas?
