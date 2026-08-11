# Sistema de Nómina

Proyecto final de la materia **Integración de Software** — Universidad APEC.

Sistema de nómina fullstack que integra un backend propietario en C#/SQL Server con un frontend open source en Next.js vía REST.

## Integrantes

A00115107 - Isaías De León
A00116414 - Lisandro Mora
A00114812 - Danae de Jesus
A00115306 - David Abreu

## Stack

**Backend (propietario)**
- ASP.NET Core Web API sobre .NET 10 (C#)
- Entity Framework Core (code-first + migraciones)
- SQL Server LocalDB (`MSSQLLocalDB`)
- Autenticación JWT + BCrypt

**Backoffice (open source)**
- Next.js 15 (App Router) + React + TypeScript
- Tailwind CSS

## Despliegue

El servidor de prueba corre en Azure sobre planes gratuitos: Azure SQL Database (free
offer) para la base, App Service Linux F1 para la API y Static Web Apps para el backoffice.

La API se publica con el workflow [`deploy-api.yml`](.github/workflows/deploy-api.yml), que
está escrito a mano porque el asistente de Azure compila desde la raíz del repositorio y
aquí la solución vive en `backend/`. Requiere el secreto `AZURE_WEBAPP_PUBLISH_PROFILE`. El
backoffice se publica con el workflow que genera el propio Static Web App.
