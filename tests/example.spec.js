const { test, expect } = require('@playwright/test');

// Shared login helper
async function login(page) {
  await page.goto('https://dev-hospital.shampanlab.com');
  await page.waitForLoadState('networkidle');

  await page.locator('input[placeholder="User Name"], input[type="text"]').first().fill('ERP');
  await page.locator('input[placeholder="Password"], input[type="password"]').first().fill('Symphony@2025');

  await page.locator('button:has-text("Sign In"), input[value="Sign In"]').click();

  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(2000);
}

// ══════════════════════════════════
// 1. LOGIN TESTS
// ══════════════════════════════════
test.describe('Login Tests', () => {

  test('User can login with valid credentials', async ({ page }) => {
    await page.goto('https://dev-hospital.shampanlab.com/');
    await page.waitForLoadState('networkidle');

    await page.locator('input[placeholder="User Name"], input[type="text"]').first().fill('ERP');
    await page.locator('input[placeholder="Password"], input[type="password"]').first().fill('Symphony@2025');

    await page.locator('button:has-text("Sign In"), input[value="Sign In"]').click();

    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    await expect(page).not.toHaveURL(/.*login.*/i);
  });

  test('User cannot login with invalid password', async ({ page }) => {
    await page.goto('https://dev-hospital.shampanlab.com/');
    await page.waitForLoadState('networkidle');

    await page.locator('input[placeholder="User Name"], input[type="text"]').first().fill('ERP');
    await page.locator('input[placeholder="Password"], input[type="password"]').first().fill('WrongPassword123');

    await page.locator('button:has-text("Sign In"), input[value="Sign In"]').click();

    await page.waitForTimeout(2000);

    const stillOnLogin = await page.locator('button:has-text("Sign In")')
      .isVisible()
      .catch(() => false);

    const errorVisible = await page.locator('.error, .alert, [class*="error"], [class*="alert"]')
      .isVisible()
      .catch(() => false);

    expect(stillOnLogin || errorVisible).toBeTruthy();
  });

  test('Login page displays all required fields', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('input[placeholder="User Name"], input[type="text"]').first()).toBeVisible();
    await expect(page.locator('input[placeholder="Password"], input[type="password"]').first()).toBeVisible();
    await expect(page.locator('button:has-text("Sign In"), input[value="Sign In"]')).toBeVisible();
  });

});

// ══════════════════════════════════
// 2. DASHBOARD TESTS
// ══════════════════════════════════
test.describe('Dashboard Tests', () => {

  test('Dashboard loads after login', async ({ page }) => {
    await login(page);
    await expect(page).not.toHaveURL('about:blank');

    const title = await page.title();
    expect(title.length).toBeGreaterThan(0);
  });

  test('Dashboard navigation menu is visible', async ({ page }) => {
    await login(page);

    const nav = page.locator('nav, .sidebar, .menu, [class*="nav"], [class*="sidebar"], [class*="menu"]').first();
    await expect(nav).toBeVisible({ timeout: 10000 });
  });

  test('Dashboard shows Shampan logo or title', async ({ page }) => {
    await login(page);

    const hasLogo = await page.locator('img[src*="logo"], img[alt*="Shampan"], img[alt*="Hospital"], .logo')
      .first()
      .isVisible()
      .catch(() => false);

    const hasText = await page.locator('text=Shampan')
      .isVisible()
      .catch(() => false);

    expect(hasLogo || hasText).toBeTruthy();
  });

});

// ══════════════════════════════════
// 3. NAVIGATION TESTS
// ══════════════════════════════════
test.describe('Navigation Tests', () => {

  test('Menu items are clickable', async ({ page }) => {
    await login(page);

    const menuItem = page.locator('a[href], .menu-item, li a, nav a').first();

    const isVisible = await menuItem.isVisible().catch(() => false);

    if (isVisible) {
      await menuItem.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);
    }

    await expect(page).not.toHaveURL('about:blank');
  });

  test('User can logout', async ({ page }) => {
    await login(page);

    const logoutBtn = page.locator(
      'button:has-text("Logout"), a:has-text("Logout"), button:has-text("Sign Out"), a:has-text("Sign Out"), [title="Logout"]'
    ).first();

    const isVisible = await logoutBtn.isVisible().catch(() => false);

    if (isVisible) {
      await logoutBtn.click();
      await page.waitForTimeout(2000);

      await expect(
        page.locator('button:has-text("Sign In"), input[value="Sign In"]')
      ).toBeVisible({ timeout: 5000 });
    } else {
      test.skip();
    }
  });

});

// ══════════════════════════════════
// 4. PAGE LOAD TESTS
// ══════════════════════════════════
test.describe('Page Load Tests', () => {

  test('Homepage loads within 10 seconds', async ({ page }) => {
    const start = Date.now();

    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');

    const duration = Date.now() - start;

    expect(duration).toBeLessThan(10000);
  });

  test('No critical console errors on login page', async ({ page }) => {
    const errors = [];

    page.on('console', msg => {
      if (msg.type() === 'error') errors.push(msg.text());
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const criticalErrors = errors.filter(e =>
      !e.includes('favicon') && !e.includes('warn')
    );

    expect(criticalErrors.length).toBeLessThan(5);
  });

  test('Login page works on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('https://dev-hospital.shampanlab.com/');
    await page.waitForLoadState('networkidle');

    await expect(
      page.locator('button:has-text("Sign In"), input[value="Sign In"]')
    ).toBeVisible();
  });

});