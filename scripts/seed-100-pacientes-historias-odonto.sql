-- ============================================================================
-- MEDICSYS: 100 Pacientes + 100 Historias Clínicas en medicsys_odontologia
-- ============================================================================
-- Este script expande la base de datos de odontología con 100 pacientes
-- y 100 historias clínicas realistas para una clínica odontológica.
-- Los primeros 20 pacientes/HC ya existen del seed anterior.
-- ============================================================================

\connect medicsys_odontologia

-- ============================================================================
-- PASO 1: Asegurar que los 20 pacientes originales existen (INSERT ON CONFLICT)
-- ============================================================================

-- Los 20 originales ya vienen del seed-pacientes-historias.sql
-- Aquí agregamos 80 pacientes nuevos (IDs 21-100)

DO $$
DECLARE
    v_odonto_id uuid := '5f932367-7db6-4e11-b539-29d143ad3aa6';
    v_first_names text[] := ARRAY[
        'Alejandra','Sebastián','Daniela','Martín','Camila','Nicolás','Valentina','Emilio',
        'Isabella','Tomás','Ariana','Samuel','Luciana','Joaquín','Renata','Matías',
        'Emma','Daniel','Sofía','Alejandro','Valeria','Leonardo','Paula','Adrián',
        'Natalia','Gabriel','Fernanda','Óscar','Catalina','Esteban','Melissa','Hugo',
        'Antonella','Rafael','Ivana','Simón','Constanza','Manuel','Regina','Felipe',
        'Lorena','Cristian','Teresa','Rodrigo','Milagros','Iván','Estrella','Francisco',
        'Ximena','Gustavo','Elena','Pablo','Helena','Sergio','Gloria','Raúl',
        'Bianca','César','Rocío','Héctor','Pilar','Alberto','Silvia','Mauricio',
        'Liliana','Julio','Graciela','Ernesto','Paola','Arturo','Estefanía','Fabián',
        'Florencia','Eduardo','Marcela','Ramiro','Angélica','Darío','Jazmín','Patricio'
    ];
    v_last_names text[] := ARRAY[
        'Aguayo Zambrano','Benítez Córdova','Calderón Durán','Dávila Estrella','Fuentes Gallardo',
        'Gavilanes Hidalgo','Haro Ibarra','Jaramillo Korol','Lema Muñoz','Navarrete Ojeda',
        'Oña Pacheco','Peñafiel Quezada','Quintero Romero','Salazar Torres','Ulloa Vaca',
        'Villacís Wray','Yépez Zambrano','Acosta Benítez','Borrero Cárdenas','Córdova Delgado',
        'Espín Freire','Granda Heredia','Intriago Jurado','Kléber Lozano','Montoya Neira',
        'Ochoa Paredes','Ponce Quiroga','Roldán Sarmiento','Solano Tello','Tapia Uquillas',
        'Uvidia Varas','Velasteguí Weil','Yánez Xavier','Zamora Alarcón','Arévalo Burbano',
        'Cárdenas Duque','Delgado Endara','Falconí Gómez','Guerrero Hidalgo','Idrovo Jácome',
        'Játiva Kuffó','León Macías','Maldonado Noboa','Narváez Ordóñez','Ojeda Palacios',
        'Pazmiño Quevedo','Rivadeneira Sosa','Sandoval Terán','Torres Unda','Urquiza Viteri',
        'Vinueza Wamán','Andrade Bósquez','Cabrera Donoso','Echeverría Flores','Fierro Guevara',
        'Hidalgo Iyela','Jarrín Kléver','Larrea Mora','Mancheno Nieto','Obando Peña',
        'Proaño Quishpe','Rivera Sacoto','Siguenza Trujillo','Tenorio Ubidia','Ullauri Valdez',
        'Vélez Weston','Andagoya Basurto','Cedeño Dueñas','Duarte Espinal','Guamán Farfán',
        'Heredia Iñiguez','Jiménez Karolys','Luna Medina','Moncayo Núñez','Ortiz Peñaloza',
        'Quijano Rosales','Saltos Troya','Toapanta Utreras','Villamarín Washima','Zúñiga Arteaga'
    ];
    v_allergies text[] := ARRAY[
        'Ninguna','Penicilina','Látex','Ninguna','Sulfonamidas','Ninguna','Eritromicina','Ninguna',
        'AINEs','Ninguna','Ninguna','Tetraciclinas','Ninguna','Yodo','Ninguna','Ninguna',
        'Cefalosporinas','Ninguna','Amoxicilina','Ninguna','Ninguna','Metronidazol','Ninguna','Ninguna',
        'Anestésicos tipo éster','Ninguna','Ninguna','Clindamicina','Ninguna','Ninguna','Aspirina','Ninguna',
        'Ninguna','Ninguna','Ninguna','Codeína','Ninguna','Lidocaína','Ninguna','Ninguna',
        'Ninguna','Dipirona','Ninguna','Ninguna','Ketorolaco','Ninguna','Ninguna','Ninguna',
        'Penicilina','Ninguna','Ninguna','Sulfonamidas','Ninguna','Ninguna','Látex','Ninguna',
        'Ninguna','Eritromicina','Ninguna','Ninguna','Yodo','Ninguna','Ninguna','Tetraciclinas',
        'Ninguna','Ninguna','AINEs','Ninguna','Ninguna','Amoxicilina','Ninguna','Ninguna',
        'Ninguna','Ninguna','Cefalosporinas','Ninguna','Ninguna','Aspirina','Ninguna','Ninguna'
    ];
    v_conditions text[] := ARRAY[
        'Ninguna','Hipertensión leve','Ninguna','Diabetes tipo 2','Ninguna',
        'Hipotiroidismo','Ninguna','Asma leve','Ninguna','Ninguna',
        'Epilepsia controlada','Ninguna','Ninguna','Hipertensión','Ninguna',
        'Ninguna','Ninguna','Gastritis crónica','Ninguna','Ninguna',
        'Rinitis alérgica','Ninguna','Anemia leve','Ninguna','Ninguna',
        'Trastorno de ansiedad','Ninguna','Ninguna','Diabetes tipo 1','Ninguna',
        'Ninguna','Hipercolesterolemia','Ninguna','Ninguna','Migraña crónica',
        'Ninguna','Ninguna','Fibrilación auricular','Ninguna','Ninguna',
        'Hipotiroidismo','Ninguna','Ninguna','Asma moderado','Ninguna',
        'Ninguna','Hipertensión','Ninguna','Ninguna','Ninguna',
        'Diabetes tipo 2','Ninguna','Artritis reumatoide','Ninguna','Ninguna',
        'Ninguna','Lupus eritematoso','Ninguna','Ninguna','Hepatitis B (portador)',
        'Ninguna','Ninguna','VIH controlado','Ninguna','Insuficiencia renal leve',
        'Ninguna','Ninguna','Hipertiroidismo','Ninguna','Ninguna',
        'Osteoporosis','Ninguna','Ninguna','Embarazo 6 meses','Ninguna',
        'Ninguna','Epilepsia','Ninguna','Ninguna','Marcapasos cardíaco'
    ];
    v_genders char[] := ARRAY['F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M','F','M'];
    v_addresses text[] := ARRAY[
        'Av. Amazonas N34-12','Calle Colón E5-67','Av. 10 de Agosto N45-23','Calle Sucre S12-34',
        'Av. República E7-89','Calle Benalcázar N23-45','Av. 6 de Diciembre N56-78','Calle Guayaquil N12-34',
        'Av. Shyris N34-56','Calle Roca E9-23','Av. Patria E5-23','Calle Venezuela N34-56',
        'Av. Naciones Unidas E7-45','Calle Olmedo S8-34','Av. Eloy Alfaro N45-67','Calle Marchena N23-34',
        'Av. González Suárez N34-56','Calle Robles E7-34','Av. La Gasca N34-12','Calle Versalles E9-12',
        'Av. Mariana de Jesús E7-89','Calle Juan León Mera N23-45','Av. El Inca E12-45','Calle Luis Cordero N45-23',
        'Av. De los Granados E12-34','Calle Whimper E7-23','Av. Atahualpa N34-67','Calle Mercadillo S4-56',
        'Av. Rodrigo de Chávez S12-34','Calle Yaguachi E9-23','Av. La Prensa N45-67','Calle Portugal E9-23',
        'Av. Pichincha E5-12','Calle Cuero y Caicedo S8-23','Av. Galo Plaza N45-78','Calle Isla Seymour N45-23',
        'Av. Colón E5-34','Calle Pasaje N23-45','Av. República del Salvador N34-56','Calle Ulloa N12-34',
        'Av. Occidental N78-45','Calle Sodiro E4-56','Av. Rumipamba N25-67','Calle Selva Alegre E8-90',
        'Av. Brasil N45-12','Calle Valladolid E7-89','Av. De La Coruña N34-56','Calle Toledo E5-23',
        'Av. Isabel La Católica N24-45','Calle Leonidas Plaza E9-12','Av. Zamora N34-78','Calle Foch E5-67',
        'Av. Santa María E12-34','Calle Reina Victoria N23-45','Av. La Niña E7-89','Calle Pinto N12-34',
        'Av. Granados N45-67','Calle Japón E5-23','Av. González Suárez N56-78','Calle Salinas S8-23',
        'Av. Oriental E12-34','Calle Junín N23-45','Av. Maldonado S34-56','Calle Chile E5-67',
        'Av. Pedro V. Maldonado S45-78','Calle Loja N12-34','Av. Napo S12-34','Calle Manabí E9-23',
        'Av. Del Maestro N34-56','Calle Imbabura S5-67','Av. Simón Bolívar N78-90','Calle Esmeraldas E4-56',
        'Av. Real Audiencia N45-12','Calle Cañaris E7-89','Av. Teniente Hugo Ortiz S34-56','Calle Moraspungo N23-45',
        'Av. Michelena S12-34','Calle Ajavi N34-56','Av. Cardenal de la Torre S45-67','Calle Tnte. García N12-34'
    ];
    v_blood_types text[] := ARRAY['O+','A+','B+','AB+','O-','A-','B-','AB-'];
    v_diagnoses text[] := ARRAY[
        'Caries dental en molar superior','Gingivitis generalizada','Pulpitis irreversible','Periodontitis crónica moderada',
        'Bruxismo severo con desgaste','Fractura dental por traumatismo','Caries incipientes múltiples','Necesidad protésica',
        'Terceros molares impactados','Maloclusión clase II','Erosión dental por reflujo','Caries cervical en premolares',
        'Disfunción temporomandibular','Absceso periapical','Diastema central','Halitosis de origen dental',
        'Blanqueamiento solicitado','Implante dental requerido','Retracción gingival','Fluorosis dental leve',
        'Quiste mucoso labial','Frenillo lingual corto','Hipersensibilidad dentinaria','Estomatitis aftosa',
        'Cálculo dental abundante','Apiñamiento anterior severo','Mordida abierta anterior','Mordida cruzada posterior',
        'Lesión periapical crónica','Diente supernumerario','Abrasión dental generalizada','Caries rampante',
        'Paladar hendido (control)','Pericoronaritis aguda','Granuloma periapical','Quiste radicular',
        'Reabsorción radicular','Luxación dental','Avulsión dental','Anodoncia parcial',
        'Amelogénesis imperfecta','Dentinogénesis imperfecta','Taurodontismo','Dilaceración radicular',
        'Odontoma compuesto','Displasia cemento-ósea','Épulis fibroso','Papiloma oral',
        'Leucoplasia oral','Fibroma traumático'
    ];
    v_treatments text[] := ARRAY[
        'Obturación con resina compuesta','Profilaxis dental y educación en higiene','Endodoncia y corona definitiva',
        'Raspado y alisado radicular','Placa de relajación nocturna','Reconstrucción con resina',
        'Obturaciones preventivas múltiples','Prótesis parcial removible','Cirugía de terceros molares',
        'Brackets ortodónticos','Sellantes y reconstrucción','Obturaciones con ionómero de vidrio',
        'Placa de descarga y fisioterapia','Drenaje y antibioterapia','Carillas de porcelana',
        'Profilaxis profunda','Blanqueamiento profesional','Implante oseointegrado',
        'Injerto gingival','Microabrasión del esmalte','Marsupialización quirúrgica',
        'Frenectomía lingual','Aplicación de desensibilizante','Tratamiento paliativo',
        'Destartaje ultrasónico','Ortodoncia correctiva','Aparatología funcional',
        'Expansor palatino','Apicectomía','Exodoncia del supernumerario',
        'Recubrimiento con resina','Tratamiento restaurador atraumático','Control y seguimiento',
        'Exodoncia + antibióticos','Endodoncia + corona','Cirugía periapical',
        'Ferulización dental','Reimplante dental','Prótesis fija','Prótesis implantosoportada',
        'Seguimiento especializado','Corona de porcelana','Incrustación cerámica','Rehabilitación oral completa',
        'Cirugía menor','Biopsia excisional','Cauterización','Derivación a especialista',
        'Control periódico','Cirugía reconstructiva'
    ];
    v_id uuid;
    v_patient_id text;
    v_dob text;
    v_year int;
    v_month int;
    v_day int;
    v_phone text;
    v_email text;
    v_diag text;
    v_treat text;
    v_status int;
    v_created text;
    v_updated text;
    v_base_date timestamp;
    v_fn text;
    v_ln text;
    v_idx int;
    v_allergy text;
    v_cond text;
    v_addr text;
    v_bt text;
BEGIN
    -- ========================================================================
    -- INSERTAR 80 PACIENTES NUEVOS (IDs 21-100)
    -- ========================================================================
    FOR i IN 1..80 LOOP
        v_idx := i;
        v_fn := v_first_names[v_idx];
        v_ln := v_last_names[v_idx];
        v_id := ('b0000001-0001-0000-0000-' || LPAD((20 + i)::text, 12, '0'))::uuid;
        
        -- Generar cédula única
        v_patient_id := '17' || LPAD((50000 + i)::text, 8, '0');
        
        -- Fecha nacimiento entre 1975-2003
        v_year := 1975 + (i % 29);
        v_month := 1 + (i % 12);
        v_day := 1 + (i % 28);
        v_dob := v_year::text || '-' || LPAD(v_month::text, 2, '0') || '-' || LPAD(v_day::text, 2, '0');
        
        v_phone := '099' || LPAD((1234521 + i)::text, 7, '0');
        v_email := LOWER(REPLACE(REPLACE(v_fn,'á','a'),'é','e')) || '.' || LOWER(SPLIT_PART(v_ln,' ',1)) || '@email.com';
        v_addr := v_addresses[1 + ((i-1) % array_length(v_addresses, 1))] || ', Quito';
        
        -- Distribuir fechas de creación a lo largo de 5 meses
        v_base_date := '2025-10-01'::timestamp + ((i-1) * interval '1.8 days');
        v_created := to_char(v_base_date, 'YYYY-MM-DD"T"HH24:MI:SS"Z"');
        
        INSERT INTO "OdontologoPatients" ("Id","OdontologoId","FirstName","LastName","IdNumber","DateOfBirth","Gender","Address","Phone","Email","CreatedAt","UpdatedAt")
        VALUES (v_id, v_odonto_id, v_fn, v_ln, v_patient_id, v_dob::date, v_genders[v_idx], v_addr, v_phone, v_email, v_created::timestamptz, v_created::timestamptz)
        ON CONFLICT ("Id") DO NOTHING;
    END LOOP;

    RAISE NOTICE '✅ 80 pacientes nuevos insertados (total: 100)';

    -- ========================================================================
    -- INSERTAR 80 HISTORIAS CLÍNICAS NUEVAS (IDs 21-100)
    -- ========================================================================
    FOR i IN 1..80 LOOP
        v_idx := i;
        v_fn := v_first_names[v_idx];
        v_ln := v_last_names[v_idx];
        v_id := ('d0000001-0001-0000-0000-' || LPAD((20 + i)::text, 12, '0'))::uuid;
        v_patient_id := '17' || LPAD((50000 + i)::text, 8, '0');
        v_allergy := v_allergies[v_idx];
        v_cond := v_conditions[v_idx];
        
        -- Fecha nacimiento
        v_year := 1975 + (i % 29);
        v_month := 1 + (i % 12);
        v_day := 1 + (i % 28);
        v_dob := v_year::text || '-' || LPAD(v_month::text, 2, '0') || '-' || LPAD(v_day::text, 2, '0');
        v_phone := '099' || LPAD((1234521 + i)::text, 7, '0');
        
        -- Diagnosis y tratamiento variados
        v_diag := v_diagnoses[1 + ((i-1) % array_length(v_diagnoses, 1))];
        v_treat := v_treatments[1 + ((i-1) % array_length(v_treatments, 1))];
        
        -- Status variado: 60% approved, 15% submitted, 15% draft, 10% rejected
        IF i % 10 <= 5 THEN v_status := 2;     -- Approved
        ELSIF i % 10 <= 7 THEN v_status := 1;   -- Submitted
        ELSIF i % 10 <= 8 THEN v_status := 0;   -- Draft
        ELSE v_status := 3;                       -- Rejected
        END IF;
        
        -- Fechas distribuidas en 5 meses
        v_base_date := '2025-10-01'::timestamp + ((i-1) * interval '1.8 days');
        v_created := to_char(v_base_date, 'YYYY-MM-DD"T"HH24:MI:SS"Z"');
        v_updated := to_char(v_base_date + interval '7 days', 'YYYY-MM-DD"T"HH24:MI:SS"Z"');
        
        INSERT INTO "OdontologoClinicalHistories" ("Id","OdontologoId","PatientName","PatientIdNumber","Data","Status","CreatedAt","UpdatedAt")
        VALUES (
            v_id, v_odonto_id,
            v_fn || ' ' || v_ln,
            v_patient_id,
            json_build_object(
                'personal', json_build_object(
                    'firstName', v_fn,
                    'lastName', v_ln,
                    'idNumber', v_patient_id,
                    'dateOfBirth', v_dob,
                    'gender', v_genders[v_idx],
                    'phone', v_phone
                ),
                'medicalHistory', json_build_object(
                    'allergies', v_allergy,
                    'medications', CASE WHEN v_cond = 'Ninguna' THEN 'Ninguna' 
                        WHEN v_cond = 'Hipertensión leve' THEN 'Losartán 50mg'
                        WHEN v_cond = 'Hipertensión' THEN 'Enalapril 10mg'
                        WHEN v_cond = 'Diabetes tipo 2' THEN 'Metformina 850mg'
                        WHEN v_cond = 'Diabetes tipo 1' THEN 'Insulina'
                        WHEN v_cond = 'Hipotiroidismo' THEN 'Levotiroxina 50mcg'
                        WHEN v_cond = 'Hipertiroidismo' THEN 'Metimazol 10mg'
                        WHEN v_cond = 'Asma leve' THEN 'Salbutamol PRN'
                        WHEN v_cond = 'Asma moderado' THEN 'Salbutamol + Fluticasona'
                        WHEN v_cond = 'Epilepsia controlada' THEN 'Carbamazepina 200mg'
                        WHEN v_cond = 'Epilepsia' THEN 'Ácido valproico 500mg'
                        WHEN v_cond = 'Trastorno de ansiedad' THEN 'Sertralina 50mg'
                        WHEN v_cond = 'Gastritis crónica' THEN 'Omeprazol 20mg'
                        WHEN v_cond = 'Fibrilación auricular' THEN 'Warfarina 5mg'
                        WHEN v_cond = 'Hipercolesterolemia' THEN 'Atorvastatina 20mg'
                        WHEN v_cond = 'Artritis reumatoide' THEN 'Metotrexato + Prednisona'
                        WHEN v_cond = 'Lupus eritematoso' THEN 'Hidroxicloroquina 200mg'
                        WHEN v_cond = 'VIH controlado' THEN 'Antirretrovirales (TAR)'
                        WHEN v_cond = 'Osteoporosis' THEN 'Alendronato 70mg semanal'
                        WHEN v_cond = 'Insuficiencia renal leve' THEN 'Control dietético'
                        WHEN v_cond = 'Migraña crónica' THEN 'Sumatriptán PRN'
                        WHEN v_cond = 'Rinitis alérgica' THEN 'Loratadina 10mg'
                        WHEN v_cond = 'Anemia leve' THEN 'Hierro + Ácido fólico'
                        WHEN v_cond = 'Marcapasos cardíaco' THEN 'Amiodarona 200mg'
                        ELSE 'Ninguna'
                    END,
                    'conditions', v_cond
                ),
                'diagnosis', v_diag,
                'treatment', v_treat,
                'observations', CASE 
                    WHEN v_allergy != 'Ninguna' THEN 'ALERTA: Alergia a ' || v_allergy || '. ' 
                    ELSE '' 
                END || CASE
                    WHEN v_cond != 'Ninguna' THEN 'Condición: ' || v_cond || '. Control periódico requerido.'
                    ELSE 'Paciente sin antecedentes relevantes. Control semestral.'
                END,
                'odontogram', json_build_object(
                    'tooth' || (11 + (i % 7) * 10), json_build_object(
                        'status', CASE WHEN i % 5 = 0 THEN 'sano' WHEN i % 5 = 1 THEN 'caries' WHEN i % 5 = 2 THEN 'obturado' WHEN i % 5 = 3 THEN 'ausente' ELSE 'fractura' END,
                        'treatment', CASE WHEN i % 5 = 0 THEN 'preventivo' WHEN i % 5 = 1 THEN 'obturación' WHEN i % 5 = 2 THEN 'control' WHEN i % 5 = 3 THEN 'implante' ELSE 'reconstrucción' END
                    ),
                    'tooth' || (21 + (i % 5) * 10), json_build_object(
                        'status', CASE WHEN i % 3 = 0 THEN 'sano' WHEN i % 3 = 1 THEN 'caries' ELSE 'obturado' END
                    )
                )
            )::jsonb,
            v_status,
            v_created::timestamptz,
            v_updated::timestamptz
        )
        ON CONFLICT ("Id") DO NOTHING;
    END LOOP;

    RAISE NOTICE '✅ 80 historias clínicas nuevas insertadas (total: 100)';

END $$;

-- ============================================================================
-- Verificación de conteos
-- ============================================================================
SELECT 'OdontologoPatients' AS tabla, COUNT(*) AS total FROM "OdontologoPatients"
UNION ALL
SELECT 'OdontologoClinicalHistories', COUNT(*) FROM "OdontologoClinicalHistories"
ORDER BY tabla;
