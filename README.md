# Sabemi Tec — Webhooks de Pagamento (Backend)

Teste técnico: serviço que recebe notificações (webhooks) de pagamento de um banco
parceiro, garante idempotência, processa em background e expõe os dados para um
painel (frontend em repositório separado).

## Arquitetura

```
src/
  Sabemi.Webhooks.Model        # Entidades: EventoBrutoLog, StatusContrato
  Sabemi.Webhooks.Service      # Services + interfaces (contratos de Repository/Service)
  Sabemi.Webhooks.Repository   # EF Core (Postgres), implementação dos Repositories, fila de background
  Sabemi.Webhooks.Api          # Controllers, Program.cs, appsettings
```

Fluxo de dependência: `Api -> Service + Repository -> Model`.
`Service` não conhece EF Core nem detalhes de banco — só interfaces
(`IEventoBrutoLogRepository`, `IStatusContratoRepository`), implementadas em
`Repository` (que também concentra o `DbContext` e a fila de background, por
serem parte da camada de acesso a dados/infraestrutura). Isso é o que permite
trocar Postgres por outro banco, ou mockar os repositórios em teste, sem
tocar na regra de negócio.

## Segurança: ApiKey no header

Implementado: **ApiKey simples**, no header `X-Api-Key`.

- Banco parceiro e Sabemi combinam uma chave fixa (`Webhook:ApiKey`).
- O banco envia essa chave, sem alteração nenhuma, no header `X-Api-Key`.
- O backend (`ApiKeyValidator`, em `Service/Services`) compara o valor
  recebido com o configurado usando `CryptographicOperations.FixedTimeEquals`
  (comparação em tempo constante — evita timing attack). Não há hash, cálculo
  ou assinatura envolvidos: é literalmente a chave combinada, comparada como
  está.
- Requisições com API Key ausente/inválida retornam `401` **e são
  registradas mesmo assim** na tabela de log (com `StatusProcessamento =
  ErroValidacao`), para aparecerem no painel com alerta visual — é o
  comportamento pedido em "Visualização de Erros". A mensagem de erro
  diferencia os dois casos (`"Header X-Api-Key ausente."` vs. `"API Key
  inválida."`) pra facilitar o diagnóstico.

Ver `src/Sabemi.Webhooks.Service/Services/ApiKeyValidator.cs` e o uso em
`src/Sabemi.Webhooks.Api/Controllers/WebhooksController.cs`.

## Idempotência

`log_eventos_brutos.id_transacao` tem **índice único** no banco. O fluxo:

1. `WebhookPagamentoService.ReceberAsync` primeiro consulta se já existe um
   evento com aquele `id_transacao` — se sim, retorna sem reprocessar.
2. Se não existe, insere. Se duas requisições concorrentes chegarem ao mesmo
   tempo (corrida), a constraint única do banco rejeita a segunda inserção; o
   repositório converte esse erro em `IdTransacaoDuplicadaException`, que o
   serviço trata como "já processado" — sem 500, sem duplicar.

## Resiliência / processamento em background

O endpoint `POST /webhooks/pagamento` **não espera** o processamento pesado:

1. Valida assinatura, valida idempotência, persiste o evento com
   `StatusProcessamento = Pendente` e responde `202 Accepted` imediatamente.
2. O `Id` do evento é colocado em uma fila em memória
   (`System.Threading.Channels`, `ChannelBackgroundTaskQueue`).
3. Um `BackgroundService` (`PagamentoProcessingBackgroundService`) consome a
   fila continuamente e chama `PagamentoProcessingService.ProcessarAsync`, que
   simula a regra de negócio pesada com `Task.Delay(2s)` e depois atualiza a
   tabela `status_contrato`.

## Tabelas

- `log_eventos_brutos` — todo evento recebido, incluindo falhas de validação
  (raw payload + status do processamento + mensagem de erro).
- `status_contrato` — foto mais recente do contrato (1 linha por
  `id_contrato`, upsert a cada evento processado com sucesso).

## Banco de dados: Supabase ou Railway Postgres (sem instalar nada local)

O backend lê a connection string de `ConnectionStrings:Default`
(`appsettings.json`) ou, se ausente, da variável de ambiente `DATABASE_URL` no
formato URI padrão (`postgres://user:senha@host:porta/database`) — é
exatamente o formato que Supabase e Railway fornecem prontos.

**Opção recomendada (mais simples para deploy no Railway):** crie um serviço
PostgreSQL dentro do próprio projeto Railway — ele já injeta `DATABASE_URL`
automaticamente no serviço da API. Para rodar localmente, copie essa mesma
`DATABASE_URL` do painel do Railway (aba "Connect" → conexão pública/TCP
proxy, não a interna).

**Alternativa: Supabase** — crie um projeto gratuito, pegue a connection
string em *Project Settings → Database → Connection string (URI)* e exporte
como `DATABASE_URL`.

## Rodando localmente

```powershell
$env:DATABASE_URL = "postgres://usuario:senha@host:5432/postgres"
cd src/Sabemi.Webhooks.Api
dotnet run
```

Ou configure `DATABASE_URL` direto no profile de `dotnet run` em
`src/Sabemi.Webhooks.Api/Properties/launchSettings.json` (esse arquivo é
gitignorado por conter a senha em texto puro).

O `Program.cs` chama `EnsureCreatedAsync()` no startup — cria as tabelas
automaticamente na primeira execução, sem precisar rodar migrations à mão.

Com a API rodando em ambiente de desenvolvimento, o Swagger UI fica em
`http://localhost:5080/swagger` (via `Swashbuckle.AspNetCore`).

## Frontend

O dashboard (React + Vite) vive em repositório separado. Aponte
`VITE_API_URL` dele para a URL desta API (local: `http://localhost:5080`).

## Deploy no Railway

1. Suba este repositório como um serviço no Railway (mais um serviço
   Postgres, e o frontend como serviço/site estático separado).
2. No serviço da API, configure as variáveis de ambiente:
   - `DATABASE_URL` (injetada automaticamente se o Postgres for do próprio
     projeto Railway; senão, cole a do Supabase).
   - `Webhook__ApiKey` (note o `__` duplo — é assim que ASP.NET Core lê
     seções aninhadas de configuração via variável de ambiente).
   - `Cors__AllowedOrigins__0` = URL pública do frontend.

## Abrindo no Visual Studio

`Sabemi.Webhooks.sln` é um `.sln` tradicional (não `.slnx`) — abra direto com
duplo clique ou "Abrir projeto/solução" no Visual Studio.

A solução tem 4 projetos, mas só `Sabemi.Webhooks.Api` é executável (os
outros três são bibliotecas de classe). Isso não fica salvo no `.sln` — na
primeira vez que abrir, clique com botão direito em **`Sabemi.Webhooks.Api`**
no Solution Explorer → **"Definir como Projeto de Inicialização"**.
