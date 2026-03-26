# Guardrails - Mi Pizza AI Agent

## Arquitectura General

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Usuario (DevUI)                             │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ mensaje
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        OpenAICliente                                │
│  ┌───────────────┐  ┌────────────────┐  ┌────────────────────────┐ │
│  │  RateLimiter   │→│ InputGuardrail │→│  Azure OpenAI (GPT-4o) │ │
│  │ 20 req/min/ses│  │                │  │                        │ │
│  └───────────────┘  └────────────────┘  └───────────┬────────────┘ │
│                                                      │              │
│                                          ┌───────────▼────────────┐ │
│                                          │   OutputGuardrail      │ │
│                                          │  (sanitiza respuesta)  │ │
│                                          └───────────┬────────────┘ │
└──────────────────────────────────────────────────────┼──────────────┘
                               │                       │
                               │ tool calls            │ respuesta
                               ▼                       ▼
┌──────────────────────────────────────────┐    ┌─────────────┐
│           PizzaDbQueryTool               │    │   Usuario   │
│  ┌────────────────────────────────────┐  │    └─────────────┘
│  │          SqlGuardrail              │  │
│  │  • Solo SELECT/WITH               │  │
│  │  • Sin múltiples statements       │  │
│  │  • Sin funciones peligrosas       │  │
│  │  • Sin schemas del sistema        │  │
│  │  • Sin comentarios SQL            │  │
│  └──────────────┬─────────────────────┘  │
│                 │ query validado          │
│                 ▼                         │
│        PostgreSQL (Mi Pizza DB)          │
└──────────────────────────────────────────┘
```

## Flujo de un mensaje

```
Usuario envía mensaje
       │
       ▼
┌──────────────┐    NO     ┌──────────────────────────────────┐
│ RateLimiter  │─────────→ │ "Has alcanzado el límite de      │
│ ¿permitido?  │           │  20 mensajes por minuto..."      │
└──────┬───────┘           └──────────────────────────────────┘
       │ SÍ
       ▼
┌──────────────┐    NO     ┌──────────────────────────────────┐
│InputGuardrail│─────────→ │ "Solo puedo responder preguntas  │
│ ¿válido?     │           │  relacionadas con Mi Pizza 🍕"   │
└──────┬───────┘           └──────────────────────────────────┘
       │ SÍ
       ▼
┌──────────────┐
│ Azure OpenAI │──→ tool call: EjecutarConsulta(sql)
│  (modelo)    │           │
└──────┬───────┘           ▼
       │           ┌──────────────┐    NO     ┌──────────────┐
       │           │ SqlGuardrail │─────────→ │ "Error: ..." │
       │           │  ¿válido?    │           └──────────────┘
       │           └──────┬───────┘
       │                  │ SÍ
       │                  ▼
       │           ┌──────────────┐
       │           │  PostgreSQL  │
       │           └──────────────┘
       ▼
┌───────────────┐
│OutputGuardrail│──→ Sanitiza credenciales, bloquea fuga de prompt
└──────┬────────┘
       │
       ▼
   Respuesta al usuario
```

## Componentes

### 1. InputGuardrail (`Guardrails/InputGuardrail.cs`)

Valida los mensajes del usuario antes de que lleguen al modelo.

| Protección | Detalle |
|---|---|
| Prompt injection | Detecta frases como "ignore previous instructions", "jailbreak", "DAN mode", "bypass restrictions", etc. |
| SQL injection en input | Bloquea `DROP TABLE`, `DELETE FROM`, `pg_sleep`, `information_schema`, etc. directamente en el texto del usuario |
| Longitud máxima | 2000 caracteres por mensaje |
| Caracteres sospechosos | Detecta secuencias de 10+ caracteres especiales consecutivos (posible ataque de encoding) |

Respuesta al bloqueo: `"Lo siento, solo puedo responder preguntas relacionadas con Mi Pizza 🍕"`

### 2. OutputGuardrail (`Guardrails/OutputGuardrail.cs`)

Sanitiza las respuestas del modelo antes de enviarlas al usuario.

| Protección | Detalle |
|---|---|
| Fuga de credenciales | Detecta y redacta patrones como `api_key`, `password`, `Bearer`, `sk-`, `AKIA`, certificados PEM |
| Fuga de system prompt | Bloquea respuestas donde el modelo revela sus instrucciones ("mis instrucciones son", "mi prompt es", etc.) |

Acción: Redacta con `[REDACTED]` o bloquea la respuesta completa según el caso.

### 3. SqlGuardrail (`Guardrails/SqlGuardrail.cs`)

Validación robusta de consultas SQL antes de ejecutarlas contra PostgreSQL.

| Protección | Detalle |
|---|---|
| Solo lectura | Únicamente permite `SELECT` y `WITH` (CTEs) |
| Múltiples statements | Detecta `;` fuera de strings para prevenir inyección de comandos |
| Palabras clave prohibidas | `INSERT`, `UPDATE`, `DELETE`, `DROP`, `ALTER`, `TRUNCATE`, `CREATE`, `GRANT`, `REVOKE`, `COPY`, `SET`, `RESET`, `LOAD` |
| Funciones peligrosas | `pg_sleep`, `pg_read_file`, `pg_ls_dir`, `pg_terminate_backend`, `pg_cancel_backend`, `set_config`, `lo_import`, `lo_export` |
| Schemas del sistema | Bloquea acceso a `information_schema`, `pg_catalog`, `pg_tables`, `pg_roles`, `pg_user`, `pg_shadow`, `pg_authid` |
| Comentarios SQL | Bloquea `--` y `/* */` que pueden ocultar inyecciones |
| UNION injection | Detecta `UNION` combinado con schemas del sistema |

### 4. RateLimiter (`Guardrails/RateLimiter.cs`)

Control de tasa por sesión usando ventana deslizante.

| Parámetro | Valor |
|---|---|
| Máximo requests | 20 por sesión |
| Ventana de tiempo | 1 minuto |
| Algoritmo | Ventana deslizante (sliding window) |
| Almacenamiento | En memoria (`ConcurrentDictionary`) |

## Integración

Los guardrails se ejecutan en `OpenAICliente` en ambos flujos:

- `GetResponseAsync` (respuesta completa): RateLimiter → InputGuardrail → Modelo → OutputGuardrail
- `GetStreamingResponseAsync` (streaming): RateLimiter → InputGuardrail → Modelo → OutputGuardrail (en memoria)

El `SqlGuardrail` se ejecuta dentro de `PizzaDbQueryTool.EjecutarConsulta()` cada vez que el modelo invoca la herramienta.

## Estructura de archivos

```
PrjDevUI/
├── Guardrails/
│   ├── InputGuardrail.cs      # Validación de entrada del usuario
│   ├── OutputGuardrail.cs     # Sanitización de salida del modelo
│   ├── SqlGuardrail.cs        # Validación SQL robusta
│   └── RateLimiter.cs         # Control de tasa por sesión
├── OpenAICliente.cs           # Integra Input/Output/RateLimit
├── PizzaDbQueryTool.cs        # Integra SqlGuardrail
└── guardrail.md               # Este documento
```
