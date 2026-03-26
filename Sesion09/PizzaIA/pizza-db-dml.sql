-- =============================================
-- Script DML - Datos de prueba - Mi Pizza
-- Base de datos: PostgreSQL
-- Schema: pizza
-- =============================================

-- Dimensiones de tamaño
INSERT INTO pizza.DimensionTamano (pk_tamano, cDescripcion, nDiametroCm, nPorciones) VALUES
(1, 'Personal',   20, 4),
(2, 'Mediana',    30, 6),
(3, 'Familiar',   40, 8),
(4, 'Extra Grande',50, 12);

-- Tipos de masa
INSERT INTO pizza.DimensionMasa (pk_masa, cDescripcion) VALUES
(1, 'Clásica'),
(2, 'Delgada'),
(3, 'Rellena de queso'),
(4, 'Integral');

-- Categorías
INSERT INTO pizza.MCategoria (cNombre, cDescripcion) VALUES
('Pizzas Clásicas',    'Las favoritas de siempre'),
('Pizzas Premium',     'Ingredientes gourmet y combinaciones especiales'),
('Pizzas Vegetarianas','Sin carne, llenas de sabor'),
('Bebidas',            'Refrescos, jugos y más'),
('Complementos',       'Entradas y acompañamientos');

-- Ingredientes
INSERT INTO pizza.MIngrediente (cNombre, cTipo, lDisponible, dCostoUnitario) VALUES
('Mozzarella',         'Queso',     1, 2.50),
('Pepperoni',          'Carne',     1, 3.00),
('Jamón',              'Carne',     1, 2.80),
('Champiñones',        'Vegetal',   1, 1.50),
('Pimiento',           'Vegetal',   1, 1.20),
('Cebolla',            'Vegetal',   1, 0.80),
('Aceitunas negras',   'Vegetal',   1, 1.80),
('Tocino',             'Carne',     1, 3.50),
('Piña',               'Fruta',     1, 1.00),
('Salchicha italiana', 'Carne',     1, 3.20),
('Albahaca fresca',    'Hierba',    1, 0.60),
('Tomate cherry',      'Vegetal',   1, 1.40),
('Queso parmesano',    'Queso',     1, 4.00),
('Anchoas',            'Pescado',   1, 3.80),
('Jalapeño',           'Vegetal',   1, 0.90);

-- Productos (pizzas, bebidas, complementos)
INSERT INTO pizza.MProducto (fk_categoria, cNombre, cDescripcion, lActivo) VALUES
(1, 'Margherita',       'Salsa de tomate, mozzarella y albahaca fresca', 1),
(1, 'Pepperoni',        'Salsa de tomate, mozzarella y pepperoni', 1),
(1, 'Hawaiana',         'Salsa de tomate, mozzarella, jamón y piña', 1),
(2, 'Quattro Formaggi', 'Mozzarella, parmesano, gorgonzola y provolone', 1),
(2, 'Meat Lovers',      'Pepperoni, jamón, tocino y salchicha italiana', 1),
(3, 'Vegetariana',      'Champiñones, pimiento, cebolla, aceitunas y tomate', 1),
(3, 'Caprese',          'Mozzarella fresca, tomate cherry y albahaca', 1),
(4, 'Gaseosa 500ml',    'Coca-Cola, Inca Kola, Sprite', 1),
(4, 'Agua mineral',     'Botella 500ml', 1),
(5, 'Palitos de ajo',   'Con salsa de queso', 1);

-- Precios por producto/tamaño/masa (pizzas)
INSERT INTO pizza.MProducto_Det_Precio (fk_producto, fk_tamano, fk_masa, dPrecio, fVigenciaInicio) VALUES
-- Margherita
(1, 1, 1, 18.90, '2025-01-01'), (1, 2, 1, 29.90, '2025-01-01'), (1, 3, 1, 42.90, '2025-01-01'),
(1, 1, 2, 19.90, '2025-01-01'), (1, 2, 2, 31.90, '2025-01-01'),
-- Pepperoni
(2, 1, 1, 22.90, '2025-01-01'), (2, 2, 1, 34.90, '2025-01-01'), (2, 3, 1, 48.90, '2025-01-01'),
(2, 1, 3, 26.90, '2025-01-01'), (2, 2, 3, 38.90, '2025-01-01'),
-- Hawaiana
(3, 1, 1, 21.90, '2025-01-01'), (3, 2, 1, 33.90, '2025-01-01'), (3, 3, 1, 46.90, '2025-01-01'),
-- Quattro Formaggi
(4, 2, 1, 39.90, '2025-01-01'), (4, 3, 1, 54.90, '2025-01-01'),
-- Meat Lovers
(5, 2, 1, 42.90, '2025-01-01'), (5, 3, 1, 58.90, '2025-01-01'), (5, 3, 3, 64.90, '2025-01-01'),
-- Vegetariana
(6, 1, 1, 19.90, '2025-01-01'), (6, 2, 1, 31.90, '2025-01-01'), (6, 3, 1, 44.90, '2025-01-01'),
-- Caprese
(7, 2, 1, 35.90, '2025-01-01'), (7, 3, 1, 49.90, '2025-01-01'),
-- Bebidas y complementos (tamaño personal, masa clásica como placeholder)
(8, 1, 1, 5.00, '2025-01-01'),
(9, 1, 1, 3.00, '2025-01-01'),
(10, 1, 1, 12.90, '2025-01-01');

-- Ingredientes por producto
INSERT INTO pizza.MProducto_Det_Ingrediente (fk_producto, fk_ingrediente, nCantidad, lEsOpcional) VALUES
(1, 1, 200, 0), (1, 11, 10, 0),                          -- Margherita
(2, 1, 200, 0), (2, 2, 100, 0),                           -- Pepperoni
(3, 1, 200, 0), (3, 3, 80, 0), (3, 9, 60, 0),            -- Hawaiana
(4, 1, 150, 0), (4, 13, 50, 0),                           -- Quattro Formaggi
(5, 2, 80, 0), (5, 3, 80, 0), (5, 8, 60, 0), (5, 10, 60, 0), -- Meat Lovers
(6, 4, 60, 0), (6, 5, 40, 0), (6, 6, 30, 0), (6, 7, 20, 0), (6, 12, 30, 0), -- Vegetariana
(7, 1, 180, 0), (7, 12, 50, 0), (7, 11, 10, 0);          -- Caprese

-- Clientes (20)
INSERT INTO pizza.MCliente (cNombre, cTelefono, cEmail, cDireccion) VALUES
('Carlos Mendoza',    '987654321', 'carlos.mendoza@email.com',    'Av. Arequipa 1234, Miraflores'),
('María López',       '912345678', 'maria.lopez@email.com',       'Jr. Cusco 567, San Isidro'),
('Jorge Ramírez',     '945678123', 'jorge.ramirez@email.com',     'Calle Lima 890, Surco'),
('Ana Torres',        '956781234', 'ana.torres@email.com',        'Av. Javier Prado 2345, La Molina'),
('Luis García',       '967812345', 'luis.garcia@email.com',       'Jr. Huancayo 123, Lince'),
('Patricia Flores',   '978123456', 'patricia.flores@email.com',   'Av. Brasil 4567, Jesús María'),
('Roberto Díaz',      '989234567', 'roberto.diaz@email.com',      'Calle Los Olivos 789, SMP'),
('Carmen Vargas',     '991345678', 'carmen.vargas@email.com',     'Av. La Marina 1011, San Miguel'),
('Fernando Ruiz',     '902456789', 'fernando.ruiz@email.com',     'Jr. Tacna 234, Cercado'),
('Sofía Castillo',    '913567890', 'sofia.castillo@email.com',    'Av. Benavides 5678, Miraflores'),
('Diego Morales',     '924678901', 'diego.morales@email.com',     'Calle Schell 345, Miraflores'),
('Lucía Herrera',     '935789012', 'lucia.herrera@email.com',     'Av. Primavera 6789, Surco'),
('Andrés Paredes',    '946890123', 'andres.paredes@email.com',    'Jr. Ica 456, Breña'),
('Valentina Ríos',    '957901234', 'valentina.rios@email.com',    'Av. Salaverry 7890, San Isidro'),
('Miguel Soto',       '968012345', 'miguel.soto@email.com',       'Calle Berlín 567, Miraflores'),
('Isabella Navarro',  '979123456', 'isabella.navarro@email.com',  'Av. Angamos 8901, Surquillo'),
('Sebastián Cruz',    '980234567', 'sebastian.cruz@email.com',    'Jr. Junín 678, Cercado'),
('Camila Peña',       '991345670', 'camila.pena@email.com',       'Av. Petit Thouars 9012, Lince'),
('Alejandro Vega',    '902456781', 'alejandro.vega@email.com',    'Calle Colón 789, Miraflores'),
('Daniela Rojas',     '913567892', 'daniela.rojas@email.com',     'Av. Del Ejército 1234, Miraflores');

-- Pedidos (20)
INSERT INTO pizza.MPedido (fk_cliente, cTipoEntrega, cEstado, dTotal, cDireccionEntrega, fFechaPedido, fFechaEntrega) VALUES
(1,  'delivery',  'entregado', 64.80,  'Av. Arequipa 1234, Miraflores',       '2025-06-01 12:30:00', '2025-06-01 13:15:00'),
(2,  'recojo',    'entregado', 34.90,  NULL,                                   '2025-06-01 13:00:00', '2025-06-01 13:20:00'),
(3,  'delivery',  'entregado', 48.90,  'Calle Lima 890, Surco',               '2025-06-02 19:00:00', '2025-06-02 19:45:00'),
(4,  'delivery',  'entregado', 87.80,  'Av. Javier Prado 2345, La Molina',    '2025-06-02 20:15:00', '2025-06-02 21:00:00'),
(5,  'recojo',    'entregado', 22.90,  NULL,                                   '2025-06-03 12:00:00', '2025-06-03 12:15:00'),
(6,  'delivery',  'entregado', 73.80,  'Av. Brasil 4567, Jesús María',        '2025-06-03 19:30:00', '2025-06-03 20:10:00'),
(7,  'delivery',  'entregado', 42.90,  'Calle Los Olivos 789, SMP',           '2025-06-04 13:00:00', '2025-06-04 13:40:00'),
(8,  'recojo',    'entregado', 58.90,  NULL,                                   '2025-06-04 20:00:00', '2025-06-04 20:20:00'),
(9,  'delivery',  'entregado', 39.90,  'Jr. Tacna 234, Cercado',              '2025-06-05 12:45:00', '2025-06-05 13:30:00'),
(10, 'delivery',  'entregado', 96.70,  'Av. Benavides 5678, Miraflores',      '2025-06-05 19:00:00', '2025-06-05 19:50:00'),
(11, 'recojo',    'entregado', 29.90,  NULL,                                   '2025-06-06 13:15:00', '2025-06-06 13:30:00'),
(12, 'delivery',  'entregado', 54.90,  'Av. Primavera 6789, Surco',           '2025-06-06 20:30:00', '2025-06-06 21:15:00'),
(13, 'delivery',  'cancelado', 42.90,  'Jr. Ica 456, Breña',                  '2025-06-07 12:00:00', NULL),
(14, 'delivery',  'entregado', 64.90,  'Av. Salaverry 7890, San Isidro',      '2025-06-07 19:45:00', '2025-06-07 20:30:00'),
(15, 'recojo',    'entregado', 18.90,  NULL,                                   '2025-06-08 12:30:00', '2025-06-08 12:45:00'),
(16, 'delivery',  'en_camino', 77.80,  'Av. Angamos 8901, Surquillo',         '2025-06-08 20:00:00', NULL),
(17, 'delivery',  'preparando',34.90,  'Jr. Junín 678, Cercado',              '2025-06-08 20:30:00', NULL),
(18, 'recojo',    'pendiente', 46.90,  NULL,                                   '2025-06-08 20:45:00', NULL),
(19, 'delivery',  'entregado', 109.70, 'Calle Colón 789, Miraflores',         '2025-06-07 13:00:00', '2025-06-07 13:50:00'),
(20, 'delivery',  'entregado', 35.90,  'Av. Del Ejército 1234, Miraflores',   '2025-06-08 12:00:00', '2025-06-08 12:40:00');

-- Detalle de pedidos
INSERT INTO pizza.MPedido_Det (fk_pedido, fk_producto, fk_tamano, fk_masa, nCantidad, dPrecioUnitario, dSubtotal, cObservacion) VALUES
(1,  2, 2, 1, 1, 34.90, 34.90, NULL),
(1,  1, 2, 1, 1, 29.90, 29.90, 'Sin albahaca'),
(2,  2, 2, 1, 1, 34.90, 34.90, NULL),
(3,  2, 3, 1, 1, 48.90, 48.90, 'Extra queso'),
(4,  5, 3, 1, 1, 58.90, 58.90, NULL),
(4,  1, 2, 1, 1, 29.90, 29.90, NULL),
(5,  2, 1, 1, 1, 22.90, 22.90, NULL),
(6,  4, 3, 1, 1, 54.90, 54.90, NULL),
(6,  1, 1, 1, 1, 18.90, 18.90, NULL),
(7,  5, 2, 1, 1, 42.90, 42.90, 'Bien cocida'),
(8,  5, 3, 1, 1, 58.90, 58.90, NULL),
(9,  4, 2, 1, 1, 39.90, 39.90, NULL),
(10, 5, 3, 3, 1, 64.90, 64.90, 'Masa rellena de queso'),
(10, 6, 2, 1, 1, 31.90, 31.90, NULL),
(11, 1, 2, 1, 1, 29.90, 29.90, NULL),
(12, 4, 3, 1, 1, 54.90, 54.90, NULL),
(13, 5, 2, 1, 1, 42.90, 42.90, NULL),
(14, 5, 3, 3, 1, 64.90, 64.90, NULL),
(15, 1, 1, 1, 1, 18.90, 18.90, 'Para llevar'),
(16, 3, 3, 1, 1, 46.90, 46.90, NULL),
(16, 6, 2, 1, 1, 31.90, 31.90, NULL),
(17, 2, 2, 1, 1, 34.90, 34.90, NULL),
(18, 3, 3, 1, 1, 46.90, 46.90, 'Sin cebolla'),
(19, 5, 3, 3, 1, 64.90, 64.90, NULL),
(19, 7, 3, 1, 1, 49.90, 49.90, NULL),
(20, 7, 2, 1, 1, 35.90, 35.90, NULL);

-- Ingredientes extra en pedidos
INSERT INTO pizza.MPedido_Det_Ingrediente_Extra (fk_pedidoDet, fk_ingrediente, nCantidad, dPrecioExtra) VALUES
(3,  1,  100, 5.00),   -- Extra mozzarella en pedido 3
(10, 8,  50,  4.50),   -- Extra tocino en pedido 7
(13, 1,  100, 5.00);   -- Extra mozzarella en pedido 10

-- Pagos
INSERT INTO pizza.MPago (fk_pedido, cMetodoPago, dMonto, cEstado, fFechaPago) VALUES
(1,  'tarjeta',     64.80,  'pagado',    '2025-06-01 12:30:00'),
(2,  'efectivo',    34.90,  'pagado',    '2025-06-01 13:20:00'),
(3,  'yape',        48.90,  'pagado',    '2025-06-02 19:00:00'),
(4,  'tarjeta',     87.80,  'pagado',    '2025-06-02 20:15:00'),
(5,  'efectivo',    22.90,  'pagado',    '2025-06-03 12:15:00'),
(6,  'plin',        73.80,  'pagado',    '2025-06-03 19:30:00'),
(7,  'tarjeta',     42.90,  'pagado',    '2025-06-04 13:00:00'),
(8,  'efectivo',    58.90,  'pagado',    '2025-06-04 20:20:00'),
(9,  'yape',        39.90,  'pagado',    '2025-06-05 12:45:00'),
(10, 'tarjeta',     96.70,  'pagado',    '2025-06-05 19:00:00'),
(11, 'efectivo',    29.90,  'pagado',    '2025-06-06 13:30:00'),
(12, 'plin',        54.90,  'pagado',    '2025-06-06 20:30:00'),
(13, 'yape',        42.90,  'reembolsado','2025-06-07 12:00:00'),
(14, 'tarjeta',     64.90,  'pagado',    '2025-06-07 19:45:00'),
(15, 'efectivo',    18.90,  'pagado',    '2025-06-08 12:45:00'),
(16, 'tarjeta',     77.80,  'pendiente', '2025-06-08 20:00:00'),
(17, 'yape',        34.90,  'pendiente', '2025-06-08 20:30:00'),
(18, 'efectivo',    46.90,  'pendiente', NULL),
(19, 'tarjeta',     109.70, 'pagado',    '2025-06-07 13:00:00'),
(20, 'plin',        35.90,  'pagado',    '2025-06-08 12:00:00');
