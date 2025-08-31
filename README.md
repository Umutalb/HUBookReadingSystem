# Hazal & Umut Book Reading System API

This project is a *Book Reading Tracking System* built with *ASP.NET Core 6.0, **Entity Framework Core, and **PostgreSQL*.  
It allows users to track their reading progress, manage rounds, and keep statistics about completed books.  

Originally, the project was developed using *.NET 8.0, but it was downgraded to **.NET 6.0* for compatibility with *Railway deployment*.

## Features
- Reader management (create, update, track progress)  
- PIN-based authentication (secure with PBKDF2 hash + salt)  
- Session management with secure cookies  
- Add, update, delete reading items  
- Round (session) system to separate book lists by time  
- Detailed statistics and history endpoints  
- Swagger integration for API documentation  

## Tech Stack
- *ASP.NET Core 6.0* (Web API)  
- *Entity Framework Core* (Code-First Migrations)  
- *PostgreSQL* (hosted on Railway)  
- *Swagger / OpenAPI* for documentation  

## Deployment
- Backend deployed on *Railway*  
- Frontend deployed on *Netlify*, connected via CORS  

## License
MIT