# Diccionario de Datos - Base de Datos PizzaStore

Base de datos PostgreSQL para un sistema de venta de pizzas.
Schema principal: `pizza`

---

## pizza.DimensionTamano
| Campo | Tipo | Clave | Descripción |
|-------|------|-------|-------------|
| pk_tamano | INT2 | PK | Identificador único |
| cDescripcion | VARCHAR(20) | | Descripción (Personal, Mediana, Grande, Familiar) |
| nDiametroCm | INT2 | | Diámetro en centímetros |
| nPorciones | INT2 | | Número de porciones |

## pizza.DimensionMasa
| Campo | Tipo | Clave | Descripción |
|-------|------|-------|-------------|
| pk_masa | INT2 | PK | Identificador único |
| cDescripcion | VARCHAR(30) | | Descripción (Delgada, Tradicional, Bordes rellenos) |

## pizza.MCategoria
| Campo | Tipo | Clave | Descripción |
|-------|------|-------|-------------|
| pk_categoria | INT4 | PK | Identificador único |
| cNombre | VARCHAR(50) | | Nombre de la categoría (Clásicas, Especiales, Veganas) |
| cDescripcion | VARCHAR(150) | | Descripción de la categoría |

## pizza.MIngrediente
| Campo | Tipo | Clave | Descripción |
|-------|------|-------|-------------|
| pk_ingrediente | INT4 | PK | Identificador único |
| cNombre | VARCHAR(80) | | Nombre del ingrediente |
| cTipo | VARCHAR(30) | | Tipo (queso, carne, vegetal, salsa, otro) |
| lDisponible | INT2 | | Disponibilidad (1=disponible, 0=no disponible) |
| dCostoUnitario | DECIMAL | | Costo unitario del ingrediente |

## pizza.MProducto
| Campo | Tipo | Clave | Descripción | Tabla Referencia |
|-------|------|-------|-------------|-----------------|
| pk_producto | INT4 | PK | Identificador único | |
| fk_categoria | INT4 | FK | Categoría del producto | pizza.MCategoria |
| cNombre | VARCHAR(100) | | Nombre del producto |  |
| cDescripcion | VARCHAR(300) | | Descripción del producto | |
| lActivo | INT2 | | Estado activo (1=activo, 0=inactivo) | default: 1 |
| fRegCreaFecha | TIMESTAMP | | Fecha de creación | default: CURRENT_TIMESTAMP |

## pizza.MProducto_Det_Precio
| Campo | Tipo | Clave | Descripción | Tabla Referencia |
|-------|------|-------|-------------|-----------------|
| pk_productoPrecio | SERIAL | PK | Identificador único | |
| fk_producto | INT4 | FK | Producto | pizza.MProducto |
| fk_tamano | INT2 | FK | Tamaño | pizza.DimensionTamano |
| fk_masa | INT2 | FK | Tipo de masa | pizza.DimensionMasa |
| dPrecio | DECIMAL | | Precio de venta | |
| fVigenciaInicio | DATE | | Inicio de vigencia del precio | |
| fVigenciaFin | DATE | | Fin de vigencia del precio | |

## pizza.MProducto_Det_Ingrediente
| Campo | Tipo | Clave | Descripción | Tabla Referencia |
|-------|------|-------|-------------|-----------------|
| pk_productoIngrediente | SERIAL | PK | Identificador único | |
| fk_producto | INT4 | FK | Producto | pizza.MProducto |
| fk_ingrediente | INT4 | FK | Ingrediente | pizza.MIngrediente |
| nCantidad | DECIMAL | | Cantidad del ingrediente en gramos | |
| lEsOpcional | INT2 | | Si el ingrediente es opcional (1=sí, 0=no) | default: 0 |

## pizza.MCliente
| Campo | Tipo | Clave | Descripción |
|-------|------|-------|-------------|
| pk_cliente | SERIAL | PK | Identificador único |
| cNombre | VARCHAR(100) | | Nombre completo |
| cTelefono | VARCHAR(15) | | Teléfono de contacto |
| cEmail | VARCHAR(100) | | Correo electrónico |
| cDireccion | VARCHAR(200) | | Dirección principal |
| fRegCreaFecha | TIMESTAMP | | Fecha de registro | default: CURRENT_TIMESTAMP |

## pizza.MPedido
| Campo | Tipo | Clave | Descripción | Tabla Referencia |
|-------|------|-------|-------------|-----------------|
| pk_pedido | SERIAL | PK | Identificador único | |
| fk_cliente | INT4 | FK | Cliente | pizza.MCliente |
| cTipoEntrega | VARCHAR(20) | | Tipo de entrega (local, delivery, recoger) | |
| cEstado | VARCHAR(20) | | Estado (pendiente, preparando, listo, entregado, cancelado) | default: 'pendiente' |
| dTotal | DECIMAL | | Total del pedido | |
| cDireccionEntrega | VARCHAR(200) | | Dirección de entrega (si aplica) | |
| fFechaPedido | TIMESTAMP | | Fecha y hora del pedido | default: CURRENT_TIMESTAMP |
| fFechaEntrega | TIMESTAMP | | Fecha y hora de entrega real | |

## pizza.MPedido_Det
| Campo | Tipo | Clave | Descripción | Tabla Referencia |
|-------|------|-------|-------------|-----------------|
| pk_pedidoDet | SERIAL | PK | Identificador único | |
| fk_pedido | INT4 | FK | Pedido | pizza.MPedido |
| fk_producto | INT4 | FK | Producto | pizza.MProducto |
| fk_tamano | INT2 | FK | Tamaño elegido | pizza.DimensionTamano |
| fk_masa | INT2 | FK | Masa elegida | pizza.DimensionMasa |
| nCantidad | INT2 | | Cantidad de unidades | |
| dPrecioUnitario | DECIMAL | | Precio unitario al momento del pedido | |
| dSubtotal | DECIMAL | | Subtotal (cantidad x precio) | |
| cObservacion | VARCHAR(200) | | Observaciones del ítem | |

## pizza.MPedido_Det_Ingrediente_Extra
| Campo | Tipo | Clave | Descripción | Tabla Referencia |
|-------|------|-------|-------------|-----------------|
| pk_pedidoDetExtra | SERIAL | PK | Identificador único | |
| fk_pedidoDet | INT4 | FK | Detalle del pedido | pizza.MPedido_Det |
| fk_ingrediente | INT4 | FK | Ingrediente extra | pizza.MIngrediente |
| nCantidad | DECIMAL | | Cantidad extra en gramos | |
| dPrecioExtra | DECIMAL | | Costo adicional por el extra | |

## pizza.MPago
| Campo | Tipo | Clave | Descripción | Tabla Referencia |
|-------|------|-------|-------------|-----------------|
| pk_pago | SERIAL | PK | Identificador único | |
| fk_pedido | INT4 | FK | Pedido | pizza.MPedido |
| cMetodoPago | VARCHAR(30) | | Método (efectivo, tarjeta, transferencia, app) | |
| dMonto | DECIMAL | | Monto pagado | |
| cEstado | VARCHAR(20) | | Estado (pendiente, completado, rechazado) | default: 'pendiente' |
| fFechaPago | TIMESTAMP | | Fecha y hora del pago | default: CURRENT_TIMESTAMP |
