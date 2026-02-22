-- ============================================================================
-- MEDICSYS - Datos contables de prueba: 5 meses (Oct 2025 - Feb 2026)
-- ============================================================================
-- Este script llena las tablas de contabilidad con datos realistas para
-- una clínica odontológica: categorías, facturas, asientos contables,
-- gastos operativos y órdenes de compra.
-- ============================================================================

BEGIN;

-- Necesitamos un OdontologoId ficticio (se usará en Expenses, PurchaseOrders, etc.)
-- Usamos un UUID fijo que después se puede reemplazar por el real del seed
DO $$
DECLARE
    v_odonto_id uuid;
BEGIN
    -- Intentar obtener el odontólogo real del seed
    SELECT "Id" INTO v_odonto_id
    FROM "OdontologoPatients" LIMIT 1;

    IF v_odonto_id IS NULL THEN
        v_odonto_id := 'a0000000-0000-0000-0000-000000000001'::uuid;
    ELSE
        -- Tomar el OdontologoId del paciente
        SELECT "OdontologoId" INTO v_odonto_id
        FROM "OdontologoPatients" LIMIT 1;
    END IF;

    -- Limpiar datos previos para re-ejecución idempotente
    DELETE FROM "AccountingEntries";
    DELETE FROM "AccountingCategories";
    DELETE FROM "InvoiceItems";
    DELETE FROM "Invoices";
    DELETE FROM "Expenses";
    DELETE FROM "PurchaseItems";
    DELETE FROM "PurchaseOrders";

    -- ========================================================================
    -- 1. CATEGORÍAS CONTABLES
    -- ========================================================================

    -- Ingresos (Type = 0 = Income)
    INSERT INTO "AccountingCategories" ("Id", "Name", "Group", "Type", "MonthlyBudget", "IsActive") VALUES
    ('c1000000-0001-0000-0000-000000000001', 'Consultas odontológicas',     'Ingresos Operativos',     0, 3000.00, true),
    ('c1000000-0002-0000-0000-000000000001', 'Tratamientos especializados', 'Ingresos Operativos',     0, 5000.00, true),
    ('c1000000-0003-0000-0000-000000000001', 'Cirugías menores',            'Ingresos Operativos',     0, 2000.00, true),
    ('c1000000-0004-0000-0000-000000000001', 'Ortodoncia',                  'Ingresos Operativos',     0, 4000.00, true),
    ('c1000000-0005-0000-0000-000000000001', 'Blanqueamiento dental',       'Ingresos Operativos',     0, 1500.00, true),
    ('c1000000-0006-0000-0000-000000000001', 'Radiografías',                'Ingresos Complementarios', 0, 800.00, true),
    ('c1000000-0007-0000-0000-000000000001', 'Ingresos por servicios',      'Ingresos',                0, 0, true);

    -- Gastos (Type = 1 = Expense)
    INSERT INTO "AccountingCategories" ("Id", "Name", "Group", "Type", "MonthlyBudget", "IsActive") VALUES
    ('c2000000-0001-0000-0000-000000000001', 'Materiales dentales',     'Gastos Operativos',       1, 1200.00, true),
    ('c2000000-0002-0000-0000-000000000001', 'Instrumental descartable','Gastos Operativos',       1,  600.00, true),
    ('c2000000-0003-0000-0000-000000000001', 'Medicamentos',            'Gastos Operativos',       1,  400.00, true),
    ('c2000000-0004-0000-0000-000000000001', 'Salarios',                'Gastos de Personal',      1, 3500.00, true),
    ('c2000000-0005-0000-0000-000000000001', 'Seguro social',           'Gastos de Personal',      1,  800.00, true),
    ('c2000000-0006-0000-0000-000000000001', 'Arriendo local',          'Gastos Fijos',            1, 1200.00, true),
    ('c2000000-0007-0000-0000-000000000001', 'Servicios básicos',       'Gastos Fijos',            1,  350.00, true),
    ('c2000000-0008-0000-0000-000000000001', 'Internet y teléfono',     'Gastos Fijos',            1,  120.00, true),
    ('c2000000-0009-0000-0000-000000000001', 'Suministros de oficina',  'Gastos Administrativos',  1,  100.00, true),
    ('c2000000-0010-0000-0000-000000000001', 'Mantenimiento equipos',   'Gastos Administrativos',  1,  300.00, true),
    ('c2000000-0011-0000-0000-000000000001', 'Publicidad y marketing',  'Gastos Administrativos',  1,  250.00, true),
    ('c2000000-0012-0000-0000-000000000001', 'Impuestos y tasas',       'Gastos Tributarios',      1,  500.00, true),
    ('c2000000-0013-0000-0000-000000000001', 'Depreciación equipos',    'Gastos Depreciación',     1,  200.00, true);

    -- ========================================================================
    -- 2. FACTURAS (5 meses: oct 2025 - feb 2026) con sus items
    -- ========================================================================

    -- === OCTUBRE 2025 (8 facturas) ===
    INSERT INTO "Invoices" ("Id","Number","EstablishmentCode","EmissionPoint","Sequential","IssuedAt","CustomerIdentificationType","CustomerIdentification","CustomerName","CustomerAddress","CustomerPhone","CustomerEmail","Subtotal","DiscountTotal","Tax","Total","TotalToCharge","PaymentMethod","Status","SriEnvironment","SriAuthorizationNumber","SriAuthorizedAt","CreatedAt","UpdatedAt") VALUES
    ('f1000000-0001-0000-0000-000000000001','001-001-000000001','001','001',1,'2025-10-03T10:00:00Z','05','1712345678001','María González','Av. 10 de Agosto 123, Quito','0999123456','maria@email.com',120.00,0,18.00,138.00,138.00,0,1,'Pruebas','AUTH2510001','2025-10-03T10:05:00Z','2025-10-03T10:00:00Z','2025-10-03T10:00:00Z'),
    ('f1000000-0002-0000-0000-000000000001','001-001-000000002','001','001',2,'2025-10-07T14:30:00Z','05','0987654321001','Juan Pérez','Calle García Moreno 456','0998765432','juan@email.com',250.00,25.00,33.75,258.75,258.75,0,1,'Pruebas','AUTH2510002','2025-10-07T14:35:00Z','2025-10-07T14:30:00Z','2025-10-07T14:30:00Z'),
    ('f1000000-0003-0000-0000-000000000001','001-001-000000003','001','001',3,'2025-10-10T09:00:00Z','05','1122334455001','Ana Martínez','Av. América N45','0997654321','ana@email.com',80.00,0,12.00,92.00,92.00,2,1,'Pruebas','AUTH2510003','2025-10-10T09:05:00Z','2025-10-10T09:00:00Z','2025-10-10T09:00:00Z'),
    ('f1000000-0004-0000-0000-000000000001','001-001-000000004','001','001',4,'2025-10-14T11:00:00Z','05','5544332211001','Carlos López','Calle Colón E5-67','0996543210','carlos@email.com',450.00,0,67.50,517.50,517.50,1,1,'Pruebas','AUTH2510004','2025-10-14T11:05:00Z','2025-10-14T11:00:00Z','2025-10-14T11:00:00Z'),
    ('f1000000-0005-0000-0000-000000000001','001-001-000000005','001','001',5,'2025-10-18T15:00:00Z','05','6677889900001','Laura Ramírez','Av. 6 de Diciembre','0995432109','laura@email.com',180.00,0,27.00,207.00,207.00,0,1,'Pruebas','AUTH2510005','2025-10-18T15:05:00Z','2025-10-18T15:00:00Z','2025-10-18T15:00:00Z'),
    ('f1000000-0006-0000-0000-000000000001','001-001-000000006','001','001',6,'2025-10-22T09:30:00Z','05','1234509876001','Pedro Sánchez','Av. Orellana 234','0993216789','pedro@email.com',350.00,35.00,47.25,362.25,362.25,0,1,'Pruebas','AUTH2510006','2025-10-22T09:35:00Z','2025-10-22T09:30:00Z','2025-10-22T09:30:00Z'),
    ('f1000000-0007-0000-0000-000000000001','001-001-000000007','001','001',7,'2025-10-25T16:00:00Z','05','9876543210001','Rosa Fernández','Calle Roca 567','0991234567','rosa@email.com',95.00,0,14.25,109.25,109.25,2,1,'Pruebas','AUTH2510007','2025-10-25T16:05:00Z','2025-10-25T16:00:00Z','2025-10-25T16:00:00Z'),
    ('f1000000-0008-0000-0000-000000000001','001-001-000000008','001','001',8,'2025-10-29T10:00:00Z','05','1357924680001','Diego Morales','Av. Naciones Unidas','0998877665','diego@email.com',200.00,20.00,27.00,207.00,207.00,1,1,'Pruebas','AUTH2510008','2025-10-29T10:05:00Z','2025-10-29T10:00:00Z','2025-10-29T10:00:00Z');

    -- Items de facturas octubre
    INSERT INTO "InvoiceItems" ("Id","InvoiceId","Description","Quantity","UnitPrice","DiscountPercent","Subtotal","TaxRate","Tax","Total") VALUES
    (gen_random_uuid(),'f1000000-0001-0000-0000-000000000001','Limpieza dental profesional',1,70.00,0,70.00,0.15,10.50,80.50),
    (gen_random_uuid(),'f1000000-0001-0000-0000-000000000001','Consulta odontológica',1,50.00,0,50.00,0.15,7.50,57.50),
    (gen_random_uuid(),'f1000000-0002-0000-0000-000000000001','Tratamiento de conducto',1,200.00,10,180.00,0.15,27.00,207.00),
    (gen_random_uuid(),'f1000000-0002-0000-0000-000000000001','Radiografía panorámica',1,50.00,0,50.00,0.15,7.50,57.50),
    (gen_random_uuid(),'f1000000-0003-0000-0000-000000000001','Extracción dental simple',1,80.00,0,80.00,0.15,12.00,92.00),
    (gen_random_uuid(),'f1000000-0004-0000-0000-000000000001','Ortodoncia - Colocación brackets',1,450.00,0,450.00,0.15,67.50,517.50),
    (gen_random_uuid(),'f1000000-0005-0000-0000-000000000001','Blanqueamiento dental LED',1,180.00,0,180.00,0.15,27.00,207.00),
    (gen_random_uuid(),'f1000000-0006-0000-0000-000000000001','Corona dental porcelana',1,350.00,10,315.00,0.15,47.25,362.25),
    (gen_random_uuid(),'f1000000-0007-0000-0000-000000000001','Profilaxis dental',1,45.00,0,45.00,0.15,6.75,51.75),
    (gen_random_uuid(),'f1000000-0007-0000-0000-000000000001','Aplicación de flúor',1,50.00,0,50.00,0.15,7.50,57.50),
    (gen_random_uuid(),'f1000000-0008-0000-0000-000000000001','Obturación con resina (2 piezas)',2,100.00,10,180.00,0.15,27.00,207.00);

    -- === NOVIEMBRE 2025 (9 facturas) ===
    INSERT INTO "Invoices" ("Id","Number","EstablishmentCode","EmissionPoint","Sequential","IssuedAt","CustomerIdentificationType","CustomerIdentification","CustomerName","CustomerAddress","CustomerPhone","CustomerEmail","Subtotal","DiscountTotal","Tax","Total","TotalToCharge","PaymentMethod","Status","SriEnvironment","SriAuthorizationNumber","SriAuthorizedAt","CreatedAt","UpdatedAt") VALUES
    ('f1100000-0001-0000-0000-000000000001','001-001-000000009','001','001',9, '2025-11-03T09:00:00Z','05','1712345678001','María González','Av. 10 de Agosto 123','0999123456','maria@email.com',150.00,0,22.50,172.50,172.50,0,1,'Pruebas','AUTH2511001','2025-11-03T09:05:00Z','2025-11-03T09:00:00Z','2025-11-03T09:00:00Z'),
    ('f1100000-0002-0000-0000-000000000001','001-001-000000010','001','001',10,'2025-11-05T14:00:00Z','05','0987654321001','Juan Pérez','Calle García Moreno 456','0998765432','juan@email.com',300.00,0,45.00,345.00,345.00,1,1,'Pruebas','AUTH2511002','2025-11-05T14:05:00Z','2025-11-05T14:00:00Z','2025-11-05T14:00:00Z'),
    ('f1100000-0003-0000-0000-000000000001','001-001-000000011','001','001',11,'2025-11-08T10:30:00Z','05','1122334455001','Ana Martínez','Av. América N45','0997654321','ana@email.com',85.00,0,12.75,97.75,97.75,0,1,'Pruebas','AUTH2511003','2025-11-08T10:35:00Z','2025-11-08T10:30:00Z','2025-11-08T10:30:00Z'),
    ('f1100000-0004-0000-0000-000000000001','001-001-000000012','001','001',12,'2025-11-12T11:00:00Z','05','5544332211001','Carlos López','Calle Colón E5-67','0996543210','carlos@email.com',200.00,20.00,27.00,207.00,207.00,0,1,'Pruebas','AUTH2511004','2025-11-12T11:05:00Z','2025-11-12T11:00:00Z','2025-11-12T11:00:00Z'),
    ('f1100000-0005-0000-0000-000000000001','001-001-000000013','001','001',13,'2025-11-15T15:30:00Z','05','6677889900001','Laura Ramírez','Av. 6 de Diciembre','0995432109','laura@email.com',500.00,50.00,67.50,517.50,517.50,1,1,'Pruebas','AUTH2511005','2025-11-15T15:35:00Z','2025-11-15T15:30:00Z','2025-11-15T15:30:00Z'),
    ('f1100000-0006-0000-0000-000000000001','001-001-000000014','001','001',14,'2025-11-19T09:00:00Z','05','2468013579001','Sofía Herrera','Calle Amazonas 789','0994321098','sofia@email.com',120.00,0,18.00,138.00,138.00,2,1,'Pruebas','AUTH2511006','2025-11-19T09:05:00Z','2025-11-19T09:00:00Z','2025-11-19T09:00:00Z'),
    ('f1100000-0007-0000-0000-000000000001','001-001-000000015','001','001',15,'2025-11-22T16:00:00Z','05','1234509876001','Pedro Sánchez','Av. Orellana 234','0993216789','pedro@email.com',400.00,0,60.00,460.00,460.00,0,1,'Pruebas','AUTH2511007','2025-11-22T16:05:00Z','2025-11-22T16:00:00Z','2025-11-22T16:00:00Z'),
    ('f1100000-0008-0000-0000-000000000001','001-001-000000016','001','001',16,'2025-11-26T10:00:00Z','05','9876543210001','Rosa Fernández','Calle Roca 567','0991234567','rosa@email.com',75.00,0,11.25,86.25,86.25,0,1,'Pruebas','AUTH2511008','2025-11-26T10:05:00Z','2025-11-26T10:00:00Z','2025-11-26T10:00:00Z'),
    ('f1100000-0009-0000-0000-000000000001','001-001-000000017','001','001',17,'2025-11-28T14:30:00Z','05','1357924680001','Diego Morales','Av. Naciones Unidas','0998877665','diego@email.com',160.00,0,24.00,184.00,184.00,0,1,'Pruebas','AUTH2511009','2025-11-28T14:35:00Z','2025-11-28T14:30:00Z','2025-11-28T14:30:00Z');

    -- Items de facturas noviembre
    INSERT INTO "InvoiceItems" ("Id","InvoiceId","Description","Quantity","UnitPrice","DiscountPercent","Subtotal","TaxRate","Tax","Total") VALUES
    (gen_random_uuid(),'f1100000-0001-0000-0000-000000000001','Control post-tratamiento',1,50.00,0,50.00,0.15,7.50,57.50),
    (gen_random_uuid(),'f1100000-0001-0000-0000-000000000001','Obturación con resina',1,100.00,0,100.00,0.15,15.00,115.00),
    (gen_random_uuid(),'f1100000-0002-0000-0000-000000000001','Ortodoncia - Control mensual',1,300.00,0,300.00,0.15,45.00,345.00),
    (gen_random_uuid(),'f1100000-0003-0000-0000-000000000001','Limpieza dental',1,60.00,0,60.00,0.15,9.00,69.00),
    (gen_random_uuid(),'f1100000-0003-0000-0000-000000000001','Radiografía periapical',1,25.00,0,25.00,0.15,3.75,28.75),
    (gen_random_uuid(),'f1100000-0004-0000-0000-000000000001','Endodoncia molar',1,200.00,10,180.00,0.15,27.00,207.00),
    (gen_random_uuid(),'f1100000-0005-0000-0000-000000000001','Implante dental titanio',1,500.00,10,450.00,0.15,67.50,517.50),
    (gen_random_uuid(),'f1100000-0006-0000-0000-000000000001','Profilaxis dental',1,45.00,0,45.00,0.15,6.75,51.75),
    (gen_random_uuid(),'f1100000-0006-0000-0000-000000000001','Sellante dental (4 piezas)',4,18.75,0,75.00,0.15,11.25,86.25),
    (gen_random_uuid(),'f1100000-0007-0000-0000-000000000001','Prótesis parcial removible',1,400.00,0,400.00,0.15,60.00,460.00),
    (gen_random_uuid(),'f1100000-0008-0000-0000-000000000001','Consulta odontológica',1,50.00,0,50.00,0.15,7.50,57.50),
    (gen_random_uuid(),'f1100000-0008-0000-0000-000000000001','Radiografía periapical',1,25.00,0,25.00,0.15,3.75,28.75),
    (gen_random_uuid(),'f1100000-0009-0000-0000-000000000001','Carillas dentales (2 piezas)',2,80.00,0,160.00,0.15,24.00,184.00);

    -- === DICIEMBRE 2025 (10 facturas) ===
    INSERT INTO "Invoices" ("Id","Number","EstablishmentCode","EmissionPoint","Sequential","IssuedAt","CustomerIdentificationType","CustomerIdentification","CustomerName","CustomerAddress","CustomerPhone","CustomerEmail","Subtotal","DiscountTotal","Tax","Total","TotalToCharge","PaymentMethod","Status","SriEnvironment","SriAuthorizationNumber","SriAuthorizedAt","CreatedAt","UpdatedAt") VALUES
    ('f1200000-0001-0000-0000-000000000001','001-001-000000018','001','001',18,'2025-12-02T09:00:00Z','05','1712345678001','María González','Av. 10 de Agosto 123','0999123456','maria@email.com',200.00,0,30.00,230.00,230.00,0,1,'Pruebas','AUTH2512001','2025-12-02T09:05:00Z','2025-12-02T09:00:00Z','2025-12-02T09:00:00Z'),
    ('f1200000-0002-0000-0000-000000000001','001-001-000000019','001','001',19,'2025-12-04T14:00:00Z','05','0987654321001','Juan Pérez','Calle García Moreno 456','0998765432','juan@email.com',350.00,35.00,47.25,362.25,362.25,1,1,'Pruebas','AUTH2512002','2025-12-04T14:05:00Z','2025-12-04T14:00:00Z','2025-12-04T14:00:00Z'),
    ('f1200000-0003-0000-0000-000000000001','001-001-000000020','001','001',20,'2025-12-08T10:00:00Z','05','1122334455001','Ana Martínez','Av. América N45','0997654321','ana@email.com',150.00,15.00,20.25,155.25,155.25,0,1,'Pruebas','AUTH2512003','2025-12-08T10:05:00Z','2025-12-08T10:00:00Z','2025-12-08T10:00:00Z'),
    ('f1200000-0004-0000-0000-000000000001','001-001-000000021','001','001',21,'2025-12-10T11:30:00Z','05','5544332211001','Carlos López','Calle Colón E5-67','0996543210','carlos@email.com',300.00,0,45.00,345.00,345.00,0,1,'Pruebas','AUTH2512004','2025-12-10T11:35:00Z','2025-12-10T11:30:00Z','2025-12-10T11:30:00Z'),
    ('f1200000-0005-0000-0000-000000000001','001-001-000000022','001','001',22,'2025-12-12T15:00:00Z','05','6677889900001','Laura Ramírez','Av. 6 de Diciembre','0995432109','laura@email.com',450.00,0,67.50,517.50,517.50,1,1,'Pruebas','AUTH2512005','2025-12-12T15:05:00Z','2025-12-12T15:00:00Z','2025-12-12T15:00:00Z'),
    ('f1200000-0006-0000-0000-000000000001','001-001-000000023','001','001',23,'2025-12-15T09:30:00Z','05','2468013579001','Sofía Herrera','Calle Amazonas 789','0994321098','sofia@email.com',180.00,0,27.00,207.00,207.00,0,1,'Pruebas','AUTH2512006','2025-12-15T09:35:00Z','2025-12-15T09:30:00Z','2025-12-15T09:30:00Z'),
    ('f1200000-0007-0000-0000-000000000001','001-001-000000024','001','001',24,'2025-12-18T16:00:00Z','05','1234509876001','Pedro Sánchez','Av. Orellana 234','0993216789','pedro@email.com',95.00,0,14.25,109.25,109.25,2,1,'Pruebas','AUTH2512007','2025-12-18T16:05:00Z','2025-12-18T16:00:00Z','2025-12-18T16:00:00Z'),
    ('f1200000-0008-0000-0000-000000000001','001-001-000000025','001','001',25,'2025-12-20T10:00:00Z','05','9876543210001','Rosa Fernández','Calle Roca 567','0991234567','rosa@email.com',120.00,0,18.00,138.00,138.00,0,1,'Pruebas','AUTH2512008','2025-12-20T10:05:00Z','2025-12-20T10:00:00Z','2025-12-20T10:00:00Z'),
    ('f1200000-0009-0000-0000-000000000001','001-001-000000026','001','001',26,'2025-12-23T14:30:00Z','05','1357924680001','Diego Morales','Av. Naciones Unidas','0998877665','diego@email.com',250.00,25.00,33.75,258.75,258.75,0,1,'Pruebas','AUTH2512009','2025-12-23T14:35:00Z','2025-12-23T14:30:00Z','2025-12-23T14:30:00Z'),
    ('f1200000-0010-0000-0000-000000000001','001-001-000000027','001','001',27,'2025-12-28T11:00:00Z','05','3692581470001','Andrés Vega','Av. República 456','0992345678','andres@email.com',500.00,0,75.00,575.00,575.00,1,1,'Pruebas','AUTH2512010','2025-12-28T11:05:00Z','2025-12-28T11:00:00Z','2025-12-28T11:00:00Z');

    -- Items de facturas diciembre
    INSERT INTO "InvoiceItems" ("Id","InvoiceId","Description","Quantity","UnitPrice","DiscountPercent","Subtotal","TaxRate","Tax","Total") VALUES
    (gen_random_uuid(),'f1200000-0001-0000-0000-000000000001','Obturación con resina',2,100.00,0,200.00,0.15,30.00,230.00),
    (gen_random_uuid(),'f1200000-0002-0000-0000-000000000001','Corona dental porcelana',1,350.00,10,315.00,0.15,47.25,362.25),
    (gen_random_uuid(),'f1200000-0003-0000-0000-000000000001','Limpieza y blanqueamiento',1,150.00,10,135.00,0.15,20.25,155.25),
    (gen_random_uuid(),'f1200000-0004-0000-0000-000000000001','Ortodoncia - Control mensual',1,300.00,0,300.00,0.15,45.00,345.00),
    (gen_random_uuid(),'f1200000-0005-0000-0000-000000000001','Implante dental titanio',1,450.00,0,450.00,0.15,67.50,517.50),
    (gen_random_uuid(),'f1200000-0006-0000-0000-000000000001','Blanqueamiento dental LED',1,180.00,0,180.00,0.15,27.00,207.00),
    (gen_random_uuid(),'f1200000-0007-0000-0000-000000000001','Profilaxis dental',1,45.00,0,45.00,0.15,6.75,51.75),
    (gen_random_uuid(),'f1200000-0007-0000-0000-000000000001','Aplicación de flúor',1,50.00,0,50.00,0.15,7.50,57.50),
    (gen_random_uuid(),'f1200000-0008-0000-0000-000000000001','Consulta + Radiografía',1,120.00,0,120.00,0.15,18.00,138.00),
    (gen_random_uuid(),'f1200000-0009-0000-0000-000000000001','Carillas dentales (3 piezas)',3,83.33,10,225.00,0.15,33.75,258.75),
    (gen_random_uuid(),'f1200000-0010-0000-0000-000000000001','Prótesis fija (puente 3 piezas)',1,500.00,0,500.00,0.15,75.00,575.00);

    -- === ENERO 2026 (10 facturas) ===
    INSERT INTO "Invoices" ("Id","Number","EstablishmentCode","EmissionPoint","Sequential","IssuedAt","CustomerIdentificationType","CustomerIdentification","CustomerName","CustomerAddress","CustomerPhone","CustomerEmail","Subtotal","DiscountTotal","Tax","Total","TotalToCharge","PaymentMethod","Status","SriEnvironment","SriAuthorizationNumber","SriAuthorizedAt","CreatedAt","UpdatedAt") VALUES
    ('f0100000-0001-0000-0000-000000000001','001-001-000000028','001','001',28,'2026-01-05T09:00:00Z','05','1712345678001','María González','Av. 10 de Agosto 123','0999123456','maria@email.com',100.00,0,15.00,115.00,115.00,0,1,'Pruebas','AUTH2601001','2026-01-05T09:05:00Z','2026-01-05T09:00:00Z','2026-01-05T09:00:00Z'),
    ('f0100000-0002-0000-0000-000000000001','001-001-000000029','001','001',29,'2026-01-08T14:00:00Z','05','0987654321001','Juan Pérez','Calle García Moreno 456','0998765432','juan@email.com',280.00,0,42.00,322.00,322.00,1,1,'Pruebas','AUTH2601002','2026-01-08T14:05:00Z','2026-01-08T14:00:00Z','2026-01-08T14:00:00Z'),
    ('f0100000-0003-0000-0000-000000000001','001-001-000000030','001','001',30,'2026-01-10T10:30:00Z','05','1122334455001','Ana Martínez','Av. América N45','0997654321','ana@email.com',180.00,18.00,24.30,186.30,186.30,0,1,'Pruebas','AUTH2601003','2026-01-10T10:35:00Z','2026-01-10T10:30:00Z','2026-01-10T10:30:00Z'),
    ('f0100000-0004-0000-0000-000000000001','001-001-000000031','001','001',31,'2026-01-13T11:00:00Z','05','5544332211001','Carlos López','Calle Colón E5-67','0996543210','carlos@email.com',350.00,0,52.50,402.50,402.50,0,1,'Pruebas','AUTH2601004','2026-01-13T11:05:00Z','2026-01-13T11:00:00Z','2026-01-13T11:00:00Z'),
    ('f0100000-0005-0000-0000-000000000001','001-001-000000032','001','001',32,'2026-01-16T15:00:00Z','05','6677889900001','Laura Ramírez','Av. 6 de Diciembre','0995432109','laura@email.com',220.00,0,33.00,253.00,253.00,2,1,'Pruebas','AUTH2601005','2026-01-16T15:05:00Z','2026-01-16T15:00:00Z','2026-01-16T15:00:00Z'),
    ('f0100000-0006-0000-0000-000000000001','001-001-000000033','001','001',33,'2026-01-19T09:30:00Z','05','2468013579001','Sofía Herrera','Calle Amazonas 789','0994321098','sofia@email.com',380.00,38.00,51.30,393.30,393.30,0,1,'Pruebas','AUTH2601006','2026-01-19T09:35:00Z','2026-01-19T09:30:00Z','2026-01-19T09:30:00Z'),
    ('f0100000-0007-0000-0000-000000000001','001-001-000000034','001','001',34,'2026-01-22T16:00:00Z','05','1234509876001','Pedro Sánchez','Av. Orellana 234','0993216789','pedro@email.com',150.00,0,22.50,172.50,172.50,0,1,'Pruebas','AUTH2601007','2026-01-22T16:05:00Z','2026-01-22T16:00:00Z','2026-01-22T16:00:00Z'),
    ('f0100000-0008-0000-0000-000000000001','001-001-000000035','001','001',35,'2026-01-25T10:00:00Z','05','9876543210001','Rosa Fernández','Calle Roca 567','0991234567','rosa@email.com',90.00,0,13.50,103.50,103.50,0,1,'Pruebas','AUTH2601008','2026-01-25T10:05:00Z','2026-01-25T10:00:00Z','2026-01-25T10:00:00Z'),
    ('f0100000-0009-0000-0000-000000000001','001-001-000000036','001','001',36,'2026-01-28T14:30:00Z','05','1357924680001','Diego Morales','Av. Naciones Unidas','0998877665','diego@email.com',420.00,0,63.00,483.00,483.00,1,1,'Pruebas','AUTH2601009','2026-01-28T14:35:00Z','2026-01-28T14:30:00Z','2026-01-28T14:30:00Z'),
    ('f0100000-0010-0000-0000-000000000001','001-001-000000037','001','001',37,'2026-01-30T11:00:00Z','05','3692581470001','Andrés Vega','Av. República 456','0992345678','andres@email.com',160.00,16.00,21.60,165.60,165.60,0,1,'Pruebas','AUTH2601010','2026-01-30T11:05:00Z','2026-01-30T11:00:00Z','2026-01-30T11:00:00Z');

    -- Items de facturas enero
    INSERT INTO "InvoiceItems" ("Id","InvoiceId","Description","Quantity","UnitPrice","DiscountPercent","Subtotal","TaxRate","Tax","Total") VALUES
    (gen_random_uuid(),'f0100000-0001-0000-0000-000000000001','Consulta odontológica',1,50.00,0,50.00,0.15,7.50,57.50),
    (gen_random_uuid(),'f0100000-0001-0000-0000-000000000001','Radiografía periapical',2,25.00,0,50.00,0.15,7.50,57.50),
    (gen_random_uuid(),'f0100000-0002-0000-0000-000000000001','Tratamiento de conducto molar',1,280.00,0,280.00,0.15,42.00,322.00),
    (gen_random_uuid(),'f0100000-0003-0000-0000-000000000001','Blanqueamiento dental',1,180.00,10,162.00,0.15,24.30,186.30),
    (gen_random_uuid(),'f0100000-0004-0000-0000-000000000001','Ortodoncia - Control mensual',1,300.00,0,300.00,0.15,45.00,345.00),
    (gen_random_uuid(),'f0100000-0004-0000-0000-000000000001','Radiografía panorámica',1,50.00,0,50.00,0.15,7.50,57.50),
    (gen_random_uuid(),'f0100000-0005-0000-0000-000000000001','Extracción tercer molar',1,220.00,0,220.00,0.15,33.00,253.00),
    (gen_random_uuid(),'f0100000-0006-0000-0000-000000000001','Corona dental porcelana',1,380.00,10,342.00,0.15,51.30,393.30),
    (gen_random_uuid(),'f0100000-0007-0000-0000-000000000001','Limpieza dental profesional',1,70.00,0,70.00,0.15,10.50,80.50),
    (gen_random_uuid(),'f0100000-0007-0000-0000-000000000001','Obturación con resina',1,80.00,0,80.00,0.15,12.00,92.00),
    (gen_random_uuid(),'f0100000-0008-0000-0000-000000000001','Profilaxis dental',1,45.00,0,45.00,0.15,6.75,51.75),
    (gen_random_uuid(),'f0100000-0008-0000-0000-000000000001','Aplicación de flúor',1,45.00,0,45.00,0.15,6.75,51.75),
    (gen_random_uuid(),'f0100000-0009-0000-0000-000000000001','Implante dental + Corona',1,420.00,0,420.00,0.15,63.00,483.00),
    (gen_random_uuid(),'f0100000-0010-0000-0000-000000000001','Carillas dentales (2 piezas)',2,80.00,10,144.00,0.15,21.60,165.60);

    -- === FEBRERO 2026 (7 facturas, mes parcial) ===
    INSERT INTO "Invoices" ("Id","Number","EstablishmentCode","EmissionPoint","Sequential","IssuedAt","CustomerIdentificationType","CustomerIdentification","CustomerName","CustomerAddress","CustomerPhone","CustomerEmail","Subtotal","DiscountTotal","Tax","Total","TotalToCharge","PaymentMethod","Status","SriEnvironment","SriAuthorizationNumber","SriAuthorizedAt","CreatedAt","UpdatedAt") VALUES
    ('f0200000-0001-0000-0000-000000000001','001-001-000000038','001','001',38,'2026-02-02T09:00:00Z','05','1712345678001','María González','Av. 10 de Agosto 123','0999123456','maria@email.com',130.00,0,19.50,149.50,149.50,0,1,'Pruebas','AUTH2602001','2026-02-02T09:05:00Z','2026-02-02T09:00:00Z','2026-02-02T09:00:00Z'),
    ('f0200000-0002-0000-0000-000000000001','001-001-000000039','001','001',39,'2026-02-04T14:00:00Z','05','0987654321001','Juan Pérez','Calle García Moreno 456','0998765432','juan@email.com',300.00,0,45.00,345.00,345.00,0,1,'Pruebas','AUTH2602002','2026-02-04T14:05:00Z','2026-02-04T14:00:00Z','2026-02-04T14:00:00Z'),
    ('f0200000-0003-0000-0000-000000000001','001-001-000000040','001','001',40,'2026-02-06T10:30:00Z','05','5544332211001','Carlos López','Calle Colón E5-67','0996543210','carlos@email.com',220.00,0,33.00,253.00,253.00,1,1,'Pruebas','AUTH2602003','2026-02-06T10:35:00Z','2026-02-06T10:30:00Z','2026-02-06T10:30:00Z'),
    ('f0200000-0004-0000-0000-000000000001','001-001-000000041','001','001',41,'2026-02-09T11:00:00Z','05','6677889900001','Laura Ramírez','Av. 6 de Diciembre','0995432109','laura@email.com',180.00,18.00,24.30,186.30,186.30,0,1,'Pruebas','AUTH2602004','2026-02-09T11:05:00Z','2026-02-09T11:00:00Z','2026-02-09T11:00:00Z'),
    ('f0200000-0005-0000-0000-000000000001','001-001-000000042','001','001',42,'2026-02-11T15:00:00Z','05','2468013579001','Sofía Herrera','Calle Amazonas 789','0994321098','sofia@email.com',400.00,40.00,54.00,414.00,414.00,0,1,'Pruebas','AUTH2602005','2026-02-11T15:05:00Z','2026-02-11T15:00:00Z','2026-02-11T15:00:00Z'),
    ('f0200000-0006-0000-0000-000000000001','001-001-000000043','001','001',43,'2026-02-13T09:30:00Z','05','1234509876001','Pedro Sánchez','Av. Orellana 234','0993216789','pedro@email.com',150.00,0,22.50,172.50,172.50,2,1,'Pruebas','AUTH2602006','2026-02-13T09:35:00Z','2026-02-13T09:30:00Z','2026-02-13T09:30:00Z'),
    ('f0200000-0007-0000-0000-000000000001','001-001-000000044','001','001',44,'2026-02-15T10:00:00Z','05','9876543210001','Rosa Fernández','Calle Roca 567','0991234567','rosa@email.com',85.00,0,12.75,97.75,97.75,0,1,'Pruebas','AUTH2602007','2026-02-15T10:05:00Z','2026-02-15T10:00:00Z','2026-02-15T10:00:00Z');

    -- Items de facturas febrero
    INSERT INTO "InvoiceItems" ("Id","InvoiceId","Description","Quantity","UnitPrice","DiscountPercent","Subtotal","TaxRate","Tax","Total") VALUES
    (gen_random_uuid(),'f0200000-0001-0000-0000-000000000001','Consulta + Limpieza dental',1,130.00,0,130.00,0.15,19.50,149.50),
    (gen_random_uuid(),'f0200000-0002-0000-0000-000000000001','Ortodoncia - Control mensual',1,300.00,0,300.00,0.15,45.00,345.00),
    (gen_random_uuid(),'f0200000-0003-0000-0000-000000000001','Extracción + Obturación',1,220.00,0,220.00,0.15,33.00,253.00),
    (gen_random_uuid(),'f0200000-0004-0000-0000-000000000001','Blanqueamiento dental',1,180.00,10,162.00,0.15,24.30,186.30),
    (gen_random_uuid(),'f0200000-0005-0000-0000-000000000001','Implante dental titanio',1,400.00,10,360.00,0.15,54.00,414.00),
    (gen_random_uuid(),'f0200000-0006-0000-0000-000000000001','Prótesis parcial',1,150.00,0,150.00,0.15,22.50,172.50),
    (gen_random_uuid(),'f0200000-0007-0000-0000-000000000001','Profilaxis dental',1,45.00,0,45.00,0.15,6.75,51.75),
    (gen_random_uuid(),'f0200000-0007-0000-0000-000000000001','Radiografía periapical',2,20.00,0,40.00,0.15,6.00,46.00);


    -- ========================================================================
    -- 3. ASIENTOS CONTABLES (AccountingEntries) - 5 meses completos
    -- ========================================================================
    -- Ingresos por facturas (Source='Invoice') + Gastos manuales (Source='Manual')

    -- === OCTUBRE 2025 - INGRESOS (vinculados a facturas) ===
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","InvoiceId","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2025-10-03T00:00:00Z',0,'c1000000-0001-0000-0000-000000000001','Factura 001-001-000000001',138.00,0,NULL,'f1000000-0001-0000-0000-000000000001','Invoice','2025-10-03T10:00:00Z'),
    (gen_random_uuid(),'2025-10-07T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000002',258.75,0,NULL,'f1000000-0002-0000-0000-000000000001','Invoice','2025-10-07T14:30:00Z'),
    (gen_random_uuid(),'2025-10-10T00:00:00Z',0,'c1000000-0003-0000-0000-000000000001','Factura 001-001-000000003',92.00,2,NULL,'f1000000-0003-0000-0000-000000000001','Invoice','2025-10-10T09:00:00Z'),
    (gen_random_uuid(),'2025-10-14T00:00:00Z',0,'c1000000-0004-0000-0000-000000000001','Factura 001-001-000000004',517.50,1,NULL,'f1000000-0004-0000-0000-000000000001','Invoice','2025-10-14T11:00:00Z'),
    (gen_random_uuid(),'2025-10-18T00:00:00Z',0,'c1000000-0005-0000-0000-000000000001','Factura 001-001-000000005',207.00,0,NULL,'f1000000-0005-0000-0000-000000000001','Invoice','2025-10-18T15:00:00Z'),
    (gen_random_uuid(),'2025-10-22T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000006',362.25,0,NULL,'f1000000-0006-0000-0000-000000000001','Invoice','2025-10-22T09:30:00Z'),
    (gen_random_uuid(),'2025-10-25T00:00:00Z',0,'c1000000-0006-0000-0000-000000000001','Factura 001-001-000000007',109.25,2,NULL,'f1000000-0007-0000-0000-000000000001','Invoice','2025-10-25T16:00:00Z'),
    (gen_random_uuid(),'2025-10-29T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000008',207.00,1,NULL,'f1000000-0008-0000-0000-000000000001','Invoice','2025-10-29T10:00:00Z');

    -- OCTUBRE 2025 - GASTOS
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2025-10-01T00:00:00Z',1,'c2000000-0006-0000-0000-000000000001','Arriendo local - Octubre 2025',1200.00,2,'TRF-2510-001','Manual','2025-10-01T08:00:00Z'),
    (gen_random_uuid(),'2025-10-01T00:00:00Z',1,'c2000000-0004-0000-0000-000000000001','Salarios - Octubre 2025',3200.00,2,'TRF-2510-002','Manual','2025-10-01T08:00:00Z'),
    (gen_random_uuid(),'2025-10-01T00:00:00Z',1,'c2000000-0005-0000-0000-000000000001','Seguro social IESS - Octubre',720.00,2,'TRF-2510-003','Manual','2025-10-01T08:00:00Z'),
    (gen_random_uuid(),'2025-10-05T00:00:00Z',1,'c2000000-0007-0000-0000-000000000001','Agua, luz - Octubre 2025',285.00,0,'REC-2510-001','Manual','2025-10-05T09:00:00Z'),
    (gen_random_uuid(),'2025-10-05T00:00:00Z',1,'c2000000-0008-0000-0000-000000000001','Internet y teléfono - Octubre',115.00,2,'TRF-2510-004','Manual','2025-10-05T09:30:00Z'),
    (gen_random_uuid(),'2025-10-08T00:00:00Z',1,'c2000000-0001-0000-0000-000000000001','Compra resinas composite',450.00,0,'FAC-PROV-2510-01','Manual','2025-10-08T10:00:00Z'),
    (gen_random_uuid(),'2025-10-12T00:00:00Z',1,'c2000000-0002-0000-0000-000000000001','Guantes, mascarillas, gasas',180.00,0,'FAC-PROV-2510-02','Manual','2025-10-12T11:00:00Z'),
    (gen_random_uuid(),'2025-10-15T00:00:00Z',1,'c2000000-0003-0000-0000-000000000001','Anestesia lidocaína + agujas',120.00,0,'FAC-PROV-2510-03','Manual','2025-10-15T10:00:00Z'),
    (gen_random_uuid(),'2025-10-20T00:00:00Z',1,'c2000000-0009-0000-0000-000000000001','Papelería y suministros',65.00,0,NULL,'Manual','2025-10-20T09:00:00Z'),
    (gen_random_uuid(),'2025-10-25T00:00:00Z',1,'c2000000-0010-0000-0000-000000000001','Mantenimiento compresor dental',180.00,0,'FAC-MANT-2510-01','Manual','2025-10-25T14:00:00Z'),
    (gen_random_uuid(),'2025-10-28T00:00:00Z',1,'c2000000-0011-0000-0000-000000000001','Publicidad redes sociales - Oct',200.00,1,'PAY-2510-001','Manual','2025-10-28T10:00:00Z'),
    (gen_random_uuid(),'2025-10-31T00:00:00Z',1,'c2000000-0013-0000-0000-000000000001','Depreciación equipos - Octubre',195.00,NULL,NULL,'Manual','2025-10-31T00:00:00Z');

    -- === NOVIEMBRE 2025 - INGRESOS ===
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","InvoiceId","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2025-11-03T00:00:00Z',0,'c1000000-0001-0000-0000-000000000001','Factura 001-001-000000009',172.50,0,NULL,'f1100000-0001-0000-0000-000000000001','Invoice','2025-11-03T09:00:00Z'),
    (gen_random_uuid(),'2025-11-05T00:00:00Z',0,'c1000000-0004-0000-0000-000000000001','Factura 001-001-000000010',345.00,1,NULL,'f1100000-0002-0000-0000-000000000001','Invoice','2025-11-05T14:00:00Z'),
    (gen_random_uuid(),'2025-11-08T00:00:00Z',0,'c1000000-0001-0000-0000-000000000001','Factura 001-001-000000011',97.75,0,NULL,'f1100000-0003-0000-0000-000000000001','Invoice','2025-11-08T10:30:00Z'),
    (gen_random_uuid(),'2025-11-12T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000012',207.00,0,NULL,'f1100000-0004-0000-0000-000000000001','Invoice','2025-11-12T11:00:00Z'),
    (gen_random_uuid(),'2025-11-15T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000013',517.50,1,NULL,'f1100000-0005-0000-0000-000000000001','Invoice','2025-11-15T15:30:00Z'),
    (gen_random_uuid(),'2025-11-19T00:00:00Z',0,'c1000000-0001-0000-0000-000000000001','Factura 001-001-000000014',138.00,2,NULL,'f1100000-0006-0000-0000-000000000001','Invoice','2025-11-19T09:00:00Z'),
    (gen_random_uuid(),'2025-11-22T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000015',460.00,0,NULL,'f1100000-0007-0000-0000-000000000001','Invoice','2025-11-22T16:00:00Z'),
    (gen_random_uuid(),'2025-11-26T00:00:00Z',0,'c1000000-0006-0000-0000-000000000001','Factura 001-001-000000016',86.25,0,NULL,'f1100000-0008-0000-0000-000000000001','Invoice','2025-11-26T10:00:00Z'),
    (gen_random_uuid(),'2025-11-28T00:00:00Z',0,'c1000000-0005-0000-0000-000000000001','Factura 001-001-000000017',184.00,0,NULL,'f1100000-0009-0000-0000-000000000001','Invoice','2025-11-28T14:30:00Z');

    -- NOVIEMBRE 2025 - GASTOS
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2025-11-01T00:00:00Z',1,'c2000000-0006-0000-0000-000000000001','Arriendo local - Noviembre 2025',1200.00,2,'TRF-2511-001','Manual','2025-11-01T08:00:00Z'),
    (gen_random_uuid(),'2025-11-01T00:00:00Z',1,'c2000000-0004-0000-0000-000000000001','Salarios - Noviembre 2025',3200.00,2,'TRF-2511-002','Manual','2025-11-01T08:00:00Z'),
    (gen_random_uuid(),'2025-11-01T00:00:00Z',1,'c2000000-0005-0000-0000-000000000001','Seguro social IESS - Noviembre',720.00,2,'TRF-2511-003','Manual','2025-11-01T08:00:00Z'),
    (gen_random_uuid(),'2025-11-05T00:00:00Z',1,'c2000000-0007-0000-0000-000000000001','Agua, luz - Noviembre 2025',310.00,0,'REC-2511-001','Manual','2025-11-05T09:00:00Z'),
    (gen_random_uuid(),'2025-11-05T00:00:00Z',1,'c2000000-0008-0000-0000-000000000001','Internet y teléfono - Noviembre',115.00,2,'TRF-2511-004','Manual','2025-11-05T09:30:00Z'),
    (gen_random_uuid(),'2025-11-10T00:00:00Z',1,'c2000000-0001-0000-0000-000000000001','Compra amalgamas y cementos',380.00,0,'FAC-PROV-2511-01','Manual','2025-11-10T10:00:00Z'),
    (gen_random_uuid(),'2025-11-14T00:00:00Z',1,'c2000000-0002-0000-0000-000000000001','Material descartable (guantes, algodón)',210.00,0,'FAC-PROV-2511-02','Manual','2025-11-14T11:00:00Z'),
    (gen_random_uuid(),'2025-11-18T00:00:00Z',1,'c2000000-0003-0000-0000-000000000001','Medicamentos + anestesia',150.00,0,'FAC-PROV-2511-03','Manual','2025-11-18T10:00:00Z'),
    (gen_random_uuid(),'2025-11-22T00:00:00Z',1,'c2000000-0009-0000-0000-000000000001','Suministros de oficina',55.00,0,NULL,'Manual','2025-11-22T09:00:00Z'),
    (gen_random_uuid(),'2025-11-25T00:00:00Z',1,'c2000000-0011-0000-0000-000000000001','Publicidad redes sociales - Nov',220.00,1,'PAY-2511-001','Manual','2025-11-25T10:00:00Z'),
    (gen_random_uuid(),'2025-11-30T00:00:00Z',1,'c2000000-0013-0000-0000-000000000001','Depreciación equipos - Noviembre',195.00,NULL,NULL,'Manual','2025-11-30T00:00:00Z'),
    (gen_random_uuid(),'2025-11-30T00:00:00Z',1,'c2000000-0012-0000-0000-000000000001','Declaración IVA - Octubre 2025',280.00,2,'SRI-IVA-2510','Manual','2025-11-30T11:00:00Z');

    -- === DICIEMBRE 2025 - INGRESOS ===
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","InvoiceId","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2025-12-02T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000018',230.00,0,NULL,'f1200000-0001-0000-0000-000000000001','Invoice','2025-12-02T09:00:00Z'),
    (gen_random_uuid(),'2025-12-04T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000019',362.25,1,NULL,'f1200000-0002-0000-0000-000000000001','Invoice','2025-12-04T14:00:00Z'),
    (gen_random_uuid(),'2025-12-08T00:00:00Z',0,'c1000000-0005-0000-0000-000000000001','Factura 001-001-000000020',155.25,0,NULL,'f1200000-0003-0000-0000-000000000001','Invoice','2025-12-08T10:00:00Z'),
    (gen_random_uuid(),'2025-12-10T00:00:00Z',0,'c1000000-0004-0000-0000-000000000001','Factura 001-001-000000021',345.00,0,NULL,'f1200000-0004-0000-0000-000000000001','Invoice','2025-12-10T11:30:00Z'),
    (gen_random_uuid(),'2025-12-12T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000022',517.50,1,NULL,'f1200000-0005-0000-0000-000000000001','Invoice','2025-12-12T15:00:00Z'),
    (gen_random_uuid(),'2025-12-15T00:00:00Z',0,'c1000000-0005-0000-0000-000000000001','Factura 001-001-000000023',207.00,0,NULL,'f1200000-0006-0000-0000-000000000001','Invoice','2025-12-15T09:30:00Z'),
    (gen_random_uuid(),'2025-12-18T00:00:00Z',0,'c1000000-0006-0000-0000-000000000001','Factura 001-001-000000024',109.25,2,NULL,'f1200000-0007-0000-0000-000000000001','Invoice','2025-12-18T16:00:00Z'),
    (gen_random_uuid(),'2025-12-20T00:00:00Z',0,'c1000000-0001-0000-0000-000000000001','Factura 001-001-000000025',138.00,0,NULL,'f1200000-0008-0000-0000-000000000001','Invoice','2025-12-20T10:00:00Z'),
    (gen_random_uuid(),'2025-12-23T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000026',258.75,0,NULL,'f1200000-0009-0000-0000-000000000001','Invoice','2025-12-23T14:30:00Z'),
    (gen_random_uuid(),'2025-12-28T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000027',575.00,1,NULL,'f1200000-0010-0000-0000-000000000001','Invoice','2025-12-28T11:00:00Z');

    -- DICIEMBRE 2025 - GASTOS
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2025-12-01T00:00:00Z',1,'c2000000-0006-0000-0000-000000000001','Arriendo local - Diciembre 2025',1200.00,2,'TRF-2512-001','Manual','2025-12-01T08:00:00Z'),
    (gen_random_uuid(),'2025-12-01T00:00:00Z',1,'c2000000-0004-0000-0000-000000000001','Salarios - Diciembre 2025',3200.00,2,'TRF-2512-002','Manual','2025-12-01T08:00:00Z'),
    (gen_random_uuid(),'2025-12-01T00:00:00Z',1,'c2000000-0005-0000-0000-000000000001','Seguro social IESS - Diciembre',720.00,2,'TRF-2512-003','Manual','2025-12-01T08:00:00Z'),
    (gen_random_uuid(),'2025-12-01T00:00:00Z',1,'c2000000-0004-0000-0000-000000000001','Décimo tercer sueldo (Navidad)',3200.00,2,'TRF-2512-DEC','Manual','2025-12-01T08:00:00Z'),
    (gen_random_uuid(),'2025-12-05T00:00:00Z',1,'c2000000-0007-0000-0000-000000000001','Agua, luz - Diciembre 2025',320.00,0,'REC-2512-001','Manual','2025-12-05T09:00:00Z'),
    (gen_random_uuid(),'2025-12-05T00:00:00Z',1,'c2000000-0008-0000-0000-000000000001','Internet y teléfono - Diciembre',115.00,2,'TRF-2512-004','Manual','2025-12-05T09:30:00Z'),
    (gen_random_uuid(),'2025-12-10T00:00:00Z',1,'c2000000-0001-0000-0000-000000000001','Compra materiales dentales surtido',520.00,0,'FAC-PROV-2512-01','Manual','2025-12-10T10:00:00Z'),
    (gen_random_uuid(),'2025-12-12T00:00:00Z',1,'c2000000-0002-0000-0000-000000000001','Material descartable',190.00,0,'FAC-PROV-2512-02','Manual','2025-12-12T11:00:00Z'),
    (gen_random_uuid(),'2025-12-15T00:00:00Z',1,'c2000000-0003-0000-0000-000000000001','Medicamentos diciembre',135.00,0,'FAC-PROV-2512-03','Manual','2025-12-15T10:00:00Z'),
    (gen_random_uuid(),'2025-12-18T00:00:00Z',1,'c2000000-0010-0000-0000-000000000001','Calibración autoclave',250.00,0,'FAC-MANT-2512-01','Manual','2025-12-18T14:00:00Z'),
    (gen_random_uuid(),'2025-12-20T00:00:00Z',1,'c2000000-0009-0000-0000-000000000001','Suministros de oficina + decoración',95.00,0,NULL,'Manual','2025-12-20T09:00:00Z'),
    (gen_random_uuid(),'2025-12-22T00:00:00Z',1,'c2000000-0011-0000-0000-000000000001','Publicidad Navidad - Redes sociales',350.00,1,'PAY-2512-001','Manual','2025-12-22T10:00:00Z'),
    (gen_random_uuid(),'2025-12-30T00:00:00Z',1,'c2000000-0012-0000-0000-000000000001','Declaración IVA - Noviembre 2025',310.00,2,'SRI-IVA-2511','Manual','2025-12-30T11:00:00Z'),
    (gen_random_uuid(),'2025-12-31T00:00:00Z',1,'c2000000-0013-0000-0000-000000000001','Depreciación equipos - Diciembre',195.00,NULL,NULL,'Manual','2025-12-31T00:00:00Z');

    -- === ENERO 2026 - INGRESOS ===
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","InvoiceId","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2026-01-05T00:00:00Z',0,'c1000000-0001-0000-0000-000000000001','Factura 001-001-000000028',115.00,0,NULL,'f0100000-0001-0000-0000-000000000001','Invoice','2026-01-05T09:00:00Z'),
    (gen_random_uuid(),'2026-01-08T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000029',322.00,1,NULL,'f0100000-0002-0000-0000-000000000001','Invoice','2026-01-08T14:00:00Z'),
    (gen_random_uuid(),'2026-01-10T00:00:00Z',0,'c1000000-0005-0000-0000-000000000001','Factura 001-001-000000030',186.30,0,NULL,'f0100000-0003-0000-0000-000000000001','Invoice','2026-01-10T10:30:00Z'),
    (gen_random_uuid(),'2026-01-13T00:00:00Z',0,'c1000000-0004-0000-0000-000000000001','Factura 001-001-000000031',402.50,0,NULL,'f0100000-0004-0000-0000-000000000001','Invoice','2026-01-13T11:00:00Z'),
    (gen_random_uuid(),'2026-01-16T00:00:00Z',0,'c1000000-0003-0000-0000-000000000001','Factura 001-001-000000032',253.00,2,NULL,'f0100000-0005-0000-0000-000000000001','Invoice','2026-01-16T15:00:00Z'),
    (gen_random_uuid(),'2026-01-19T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000033',393.30,0,NULL,'f0100000-0006-0000-0000-000000000001','Invoice','2026-01-19T09:30:00Z'),
    (gen_random_uuid(),'2026-01-22T00:00:00Z',0,'c1000000-0001-0000-0000-000000000001','Factura 001-001-000000034',172.50,0,NULL,'f0100000-0007-0000-0000-000000000001','Invoice','2026-01-22T16:00:00Z'),
    (gen_random_uuid(),'2026-01-25T00:00:00Z',0,'c1000000-0001-0000-0000-000000000001','Factura 001-001-000000035',103.50,0,NULL,'f0100000-0008-0000-0000-000000000001','Invoice','2026-01-25T10:00:00Z'),
    (gen_random_uuid(),'2026-01-28T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000036',483.00,1,NULL,'f0100000-0009-0000-0000-000000000001','Invoice','2026-01-28T14:30:00Z'),
    (gen_random_uuid(),'2026-01-30T00:00:00Z',0,'c1000000-0005-0000-0000-000000000001','Factura 001-001-000000037',165.60,0,NULL,'f0100000-0010-0000-0000-000000000001','Invoice','2026-01-30T11:00:00Z');

    -- ENERO 2026 - GASTOS
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2026-01-02T00:00:00Z',1,'c2000000-0006-0000-0000-000000000001','Arriendo local - Enero 2026',1200.00,2,'TRF-2601-001','Manual','2026-01-02T08:00:00Z'),
    (gen_random_uuid(),'2026-01-02T00:00:00Z',1,'c2000000-0004-0000-0000-000000000001','Salarios - Enero 2026',3200.00,2,'TRF-2601-002','Manual','2026-01-02T08:00:00Z'),
    (gen_random_uuid(),'2026-01-02T00:00:00Z',1,'c2000000-0005-0000-0000-000000000001','Seguro social IESS - Enero',720.00,2,'TRF-2601-003','Manual','2026-01-02T08:00:00Z'),
    (gen_random_uuid(),'2026-01-06T00:00:00Z',1,'c2000000-0007-0000-0000-000000000001','Agua, luz - Enero 2026',295.00,0,'REC-2601-001','Manual','2026-01-06T09:00:00Z'),
    (gen_random_uuid(),'2026-01-06T00:00:00Z',1,'c2000000-0008-0000-0000-000000000001','Internet y teléfono - Enero',115.00,2,'TRF-2601-004','Manual','2026-01-06T09:30:00Z'),
    (gen_random_uuid(),'2026-01-09T00:00:00Z',1,'c2000000-0001-0000-0000-000000000001','Compra resinas y composite',480.00,0,'FAC-PROV-2601-01','Manual','2026-01-09T10:00:00Z'),
    (gen_random_uuid(),'2026-01-13T00:00:00Z',1,'c2000000-0002-0000-0000-000000000001','Material descartable enero',195.00,0,'FAC-PROV-2601-02','Manual','2026-01-13T11:00:00Z'),
    (gen_random_uuid(),'2026-01-17T00:00:00Z',1,'c2000000-0003-0000-0000-000000000001','Medicamentos y anestesia',140.00,0,'FAC-PROV-2601-03','Manual','2026-01-17T10:00:00Z'),
    (gen_random_uuid(),'2026-01-20T00:00:00Z',1,'c2000000-0009-0000-0000-000000000001','Papelería y suministros',70.00,0,NULL,'Manual','2026-01-20T09:00:00Z'),
    (gen_random_uuid(),'2026-01-24T00:00:00Z',1,'c2000000-0010-0000-0000-000000000001','Mantenimiento unidad dental',220.00,0,'FAC-MANT-2601-01','Manual','2026-01-24T14:00:00Z'),
    (gen_random_uuid(),'2026-01-27T00:00:00Z',1,'c2000000-0011-0000-0000-000000000001','Publicidad redes - Enero',180.00,1,'PAY-2601-001','Manual','2026-01-27T10:00:00Z'),
    (gen_random_uuid(),'2026-01-30T00:00:00Z',1,'c2000000-0012-0000-0000-000000000001','Declaración IVA - Diciembre 2025',350.00,2,'SRI-IVA-2512','Manual','2026-01-30T11:00:00Z'),
    (gen_random_uuid(),'2026-01-31T00:00:00Z',1,'c2000000-0013-0000-0000-000000000001','Depreciación equipos - Enero',195.00,NULL,NULL,'Manual','2026-01-31T00:00:00Z');

    -- === FEBRERO 2026 - INGRESOS ===
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","InvoiceId","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2026-02-02T00:00:00Z',0,'c1000000-0001-0000-0000-000000000001','Factura 001-001-000000038',149.50,0,NULL,'f0200000-0001-0000-0000-000000000001','Invoice','2026-02-02T09:00:00Z'),
    (gen_random_uuid(),'2026-02-04T00:00:00Z',0,'c1000000-0004-0000-0000-000000000001','Factura 001-001-000000039',345.00,0,NULL,'f0200000-0002-0000-0000-000000000001','Invoice','2026-02-04T14:00:00Z'),
    (gen_random_uuid(),'2026-02-06T00:00:00Z',0,'c1000000-0003-0000-0000-000000000001','Factura 001-001-000000040',253.00,1,NULL,'f0200000-0003-0000-0000-000000000001','Invoice','2026-02-06T10:30:00Z'),
    (gen_random_uuid(),'2026-02-09T00:00:00Z',0,'c1000000-0005-0000-0000-000000000001','Factura 001-001-000000041',186.30,0,NULL,'f0200000-0004-0000-0000-000000000001','Invoice','2026-02-09T11:00:00Z'),
    (gen_random_uuid(),'2026-02-11T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000042',414.00,0,NULL,'f0200000-0005-0000-0000-000000000001','Invoice','2026-02-11T15:00:00Z'),
    (gen_random_uuid(),'2026-02-13T00:00:00Z',0,'c1000000-0002-0000-0000-000000000001','Factura 001-001-000000043',172.50,2,NULL,'f0200000-0006-0000-0000-000000000001','Invoice','2026-02-13T09:30:00Z'),
    (gen_random_uuid(),'2026-02-15T00:00:00Z',0,'c1000000-0006-0000-0000-000000000001','Factura 001-001-000000044',97.75,0,NULL,'f0200000-0007-0000-0000-000000000001','Invoice','2026-02-15T10:00:00Z');

    -- FEBRERO 2026 - GASTOS
    INSERT INTO "AccountingEntries" ("Id","Date","Type","CategoryId","Description","Amount","PaymentMethod","Reference","Source","CreatedAt") VALUES
    (gen_random_uuid(),'2026-02-01T00:00:00Z',1,'c2000000-0006-0000-0000-000000000001','Arriendo local - Febrero 2026',1200.00,2,'TRF-2602-001','Manual','2026-02-01T08:00:00Z'),
    (gen_random_uuid(),'2026-02-01T00:00:00Z',1,'c2000000-0004-0000-0000-000000000001','Salarios - Febrero 2026',3200.00,2,'TRF-2602-002','Manual','2026-02-01T08:00:00Z'),
    (gen_random_uuid(),'2026-02-01T00:00:00Z',1,'c2000000-0005-0000-0000-000000000001','Seguro social IESS - Febrero',720.00,2,'TRF-2602-003','Manual','2026-02-01T08:00:00Z'),
    (gen_random_uuid(),'2026-02-05T00:00:00Z',1,'c2000000-0007-0000-0000-000000000001','Agua, luz - Febrero 2026',275.00,0,'REC-2602-001','Manual','2026-02-05T09:00:00Z'),
    (gen_random_uuid(),'2026-02-05T00:00:00Z',1,'c2000000-0008-0000-0000-000000000001','Internet y teléfono - Febrero',115.00,2,'TRF-2602-004','Manual','2026-02-05T09:30:00Z'),
    (gen_random_uuid(),'2026-02-08T00:00:00Z',1,'c2000000-0001-0000-0000-000000000001','Compra materiales varios',410.00,0,'FAC-PROV-2602-01','Manual','2026-02-08T10:00:00Z'),
    (gen_random_uuid(),'2026-02-10T00:00:00Z',1,'c2000000-0002-0000-0000-000000000001','Material descartable febrero',175.00,0,'FAC-PROV-2602-02','Manual','2026-02-10T11:00:00Z'),
    (gen_random_uuid(),'2026-02-12T00:00:00Z',1,'c2000000-0003-0000-0000-000000000001','Medicamentos y anestesia',125.00,0,'FAC-PROV-2602-03','Manual','2026-02-12T10:00:00Z'),
    (gen_random_uuid(),'2026-02-14T00:00:00Z',1,'c2000000-0009-0000-0000-000000000001','Suministros de oficina',60.00,0,NULL,'Manual','2026-02-14T09:00:00Z'),
    (gen_random_uuid(),'2026-02-15T00:00:00Z',1,'c2000000-0011-0000-0000-000000000001','Publicidad San Valentín',150.00,1,'PAY-2602-001','Manual','2026-02-15T10:00:00Z');

    -- ========================================================================
    -- 4. GASTOS OPERATIVOS (tabla Expenses) - 5 meses
    -- ========================================================================

    -- OCTUBRE 2025
    INSERT INTO "Expenses" ("Id","OdontologoId","Description","Amount","ExpenseDate","Category","PaymentMethod","InvoiceNumber","Supplier","Notes","CreatedAt","UpdatedAt") VALUES
    (gen_random_uuid(), v_odonto_id, 'Compra de resinas composite A2, A3, B1', 450.00, '2025-10-08T10:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-001-2510', 'Dentrix Pro', 'Stock para 2 meses', '2025-10-08T10:00:00Z', '2025-10-08T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Guantes nitrilo (10 cajas), mascarillas (5 cajas), gasas', 180.00, '2025-10-12T11:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-002-2510', 'Suministros Médicos S.A.', NULL, '2025-10-12T11:00:00Z', '2025-10-12T11:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Anestesia lidocaína 2% (20 cartuchos) + agujas dentales', 120.00, '2025-10-15T10:00:00Z', 'Medicamentos', 'Efectivo', 'FAC-003-2510', 'Farmacia Dental', NULL, '2025-10-15T10:00:00Z', '2025-10-15T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Arriendo consultorio odontológico', 1200.00, '2025-10-01T08:00:00Z', 'Arriendo', 'Transferencia', NULL, 'Inmobiliaria Centro', 'Pago mensual', '2025-10-01T08:00:00Z', '2025-10-01T08:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Servicio de agua y luz', 285.00, '2025-10-05T09:00:00Z', 'Servicios Básicos', 'Efectivo', 'REC-2510-001', 'EPMAPS/EEQ', NULL, '2025-10-05T09:00:00Z', '2025-10-05T09:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Internet fibra óptica + línea telefónica', 115.00, '2025-10-05T09:30:00Z', 'Servicios Básicos', 'Transferencia', NULL, 'CNT', NULL, '2025-10-05T09:30:00Z', '2025-10-05T09:30:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Mantenimiento preventivo compresor dental', 180.00, '2025-10-25T14:00:00Z', 'Mantenimiento', 'Efectivo', 'FAC-MANT-2510-01', 'Equipos Médicos EC', NULL, '2025-10-25T14:00:00Z', '2025-10-25T14:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Publicidad redes sociales (Facebook + Instagram)', 200.00, '2025-10-28T10:00:00Z', 'Marketing', 'Tarjeta', 'PAY-2510-001', 'Meta Platforms', 'Campaña octubre', '2025-10-28T10:00:00Z', '2025-10-28T10:00:00Z');

    -- NOVIEMBRE 2025
    INSERT INTO "Expenses" ("Id","OdontologoId","Description","Amount","ExpenseDate","Category","PaymentMethod","InvoiceNumber","Supplier","Notes","CreatedAt","UpdatedAt") VALUES
    (gen_random_uuid(), v_odonto_id, 'Amalgamas y cementos dentales', 380.00, '2025-11-10T10:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-001-2511', 'Dentrix Pro', NULL, '2025-11-10T10:00:00Z', '2025-11-10T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Material descartable (guantes, algodón, eyectores)', 210.00, '2025-11-14T11:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-002-2511', 'Suministros Médicos S.A.', NULL, '2025-11-14T11:00:00Z', '2025-11-14T11:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Medicamentos + anestesia noviembre', 150.00, '2025-11-18T10:00:00Z', 'Medicamentos', 'Efectivo', 'FAC-003-2511', 'Farmacia Dental', NULL, '2025-11-18T10:00:00Z', '2025-11-18T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Arriendo consultorio', 1200.00, '2025-11-01T08:00:00Z', 'Arriendo', 'Transferencia', NULL, 'Inmobiliaria Centro', 'Pago mensual', '2025-11-01T08:00:00Z', '2025-11-01T08:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Agua y luz noviembre', 310.00, '2025-11-05T09:00:00Z', 'Servicios Básicos', 'Efectivo', 'REC-2511-001', 'EPMAPS/EEQ', NULL, '2025-11-05T09:00:00Z', '2025-11-05T09:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Internet + teléfono noviembre', 115.00, '2025-11-05T09:30:00Z', 'Servicios Básicos', 'Transferencia', NULL, 'CNT', NULL, '2025-11-05T09:30:00Z', '2025-11-05T09:30:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Publicidad redes sociales noviembre', 220.00, '2025-11-25T10:00:00Z', 'Marketing', 'Tarjeta', 'PAY-2511-001', 'Meta Platforms', NULL, '2025-11-25T10:00:00Z', '2025-11-25T10:00:00Z');

    -- DICIEMBRE 2025
    INSERT INTO "Expenses" ("Id","OdontologoId","Description","Amount","ExpenseDate","Category","PaymentMethod","InvoiceNumber","Supplier","Notes","CreatedAt","UpdatedAt") VALUES
    (gen_random_uuid(), v_odonto_id, 'Compra materiales dentales surtido', 520.00, '2025-12-10T10:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-001-2512', 'Dentrix Pro', 'Incluye brackets', '2025-12-10T10:00:00Z', '2025-12-10T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Material descartable diciembre', 190.00, '2025-12-12T11:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-002-2512', 'Suministros Médicos S.A.', NULL, '2025-12-12T11:00:00Z', '2025-12-12T11:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Medicamentos diciembre', 135.00, '2025-12-15T10:00:00Z', 'Medicamentos', 'Efectivo', 'FAC-003-2512', 'Farmacia Dental', NULL, '2025-12-15T10:00:00Z', '2025-12-15T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Arriendo consultorio diciembre', 1200.00, '2025-12-01T08:00:00Z', 'Arriendo', 'Transferencia', NULL, 'Inmobiliaria Centro', NULL, '2025-12-01T08:00:00Z', '2025-12-01T08:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Agua y luz diciembre', 320.00, '2025-12-05T09:00:00Z', 'Servicios Básicos', 'Efectivo', 'REC-2512-001', 'EPMAPS/EEQ', NULL, '2025-12-05T09:00:00Z', '2025-12-05T09:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Internet + teléfono', 115.00, '2025-12-05T09:30:00Z', 'Servicios Básicos', 'Transferencia', NULL, 'CNT', NULL, '2025-12-05T09:30:00Z', '2025-12-05T09:30:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Calibración y mantenimiento autoclave', 250.00, '2025-12-18T14:00:00Z', 'Mantenimiento', 'Efectivo', 'FAC-MANT-2512-01', 'Equipos Médicos EC', NULL, '2025-12-18T14:00:00Z', '2025-12-18T14:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Publicidad Navidad redes sociales', 350.00, '2025-12-22T10:00:00Z', 'Marketing', 'Tarjeta', 'PAY-2512-001', 'Meta Platforms', 'Campaña navideña', '2025-12-22T10:00:00Z', '2025-12-22T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Decoración navideña consultorio', 95.00, '2025-12-20T09:00:00Z', 'Suministros Oficina', 'Efectivo', NULL, 'De Prati', NULL, '2025-12-20T09:00:00Z', '2025-12-20T09:00:00Z');

    -- ENERO 2026
    INSERT INTO "Expenses" ("Id","OdontologoId","Description","Amount","ExpenseDate","Category","PaymentMethod","InvoiceNumber","Supplier","Notes","CreatedAt","UpdatedAt") VALUES
    (gen_random_uuid(), v_odonto_id, 'Resinas composite y adhesivos', 480.00, '2026-01-09T10:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-001-2601', 'Dentrix Pro', NULL, '2026-01-09T10:00:00Z', '2026-01-09T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Material descartable enero', 195.00, '2026-01-13T11:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-002-2601', 'Suministros Médicos S.A.', NULL, '2026-01-13T11:00:00Z', '2026-01-13T11:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Medicamentos y anestesia', 140.00, '2026-01-17T10:00:00Z', 'Medicamentos', 'Efectivo', 'FAC-003-2601', 'Farmacia Dental', NULL, '2026-01-17T10:00:00Z', '2026-01-17T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Arriendo consultorio enero', 1200.00, '2026-01-02T08:00:00Z', 'Arriendo', 'Transferencia', NULL, 'Inmobiliaria Centro', NULL, '2026-01-02T08:00:00Z', '2026-01-02T08:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Agua y luz enero', 295.00, '2026-01-06T09:00:00Z', 'Servicios Básicos', 'Efectivo', 'REC-2601-001', 'EPMAPS/EEQ', NULL, '2026-01-06T09:00:00Z', '2026-01-06T09:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Internet + teléfono enero', 115.00, '2026-01-06T09:30:00Z', 'Servicios Básicos', 'Transferencia', NULL, 'CNT', NULL, '2026-01-06T09:30:00Z', '2026-01-06T09:30:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Mantenimiento unidad dental', 220.00, '2026-01-24T14:00:00Z', 'Mantenimiento', 'Efectivo', 'FAC-MANT-2601-01', 'Equipos Médicos EC', NULL, '2026-01-24T14:00:00Z', '2026-01-24T14:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Publicidad redes enero', 180.00, '2026-01-27T10:00:00Z', 'Marketing', 'Tarjeta', 'PAY-2601-001', 'Meta Platforms', NULL, '2026-01-27T10:00:00Z', '2026-01-27T10:00:00Z');

    -- FEBRERO 2026
    INSERT INTO "Expenses" ("Id","OdontologoId","Description","Amount","ExpenseDate","Category","PaymentMethod","InvoiceNumber","Supplier","Notes","CreatedAt","UpdatedAt") VALUES
    (gen_random_uuid(), v_odonto_id, 'Materiales dentales varios', 410.00, '2026-02-08T10:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-001-2602', 'Dentrix Pro', NULL, '2026-02-08T10:00:00Z', '2026-02-08T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Material descartable febrero', 175.00, '2026-02-10T11:00:00Z', 'Materiales Dentales', 'Efectivo', 'FAC-002-2602', 'Suministros Médicos S.A.', NULL, '2026-02-10T11:00:00Z', '2026-02-10T11:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Medicamentos y anestesia febrero', 125.00, '2026-02-12T10:00:00Z', 'Medicamentos', 'Efectivo', 'FAC-003-2602', 'Farmacia Dental', NULL, '2026-02-12T10:00:00Z', '2026-02-12T10:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Arriendo consultorio febrero', 1200.00, '2026-02-01T08:00:00Z', 'Arriendo', 'Transferencia', NULL, 'Inmobiliaria Centro', NULL, '2026-02-01T08:00:00Z', '2026-02-01T08:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Agua y luz febrero', 275.00, '2026-02-05T09:00:00Z', 'Servicios Básicos', 'Efectivo', 'REC-2602-001', 'EPMAPS/EEQ', NULL, '2026-02-05T09:00:00Z', '2026-02-05T09:00:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Internet + teléfono febrero', 115.00, '2026-02-05T09:30:00Z', 'Servicios Básicos', 'Transferencia', NULL, 'CNT', NULL, '2026-02-05T09:30:00Z', '2026-02-05T09:30:00Z'),
    (gen_random_uuid(), v_odonto_id, 'Publicidad San Valentín', 150.00, '2026-02-15T10:00:00Z', 'Marketing', 'Tarjeta', 'PAY-2602-001', 'Meta Platforms', 'Campaña Feb', '2026-02-15T10:00:00Z', '2026-02-15T10:00:00Z');

    RAISE NOTICE '✅ Datos contables de 5 meses insertados correctamente';
    RAISE NOTICE 'OdontologoId usado: %', v_odonto_id;

END $$;

COMMIT;
