# Order Trading System

A distributed system designed for order generation and accumulation, simulating a trading environment where orders are sent from a generator to an exchange using the FIX 4.4 protocol.

## Technologies Used

- **Frontend**: Angular (v21), TypeScript, Node.js
- **Backend (API & Exchange)**: .NET 10, C#
- **Database**: SQLite (via Entity Framework Core)
- **Messaging Protocol**: FIX 4.4 (implemented with QuickFIX/n)

## Installation and Setup

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS recommended)
- [Angular CLI](https://angular.io/cli) (`npm install -g @angular/cli`)

### Running the Project

To run the entire system, you need to start the three components in the following order:

#### 1. OrderAccumulator (The Exchange)
The Accumulator acts as the FIX Acceptor.
```bash
cd OrderAccumulator/OrderAccumulator.API
dotnet run
```

#### 2. OrderGenerator (The API)
The Generator acts as the FIX Initiator and provides the REST API for the frontend.
```bash
cd OrderGenerator/OrderGenerator.API
dotnet run
```

#### 3. Frontend Application
The user interface for generating and monitoring orders.
```bash
cd order-generator
npm install
npm start
```
The application will be available at `http://localhost:4200`.

## Important Notes
- **FIX Protocol**: Ensure that the `initiator.cfg` and `acceptor.cfg` (or equivalent) are correctly configured for the communication between the Generator and the Accumulator.
- **Database**: The system uses SQLite for persistence, creating `orders.db` files locally in the API directories.

## Docker

Uma composição Docker está disponível para subir as APIs, bancos PostgreSQL e o Redis de cache.

- Para subir tudo (build das imagens e execução):

```bash
docker compose up --build
```

- Serviços expostos por padrão:
	- `order-generator-api` → `http://localhost:5159`
	- `order-accumulator-api` → `http://localhost:64164`
	- `order-generator-ui` → `http://localhost:4200`
	- PostgreSQL (db-ordergenerator) → host `localhost:5433` (mapeado para 5432 no container)
	- PostgreSQL (db-orderaccumulator) → host `localhost:5434` (mapeado para 5432 no container)
	- Redis → host `localhost:6379`

- Notas importantes:
	- As APIs dependem de PostgreSQL e Redis; o `docker-compose.yml` inclui healthchecks e `pg_isready` para aguardar os bancos.
	- Se precisar acompanhar os logs durante a inicialização:

```bash
docker compose logs -f order-accumulator-api db-orderaccumulator redis
```

	- Para remover volumes e dados persistidos (atenção: apaga dados):

```bash
docker compose down -v
```

## Observability

Stack self-hosted de observabilidade baseado em **OpenTelemetry + Grafana LGTM** (Loki, Grafana, Tempo, Prometheus). Tudo sobe junto com `docker compose up`.

| Componente | Porta | URL |
|---|---|---|
| Grafana | 3000 | http://localhost:3000 (admin / admin) |
| Prometheus | 9090 | http://localhost:9090 |
| Tempo (traces) | 3200 | http://localhost:3200 |
| OTel Collector (OTLP) | 4317 (gRPC) / 4318 (HTTP) | receiver interno |

- O **OpenTelemetry Collector** recebe OTLP dos serviços .NET e roteia traces para o **Tempo**, metrics para o **Prometheus** (via pull endpoint em `:8889`) e logs para o **Loki** (PR 3).
- Datasources do **Grafana** (Prometheus + Tempo) são auto-provisionados a partir de `observability/grafana/provisioning/`.
- Os volumes `prometheus_data`, `tempo_data` e `grafana_data` persistem dados entre `docker compose down` (use `down -v` para limpar).
- A instrumentação OpenTelemetry nos serviços .NET é adicionada no **PR 2**; o roteamento de logs via Serilog para Loki vem no **PR 3**.

---
> This is a challenge by [Coodesh](https://coodesh.com/)
