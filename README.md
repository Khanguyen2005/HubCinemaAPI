# 🎬 HubCinema API – Movie Ticket Booking Management System  

## 📖 Overview  
**HubCinema API** is the backend system for an online movie ticket booking platform.  
It provides RESTful APIs that manage users, movies, showtimes, seat reservations, payments, and reporting for cinema administrators.  
The system is designed to optimize ticket booking, prevent double bookings, and enhance user experience through real-time seat management and secure online payment integration.  

---

## 🚀 Features  
- 🔐 **JWT Authentication** – secure login and role-based access control (Customer, Staff, Admin).  
- 🎟️ **Online Ticket Booking** – users can select movies, seats, and combos and pay online via VNPay.  
- 🪑 **Temporary Seat Reservation using Redis** – prevents double booking and reduces database load.  
- 💳 **VNPay Integration** – handles payment gateway and callback verification.  
- 🏢 **Admin Dashboard APIs** – manage movies, theaters, rooms, promotions, and reports.  
- 📊 **Statistics & Reports** – revenue tracking, daily/weekly performance reports.  
- ⚙️ **CI/CD with GitHub Actions** – automatic build and deployment pipelines.  
- ☁️ **Cloud Deployment** – hosted on cloud environment for scalability and reliability.  

---

## 🏗️ System Architecture  

The project follows a **Multi-Layer Architecture** combined with **MVC**:  
- **Presentation Layer (View):** Web interfaces using ASP.NET Razor.  
- **Business Logic Layer (Controller/Services):** Handles business rules and data flow.  
- **Data Access Layer (Models/Repositories):** Uses Entity Framework Core and LINQ for database operations.  

🧩 **Tech Stack:**  
- **Language:** C#  
- **Framework:** ASP.NET Core MVC  
- **Database:** SQL Server  
- **Cache:** Redis  
- **Authentication:** JWT  
- **API Docs:** Swagger (Swashbuckle)  
- **Version Control:** Git & GitHub  
- **DevOps:** GitHub Actions (CI/CD)  

---

<img width="1904" height="909" alt="image" src="https://github.com/user-attachments/assets/aced8f2c-2091-47e6-ad1e-09e70dc17498" />
<img width="1899" height="912" alt="image" src="https://github.com/user-attachments/assets/da6f1aad-472c-444b-8850-e71d9af38e0d" />

