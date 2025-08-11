# 📦 Inventory-App

A complete **ASP.NET Core MVC** inventory management system to track, manage, and organize stock items with a clean layered architecture.

---

## 🚀 Features
- Add, update, and delete inventory items
- View real-time stock levels
- Search and filter items
- Categorization with enums (e.g., stock status, type)
- Database migration support
- Separation of concerns via **Controller → Service → Repository** pattern
- Responsive front-end with Razor Views

---

## 🛠 Technology Stack
- **Framework:** ASP.NET Core MVC (C#)
- **Frontend:** Razor Views, HTML, CSS, JavaScript
- **Database:** Entity Framework Core (SQL Server/MySQL/SQLite)
- **Other:**  
  - DTOs for data transfer  
  - Mapping for clean entity conversions  
  - Migrations for schema updates

---

## 📂 Folder Structure

Controllers/ → Handles HTTP requests and routes
Dtos/ → Data Transfer Objects
Enum/ → Enumerations for statuses or categories
Mapping/ → Entity ↔ DTO mapping logic
Migrations/ → EF Core migration scripts
Models/ → Entity models for the database
Properties/ → Assembly metadata
Service/ → Business logic services
Views/ → Razor UI pages
wwwroot/ → Static assets (CSS, JS, images)
Program.cs → Application entry point
appsettings*.json → Configuration files
Inventree-App.sln → Visual Studio solution file
Inventree-App.csproj → Project file