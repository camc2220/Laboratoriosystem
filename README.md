# LABOTEC Suite (API + Web + Docker Compose)

## Stack
- API: .NET 8 + MySQL + Identity + JWT + Swagger + Serilog + Azure Blob (AzURITE en local)
- Web: React (Vite) con login, tablas paginadas/ordenadas, filtros, y carga de PDF de resultados
- Compose: mysql, azurite, api, web

## Run
```bash
docker compose build
docker compose up -d
# Web: http://localhost:8081
# API: http://localhost:8080/swagger
# MySQL: localhost:3306 (LabotecDb / labotec / SuperClave!2025)
# Azurite Blob: http://localhost:10000/devstoreaccount1
```
Usuario seed: **admin / Admin#2025!**
