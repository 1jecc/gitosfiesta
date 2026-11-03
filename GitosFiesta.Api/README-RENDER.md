# Publicar GitosFiesta.Api en Render

1. Subir el repositorio a GitHub.
2. En Render crear `New > Web Service`.
3. Seleccionar el repositorio.
4. Runtime: `Docker`.
5. Dockerfile path: `GitosFiesta.Api/Dockerfile`.
6. Port: `10000`.
7. Health check path: `/health`.
8. Desplegar.

La URL final tendrá una forma similar a:

`https://gitosfiesta-api.onrender.com`

Actualizar esa URL en `gitosfiesta/MauiProgram.cs`.

El endpoint es:

`POST /api/contratos/{eventoId}/pdf`

El backend actual genera una versión básica del contrato. La migración del diseño completo de `ContratoPdfService` debe hacerse después de validar la conectividad y el despliegue.
