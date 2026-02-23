const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const baseUrl = 'http://localhost:4200';
const timestamp = Math.floor(Date.now() / 1000);

const cases = [
  {
    roleName: 'Admin',
    roleButtonRegex: /Administrador/i,
    email: 'admin@medicsys.com',
    password: 'Admin123!',
    expectedUrlIncludes: '/professor'
  },
  {
    roleName: 'Profesor',
    roleButtonRegex: /Profesor/i,
    email: 'profesor@medicsys.com',
    password: 'Profesor123!',
    expectedUrlIncludes: '/professor'
  },
  {
    roleName: 'Estudiante',
    roleButtonRegex: /Estudiante/i,
    email: 'estudiante1@medicsys.com',
    password: 'Estudiante123!',
    expectedUrlIncludes: '/student'
  },
  {
    roleName: 'Odontologo',
    roleButtonRegex: /Odont[oó]logo/i,
    email: 'odontologo@medicsys.com',
    password: 'Odontologo123!',
    expectedUrlIncludes: '/odontologo/dashboard'
  }
];

async function loginAndValidate(browser, testCase) {
  const context = await browser.newContext();
  const page = await context.newPage();
  const result = {
    role: testCase.roleName,
    pass: false,
    details: [],
    finalUrl: ''
  };

  try {
    await page.goto(`${baseUrl}/login`, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.getByRole('button', { name: testCase.roleButtonRegex }).first().click({ timeout: 10000 });

    await page.locator('#email').fill(testCase.email);
    const passwordInput = page.locator('input[type="password"]').first();
    await passwordInput.fill(testCase.password);

    await page.getByRole('button', { name: /Iniciar Sesi[oó]n/i }).first().click();
    await page.waitForLoadState('networkidle', { timeout: 30000 });

    result.finalUrl = page.url();
    const urlOk = result.finalUrl.includes(testCase.expectedUrlIncludes);
    result.details.push(urlOk
      ? `URL OK: ${result.finalUrl}`
      : `URL inesperada: ${result.finalUrl}, esperado incluye ${testCase.expectedUrlIncludes}`);

    let roleSpecificOk = true;

    if (testCase.roleName === 'Profesor') {
      const navHistory = await page.getByRole('link', { name: /Historias Cl[ií]nicas/i }).first().isVisible().catch(() => false);
      const navPatients = await page.getByRole('link', { name: /^Pacientes$/i }).first().isVisible().catch(() => false);
      result.details.push(navHistory ? 'Nav profesor: link Historias Clinicas visible' : 'Nav profesor: falta link Historias Clinicas');
      result.details.push(navPatients ? 'Nav profesor: link Pacientes visible' : 'Nav profesor: falta link Pacientes');

      await page.getByRole('button', { name: /Registro de pacientes/i }).first().click({ timeout: 10000 });
      await page.waitForURL('**/professor/patients', { timeout: 15000 });
      const patientsHeaderSelector = 'h3:has-text("Pacientes registrados")';
      await page.waitForSelector(patientsHeaderSelector, { state: 'visible', timeout: 15000 }).catch(() => null);
      const patientsHeader = await page.locator(patientsHeaderSelector).first().isVisible().catch(() => false);
      result.details.push(patientsHeader ? 'Modulo Pacientes OK' : 'Modulo Pacientes no visible');

      await page.getByRole('button', { name: /Historias cl[ií]nicas/i }).first().click({ timeout: 10000 });
      await page.waitForURL('**/professor/histories', { timeout: 15000 });
      const historiesHeaderSelector = 'h3:has-text("Acciones por lote")';
      await page.waitForSelector(historiesHeaderSelector, { state: 'visible', timeout: 15000 }).catch(() => null);
      const historiesHeader = await page.locator(historiesHeaderSelector).first().isVisible().catch(() => false);
      result.details.push(historiesHeader ? 'Modulo Historias Clinicas OK' : 'Modulo Historias Clinicas no visible');

      roleSpecificOk = navHistory && navPatients && patientsHeader && historiesHeader;
    }

    result.pass = urlOk && roleSpecificOk;
  } catch (error) {
    result.pass = false;
    result.details.push(`Error: ${error.message}`);
    result.finalUrl = page.url();
  } finally {
    await context.close();
  }

  return result;
}

async function run() {
  const browser = await chromium.launch({ headless: true });
  const results = [];
  try {
    for (const testCase of cases) {
      const res = await loginAndValidate(browser, testCase);
      results.push(res);
    }
  } finally {
    await browser.close();
  }

  const passed = results.filter(r => r.pass).length;
  const failed = results.length - passed;
  const report = {
    timestampUtc: new Date().toISOString(),
    baseUrl,
    passed,
    failed,
    total: results.length,
    results
  };

  const reportPath = path.resolve(__dirname, '..', 'docs', 'desarrollo', `UAT_UI_ROLES_${timestamp}.json`);
  fs.writeFileSync(reportPath, JSON.stringify(report, null, 2), 'utf8');

  console.log(`UAT_REPORT=${reportPath}`);
  console.log(`UAT_PASS=${passed}`);
  console.log(`UAT_FAIL=${failed}`);
  for (const item of results) {
    console.log(`[${item.pass ? 'PASS' : 'FAIL'}] ${item.role} -> ${item.finalUrl}`);
    for (const detail of item.details) {
      console.log(`  - ${detail}`);
    }
  }

  if (failed > 0) {
    process.exitCode = 1;
  }
}

run().catch(error => {
  console.error(error);
  process.exit(1);
});
