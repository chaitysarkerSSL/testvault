# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: example.spec.js >> Navigation Tests >> User can logout
- Location: tests\example.spec.js:128:3

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: page.goto: Test timeout of 30000ms exceeded.
Call log:
  - navigating to "https://dev-hospital.shampanlab.com/", waiting until "load"

```

# Page snapshot

```yaml
- generic [ref=e3]:
  - generic [ref=e4]:
    - link "Company Logo" [ref=e6] [cursor=pointer]:
      - /url: https://www.symphonysofttech.com/
      - img "Company Logo" [ref=e7]
    - heading "Hospital Management System" [level=5] [ref=e8]
    - generic [ref=e10]:
      - generic [ref=e11]:
        - textbox "User Name" [ref=e12]
        - generic [ref=e15]: 
      - generic [ref=e16]:
        - textbox "Password" [ref=e17]
        - generic [ref=e20]: 
      - generic [ref=e21]:
        - combobox [ref=e22]:
          - option "Symphony Softtech Ltd." [selected]
        - generic [ref=e25]: 
      - button "Sign In" [ref=e28] [cursor=pointer]
  - link "Symphony Logo" [ref=e31] [cursor=pointer]:
    - /url: https://www.symphonysofttech.com/
    - img "Symphony Logo" [ref=e32]
```

# Test source

```ts
  1   | const { test, expect } = require('@playwright/test');
  2   | 
  3   | // Shared login helper
  4   | async function login(page) {
> 5   |   await page.goto('https://dev-hospital.shampanlab.com');
      |              ^ Error: page.goto: Test timeout of 30000ms exceeded.
  6   |   await page.waitForLoadState('networkidle');
  7   | 
  8   |   await page.locator('input[placeholder="User Name"], input[type="text"]').first().fill('ERP');
  9   |   await page.locator('input[placeholder="Password"], input[type="password"]').first().fill('Symphony@2025');
  10  | 
  11  |   await page.locator('button:has-text("Sign In"), input[value="Sign In"]').click();
  12  | 
  13  |   await page.waitForLoadState('networkidle');
  14  |   await page.waitForTimeout(2000);
  15  | }
  16  | 
  17  | // ══════════════════════════════════
  18  | // 1. LOGIN TESTS
  19  | // ══════════════════════════════════
  20  | test.describe('Login Tests', () => {
  21  | 
  22  |   test('User can login with valid credentials', async ({ page }) => {
  23  |     await page.goto('https://dev-hospital.shampanlab.com/');
  24  |     await page.waitForLoadState('networkidle');
  25  | 
  26  |     await page.locator('input[placeholder="User Name"], input[type="text"]').first().fill('ERP');
  27  |     await page.locator('input[placeholder="Password"], input[type="password"]').first().fill('Symphony@2025');
  28  | 
  29  |     await page.locator('button:has-text("Sign In"), input[value="Sign In"]').click();
  30  | 
  31  |     await page.waitForLoadState('networkidle');
  32  |     await page.waitForTimeout(2000);
  33  | 
  34  |     await expect(page).not.toHaveURL(/.*login.*/i);
  35  |   });
  36  | 
  37  |   test('User cannot login with invalid password', async ({ page }) => {
  38  |     await page.goto('https://dev-hospital.shampanlab.com/');
  39  |     await page.waitForLoadState('networkidle');
  40  | 
  41  |     await page.locator('input[placeholder="User Name"], input[type="text"]').first().fill('ERP');
  42  |     await page.locator('input[placeholder="Password"], input[type="password"]').first().fill('WrongPassword123');
  43  | 
  44  |     await page.locator('button:has-text("Sign In"), input[value="Sign In"]').click();
  45  | 
  46  |     await page.waitForTimeout(2000);
  47  | 
  48  |     const stillOnLogin = await page.locator('button:has-text("Sign In")')
  49  |       .isVisible()
  50  |       .catch(() => false);
  51  | 
  52  |     const errorVisible = await page.locator('.error, .alert, [class*="error"], [class*="alert"]')
  53  |       .isVisible()
  54  |       .catch(() => false);
  55  | 
  56  |     expect(stillOnLogin || errorVisible).toBeTruthy();
  57  |   });
  58  | 
  59  |   test('Login page displays all required fields', async ({ page }) => {
  60  |     await page.goto('/');
  61  |     await page.waitForLoadState('networkidle');
  62  | 
  63  |     await expect(page.locator('input[placeholder="User Name"], input[type="text"]').first()).toBeVisible();
  64  |     await expect(page.locator('input[placeholder="Password"], input[type="password"]').first()).toBeVisible();
  65  |     await expect(page.locator('button:has-text("Sign In"), input[value="Sign In"]')).toBeVisible();
  66  |   });
  67  | 
  68  | });
  69  | 
  70  | // ══════════════════════════════════
  71  | // 2. DASHBOARD TESTS
  72  | // ══════════════════════════════════
  73  | test.describe('Dashboard Tests', () => {
  74  | 
  75  |   test('Dashboard loads after login', async ({ page }) => {
  76  |     await login(page);
  77  |     await expect(page).not.toHaveURL('about:blank');
  78  | 
  79  |     const title = await page.title();
  80  |     expect(title.length).toBeGreaterThan(0);
  81  |   });
  82  | 
  83  |   test('Dashboard navigation menu is visible', async ({ page }) => {
  84  |     await login(page);
  85  | 
  86  |     const nav = page.locator('nav, .sidebar, .menu, [class*="nav"], [class*="sidebar"], [class*="menu"]').first();
  87  |     await expect(nav).toBeVisible({ timeout: 10000 });
  88  |   });
  89  | 
  90  |   test('Dashboard shows Shampan logo or title', async ({ page }) => {
  91  |     await login(page);
  92  | 
  93  |     const hasLogo = await page.locator('img[src*="logo"], img[alt*="Shampan"], img[alt*="Hospital"], .logo')
  94  |       .first()
  95  |       .isVisible()
  96  |       .catch(() => false);
  97  | 
  98  |     const hasText = await page.locator('text=Shampan')
  99  |       .isVisible()
  100 |       .catch(() => false);
  101 | 
  102 |     expect(hasLogo || hasText).toBeTruthy();
  103 |   });
  104 | 
  105 | });
```