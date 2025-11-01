# LABOTEC Suite (API + Web + Docker Compose)

## Stack
- API: .NET 8 + MySQL + Identity + JWT + Swagger + Serilog + Azure Blob (AzURITE en local)
- Web: React (Vite) con login, tablas paginadas/ordenadas, filtros, y carga de PDF de resultados
- Compose: mysql, azurite, api, web

## Run local con Docker
```bash
docker compose build
docker compose up -d
# Web: http://localhost:8081
# API: http://localhost:8080/swagger
# MySQL: localhost:3306 (LabotecDb / labotec / SuperClave!2025)
# Azurite Blob: http://localhost:10000/devstoreaccount1
```
Usuario seed: **admin / Admin#2025!**

## Despliegue en Railway

### 1. Base de datos
1. En Railway crea un nuevo servicio **MySQL** y espera a que termine el aprovisionamiento.
2. Copia la variable `MYSQL_URL` (o, en su defecto, las variables `MYSQLHOST`, `MYSQLPORT`, `MYSQLUSER`, `MYSQLPASSWORD`, `MYSQLDATABASE`).

### 2. API (`Labotec.Api`)
1. Crea un servicio **Docker** apuntando a este repositorio, indicando `Labotec.Api` como directorio de trabajo (en Railway establece `RAILWAY_DOCKERFILE_PATH=Labotec.Api/Dockerfile`).
2. Variables necesarias:
   - `MYSQL_URL` (o los campos individuales `MYSQLHOST`, `MYSQLPORT`, `MYSQLUSER`, `MYSQLPASSWORD`, `MYSQLDATABASE`) usando los valores del servicio MySQL.
   - `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key` (puedes reutilizar los de `appsettings.json` o establecer otros).
   - `AzureBlob__ConnectionString` y `AzureBlob__Container` apuntando a tu cuenta de Azure Storage (o cualquier almacenamiento compatible con la API de blobs).
   - `Cors__AllowedOrigins__0` con la URL pública del frontend (añade más índices para orígenes adicionales).
3. La API detecta automáticamente la variable `PORT` que inyecta Railway, por lo que no necesitas configurarla.

### 3. Frontend (`labotec-web`)
1. Crea otro servicio **Docker** apuntando al subdirectorio `labotec-web` (`RAILWAY_DOCKERFILE_PATH=labotec-web/Dockerfile`).
2. Establece `VITE_API_BASE` con la URL pública del servicio API (por ejemplo, `https://labotec-api.up.railway.app`).
3. Una vez desplegado, habilita HTTPS y, si se requiere autenticación, configura en la API el origen correspondiente en `Cors__AllowedOrigins`.

> Consejo: si usas dominios personalizados en Railway, recuerda actualizar los orígenes permitidos en la API y volver a ejecutar el build del frontend con el nuevo valor de `VITE_API_BASE`.
