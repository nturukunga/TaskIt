# TaskIt - Distributed Task Management System  
[![GitHub stars](https://img.shields.io/github/stars/nturukunga/TaskIt?style=social)](https://github.com/nturukunga/TaskIt)  
![MIT License](https://img.shields.io/badge/license-MIT-blue)  

| 🖥️ [ASP.NET Core] | 🐘 [PostgreSQL] | 📱 [Mobile-Friendly]  

## 🌟 Overview  
TaskIt is a robust task management system built for distributed teams using ASP.NET Core MVC and PostgreSQL. Streamline task creation, assignment, and tracking with role-based access control and real-time collaboration features.  

---

## 🚀 Key Features  
| Category              | Highlights                                                                 |
|-----------------------|----------------------------------------------------------------------------|
| **Task Management**   | Priority labeling, due dates, assignment notifications, custom workflows  |
| **Team Collaboration**| Real-time updates, comment threads, activity history tracking             |
| **Dashboard Analytics**| Progress charts, completion metrics, task distribution visualization    |
| **Security**          | ASP.NET Identity, role-based permissions, audit trails                   |
| **Scalability**       | PostgreSQL backend, DTO optimization, paginated responses               |

---

## ⚙️ Technical Stack  
**Frontend**  
- ASP.NET Core MVC (Razor Pages)  
- Bootstrap 5 + jQuery  
- Responsive Web Design  

**Backend**  
- .NET 7.0  
- Entity Framework Core  
- AutoMapper for DTOs  

**Database**  
- PostgreSQL 14+  
- EF Core Migrations  

---

## 🛠️ Getting Started  

### Prerequisites  
- Visual Studio 2022  
- .NET 7.0 SDK  
- PostgreSQL 14+  

### Installation  
1. Clone repository:  
   ```bash
   git clone https://github.com/nturukunga/TaskIt.git
   cd TaskIt

Configure database:
Update appsettings.json:

json
Copy
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=TaskIt;Username=postgres;Password=your_db_password"
}
Apply migrations:
In Package Manager Console:

powershell
Copy
Add-Migration InitialCreate
Update-Database
Launch application:

bash
Copy
dotnet run
Access at https://localhost:5001

☁️ Deployment
Recommended Hosting

Platform	Use Case
Azure	Full .NET ecosystem integration
AWS Elastic	Scalable container deployment
Heroku	PostgreSQL add-on support
Production Checklist

Configure HTTPS

Implement CI/CD pipeline

Set up automated backups

Enable application logging

📜 License
MIT License - Free for commercial use. See LICENSE.

❓ Support
For assistance:

Check Wiki Documentation

Open GitHub Issue

Email: info.native254@gmail.com

**Crafted with 🧠 and ☕ by Howie and AI ofcourse**  
[![Twitter](https://img.shields.io/badge/-@Howie251-1DA1F2?logo=twitter&logoColor=white)](https://twitter.com/Howie251) 
[![GitHub](https://img.shields.io/badge/-GitHub-181717?logo=github&logoColor=white)](https://github.com/nturukunga)
