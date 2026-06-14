# URL EN RENDER: 
https://examenfinalanalisissistemasnetguard-1.onrender.com/Swagger

# NetGuardGT — API REST de Gestión de Incidentes de Red

Sistema de gestión de incidentes para **NetGuard GT**, proveedor de telecomunicaciones en Guatemala. Permite registrar, asignar, escalar y cerrar incidentes de red aplicando reglas de negocio estrictas sobre SLA, especialización de técnicos y flujo de estados.

---

## Tabla de Contenido

- [Descripción del Sistema](#descripción-del-sistema)
- [Tecnologías Utilizadas](#tecnologías-utilizadas)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Reglas de Negocio Implementadas](#reglas-de-negocio-implementadas)
- [Cómo Ejecutar la Aplicación](#cómo-ejecutar-la-aplicación)
- [Endpoints de la API](#endpoints-de-la-api)
- [Cómo Ejecutar las Pruebas Unitarias](#cómo-ejecutar-las-pruebas-unitarias)
- [Historias de Usuario](#historias-de-usuario)
- [Informe de Utilización de IA](#informe-de-utilización-de-ia)

---

## Descripción del Sistema

NetGuardGT opera una red de telecomunicaciones con:
- **45 sitios** distribuidos en todo Guatemala (antenas, nodos, puntos de presencia PoP)
- **12 técnicos** especializados en fibra óptica, microondas, sistemas eléctricos y redes
- Promedio de **80 incidentes mensuales**

El sistema resuelve problemas críticos de la operación:
- Pérdida de información por falta de reporte oportuno
- Dificultad para generar reportes de cumplimiento de SLA
- Desbalance de carga de trabajo entre técnicos
- Incidentes críticos sin atención oportuna

---

## Tecnologías Utilizadas

| Componente | Tecnología |
|---|---|
| Framework | ASP.NET Core 9 Web API |
| Base de Datos | Entity Framework Core 9 (InMemory) |
| Documentación | Swagger / OpenAPI (Swashbuckle 6.9) |
| Pruebas | xUnit + FluentAssertions |
| Lenguaje | C# 13 / .NET 9 |

---

## Estructura del Proyecto

```
NetGuardGT/
├── NetGuardGT.Api/                  → Proyecto principal de la API
│   ├── BackgroundServices/
│   │   └── EscalationBackgroundService.cs   → Escalado automático cada 5 min
│   ├── Controllers/
│   │   ├── IncidentsController.cs           → CRUD + flujo de incidentes
│   │   ├── TechniciansController.cs         → Gestión de técnicos
│   │   └── ReportsController.cs             → Reportes de incidentes y SLA
│   ├── Data/
│   │   └── AppDbContext.cs                  → DbContext con 12 técnicos pre-cargados
│   ├── Models/
│   │   ├── Enums.cs                         → Severity, IncidentStatus, IncidentType, Specialization
│   │   ├── Incident.cs                      → Modelo de incidente con cálculo de SLA
│   │   ├── Technician.cs                    → Modelo de técnico
│   │   └── IncidentHistory.cs               → Historial de cambios de estado
│   ├── Services/
│   │   ├── IncidentService.cs               → Lógica de negocio principal
│   │   ├── ReportService.cs                 → Generación de reportes
│   │   └── SpecializationRules.cs           → Mapeo tipo incidente → especialización
│   └── Program.cs                           → Configuración de la aplicación
│
└── NetGuardGT.Tests/                → Proyecto de pruebas unitarias
    ├── DbContextFactory.cs          → Helper de contexto aislado por prueba
    ├── DomainRulesTests.cs          → 9 pruebas de reglas de dominio puro
    ├── IncidentServiceCreateTests.cs → 5 pruebas de creación
    ├── IncidentServiceAssignTests.cs → 10 pruebas de asignación
    ├── IncidentServiceStatusTests.cs → 12 pruebas de transición de estados
    ├── IncidentServiceEscalationTests.cs → 7 pruebas de escalado automático
    └── ReportServiceTests.cs        → 15 pruebas de reportes
```

---

## Reglas de Negocio Implementadas

### 1. Tiempo máximo de resolución (SLA) por severidad

| Severidad | Tiempo máximo |
|---|---|
| Critical | 2 horas |
| Urgent | 4 horas |
| High | 8 horas |
| Medium | 24 horas |
| Low | 48 horas |

### 2. Límite de incidentes activos por técnico
Un técnico no puede tener más de **3 incidentes activos** (estado `Assigned` o `InProgress`) simultáneamente.

### 3. Flujo de estados (solo avanza hacia adelante)
```
Registered → Assigned → InProgress → Resolved → Closed
```
No se permiten retrocesos ni saltos de estado.

### 4. Reasignación y liberación
- Un incidente puede ser reasignado a otro técnico en cualquier momento.
- El técnico anterior puede liberar el incidente (`/release`), que regresa a estado `Registered`.

### 5. Escalado automático
Si un incidente de severidad **Critical** o **Urgent** lleva más de **2 horas** en estado `Registered` sin ser atendido, se marca automáticamente como `IsEscalated = true`. El servicio de fondo verifica esto cada 5 minutos.

### 6. Especialización requerida

| Tipo de incidente | Especialización requerida |
|---|---|
| FiberOptic | FiberOptic |
| Microwave | Microwave |
| Electrical | Electrical |
| Network | Network |
| Other | General |

Los técnicos con especialización `General` pueden atender cualquier tipo de incidente.

### 7. Historial de cambios
Cada cambio de estado, asignación o liberación queda registrado en `IncidentHistory` con timestamp, técnico responsable y nota.

---

## Cómo Ejecutar la Aplicación

### Requisitos previos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/keilylopez22/ExamenFinalAnalisisSistemasNetGuard.git
cd ExamenFinalAnalisisSistemasNetGuard

# 2. Entrar al proyecto de la API
cd NetGuardGT.Api

# 3. Restaurar dependencias
dotnet restore

# 4. Ejecutar la aplicación
dotnet run
```

La API estará disponible en:
- **HTTP:** `http://localhost:5108`
- **HTTPS:** `https://localhost:7187`
- **Swagger UI:** `http://localhost:5108/swagger`

> La base de datos es InMemory y se carga automáticamente con 12 técnicos al iniciar.

---

## Endpoints de la API

### Incidentes

| Método | Endpoint | Descripción |
|---|---|---|
| `GET` | `/api/incidents` | Listar incidentes (filtros: `status`, `severity`, `escalated`) |
| `GET` | `/api/incidents/{id}` | Obtener incidente con historial |
| `POST` | `/api/incidents` | Crear nuevo incidente |
| `POST` | `/api/incidents/{id}/assign` | Asignar o reasignar técnico |
| `POST` | `/api/incidents/{id}/status` | Cambiar estado del incidente |
| `POST` | `/api/incidents/{id}/release` | Liberar incidente (técnico lo devuelve) |
| `GET` | `/api/incidents/{id}/history` | Ver historial de cambios |

### Técnicos

| Método | Endpoint | Descripción |
|---|---|---|
| `GET` | `/api/technicians` | Listar técnicos activos |
| `GET` | `/api/technicians/{id}` | Obtener técnico con sus incidentes |
| `POST` | `/api/technicians` | Registrar nuevo técnico |
| `PUT` | `/api/technicians/{id}` | Actualizar técnico |
| `DELETE` | `/api/technicians/{id}` | Desactivar técnico |

### Reportes

| Método | Endpoint | Descripción |
|---|---|---|
| `GET` | `/api/reports/incidents` | Reporte general (totales, por estado, SLA, escalados) |
| `GET` | `/api/reports/workload` | Carga de trabajo por técnico |
| `GET` | `/api/reports/sla` | Detalle de cumplimiento de SLA por incidente |

### Ejemplos de uso con curl

**Crear un incidente:**
```bash
curl -X POST http://localhost:5108/api/incidents \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Corte de fibra óptica",
    "description": "Corte en nodo norte zona 18",
    "siteLocation": "Zona 18, Ciudad de Guatemala",
    "severity": 3,
    "type": 0
  }'
```

**Asignar técnico:**
```bash
curl -X POST http://localhost:5108/api/incidents/1/assign \
  -H "Content-Type: application/json" \
  -d '{ "technicianId": 1, "note": "Asignado por disponibilidad" }'
```

**Cambiar estado a InProgress:**
```bash
curl -X POST http://localhost:5108/api/incidents/1/status \
  -H "Content-Type: application/json" \
  -d '{ "newStatus": 2, "technicianId": 1 }'
```

**Reporte de SLA:**
```bash
curl http://localhost:5108/api/reports/sla
```

> Los valores de enums: Severity → Low=0, Medium=1, High=2, Critical=3, Urgent=4 | IncidentStatus → Registered=0, Assigned=1, InProgress=2, Resolved=3, Closed=4 | IncidentType → FiberOptic=0, Microwave=1, Electrical=2, Network=3, Other=4

---

## Cómo Ejecutar las Pruebas Unitarias

```bash
# Desde la raíz del repositorio
cd NetGuardGT.Tests
dotnet test
```

**Resultado esperado:**
```
Correctas! - Con error: 0, Superado: 58, Omitido: 0, Total: 58, Duración: ~1 s
```

### Para ver detalles de cada prueba:
```bash
dotnet test -v normal
```

### Descripción de los archivos de prueba

| Archivo | Pruebas | Qué valida |
|---|---|---|
| `DomainRulesTests` | 9 | SlaHours por severidad, mapeo de especialización, valores por defecto |
| `IncidentServiceCreateTests` | 5 | Creación correcta, persistencia, estado inicial `Registered` |
| `IncidentServiceAssignTests` | 10 | Especialización coincidente, cap de 3 activos, técnico inactivo, reasignación |
| `IncidentServiceStatusTests` | 12 | Transiciones válidas, transiciones inválidas, fechas `ResolvedAt`/`ClosedAt`, liberación |
| `IncidentServiceEscalationTests` | 7 | Escalado de Critical/Urgent >2h, no escala High, no escala si ya asignado |
| `ReportServiceTests` | 15 | Reporte general, filtro por fechas, detección de breach SLA, workload, ordenamiento |

### Principios FIRST aplicados

- **Fast:** Base de datos InMemory con nombre único por prueba. 58 tests en ~1 segundo.
- **Isolated:** Cada test crea su propio contexto de base de datos sin estado compartido.
- **Repeatable:** Sin dependencias de red, archivos ni hora del sistema sin control.
- **Self-validating:** FluentAssertions hace las aserciones claras y expresivas. Pasa o falla solo.
- **Timely:** Las pruebas fueron escritas junto al código de producción, no después.

---

## Historias de Usuario

### HU-01 — Registrar un incidente de red
**Como** operador de NOC,  
**quiero** registrar un nuevo incidente de red indicando título, descripción, ubicación, severidad y tipo,  
**para** que quede documentado en el sistema y pueda ser atendido por un técnico.

**Criterios de aceptación:**
- El incidente se crea con estado `Registered` automáticamente.
- Se calcula el tiempo SLA según la severidad indicada.
- El sistema retorna el ID del incidente creado.

---

### HU-02 — Asignar un técnico a un incidente
**Como** coordinador de operaciones,  
**quiero** asignar un técnico especializado a un incidente registrado,  
**para** que el técnico adecuado atienda el problema según su área de conocimiento.

**Criterios de aceptación:**
- Solo se puede asignar un técnico con la especialización requerida por el tipo de incidente.
- Los técnicos con especialización `General` pueden atender cualquier tipo.
- Al asignar, el estado pasa automáticamente de `Registered` a `Assigned`.
- El sistema rechaza la asignación si el técnico ya tiene 3 incidentes activos.

---

### HU-03 — Reasignar un incidente a otro técnico
**Como** coordinador de operaciones,  
**quiero** poder reasignar un incidente de un técnico a otro,  
**para** balancear la carga de trabajo o cubrir ausencias del técnico original.

**Criterios de aceptación:**
- La reasignación es posible en cualquier momento mientras el incidente no esté cerrado.
- El nuevo técnico debe cumplir con la especialización requerida.
- El historial registra quién tenía el incidente antes y quién lo recibe.

---

### HU-04 — Liberar un incidente
**Como** técnico de campo,  
**quiero** liberar un incidente que me fue asignado cuando no puedo atenderlo,  
**para** que otro técnico disponible pueda tomarlo.

**Criterios de aceptación:**
- Al liberar, el incidente regresa a estado `Registered` sin técnico asignado.
- No se puede liberar un incidente ya `Resolved` o `Closed`.
- El evento queda registrado en el historial del incidente.

---

### HU-05 — Avanzar el estado de un incidente
**Como** técnico de campo,  
**quiero** actualizar el estado del incidente mientras lo trabajo (InProgress → Resolved),  
**para** reflejar el avance real del trabajo en el sistema.

**Criterios de aceptación:**
- Los estados solo pueden avanzar: `Registered → Assigned → InProgress → Resolved → Closed`.
- No se permiten retrocesos ni saltos.
- Al pasar a `Resolved` se registra automáticamente la fecha de resolución.
- El sistema rechaza transiciones inválidas con un mensaje descriptivo.

---

### HU-06 — Consultar el historial de un incidente
**Como** supervisor técnico,  
**quiero** ver el historial completo de cambios de un incidente,  
**para** auditar quién hizo qué y cuándo durante la atención del mismo.

**Criterios de aceptación:**
- El historial muestra cada cambio de estado con fecha, técnico responsable y nota.
- Los registros están ordenados cronológicamente.
- Se incluyen eventos de asignación, liberación, escalado y cambios de estado.

---

### HU-07 — Escalado automático de incidentes críticos
**Como** jefe de operaciones,  
**quiero** que los incidentes críticos o urgentes que no sean atendidos en 2 horas se marquen automáticamente como escalados,  
**para** que el equipo de guardia sea notificado sin depender de supervisión manual.

**Criterios de aceptación:**
- El sistema verifica cada 5 minutos incidentes `Critical` y `Urgent` en estado `Registered`.
- Si llevan más de 2 horas sin cambio de estado, se marca `IsEscalated = true`.
- El escalado queda registrado en el historial del incidente.
- Un incidente ya escalado no se escala nuevamente.

---

### HU-08 — Consultar la carga de trabajo de los técnicos
**Como** coordinador de operaciones,  
**quiero** ver un reporte de cuántos incidentes activos tiene cada técnico,  
**para** detectar quién está sobrecargado y quién tiene capacidad disponible.

**Criterios de aceptación:**
- El reporte muestra nombre, especialización, incidentes activos y total asignado.
- Los técnicos se ordenan de mayor a menor carga activa.
- Solo se incluyen técnicos activos.

---

### HU-09 — Generar reporte de cumplimiento de SLA
**Como** gerente de operaciones,  
**quiero** obtener un reporte de cumplimiento de SLA con el porcentaje de incidentes resueltos a tiempo,  
**para** presentar métricas de calidad de servicio a los clientes y tomar decisiones correctivas.

**Criterios de aceptación:**
- El reporte incluye total de incidentes resueltos, cantidad de incumplimientos y porcentaje de cumplimiento.
- Cada incidente muestra su SLA objetivo, tiempo real de resolución y si fue cumplido.
- Solo se incluyen incidentes con fecha de resolución registrada.

---

### HU-10 — Filtrar incidentes por estado y severidad
**Como** operador de NOC,  
**quiero** filtrar la lista de incidentes por estado, severidad o si están escalados,  
**para** priorizar mi atención en los incidentes más críticos o pendientes.

**Criterios de aceptación:**
- Se puede filtrar por `status`, `severity` y `escalated` de forma independiente o combinada.
- Si no se aplica filtro, se devuelven todos los incidentes.
- La respuesta incluye el nombre del técnico asignado cuando aplica.

---

### HU-11 — Registrar técnicos especializados
**Como** administrador del sistema,  
**quiero** registrar nuevos técnicos con su nombre y especialización,  
**para** que estén disponibles para ser asignados a incidentes de su área.

**Criterios de aceptación:**
- Se puede crear un técnico indicando nombre y especialización.
- Los técnicos nuevos quedan activos por defecto.
- Un técnico inactivo no puede ser asignado a nuevos incidentes.

---

### HU-12 — Reporte general de incidentes por período
**Como** gerente de operaciones,  
**quiero** generar un reporte de incidentes filtrando por rango de fechas,  
**para** analizar el comportamiento de la red en períodos específicos (semana, mes, trimestre).

**Criterios de aceptación:**
- Se puede filtrar por fecha de inicio y/o fecha de fin.
- El reporte agrupa incidentes por estado, severidad y técnico asignado.
- Muestra el total de escalados y la tasa de cumplimiento de SLA del período.

---

## Informe de Utilización de IA

### Herramienta utilizada
**Amazon Q Developer** — asistente de IA integrado en el IDE (VS Code).

---

### Prompts enviados a la IA

Durante el desarrollo del proyecto se utilizó la IA como herramienta de apoyo. A continuación se documentan algunos de los prompts principales enviados:

**Prompt 1 — Diseño inicial del sistema:**
> *"Quiero una API REST en C# que permita gestionar incidentes de red implementando las reglas de negocio: tiempo máximo de resolución depende de la severidad, un técnico no puede tener más de 3 incidentes activos simultáneamente, los estados solo pueden avanzar en una dirección (Registrado, Asignado, En progreso, Resuelto, Cerrado), escalado automático si un incidente crítico lleva más de 2 horas sin ser atendido, solo técnicos con especialidad coincidente pueden ser asignados..."*

**Prompt 2 — Escalado automático:**
> *"Agrega un background service que verifique cada cierto tiempo si hay incidentes críticos o urgentes sin atender más de 2 horas y los marque como escalados automáticamente."*

**Prompt 3 — Pruebas unitarias:**
> *"Haz las pruebas unitarias en un proyecto aparte para la aplicación basándote en los principios FIRST."*

---

### Reflexión sobre el uso de la IA

El uso de Amazon Q Developer durante este proyecto fue una experiencia que permitió acelerar considerablemente el proceso de desarrollo, aunque también requirió criterio técnico para validar y ajustar cada resultado generado.

**Aspectos positivos observados:**
- La IA generó una estructura de proyecto coherente y bien organizada desde el inicio, respetando la separación de responsabilidades (Controllers, Services, Models, Data).
- La implementación de las reglas de negocio fue precisa en su mayor parte, especialmente el sistema de transiciones de estado y el mapeo de especialización.
- Las pruebas unitarias generadas siguieron correctamente los principios FIRST, con contextos aislados por prueba usando bases de datos InMemory con nombre único por `Guid`.
- La velocidad de generación de código repetitivo (modelos, DTOs, endpoints estándar) permitió enfocarse en revisar la lógica de negocio.

**Limitaciones encontradas:**
- La IA no tiene conocimiento del entorno local, por lo que no detectó automáticamente que el proceso de la aplicación estaba bloqueando el `.exe` al intentar compilar. Esto requirió identificación y solución manual del PID bloqueante.
- En la primera versión del proyecto se incluyeron las carpetas `bin/` y `obj/` en el commit de Git porque faltaba el `.gitignore`. Fue necesario corregirlo manualmente con `git rm --cached`.
- La gestión del repositorio Git requirió ajustes porque el repo se inicializó dentro de `NetGuardGT.Api/` en lugar de la raíz del workspace, lo que impidió incluir el proyecto de pruebas en el mismo commit. Se resolvió inicializando un segundo repo en la carpeta raíz.

**Correcciones realizadas durante el proceso:**
1. Se corrigió la versión del paquete `Microsoft.EntityFrameworkCore.InMemory` de `10.0.9` (incompatible con .NET 9) a `9.0.0`.
2. Se eliminaron `bin/` y `obj/` del tracking de Git y se añadió el `.gitignore` adecuado.
3. Se resolvió el conflicto de proceso bloqueante (MSB3027) identificando el PID con `taskkill` antes de cada compilación.
4. Se reubicó el repositorio Git de `NetGuardGT.Api/` a la raíz del workspace para poder incluir ambos proyectos (`NetGuardGT.Api` y `NetGuardGT.Tests`) en el mismo repositorio remoto.

**Conclusión:**
La IA es una herramienta que potencia la productividad del desarrollador, pero no lo reemplaza. Cada resultado generado fue revisado, comprendido y en varios casos corregido. El valor real está en la combinación del conocimiento técnico propio con la capacidad de generación rápida de la herramienta.
