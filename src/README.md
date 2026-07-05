# Order Trading System

A distributed system designed for order generation and accumulation, simulating a trading environment where orders are sent from a generator to an exchange using the FIX 4.4 protocol.

## 🚀 Technologies Used

- **Frontend**: Angular (v21), TypeScript, Node.js
- **Backend (API & Exchange)**: .NET 10, C#
- **Database**: SQLite (via Entity Framework Core)
- **Messaging Protocol**: FIX 4.4 (implemented with QuickFIX/n)

## 🛠️ Installation and Setup

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

## 📌 Important Notes
- **FIX Protocol**: Ensure that the `initiator.cfg` and `acceptor.cfg` (or equivalent) are correctly configured for the communication between the Generator and the Accumulator.
- **Database**: The system uses SQLite for persistence, creating `orders.db` files locally in the API directories.

---
> This is a challenge by [Coodesh](https://coodesh.com/)
