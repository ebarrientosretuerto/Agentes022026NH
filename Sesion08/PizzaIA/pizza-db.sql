-- =============================================
-- Script de creación de tablas - Mi Pizza
-- Base de datos: PostgreSQL
-- Schema: pizza
-- =============================================

CREATE SCHEMA IF NOT EXISTS pizza;

-- Dimensiones
CREATE TABLE pizza.DimensionTamano (
    pk_tamano       SMALLINT PRIMARY KEY,
    cDescripcion    VARCHAR(20) NOT NULL,
    nDiametroCm     SMALLINT,
    nPorciones      SMALLINT
);

CREATE TABLE pizza.DimensionMasa (
    pk_masa         SMALLINT PRIMARY KEY,
    cDescripcion    VARCHAR(30) NOT NULL
);

-- Maestros
CREATE TABLE pizza.MCategoria (
    pk_categoria    SERIAL PRIMARY KEY,
    cNombre         VARCHAR(50) NOT NULL,
    cDescripcion    VARCHAR(150)
);

CREATE TABLE pizza.MIngrediente (
    pk_ingrediente  SERIAL PRIMARY KEY,
    cNombre         VARCHAR(80) NOT NULL,
    cTipo           VARCHAR(30),
    lDisponible     SMALLINT DEFAULT 1,
    dCostoUnitario  DECIMAL(10,2)
);

CREATE TABLE pizza.MProducto (
    pk_producto     SERIAL PRIMARY KEY,
    fk_categoria    INTEGER NOT NULL REFERENCES pizza.MCategoria(pk_categoria),
    cNombre         VARCHAR(100) NOT NULL,
    cDescripcion    VARCHAR(300),
    lActivo         SMALLINT DEFAULT 1,
    fRegCreaFecha   TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE pizza.MProducto_Det_Precio (
    pk_productoPrecio   SERIAL PRIMARY KEY,
    fk_producto         INTEGER NOT NULL REFERENCES pizza.MProducto(pk_producto),
    fk_tamano           SMALLINT NOT NULL REFERENCES pizza.DimensionTamano(pk_tamano),
    fk_masa             SMALLINT NOT NULL REFERENCES pizza.DimensionMasa(pk_masa),
    dPrecio             DECIMAL(10,2) NOT NULL,
    fVigenciaInicio     DATE,
    fVigenciaFin        DATE
);

CREATE TABLE pizza.MProducto_Det_Ingrediente (
    pk_productoIngrediente  SERIAL PRIMARY KEY,
    fk_producto             INTEGER NOT NULL REFERENCES pizza.MProducto(pk_producto),
    fk_ingrediente          INTEGER NOT NULL REFERENCES pizza.MIngrediente(pk_ingrediente),
    nCantidad               DECIMAL(8,2),
    lEsOpcional             SMALLINT DEFAULT 0
);

-- Clientes y pedidos
CREATE TABLE pizza.MCliente (
    pk_cliente      SERIAL PRIMARY KEY,
    cNombre         VARCHAR(100) NOT NULL,
    cTelefono       VARCHAR(15),
    cEmail          VARCHAR(100),
    cDireccion      VARCHAR(200),
    fRegCreaFecha   TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE pizza.MPedido (
    pk_pedido           SERIAL PRIMARY KEY,
    fk_cliente          INTEGER NOT NULL REFERENCES pizza.MCliente(pk_cliente),
    cTipoEntrega        VARCHAR(20),
    cEstado             VARCHAR(20) DEFAULT 'pendiente',
    dTotal              DECIMAL(10,2),
    cDireccionEntrega   VARCHAR(200),
    fFechaPedido        TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    fFechaEntrega       TIMESTAMP
);

CREATE TABLE pizza.MPedido_Det (
    pk_pedidoDet        SERIAL PRIMARY KEY,
    fk_pedido           INTEGER NOT NULL REFERENCES pizza.MPedido(pk_pedido),
    fk_producto         INTEGER NOT NULL REFERENCES pizza.MProducto(pk_producto),
    fk_tamano           SMALLINT NOT NULL REFERENCES pizza.DimensionTamano(pk_tamano),
    fk_masa             SMALLINT NOT NULL REFERENCES pizza.DimensionMasa(pk_masa),
    nCantidad           SMALLINT NOT NULL,
    dPrecioUnitario     DECIMAL(10,2) NOT NULL,
    dSubtotal           DECIMAL(10,2) NOT NULL,
    cObservacion        VARCHAR(200)
);

CREATE TABLE pizza.MPedido_Det_Ingrediente_Extra (
    pk_pedidoDetExtra   SERIAL PRIMARY KEY,
    fk_pedidoDet        INTEGER NOT NULL REFERENCES pizza.MPedido_Det(pk_pedidoDet),
    fk_ingrediente      INTEGER NOT NULL REFERENCES pizza.MIngrediente(pk_ingrediente),
    nCantidad           DECIMAL(8,2),
    dPrecioExtra        DECIMAL(10,2)
);

CREATE TABLE pizza.MPago (
    pk_pago         SERIAL PRIMARY KEY,
    fk_pedido       INTEGER NOT NULL REFERENCES pizza.MPedido(pk_pedido),
    cMetodoPago     VARCHAR(30),
    dMonto          DECIMAL(10,2) NOT NULL,
    cEstado         VARCHAR(20) DEFAULT 'pendiente',
    fFechaPago      TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
