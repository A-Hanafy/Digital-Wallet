# 💳 Digital Wallet RESTful API

A robust and secure Digital Wallet backend service built with **.NET 10** and **ASP.NET Core**, adhering strictly to **Clean Architecture** principles. Designed with a strong focus on financial transaction reliability, concurrency safety, and modern API documentation.

---

## 🏗️ Architectural Overview

The project is structured into distinct decoupled layers to ensure high maintainability, testability, and separation of concerns:

📦 DigitalWallet
┣ 📂 DigitalWallet.Core          # Domain Entities, Interfaces, and Business Exceptions
┣ 📂 DigitalWallet.Application   # Business Logic, DTOs, Validators, and Specifications
┣ 📂 DigitalWallet.Infrastructure# EF Core Configurations, Repositories, UnitOfWork, and Identity
┗ 📂 DigitalWallet.API           # Controllers, Middlewares, OpenAPI/Scalar, and Extensions


---

## 🚀 Key Engineering & FinTech Highlights

### 1. Advanced Concurrency Control (Optimistic Concurrency)
* **Problem:** Preventing race conditions when multiple simultaneous requests attempt to update the same user wallet balance (e.g., concurrent withdrawals or transfers).
* **Solution:** Implemented EF Core **`[Timestamp]` / `RowVersion`** attributes. The database monitors row versions, ensuring that if a conflicting update happens concurrently, a `DbUpdateConcurrencyException` is safely triggered, preventing lost updates without locking database threads.

### 2. Idempotency Support
* **Problem:** Accidental duplicate payments or deposits caused by network retries, timeouts, or double-clicks from clients.
* **Solution:** Utilized **Idempotency Keys** for critical operations (`Deposit`, `Transfer`), guaranteeing that identical requests are processed only once.

### 3. Clean Code & Patterns
* **Specification Pattern:** Encapsulated complex query logic (such as pagination and transaction filtering) into reusable specification classes.
* **Unit of Work & Repository Pattern:** Abstracted data access operations to maintain transactional integrity across multiple repository calls.
* **AutoMapper:** Automated object mapping for clean DTO projections.

### 4. Security & Authentication
* **JWT Authentication:** Secure stateless authentication using ASP.NET Core Identity and claims-based authorization.
* **Secure API Documentation:** Integrated **Scalar** as an ultra-modern alternative to Swagger, featuring built-in JWT `Bearer` token authorization.

---

## 🛠️ Tech Stack

* **Framework:** ASP.NET Core 10 / .NET 10
* **Database & ORM:** SQL Server, Entity Framework Core
* **Security:** ASP.NET Core Identity, JSON Web Tokens (JWT)
* **Documentation:** Scalar, OpenAPI
* **Architecture:** Clean Architecture, SOLID Principles

---

## ⚙️ Getting Started & Running Locally

### Prerequisites
* .NET 10 SDK installed
* SQL Server (LocalDB or SQL Server Express)

### Installation Steps

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/A-Hanafy/Digital-Wallet.git](https://github.com/A-Hanafy/Digital-Wallet.git)
   cd Digital-Wallet
Update Connection String:
Configure your database connection string in appsettings.json under DigitalWallet.API.

Run Database Migrations:

Bash
dotnet ef database update --project DigitalWallet.Infrastructure --startup-project DigitalWallet.API
Run the Project:

Bash
dotnet run --project DigitalWallet.API
Explore & Test API:
Open your browser and navigate to the Scalar documentation UI:
https://localhost:7092/scalar/v1

💡 API Testing Flow
Register a new user via POST /api/Account/register or login via POST /api/Account/login.

Copy the returned JWT Token.

Click the Authorize / Bearer Token section in Scalar and paste your token.

Test secure endpoints (Wallets, Deposits, Transfers, and Transactions history with pagination).

👤 Author
Ahmed Hanafy

Backend Developer (.NET / ASP.NET Core)
