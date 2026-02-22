-- ============================================================================
-- MEDICSYS: 50 Pacientes + 50 Historias Clínicas en TODOS los roles
-- Fecha: 2026-02-16
-- Distribución:
--   medicsys_odontologia: 20 pacientes + 20 HC (Odontólogo)
--   medicsys_academico:   20 pacientes + 20 HC (Académico: 3 estudiantes)
--   medicsys (principal):  10 pacientes + 10 HC (Estudiantes vía ClinicalHistories)
-- ============================================================================

-- ############################################################################
-- PARTE 1: medicsys_odontologia (OdontologoPatients + OdontologoClinicalHistories)
-- ############################################################################
\connect medicsys_odontologia

-- IDs del odontólogo
-- odontologo@medicsys.com = 5f932367-7db6-4e11-b539-29d143ad3aa6

-- 20 Pacientes Odontológicos
INSERT INTO "OdontologoPatients" ("Id","OdontologoId","FirstName","LastName","IdNumber","DateOfBirth","Gender","Address","Phone","Email","CreatedAt","UpdatedAt") VALUES
('b0000001-0001-0000-0000-000000000001','5f932367-7db6-4e11-b539-29d143ad3aa6','María','González Paredes','1712345678','1985-03-15','F','Av. 10 de Agosto N24-123, Quito','0991234501','maria.gonzalez@email.com','2025-09-01T08:00:00Z','2025-09-01T08:00:00Z'),
('b0000001-0001-0000-0000-000000000002','5f932367-7db6-4e11-b539-29d143ad3aa6','Juan','Pérez Salazar','1798765432','1990-07-22','M','Calle García Moreno 456, Quito','0991234502','juan.perez@email.com','2025-09-05T09:00:00Z','2025-09-05T09:00:00Z'),
('b0000001-0001-0000-0000-000000000003','5f932367-7db6-4e11-b539-29d143ad3aa6','Ana','Martínez Vega','1722334455','1988-11-30','F','Av. América N45-123, Quito','0991234503','ana.martinez@email.com','2025-09-10T10:00:00Z','2025-09-10T10:00:00Z'),
('b0000001-0001-0000-0000-000000000004','5f932367-7db6-4e11-b539-29d143ad3aa6','Carlos','López Herrera','1754432211','1995-05-18','M','Calle Colón E5-67, Quito','0991234504','carlos.lopez@email.com','2025-09-15T08:30:00Z','2025-09-15T08:30:00Z'),
('b0000001-0001-0000-0000-000000000005','5f932367-7db6-4e11-b539-29d143ad3aa6','Laura','Ramírez Torres','1767788990','1992-09-25','F','Av. 6 de Diciembre N34-890, Quito','0991234505','laura.ramirez@email.com','2025-10-01T09:00:00Z','2025-10-01T09:00:00Z'),
('b0000001-0001-0000-0000-000000000006','5f932367-7db6-4e11-b539-29d143ad3aa6','Pedro','Sánchez Mora','1733445566','1987-02-14','M','Av. Naciones Unidas E7-45, Quito','0991234506','pedro.sanchez@email.com','2025-10-05T10:00:00Z','2025-10-05T10:00:00Z'),
('b0000001-0001-0000-0000-000000000007','5f932367-7db6-4e11-b539-29d143ad3aa6','Sofía','Herrera Ruiz','1744556677','1993-06-08','F','Calle Amazonas N23-45, Quito','0991234507','sofia.herrera@email.com','2025-10-10T08:00:00Z','2025-10-10T08:00:00Z'),
('b0000001-0001-0000-0000-000000000008','5f932367-7db6-4e11-b539-29d143ad3aa6','Roberto','Fernández Cruz','1755667788','1982-12-03','M','Av. Eloy Alfaro N45-67, Quito','0991234508','roberto.fernandez@email.com','2025-10-15T09:30:00Z','2025-10-15T09:30:00Z'),
('b0000001-0001-0000-0000-000000000009','5f932367-7db6-4e11-b539-29d143ad3aa6','Rosa','Díaz Zambrano','1766778899','1991-04-20','F','Calle Roca 567, Quito','0991234509','rosa.diaz@email.com','2025-11-01T08:00:00Z','2025-11-01T08:00:00Z'),
('b0000001-0001-0000-0000-000000000010','5f932367-7db6-4e11-b539-29d143ad3aa6','Diego','Morales Pinto','1777889900','1996-08-11','M','Av. República E7-12, Quito','0991234510','diego.morales@email.com','2025-11-05T10:00:00Z','2025-11-05T10:00:00Z'),
('b0000001-0001-0000-0000-000000000011','5f932367-7db6-4e11-b539-29d143ad3aa6','Gabriela','Castro Mendoza','1788990011','1989-01-17','F','Calle Guayaquil N12-34, Quito','0991234511','gabriela.castro@email.com','2025-11-10T08:30:00Z','2025-11-10T08:30:00Z'),
('b0000001-0001-0000-0000-000000000012','5f932367-7db6-4e11-b539-29d143ad3aa6','Andrés','Vargas Cevallos','1799001122','1994-10-05','M','Av. Shyris N34-56, Quito','0991234512','andres.vargas@email.com','2025-11-15T09:00:00Z','2025-11-15T09:00:00Z'),
('b0000001-0001-0000-0000-000000000013','5f932367-7db6-4e11-b539-29d143ad3aa6','Patricia','Reyes Aguirre','1700112233','1986-07-29','F','Calle Venezuela N34-56, Quito','0991234513','patricia.reyes@email.com','2025-12-01T08:00:00Z','2025-12-01T08:00:00Z'),
('b0000001-0001-0000-0000-000000000014','5f932367-7db6-4e11-b539-29d143ad3aa6','Fernando','Espinoza Ríos','1711223344','1998-03-12','M','Av. Amazonas N45-78, Quito','0991234514','fernando.espinoza@email.com','2025-12-05T10:00:00Z','2025-12-05T10:00:00Z'),
('b0000001-0001-0000-0000-000000000015','5f932367-7db6-4e11-b539-29d143ad3aa6','Verónica','Guzmán Lara','1722334456','1990-11-23','F','Calle Sucre S12-34, Quito','0991234515','veronica.guzman@email.com','2025-12-10T08:30:00Z','2025-12-10T08:30:00Z'),
('b0000001-0001-0000-0000-000000000016','5f932367-7db6-4e11-b539-29d143ad3aa6','Miguel','Proaño Villacís','1733445567','1984-05-07','M','Av. Patria E5-23, Quito','0991234516','miguel.proano@email.com','2025-12-15T09:00:00Z','2025-12-15T09:00:00Z'),
('b0000001-0001-0000-0000-000000000017','5f932367-7db6-4e11-b539-29d143ad3aa6','Carmen','Ortega Bravo','1744556678','1997-09-30','F','Calle Benalcázar N23-45, Quito','0991234517','carmen.ortega@email.com','2026-01-05T08:00:00Z','2026-01-05T08:00:00Z'),
('b0000001-0001-0000-0000-000000000018','5f932367-7db6-4e11-b539-29d143ad3aa6','Ricardo','Borja Narváez','1755667789','1983-08-15','M','Av. Orellana E9-12, Quito','0991234518','ricardo.borja@email.com','2026-01-10T09:30:00Z','2026-01-10T09:30:00Z'),
('b0000001-0001-0000-0000-000000000019','5f932367-7db6-4e11-b539-29d143ad3aa6','Lucía','Paredes Toapanta','1766778890','1992-02-28','F','Calle Olmedo S8-34, Quito','0991234519','lucia.paredes@email.com','2026-01-15T08:00:00Z','2026-01-15T08:00:00Z'),
('b0000001-0001-0000-0000-000000000020','5f932367-7db6-4e11-b539-29d143ad3aa6','Javier','Cabrera León','1777889901','1999-12-10','M','Av. La Prensa N45-67, Quito','0991234520','javier.cabrera@email.com','2026-02-01T09:00:00Z','2026-02-01T09:00:00Z');

-- 20 Historias Clínicas Odontológicas (Status: 0=Draft, 1=Submitted, 2=Approved, 3=Rejected)
INSERT INTO "OdontologoClinicalHistories" ("Id","OdontologoId","PatientName","PatientIdNumber","Data","Status","CreatedAt","UpdatedAt") VALUES
('d0000001-0001-0000-0000-000000000001','5f932367-7db6-4e11-b539-29d143ad3aa6','María González Paredes','1712345678',
 '{"personal":{"firstName":"María","lastName":"González Paredes","idNumber":"1712345678","dateOfBirth":"1985-03-15","gender":"F","address":"Av. 10 de Agosto N24-123, Quito","phone":"0991234501","email":"maria.gonzalez@email.com"},"medicalHistory":{"allergies":"Penicilina","medications":"Ninguna","conditions":"Hipertensión leve"},"diagnosis":"Caries dental en premolar superior derecho (pieza 14)","treatment":"Obturación con resina compuesta","observations":"Paciente colaboradora, se recomienda control en 6 meses","odontogram":{"tooth14":{"status":"caries","treatment":"obturación"},"tooth26":{"status":"sano"},"tooth36":{"status":"sano"}}}',
 2,'2025-09-01T08:00:00Z','2025-09-15T10:00:00Z'),
('d0000001-0001-0000-0000-000000000002','5f932367-7db6-4e11-b539-29d143ad3aa6','Juan Pérez Salazar','1798765432',
 '{"personal":{"firstName":"Juan","lastName":"Pérez Salazar","idNumber":"1798765432","dateOfBirth":"1990-07-22","gender":"M","address":"Calle García Moreno 456, Quito","phone":"0991234502","email":"juan.perez@email.com"},"medicalHistory":{"allergies":"Ninguna","medications":"Losartán 50mg","conditions":"Ninguna"},"diagnosis":"Gingivitis generalizada leve","treatment":"Profilaxis dental completa y aplicación de flúor","observations":"Mejorar técnica de cepillado, usar hilo dental diario","odontogram":{"tooth11":{"status":"sano"},"tooth21":{"status":"sano"},"tooth46":{"status":"obturado"}}}',
 2,'2025-09-05T09:00:00Z','2025-09-20T11:00:00Z'),
('d0000001-0001-0000-0000-000000000003','5f932367-7db6-4e11-b539-29d143ad3aa6','Ana Martínez Vega','1722334455',
 '{"personal":{"firstName":"Ana","lastName":"Martínez Vega","idNumber":"1722334455","dateOfBirth":"1988-11-30","gender":"F","phone":"0991234503"},"medicalHistory":{"allergies":"Látex","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Pulpitis irreversible en molar inferior izquierdo (pieza 36)","treatment":"Endodoncia y corona definitiva","observations":"Tratamiento en dos sesiones, antibiótico previo","odontogram":{"tooth36":{"status":"pulpitis","treatment":"endodoncia"},"tooth16":{"status":"amalgama"}}}',
 2,'2025-09-10T10:00:00Z','2025-10-01T09:00:00Z'),
('d0000001-0001-0000-0000-000000000004','5f932367-7db6-4e11-b539-29d143ad3aa6','Carlos López Herrera','1754432211',
 '{"personal":{"firstName":"Carlos","lastName":"López Herrera","idNumber":"1754432211","dateOfBirth":"1995-05-18","gender":"M","phone":"0991234504"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Diabetes tipo 2"},"diagnosis":"Periodontitis crónica moderada","treatment":"Raspado y alisado radicular por cuadrantes","observations":"Control metabólico necesario, derivar a endocrinólogo","odontogram":{"tooth17":{"status":"movilidad grado I"},"tooth27":{"status":"movilidad grado I"},"tooth37":{"status":"sano"}}}',
 2,'2025-09-15T08:30:00Z','2025-10-05T10:00:00Z'),
('d0000001-0001-0000-0000-000000000005','5f932367-7db6-4e11-b539-29d143ad3aa6','Laura Ramírez Torres','1767788990',
 '{"personal":{"firstName":"Laura","lastName":"Ramírez Torres","idNumber":"1767788990","dateOfBirth":"1992-09-25","gender":"F","phone":"0991234505"},"medicalHistory":{"allergies":"Ninguna","medications":"Anticonceptivos","conditions":"Ninguna"},"diagnosis":"Bruxismo severo con desgaste dental","treatment":"Placa de relajación nocturna y ajuste oclusal","observations":"Control de estrés recomendado","odontogram":{"tooth11":{"status":"desgaste"},"tooth21":{"status":"desgaste"},"tooth31":{"status":"desgaste"}}}',
 2,'2025-10-01T09:00:00Z','2025-10-15T10:30:00Z'),
('d0000001-0001-0000-0000-000000000006','5f932367-7db6-4e11-b539-29d143ad3aa6','Pedro Sánchez Mora','1733445566',
 '{"personal":{"firstName":"Pedro","lastName":"Sánchez Mora","idNumber":"1733445566","dateOfBirth":"1987-02-14","gender":"M","phone":"0991234506"},"medicalHistory":{"allergies":"Sulfonamidas","medications":"Ninguna","conditions":"Asma leve"},"diagnosis":"Fractura dental pieza 11 por traumatismo","treatment":"Reconstrucción con resina y evaluación de vitalidad pulpar","observations":"Control radiográfico en 3 meses","odontogram":{"tooth11":{"status":"fractura","treatment":"reconstrucción"}}}',
 2,'2025-10-05T10:00:00Z','2025-10-20T09:00:00Z'),
('d0000001-0001-0000-0000-000000000007','5f932367-7db6-4e11-b539-29d143ad3aa6','Sofía Herrera Ruiz','1744556677',
 '{"personal":{"firstName":"Sofía","lastName":"Herrera Ruiz","idNumber":"1744556677","dateOfBirth":"1993-06-08","gender":"F","phone":"0991234507"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Embarazo 5 meses"},"diagnosis":"Gingivitis del embarazo y caries incipientes","treatment":"Profilaxis suave, obturación preventiva, educación en higiene","observations":"Evitar radiografías, tratamiento conservador","odontogram":{"tooth15":{"status":"caries incipiente"},"tooth25":{"status":"caries incipiente"}}}',
 2,'2025-10-10T08:00:00Z','2025-10-25T11:00:00Z'),
('d0000001-0001-0000-0000-000000000008','5f932367-7db6-4e11-b539-29d143ad3aa6','Roberto Fernández Cruz','1755667788',
 '{"personal":{"firstName":"Roberto","lastName":"Fernández Cruz","idNumber":"1755667788","dateOfBirth":"1982-12-03","gender":"M","phone":"0991234508"},"medicalHistory":{"allergies":"Yodo","medications":"Metformina","conditions":"Diabetes tipo 2, Hipertensión"},"diagnosis":"Múltiples caries y necesidad de prótesis parcial","treatment":"Plan: extracciones piezas 16,26, obturaciones varias, prótesis parcial removible","observations":"Paciente con riesgo sistémico, interconsulta médica previa","odontogram":{"tooth16":{"status":"restos radiculares","treatment":"extracción"},"tooth26":{"status":"restos radiculares","treatment":"extracción"},"tooth35":{"status":"caries"}}}',
 1,'2025-10-15T09:30:00Z','2025-10-15T09:30:00Z'),
('d0000001-0001-0000-0000-000000000009','5f932367-7db6-4e11-b539-29d143ad3aa6','Rosa Díaz Zambrano','1766778899',
 '{"personal":{"firstName":"Rosa","lastName":"Díaz Zambrano","idNumber":"1766778899","dateOfBirth":"1991-04-20","gender":"F","phone":"0991234509"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Terceros molares impactados bilaterales","treatment":"Cirugía de terceros molares bajo anestesia local","observations":"Postoperatorio con antibióticos y antiinflamatorios","odontogram":{"tooth18":{"status":"impactado","treatment":"extracción"},"tooth28":{"status":"impactado","treatment":"extracción"}}}',
 2,'2025-11-01T08:00:00Z','2025-11-15T10:00:00Z'),
('d0000001-0001-0000-0000-000000000010','5f932367-7db6-4e11-b539-29d143ad3aa6','Diego Morales Pinto','1777889900',
 '{"personal":{"firstName":"Diego","lastName":"Morales Pinto","idNumber":"1777889900","dateOfBirth":"1996-08-11","gender":"M","phone":"0991234510"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Maloclusión clase II, apiñamiento anterior","treatment":"Referencia a ortodoncia para brackets","observations":"Paciente motivado para tratamiento ortodóntico","odontogram":{"tooth11":{"status":"apiñado"},"tooth21":{"status":"apiñado"},"tooth12":{"status":"rotado"}}}',
 2,'2025-11-05T10:00:00Z','2025-11-20T09:30:00Z'),
('d0000001-0001-0000-0000-000000000011','5f932367-7db6-4e11-b539-29d143ad3aa6','Gabriela Castro Mendoza','1788990011',
 '{"personal":{"firstName":"Gabriela","lastName":"Castro Mendoza","idNumber":"1788990011","dateOfBirth":"1989-01-17","gender":"F","phone":"0991234511"},"medicalHistory":{"allergies":"AINEs","medications":"Omeprazol","conditions":"Gastritis crónica"},"diagnosis":"Erosión dental por reflujo gastroesofágico","treatment":"Sellantes y reconstrucción de superficies erosionadas","observations":"Interconsulta con gastroenterólogo","odontogram":{"tooth11":{"status":"erosión"},"tooth21":{"status":"erosión"},"tooth31":{"status":"erosión"},"tooth41":{"status":"erosión"}}}',
 2,'2025-11-10T08:30:00Z','2025-11-25T10:00:00Z'),
('d0000001-0001-0000-0000-000000000012','5f932367-7db6-4e11-b539-29d143ad3aa6','Andrés Vargas Cevallos','1799001122',
 '{"personal":{"firstName":"Andrés","lastName":"Vargas Cevallos","idNumber":"1799001122","dateOfBirth":"1994-10-05","gender":"M","phone":"0991234512"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Caries cervical en premolares superiores","treatment":"Obturaciones con ionómero de vidrio","observations":"Cepillado agresivo es la causa, cambiar técnica","odontogram":{"tooth14":{"status":"caries cervical"},"tooth15":{"status":"caries cervical"},"tooth24":{"status":"caries cervical"}}}',
 0,'2025-11-15T09:00:00Z','2025-11-15T09:00:00Z'),
('d0000001-0001-0000-0000-000000000013','5f932367-7db6-4e11-b539-29d143ad3aa6','Patricia Reyes Aguirre','1700112233',
 '{"personal":{"firstName":"Patricia","lastName":"Reyes Aguirre","idNumber":"1700112233","dateOfBirth":"1986-07-29","gender":"F","phone":"0991234513"},"medicalHistory":{"allergies":"Anestésicos tipo éster","medications":"Levotiroxina","conditions":"Hipotiroidismo"},"diagnosis":"Disfunción temporomandibular (ATM)","treatment":"Placa de descarga, fisioterapia mandibular","observations":"Usar anestésicos tipo amida únicamente","odontogram":{"atm":{"status":"disfunción","clicking":"bilateral"}}}',
 2,'2025-12-01T08:00:00Z','2025-12-15T09:30:00Z'),
('d0000001-0001-0000-0000-000000000014','5f932367-7db6-4e11-b539-29d143ad3aa6','Fernando Espinoza Ríos','1711223344',
 '{"personal":{"firstName":"Fernando","lastName":"Espinoza Ríos","idNumber":"1711223344","dateOfBirth":"1998-03-12","gender":"M","phone":"0991234514"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Blanqueamiento dental solicitado, dentición sana","treatment":"Blanqueamiento dental profesional en consultorio","observations":"Evitar alimentos pigmentantes por 2 semanas","odontogram":{"general":{"status":"sano","color":"A3","tratamiento":"blanqueamiento"}}}',
 2,'2025-12-05T10:00:00Z','2025-12-20T10:00:00Z'),
('d0000001-0001-0000-0000-000000000015','5f932367-7db6-4e11-b539-29d143ad3aa6','Verónica Guzmán Lara','1722334456',
 '{"personal":{"firstName":"Verónica","lastName":"Guzmán Lara","idNumber":"1722334456","dateOfBirth":"1990-11-23","gender":"F","phone":"0991234515"},"medicalHistory":{"allergies":"Ninguna","medications":"Sertralina","conditions":"Trastorno de ansiedad"},"diagnosis":"Caries recurrente bajo restauración antigua pieza 46","treatment":"Retiro de restauración, remoción de caries, nueva obturación","observations":"Paciente ansiosa, considerar sedación consciente","odontogram":{"tooth46":{"status":"caries recurrente","treatment":"re-obturación"}}}',
 2,'2025-12-10T08:30:00Z','2025-12-28T09:00:00Z'),
('d0000001-0001-0000-0000-000000000016','5f932367-7db6-4e11-b539-29d143ad3aa6','Miguel Proaño Villacís','1733445567',
 '{"personal":{"firstName":"Miguel","lastName":"Proaño Villacís","idNumber":"1733445567","dateOfBirth":"1984-05-07","gender":"M","phone":"0991234516"},"medicalHistory":{"allergies":"Eritromicina","medications":"Warfarina","conditions":"Fibrilación auricular"},"diagnosis":"Absceso periapical pieza 36","treatment":"Drenaje de absceso, antibioterapia, endodoncia posterior","observations":"IMPORTANTE: Paciente anticoagulado, coordinar con cardiólogo para procedimientos invasivos","odontogram":{"tooth36":{"status":"absceso periapical","treatment":"drenaje + endodoncia"}}}',
 1,'2025-12-15T09:00:00Z','2025-12-15T09:00:00Z'),
('d0000001-0001-0000-0000-000000000017','5f932367-7db6-4e11-b539-29d143ad3aa6','Carmen Ortega Bravo','1744556678',
 '{"personal":{"firstName":"Carmen","lastName":"Ortega Bravo","idNumber":"1744556678","dateOfBirth":"1997-09-30","gender":"F","phone":"0991234517"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Diastema central superior, paciente solicita corrección estética","treatment":"Carillas de porcelana en piezas 11 y 21","observations":"Tomada de color y temporales colocados","odontogram":{"tooth11":{"status":"diastema","treatment":"carilla"},"tooth21":{"status":"diastema","treatment":"carilla"}}}',
 2,'2026-01-05T08:00:00Z','2026-01-20T10:00:00Z'),
('d0000001-0001-0000-0000-000000000018','5f932367-7db6-4e11-b539-29d143ad3aa6','Ricardo Borja Narváez','1755667789',
 '{"personal":{"firstName":"Ricardo","lastName":"Borja Narváez","idNumber":"1755667789","dateOfBirth":"1983-08-15","gender":"M","phone":"0991234518"},"medicalHistory":{"allergies":"Cefalosporinas","medications":"Atorvastatina","conditions":"Hipercolesterolemia"},"diagnosis":"Implante dental pieza 46 perdida por fractura","treatment":"Implante oseointegrado y corona sobre implante","observations":"Fase quirúrgica completada, esperar oseointegración 4 meses","odontogram":{"tooth46":{"status":"ausente","treatment":"implante"}}}',
 2,'2026-01-10T09:30:00Z','2026-02-01T10:00:00Z'),
('d0000001-0001-0000-0000-000000000019','5f932367-7db6-4e11-b539-29d143ad3aa6','Lucía Paredes Toapanta','1766778890',
 '{"personal":{"firstName":"Lucía","lastName":"Paredes Toapanta","idNumber":"1766778890","dateOfBirth":"1992-02-28","gender":"F","phone":"0991234519"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Halitosis de origen dental, cálculo supragingival abundante","treatment":"Profilaxis profunda, instrucción de higiene oral","observations":"Seguimiento mensual por 3 meses","odontogram":{"general":{"status":"cálculo supragingival","tratamiento":"profilaxis"}}}',
 0,'2026-01-15T08:00:00Z','2026-01-15T08:00:00Z'),
('d0000001-0001-0000-0000-000000000020','5f932367-7db6-4e11-b539-29d143ad3aa6','Javier Cabrera León','1777889901',
 '{"personal":{"firstName":"Javier","lastName":"Cabrera León","idNumber":"1777889901","dateOfBirth":"1999-12-10","gender":"M","phone":"0991234520"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Evaluación inicial, dentición sana sin patología","treatment":"Profilaxis preventiva y aplicación de sellantes","observations":"Paciente joven con buena higiene, control anual","odontogram":{"general":{"status":"sano"}}}',
 2,'2026-02-01T09:00:00Z','2026-02-10T10:00:00Z');


-- ############################################################################
-- PARTE 2: medicsys_academico (AcademicPatients + AcademicClinicalHistories)
-- ############################################################################
\connect medicsys_academico

-- Usuarios:
-- profesor@medicsys.com  = 668f1e82-ac47-44c1-b311-fb132d60e989
-- estudiante1@medicsys.com = fde483ba-efda-4c0b-ba2b-65f426130df8
-- estudiante2@medicsys.com = d443c18f-a661-4f57-94eb-9023c528deea
-- estudiante3@medicsys.com = df879bdb-a1c5-42d2-9118-44fa0ae11399

-- 20 Pacientes Académicos
INSERT INTO "AcademicPatients" ("Id","FirstName","LastName","IdNumber","DateOfBirth","Gender","Phone","Email","Address","BloodType","Allergies","MedicalConditions","EmergencyContact","EmergencyPhone","CreatedAt","UpdatedAt","CreatedByProfessorId") VALUES
('a0000001-0001-0000-0000-000000000001','Martín','Aguayo Salcedo','1801234501','2000-03-10T00:00:00Z','M','0981234501','martin.aguayo@email.com','Av. De los Granados E12-34, Quito','O+','Ninguna','Ninguna','Elena Salcedo','0981234590','2025-09-01T08:00:00Z','2025-09-01T08:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000002','Valentina','Bravo Cevallos','1801234502','1998-07-22T00:00:00Z','F','0981234502','valentina.bravo@email.com','Calle Luis Cordero N45-23, Quito','A+','Penicilina','Ninguna','Carlos Bravo','0981234591','2025-09-05T09:00:00Z','2025-09-05T09:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000003','Sebastián','Calderón Durán','1801234503','2001-11-15T00:00:00Z','M','0981234503','sebastian.calderon@email.com','Av. República del Salvador N34-56, Quito','B+','Ninguna','Asma leve','María Durán','0981234592','2025-09-10T10:00:00Z','2025-09-10T10:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000004','Isabella','Dávila Estrella','1801234504','1999-05-03T00:00:00Z','F','0981234504','isabella.davila@email.com','Calle Whimper E7-23, Quito','AB+','Sulfonamidas','Ninguna','Jorge Dávila','0981234593','2025-09-15T08:30:00Z','2025-09-15T08:30:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000005','Emilio','Fuentes Gallardo','1801234505','2002-01-20T00:00:00Z','M','0981234505','emilio.fuentes@email.com','Av. Colón E5-34, Quito','O-','Ninguna','Ninguna','Rosa Gallardo','0981234594','2025-10-01T09:00:00Z','2025-10-01T09:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000006','Camila','Gavilanes Hidalgo','1801234506','1997-08-14T00:00:00Z','F','0981234506','camila.gavilanes@email.com','Calle Juan León Mera N23-45, Quito','A-','Látex','Hipotiroidismo','Pedro Gavilanes','0981234595','2025-10-05T10:00:00Z','2025-10-05T10:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000007','Nicolás','Haro Ibarra','1801234507','2000-12-28T00:00:00Z','M','0981234507','nicolas.haro@email.com','Av. El Inca E12-45, Quito','B-','Ninguna','Ninguna','Lucía Ibarra','0981234596','2025-10-10T08:00:00Z','2025-10-10T08:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000008','Renata','Jaramillo Korol','1801234508','1996-04-05T00:00:00Z','F','0981234508','renata.jaramillo@email.com','Calle Ulloa N12-34, Quito','AB-','AINEs','Anemia leve','Diego Jaramillo','0981234597','2025-10-15T09:30:00Z','2025-10-15T09:30:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000009','Matías','Lema Muñoz','1801234509','2001-06-17T00:00:00Z','M','0981234509','matias.lema@email.com','Av. Galo Plaza Lasso N45-78, Quito','O+','Ninguna','Ninguna','Sandra Muñoz','0981234598','2025-11-01T08:00:00Z','2025-11-01T08:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000010','Antonella','Navarrete Ojeda','1801234510','1999-09-23T00:00:00Z','F','0981234510','antonella.navarrete@email.com','Calle Versalles N12-56, Quito','A+','Eritromicina','Ninguna','Manuel Navarrete','0981234599','2025-11-05T10:00:00Z','2025-11-05T10:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000011','Daniel','Oña Pacheco','1801234511','2000-02-11T00:00:00Z','M','0981234511','daniel.ona@email.com','Av. Mariana de Jesús E7-89, Quito','B+','Ninguna','Diabetes tipo 1','Ana Pacheco','0981234600','2025-11-10T08:30:00Z','2025-11-10T08:30:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000012','Ariana','Peñafiel Quezada','1801234512','1998-10-30T00:00:00Z','F','0981234512','ariana.penafiel@email.com','Calle Mercadillo S4-56, Quito','O-','Metronidazol','Ninguna','Luis Peñafiel','0981234601','2025-11-15T09:00:00Z','2025-11-15T09:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000013','Samuel','Quintero Romero','1801234513','2001-04-07T00:00:00Z','M','0981234513','samuel.quintero@email.com','Av. Rodrigo de Chávez S12-34, Quito','A+','Ninguna','Ninguna','Teresa Romero','0981234602','2025-12-01T08:00:00Z','2025-12-01T08:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000014','Luciana','Salazar Torres','1801234514','1997-01-19T00:00:00Z','F','0981234514','luciana.salazar@email.com','Calle Yaguachi E9-23, Quito','B-','Tetraciclinas','Epilepsia controlada','Roberto Salazar','0981234603','2025-12-05T10:00:00Z','2025-12-05T10:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000015','Tomás','Ulloa Vaca','1801234515','2002-08-25T00:00:00Z','M','0981234515','tomas.ulloa@email.com','Av. Atahualpa N34-67, Quito','AB+','Ninguna','Ninguna','Carmen Vaca','0981234604','2025-12-10T08:30:00Z','2025-12-10T08:30:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000016','Emma','Villacís Wray','1801234516','1999-12-01T00:00:00Z','F','0981234516','emma.villacis@email.com','Calle Pasaje N23-45, Quito','O+','Yodo','Ninguna','Andrés Villacís','0981234605','2025-12-15T09:00:00Z','2025-12-15T09:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000017','Joaquín','Yépez Zambrano','1801234517','2000-05-13T00:00:00Z','M','0981234517','joaquin.yepez@email.com','Av. Pichincha E5-12, Quito','A-','Ninguna','Rinitis alérgica','Gloria Zambrano','0981234606','2026-01-05T08:00:00Z','2026-01-05T08:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000018','Sofía','Acosta Benítez','1801234518','1996-03-08T00:00:00Z','F','0981234518','sofia.acosta@email.com','Calle Cuero y Caicedo S8-23, Quito','B+','Amoxicilina','Ninguna','Patricia Benítez','0981234607','2026-01-10T09:30:00Z','2026-01-10T09:30:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000019','Alejandro','Borrero Cárdenas','1801234519','2001-09-22T00:00:00Z','M','0981234519','alejandro.borrero@email.com','Av. La Gasca N34-12, Quito','O+','Ninguna','Ninguna','Fernando Borrero','0981234608','2026-01-15T08:00:00Z','2026-01-15T08:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989'),
('a0000001-0001-0000-0000-000000000020','Valeria','Córdova Delgado','1801234520','1998-11-16T00:00:00Z','F','0981234520','valeria.cordova@email.com','Calle Isla Seymour N45-23, Quito','A+','Ninguna','Migraña crónica','Ricardo Córdova','0981234609','2026-02-01T09:00:00Z','2026-02-01T09:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989');

-- 20 Historias Clínicas Académicas (distribuidas entre 3 estudiantes)
-- Status: 0=Draft, 1=Submitted, 2=Approved, 3=Rejected
-- Estudiante 1: 7 historias (fde483ba), Estudiante 2: 7 historias (d443c18f), Estudiante 3: 6 historias (df879bdb)
INSERT INTO "AcademicClinicalHistories" ("Id","StudentId","ReviewedByProfessorId","Data","Status","ProfessorComments","ReviewedAt","CreatedAt","UpdatedAt") VALUES
-- Estudiante 1
('e0000001-0001-0000-0000-000000000001','fde483ba-efda-4c0b-ba2b-65f426130df8','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Martín","lastName":"Aguayo Salcedo","idNumber":"1801234501","dateOfBirth":"2000-03-10","gender":"M","phone":"0981234501"},"dentalHistory":{"lastVisit":"2025-06-15","brushingFrequency":"3 veces al día","usesFloss":true},"diagnosis":"Caries oclusal en primer molar inferior derecho (pieza 46)","treatment":"Obturación con resina compuesta A2","odontogram":{"tooth46":{"status":"caries oclusal","treatment":"obturación"},"tooth36":{"status":"sano"}}}',
 2,'Buen diagnóstico y plan de tratamiento. Técnica de obturación correcta. Aprobado.','2025-09-15T10:00:00Z','2025-09-01T08:00:00Z','2025-09-15T10:00:00Z'),
('e0000001-0001-0000-0000-000000000002','fde483ba-efda-4c0b-ba2b-65f426130df8','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Valentina","lastName":"Bravo Cevallos","idNumber":"1801234502","dateOfBirth":"1998-07-22","gender":"F","phone":"0981234502"},"dentalHistory":{"lastVisit":"2025-03-20","brushingFrequency":"2 veces al día","usesFloss":false},"diagnosis":"Gingivitis marginal generalizada","treatment":"Profilaxis dental y educación en técnicas de higiene oral","odontogram":{"general":{"status":"gingivitis marginal","placa":"moderada"}}}',
 2,'Diagnóstico correcto. Mejorar las instrucciones de higiene al paciente. Aprobado.','2025-09-20T11:00:00Z','2025-09-05T09:00:00Z','2025-09-20T11:00:00Z'),
('e0000001-0001-0000-0000-000000000003','fde483ba-efda-4c0b-ba2b-65f426130df8','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Sebastián","lastName":"Calderón Durán","idNumber":"1801234503","dateOfBirth":"2001-11-15","gender":"M","phone":"0981234503"},"dentalHistory":{"lastVisit":"2025-01-10","brushingFrequency":"2 veces al día","usesFloss":false},"diagnosis":"Caries mesial en segundo premolar superior izquierdo (pieza 25)","treatment":"Obturación con ionómero de vidrio","odontogram":{"tooth25":{"status":"caries mesial","treatment":"obturación"}}}',
 2,'Procedimiento correcto. Considerar resina en lugar de ionómero para mejor estética. Aprobado.','2025-10-05T09:30:00Z','2025-09-10T10:00:00Z','2025-10-05T09:30:00Z'),
('e0000001-0001-0000-0000-000000000004','fde483ba-efda-4c0b-ba2b-65f426130df8','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Isabella","lastName":"Dávila Estrella","idNumber":"1801234504","dateOfBirth":"1999-05-03","gender":"F","phone":"0981234504"},"dentalHistory":{"lastVisit":"2024-12-01","brushingFrequency":"1 vez al día","usesFloss":false},"diagnosis":"Múltiples caries interproximales en sector posterior","treatment":"Plan de tratamiento: obturaciones múltiples en 3 sesiones, sellantes preventivos","odontogram":{"tooth15":{"status":"caries distal"},"tooth16":{"status":"caries mesial"},"tooth25":{"status":"caries mesial"},"tooth26":{"status":"caries oclusal"}}}',
 3,'Diagnóstico incompleto. Falta radiografía periapical para confirmar extensión de caries en pieza 16. Revisar y reenviar.','2025-10-10T10:00:00Z','2025-09-15T08:30:00Z','2025-10-10T10:00:00Z'),
('e0000001-0001-0000-0000-000000000005','fde483ba-efda-4c0b-ba2b-65f426130df8',NULL,
 '{"personal":{"firstName":"Emilio","lastName":"Fuentes Gallardo","idNumber":"1801234505","dateOfBirth":"2002-01-20","gender":"M","phone":"0981234505"},"dentalHistory":{"lastVisit":"2025-08-01","brushingFrequency":"3 veces al día","usesFloss":true},"diagnosis":"Fluorosis dental leve, manchas blancas opacas","treatment":"Microabrasión del esmalte con ácido fosfórico","odontogram":{"tooth11":{"status":"fluorosis leve"},"tooth21":{"status":"fluorosis leve"},"tooth12":{"status":"fluorosis leve"},"tooth22":{"status":"fluorosis leve"}}}',
 1,NULL,NULL,'2025-10-01T09:00:00Z','2025-10-01T09:00:00Z'),
('e0000001-0001-0000-0000-000000000006','fde483ba-efda-4c0b-ba2b-65f426130df8','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Camila","lastName":"Gavilanes Hidalgo","idNumber":"1801234506","dateOfBirth":"1997-08-14","gender":"F","phone":"0981234506"},"dentalHistory":{"lastVisit":"2025-05-20","brushingFrequency":"2 veces al día","usesFloss":true},"diagnosis":"Pieza 48 semiretenida, pericoronaritis recurrente","treatment":"Exodoncia de tercer molar inferior derecho (pieza 48)","odontogram":{"tooth48":{"status":"semiretenido","tratamiento":"exodoncia"}}}',
 2,'Indicación quirúrgica correcta. Buena técnica de colgajo y sutura. Aprobado.','2025-11-01T10:00:00Z','2025-10-05T10:00:00Z','2025-11-01T10:00:00Z'),
('e0000001-0001-0000-0000-000000000007','fde483ba-efda-4c0b-ba2b-65f426130df8',NULL,
 '{"personal":{"firstName":"Nicolás","lastName":"Haro Ibarra","idNumber":"1801234507","dateOfBirth":"2000-12-28","gender":"M","phone":"0981234507"},"dentalHistory":{"lastVisit":"2025-04-10","brushingFrequency":"2 veces al día","usesFloss":false},"diagnosis":"En evaluación - posible bruxismo","treatment":"Pendiente evaluación completa","odontogram":{}}',
 0,NULL,NULL,'2025-10-10T08:00:00Z','2025-10-10T08:00:00Z'),

-- Estudiante 2
('e0000001-0001-0000-0000-000000000008','d443c18f-a661-4f57-94eb-9023c528deea','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Renata","lastName":"Jaramillo Korol","idNumber":"1801234508","dateOfBirth":"1996-04-05","gender":"F","phone":"0981234508"},"dentalHistory":{"lastVisit":"2025-07-15","brushingFrequency":"2 veces al día","usesFloss":true},"diagnosis":"Necrosis pulpar pieza 21 por traumatismo antiguo","treatment":"Tratamiento de conducto (endodoncia) pieza 21","odontogram":{"tooth21":{"status":"necrosis pulpar","treatment":"endodoncia"}}}',
 2,'Excelente manejo del caso complejo. Endodoncia bien realizada. Aprobado.','2025-11-10T09:00:00Z','2025-10-15T09:30:00Z','2025-11-10T09:00:00Z'),
('e0000001-0001-0000-0000-000000000009','d443c18f-a661-4f57-94eb-9023c528deea','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Matías","lastName":"Lema Muñoz","idNumber":"1801234509","dateOfBirth":"2001-06-17","gender":"M","phone":"0981234509"},"dentalHistory":{"lastVisit":"2025-02-01","brushingFrequency":"1 vez al día","usesFloss":false},"diagnosis":"Cálculo dental supragingival abundante con gingivitis asociada","treatment":"Destartaje ultrasónico y pulido, reeducación en higiene","odontogram":{"general":{"status":"cálculo abundante","tratamiento":"destartaje"}}}',
 2,'Buen trabajo de remoción de cálculo. La técnica de pulido puede mejorar. Aprobado.','2025-11-20T10:00:00Z','2025-11-01T08:00:00Z','2025-11-20T10:00:00Z'),
('e0000001-0001-0000-0000-000000000010','d443c18f-a661-4f57-94eb-9023c528deea','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Antonella","lastName":"Navarrete Ojeda","idNumber":"1801234510","dateOfBirth":"1999-09-23","gender":"F","phone":"0981234510"},"dentalHistory":{"lastVisit":"2025-06-01","brushingFrequency":"2 veces al día","usesFloss":true},"diagnosis":"Diente supernumerario (mesiodens) entre piezas 11 y 21","treatment":"Exodoncia del mesiodens y seguimiento ortodóntico","odontogram":{"mesiodens":{"status":"supernumerario","treatment":"exodoncia"}}}',
 2,'Caso interesante bien documentado. Radiografía oclusal presentada. Aprobado.','2025-12-01T09:30:00Z','2025-11-05T10:00:00Z','2025-12-01T09:30:00Z'),
('e0000001-0001-0000-0000-000000000011','d443c18f-a661-4f57-94eb-9023c528deea','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Daniel","lastName":"Oña Pacheco","idNumber":"1801234511","dateOfBirth":"2000-02-11","gender":"M","phone":"0981234511"},"dentalHistory":{"lastVisit":"2025-08-20","brushingFrequency":"3 veces al día","usesFloss":true},"diagnosis":"Hipersensibilidad dentinaria cervical generalizada","treatment":"Aplicación de barniz de flúor y desensibilizante, indicar pasta desensibilizante","odontogram":{"tooth13":{"status":"sensibilidad cervical"},"tooth23":{"status":"sensibilidad cervical"},"tooth33":{"status":"sensibilidad cervical"},"tooth43":{"status":"sensibilidad cervical"}}}',
 2,'Correcto manejo conservador. Considerar ionómero de vidrio si persiste. Aprobado.','2025-12-10T10:00:00Z','2025-11-10T08:30:00Z','2025-12-10T10:00:00Z'),
('e0000001-0001-0000-0000-000000000012','d443c18f-a661-4f57-94eb-9023c528deea',NULL,
 '{"personal":{"firstName":"Ariana","lastName":"Peñafiel Quezada","idNumber":"1801234512","dateOfBirth":"1998-10-30","gender":"F","phone":"0981234512"},"dentalHistory":{"lastVisit":"2024-11-01","brushingFrequency":"1 vez al día","usesFloss":false},"diagnosis":"Estomatitis aftosa recurrente minor","treatment":"Tratamiento paliativo, enjuague con clorhexidina 0.12%","odontogram":{"mucosa":{"status":"úlceras aftosas","localización":"labio inferior y lengua"}}}',
 1,NULL,NULL,'2025-11-15T09:00:00Z','2025-11-15T09:00:00Z'),
('e0000001-0001-0000-0000-000000000013','d443c18f-a661-4f57-94eb-9023c528deea','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Samuel","lastName":"Quintero Romero","idNumber":"1801234513","dateOfBirth":"2001-04-07","gender":"M","phone":"0981234513"},"dentalHistory":{"lastVisit":"2025-09-15","brushingFrequency":"2 veces al día","usesFloss":false},"diagnosis":"Caries de biberón en paciente pediátrico (hermano menor del paciente principal)","treatment":"Coronas de acero inoxidable en molares temporales, educación a padres","odontogram":{"tooth54":{"status":"caries extensa"},"tooth64":{"status":"caries extensa"},"tooth74":{"status":"caries"},"tooth84":{"status":"caries"}}}',
 2,'Buen manejo pediátrico. Las coronas de acero están bien adaptadas. Aprobado.','2026-01-05T10:00:00Z','2025-12-01T08:00:00Z','2026-01-05T10:00:00Z'),
('e0000001-0001-0000-0000-000000000014','d443c18f-a661-4f57-94eb-9023c528deea',NULL,
 '{"personal":{"firstName":"Luciana","lastName":"Salazar Torres","idNumber":"1801234514","dateOfBirth":"1997-01-19","gender":"F","phone":"0981234514"},"dentalHistory":{"lastVisit":"2025-10-01","brushingFrequency":"2 veces al día","usesFloss":true},"diagnosis":"Pendiente de evaluación completa","treatment":"En proceso","odontogram":{}}',
 0,NULL,NULL,'2025-12-05T10:00:00Z','2025-12-05T10:00:00Z'),

-- Estudiante 3
('e0000001-0001-0000-0000-000000000015','df879bdb-a1c5-42d2-9118-44fa0ae11399','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Tomás","lastName":"Ulloa Vaca","idNumber":"1801234515","dateOfBirth":"2002-08-25","gender":"M","phone":"0981234515"},"dentalHistory":{"lastVisit":"2025-10-15","brushingFrequency":"3 veces al día","usesFloss":true},"diagnosis":"Sellantes profilácticos en molares permanentes recién erupcionados","treatment":"Aplicación de sellantes de fosas y fisuras en piezas 17, 27, 37, 47","odontogram":{"tooth17":{"status":"recién erupcionado","treatment":"sellante"},"tooth27":{"status":"recién erupcionado","treatment":"sellante"},"tooth37":{"status":"recién erupcionado","treatment":"sellante"},"tooth47":{"status":"recién erupcionado","treatment":"sellante"}}}',
 2,'Excelente indicación preventiva. Sellantes bien colocados con aislamiento adecuado. Aprobado.','2025-12-20T09:00:00Z','2025-12-10T08:30:00Z','2025-12-20T09:00:00Z'),
('e0000001-0001-0000-0000-000000000016','df879bdb-a1c5-42d2-9118-44fa0ae11399','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Emma","lastName":"Villacís Wray","idNumber":"1801234516","dateOfBirth":"1999-12-01","gender":"F","phone":"0981234516"},"dentalHistory":{"lastVisit":"2025-09-01","brushingFrequency":"2 veces al día","usesFloss":true},"diagnosis":"Quiste mucoso de retención en labio inferior","treatment":"Marsupialización y biopsia excisional","odontogram":{"mucosa":{"status":"quiste mucoso","localización":"labio inferior","tamaño":"8mm"}}}',
 2,'Buen manejo quirúrgico. Biopsia enviada correctamente. Resultado: mucocele. Aprobado.','2026-01-10T10:00:00Z','2025-12-15T09:00:00Z','2026-01-10T10:00:00Z'),
('e0000001-0001-0000-0000-000000000017','df879bdb-a1c5-42d2-9118-44fa0ae11399','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Joaquín","lastName":"Yépez Zambrano","idNumber":"1801234517","dateOfBirth":"2000-05-13","gender":"M","phone":"0981234517"},"dentalHistory":{"lastVisit":"2025-07-20","brushingFrequency":"2 veces al día","usesFloss":false},"diagnosis":"Fractura coronaria no complicada pieza 11","treatment":"Reconstrucción con resina compuesta estratificada","odontogram":{"tooth11":{"status":"fractura coronaria","treatment":"reconstrucción"}}}',
 3,'La reconstrucción no respeta la anatomía oclusal del diente. Repetir el procedimiento con mejor técnica de estratificación.','2026-01-15T10:00:00Z','2026-01-05T08:00:00Z','2026-01-15T10:00:00Z'),
('e0000001-0001-0000-0000-000000000018','df879bdb-a1c5-42d2-9118-44fa0ae11399',NULL,
 '{"personal":{"firstName":"Sofía","lastName":"Acosta Benítez","idNumber":"1801234518","dateOfBirth":"1996-03-08","gender":"F","phone":"0981234518"},"dentalHistory":{"lastVisit":"2025-11-01","brushingFrequency":"2 veces al día","usesFloss":true},"diagnosis":"Candidiasis oral (posible inmunosupresión)","treatment":"Nistatina tópica, referencia a medicina interna para estudios","odontogram":{"mucosa":{"status":"placas blancas","localización":"paladar y lengua","sospecha":"candidiasis"}}}',
 1,NULL,NULL,'2026-01-10T09:30:00Z','2026-01-10T09:30:00Z'),
('e0000001-0001-0000-0000-000000000019','df879bdb-a1c5-42d2-9118-44fa0ae11399','668f1e82-ac47-44c1-b311-fb132d60e989',
 '{"personal":{"firstName":"Alejandro","lastName":"Borrero Cárdenas","idNumber":"1801234519","dateOfBirth":"2001-09-22","gender":"M","phone":"0981234519"},"dentalHistory":{"lastVisit":"2025-12-01","brushingFrequency":"3 veces al día","usesFloss":true},"diagnosis":"Frenillo lingual corto (anquiloglosia)","treatment":"Frenectomía lingual con tijera y sutura reabsorbible","odontogram":{"frenillo":{"status":"corto","movilidad lingual":"restringida","treatment":"frenectomía"}}}',
 2,'Correcta indicación quirúrgica. Buena técnica de incisión y hemostasia. Aprobado.','2026-02-01T09:30:00Z','2026-01-15T08:00:00Z','2026-02-01T09:30:00Z'),
('e0000001-0001-0000-0000-000000000020','df879bdb-a1c5-42d2-9118-44fa0ae11399',NULL,
 '{"personal":{"firstName":"Valeria","lastName":"Córdova Delgado","idNumber":"1801234520","dateOfBirth":"1998-11-16","gender":"F","phone":"0981234520"},"dentalHistory":{"lastVisit":"2025-10-20","brushingFrequency":"2 veces al día","usesFloss":false},"diagnosis":"Evaluación inicial pendiente de completar","treatment":"En proceso de evaluación","odontogram":{}}',
 0,NULL,NULL,'2026-02-01T09:00:00Z','2026-02-01T09:00:00Z');


-- ############################################################################
-- PARTE 3: medicsys (Patients + ClinicalHistories)
-- ############################################################################
\connect medicsys

-- Usuarios estudiantes (en medicsys):
-- estudiante1@medicsys.com = fde483ba-efda-4c0b-ba2b-65f426130df8
-- estudiante2@medicsys.com = d443c18f-a661-4f57-94eb-9023c528deea
-- estudiante3@medicsys.com = df879bdb-a1c5-42d2-9118-44fa0ae11399
-- odontologo@medicsys.com = 5f932367-7db6-4e11-b539-29d143ad3aa6 (reviewer)
-- profesor@medicsys.com = 668f1e82-ac47-44c1-b311-fb132d60e989 (reviewer)

-- 10 Pacientes principales
INSERT INTO "Patients" ("Id","OdontologoId","FirstName","LastName","IdNumber","DateOfBirth","Gender","Address","Phone","Email","EmergencyContact","EmergencyPhone","Allergies","Medications","Diseases","BloodType","Notes","CreatedAt","UpdatedAt") VALUES
('c0000001-0001-0000-0000-000000000001','5f932367-7db6-4e11-b539-29d143ad3aa6','Elena','Mora Gutiérrez','1901234501','1988-04-12T00:00:00Z','F','Av. González Suárez N34-56, Quito','0971234501','elena.mora@email.com','Marco Mora','0971234590','Penicilina','Ninguna','Ninguna','A+','Paciente regular, control semestral','2025-09-01T08:00:00Z','2025-09-01T08:00:00Z'),
('c0000001-0001-0000-0000-000000000002','5f932367-7db6-4e11-b539-29d143ad3aa6','Francisco','Nieto Palacios','1901234502','1992-08-25T00:00:00Z','M','Calle Robles E7-34, Quito','0971234502','francisco.nieto@email.com','Laura Palacios','0971234591','Ninguna','Losartán','Hipertensión','O+','Control de presión antes de procedimientos','2025-09-15T09:00:00Z','2025-09-15T09:00:00Z'),
('c0000001-0001-0000-0000-000000000003','5f932367-7db6-4e11-b539-29d143ad3aa6','Gloria','Ojeda Rivera','1901234503','1985-01-30T00:00:00Z','F','Av. Amazonas N45-89, Quito','0971234503','gloria.ojeda@email.com','Roberto Rivera','0971234592','Látex, AINEs','Omeprazol','Gastritis crónica','B+','Usar guantes de nitrilo','2025-10-01T10:00:00Z','2025-10-01T10:00:00Z'),
('c0000001-0001-0000-0000-000000000004','5f932367-7db6-4e11-b539-29d143ad3aa6','Héctor','Paredes Quiroz','1901234504','1995-06-14T00:00:00Z','M','Calle Versalles E9-12, Quito','0971234504','hector.paredes@email.com','Ana Quiroz','0971234593','Ninguna','Metformina','Diabetes tipo 2','AB+','Control glucémico antes de procedimientos','2025-10-15T08:30:00Z','2025-10-15T08:30:00Z'),
('c0000001-0001-0000-0000-000000000005','5f932367-7db6-4e11-b539-29d143ad3aa6','Inés','Ramos Solano','1901234505','1990-11-07T00:00:00Z','F','Av. República E7-45, Quito','0971234505','ines.ramos@email.com','Carlos Solano','0971234594','Eritromicina','Levotiroxina','Hipotiroidismo','O-','Paciente ansiosa, considerar sedación','2025-11-01T09:00:00Z','2025-11-01T09:00:00Z'),
('c0000001-0001-0000-0000-000000000006','5f932367-7db6-4e11-b539-29d143ad3aa6','Jorge','Suárez Tapia','1901234506','1987-03-22T00:00:00Z','M','Calle Marchena N23-34, Quito','0971234506','jorge.suarez@email.com','María Tapia','0971234595','Ninguna','Ninguna','Ninguna','A-','Sin antecedentes relevantes','2025-11-15T10:00:00Z','2025-11-15T10:00:00Z'),
('c0000001-0001-0000-0000-000000000007','5f932367-7db6-4e11-b539-29d143ad3aa6','Karen','Terán Unda','1901234507','1993-09-18T00:00:00Z','F','Av. Shyris N45-67, Quito','0971234507','karen.teran@email.com','Luis Unda','0971234596','Sulfonamidas','Sertralina','Trastorno de ansiedad','B-','Manejo empático, evitar ruidos fuertes','2025-12-01T08:00:00Z','2025-12-01T08:00:00Z'),
('c0000001-0001-0000-0000-000000000008','5f932367-7db6-4e11-b539-29d143ad3aa6','Luis','Vásquez Wambold','1901234508','1998-12-05T00:00:00Z','M','Calle Portugal E9-23, Quito','0971234508','luis.vasquez@email.com','Carmen Wambold','0971234597','Ninguna','Ninguna','Ninguna','AB-','Paciente puntual y colaborador','2025-12-15T09:30:00Z','2025-12-15T09:30:00Z'),
('c0000001-0001-0000-0000-000000000009','5f932367-7db6-4e11-b539-29d143ad3aa6','Natalia','Yánez Zapata','1901234509','1991-05-29T00:00:00Z','F','Av. Eloy Alfaro N34-12, Quito','0971234509','natalia.yanez@email.com','Pedro Zapata','0971234598','Amoxicilina','Anticonceptivos','Ninguna','O+','Alergia documentada a aminopenicilinas','2026-01-05T08:00:00Z','2026-01-05T08:00:00Z'),
('c0000001-0001-0000-0000-000000000010','5f932367-7db6-4e11-b539-29d143ad3aa6','Óscar','Almeida Borja','1901234510','1984-07-10T00:00:00Z','M','Calle Reina Victoria N23-45, Quito','0971234510','oscar.almeida@email.com','Diana Borja','0971234599','Yodo','Warfarina','Fibrilación auricular','A+','ANTICOAGULADO - coordinar con cardiólogo','2026-01-15T09:00:00Z','2026-01-15T09:00:00Z');

-- 10 Historias Clínicas principales (ClinicalHistories - Status es TEXT: 'Draft','Submitted','Approved','Rejected')
INSERT INTO "ClinicalHistories" ("Id","StudentId","PatientId","Status","Data","CreatedAt","UpdatedAt","SubmittedAt","ReviewedById","ReviewedAt","ReviewNotes") VALUES
('f0000001-0001-0000-0000-000000000001','fde483ba-efda-4c0b-ba2b-65f426130df8','c0000001-0001-0000-0000-000000000001','Approved',
 '{"personal":{"firstName":"Elena","lastName":"Mora Gutiérrez","idNumber":"1901234501","dateOfBirth":"1988-04-12","gender":"F","phone":"0971234501","email":"elena.mora@email.com"},"medicalHistory":{"allergies":"Penicilina","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Caries oclusal pieza 36","treatment":"Obturación con resina A3","odontogram":{"tooth36":{"status":"caries oclusal","treatment":"obturación resina"}}}',
 '2025-09-01T08:00:00Z','2025-09-20T10:00:00Z','2025-09-10T08:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989','2025-09-20T10:00:00Z','Buen trabajo. Obturación bien adaptada. Aprobado.'),
('f0000001-0001-0000-0000-000000000002','fde483ba-efda-4c0b-ba2b-65f426130df8','c0000001-0001-0000-0000-000000000002','Approved',
 '{"personal":{"firstName":"Francisco","lastName":"Nieto Palacios","idNumber":"1901234502","dateOfBirth":"1992-08-25","gender":"M","phone":"0971234502"},"medicalHistory":{"allergies":"Ninguna","medications":"Losartán","conditions":"Hipertensión"},"diagnosis":"Gingivitis asociada a placa","treatment":"Profilaxis y educación en higiene oral","odontogram":{"general":{"status":"gingivitis","placa":"abundante"}}}',
 '2025-09-15T09:00:00Z','2025-10-05T11:00:00Z','2025-09-25T09:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989','2025-10-05T11:00:00Z','Correcto. Verificar presión arterial antes de cada cita.'),
('f0000001-0001-0000-0000-000000000003','d443c18f-a661-4f57-94eb-9023c528deea','c0000001-0001-0000-0000-000000000003','Approved',
 '{"personal":{"firstName":"Gloria","lastName":"Ojeda Rivera","idNumber":"1901234503","dateOfBirth":"1985-01-30","gender":"F","phone":"0971234503"},"medicalHistory":{"allergies":"Látex, AINEs","medications":"Omeprazol","conditions":"Gastritis crónica"},"diagnosis":"Erosión dental por reflujo ácido","treatment":"Obturaciones protectoras con ionómero de vidrio, interconsulta gastro","odontogram":{"tooth11":{"status":"erosión"},"tooth21":{"status":"erosión"}}}',
 '2025-10-01T10:00:00Z','2025-10-25T09:00:00Z','2025-10-15T10:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989','2025-10-25T09:00:00Z','Buena interconsulta. Usar guantes de nitrilo por alergia al látex.'),
('f0000001-0001-0000-0000-000000000004','d443c18f-a661-4f57-94eb-9023c528deea','c0000001-0001-0000-0000-000000000004','Approved',
 '{"personal":{"firstName":"Héctor","lastName":"Paredes Quiroz","idNumber":"1901234504","dateOfBirth":"1995-06-14","gender":"M","phone":"0971234504"},"medicalHistory":{"allergies":"Ninguna","medications":"Metformina","conditions":"Diabetes tipo 2"},"diagnosis":"Periodontitis moderada asociada a diabetes","treatment":"Raspado y alisado radicular, control metabólico","odontogram":{"tooth17":{"status":"bolsa periodontal 5mm"},"tooth27":{"status":"bolsa periodontal 4mm"}}}',
 '2025-10-15T08:30:00Z','2025-11-10T10:00:00Z','2025-10-28T08:30:00Z','5f932367-7db6-4e11-b539-29d143ad3aa6','2025-11-10T10:00:00Z','Manejo periodontal correcto. Importante el control de HbA1c.'),
('f0000001-0001-0000-0000-000000000005','d443c18f-a661-4f57-94eb-9023c528deea','c0000001-0001-0000-0000-000000000005','Submitted',
 '{"personal":{"firstName":"Inés","lastName":"Ramos Solano","idNumber":"1901234505","dateOfBirth":"1990-11-07","gender":"F","phone":"0971234505"},"medicalHistory":{"allergies":"Eritromicina","medications":"Levotiroxina","conditions":"Hipotiroidismo"},"diagnosis":"Urgencia dental: pulpitis aguda pieza 46","treatment":"Pulpotomía de emergencia, endodoncia programada","odontogram":{"tooth46":{"status":"pulpitis aguda","treatment":"pulpotomía + endodoncia"}}}',
 '2025-11-01T09:00:00Z','2025-11-15T09:00:00Z','2025-11-15T09:00:00Z',NULL,NULL,NULL),
('f0000001-0001-0000-0000-000000000006','df879bdb-a1c5-42d2-9118-44fa0ae11399','c0000001-0001-0000-0000-000000000006','Approved',
 '{"personal":{"firstName":"Jorge","lastName":"Suárez Tapia","idNumber":"1901234506","dateOfBirth":"1987-03-22","gender":"M","phone":"0971234506"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Exodoncia de tercer molar inferior retenido","treatment":"Cirugía de tercer molar bajo anestesia local troncular","odontogram":{"tooth38":{"status":"retenido horizontal","treatment":"exodoncia quirúrgica"}}}',
 '2025-11-15T10:00:00Z','2025-12-10T09:00:00Z','2025-11-25T10:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989','2025-12-10T09:00:00Z','Buena técnica quirúrgica y manejo postoperatorio adecuado.'),
('f0000001-0001-0000-0000-000000000007','df879bdb-a1c5-42d2-9118-44fa0ae11399','c0000001-0001-0000-0000-000000000007','Rejected',
 '{"personal":{"firstName":"Karen","lastName":"Terán Unda","idNumber":"1901234507","dateOfBirth":"1993-09-18","gender":"F","phone":"0971234507"},"medicalHistory":{"allergies":"Sulfonamidas","medications":"Sertralina","conditions":"Ansiedad"},"diagnosis":"Caries múltiples","treatment":"Obturaciones varias","odontogram":{"tooth15":{"status":"caries"},"tooth25":{"status":"caries"}}}',
 '2025-12-01T08:00:00Z','2026-01-05T10:00:00Z','2025-12-15T08:00:00Z','668f1e82-ac47-44c1-b311-fb132d60e989','2026-01-05T10:00:00Z','Diagnóstico muy vago. Especificar tipo de caries, extensión y plan detallado por pieza.'),
('f0000001-0001-0000-0000-000000000008','df879bdb-a1c5-42d2-9118-44fa0ae11399','c0000001-0001-0000-0000-000000000008','Approved',
 '{"personal":{"firstName":"Luis","lastName":"Vásquez Wambold","idNumber":"1901234508","dateOfBirth":"1998-12-05","gender":"M","phone":"0971234508"},"medicalHistory":{"allergies":"Ninguna","medications":"Ninguna","conditions":"Ninguna"},"diagnosis":"Retracción gingival localizada por cepillado agresivo","treatment":"Técnica de cepillado Bass modificada, cepillo suave, pasta desensibilizante","odontogram":{"tooth13":{"status":"retracción gingival clase I"},"tooth23":{"status":"retracción gingival clase I"}}}',
 '2025-12-15T09:30:00Z','2026-01-20T09:30:00Z','2026-01-05T09:30:00Z','5f932367-7db6-4e11-b539-29d143ad3aa6','2026-01-20T09:30:00Z','Buen enfoque conservador. Si progresa, considerar injerto gingival.'),
('f0000001-0001-0000-0000-000000000009','fde483ba-efda-4c0b-ba2b-65f426130df8','c0000001-0001-0000-0000-000000000009','Draft',
 '{"personal":{"firstName":"Natalia","lastName":"Yánez Zapata","idNumber":"1901234509","dateOfBirth":"1991-05-29","gender":"F","phone":"0971234509"},"medicalHistory":{"allergies":"Amoxicilina","medications":"Anticonceptivos","conditions":"Ninguna"},"diagnosis":"En evaluación","treatment":"Pendiente","odontogram":{}}',
 '2026-01-05T08:00:00Z','2026-01-05T08:00:00Z',NULL,NULL,NULL,NULL),
('f0000001-0001-0000-0000-000000000010','fde483ba-efda-4c0b-ba2b-65f426130df8','c0000001-0001-0000-0000-000000000010','Submitted',
 '{"personal":{"firstName":"Óscar","lastName":"Almeida Borja","idNumber":"1901234510","dateOfBirth":"1984-07-10","gender":"M","phone":"0971234510"},"medicalHistory":{"allergies":"Yodo","medications":"Warfarina","conditions":"Fibrilación auricular"},"diagnosis":"Necesidad protésica parcial superior","treatment":"Prótesis parcial removible acrílica superior, coordinar con cardiólogo por anticoagulación","odontogram":{"tooth14":{"status":"ausente"},"tooth15":{"status":"ausente"},"tooth24":{"status":"ausente"},"tooth25":{"status":"ausente"}}}',
 '2026-01-15T09:00:00Z','2026-02-05T09:00:00Z','2026-02-05T09:00:00Z',NULL,NULL,NULL);

-- Verificación de conteos
\connect medicsys_odontologia
SELECT 'Odontología - Pacientes' AS tabla, COUNT(*) AS total FROM "OdontologoPatients"
UNION ALL
SELECT 'Odontología - Historias', COUNT(*) FROM "OdontologoClinicalHistories";

\connect medicsys_academico
SELECT 'Académico - Pacientes' AS tabla, COUNT(*) AS total FROM "AcademicPatients"
UNION ALL
SELECT 'Académico - Historias', COUNT(*) FROM "AcademicClinicalHistories";

\connect medicsys
SELECT 'Principal - Pacientes' AS tabla, COUNT(*) AS total FROM "Patients"
UNION ALL
SELECT 'Principal - Historias', COUNT(*) FROM "ClinicalHistories";
