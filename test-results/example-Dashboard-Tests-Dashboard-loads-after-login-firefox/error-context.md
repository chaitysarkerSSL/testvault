# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: example.spec.js >> Dashboard Tests >> Dashboard loads after login
- Location: tests\example.spec.js:75:3

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: page.waitForLoadState: Test timeout of 30000ms exceeded.
```

# Page snapshot

```yaml
- generic [ref=e2]:
  - navigation [ref=e4]:
    - list [ref=e5]:
      - listitem [ref=e6]:
        - button "" [ref=e7] [cursor=pointer]:
          - generic [ref=e8]: 
      - listitem [ref=e9]:
        - link "Change Branch" [ref=e10] [cursor=pointer]:
          - /url: /Common/Home/Index?branchChange=true
  - complementary [ref=e11]:
    - link "Logo" [ref=e12] [cursor=pointer]:
      - /url: "#"
      - img "Logo" [ref=e14]
    - generic [ref=e15]:
      - navigation
      - generic [ref=e17]:
        - link "Symphony Logo" [ref=e18] [cursor=pointer]:
          - /url: https://www.symphonysofttech.com/
          - img "Symphony Logo" [ref=e19]
        - paragraph [ref=e20]: .
  - generic [ref=e22]:
    - img "User Image" [ref=e27] [cursor=pointer]
    - generic [ref=e28]:
      - generic [ref=e29]:
        - link "Admissions Admissions" [ref=e30] [cursor=pointer]:
          - /url: /ADM/Admission/Index
          - img "Admissions" [ref=e31]
          - heading [level=3]
          - paragraph [ref=e32]: Admissions
        - link "Prescriptions Prescriptions" [ref=e33] [cursor=pointer]:
          - /url: /PRM/Prescription
          - img "Prescriptions" [ref=e34]
          - heading [level=3]
          - paragraph [ref=e35]: Prescriptions
        - link "Doctors Doctors" [ref=e36] [cursor=pointer]:
          - /url: /DMS/Doctor/Index
          - img "Doctors" [ref=e37]
          - heading [level=3]
          - paragraph [ref=e38]: Doctors
        - link "Patients Patients" [ref=e39] [cursor=pointer]:
          - /url: /PMS/Patient
          - img "Patients" [ref=e40]
          - heading [level=3]
          - paragraph [ref=e41]: Patients
      - generic [ref=e43]:
        - generic [ref=e45]:
          - heading "Employee List" [level=4] [ref=e46]
          - generic [ref=e48]:
            - link "0" [ref=e49] [cursor=pointer]:
              - /url: /EMP/Employees
            - generic [ref=e50]: Total Employees
        - heading "Bed Availability" [level=4] [ref=e53]
  - contentinfo [ref=e55]:
    - generic [ref=e56]:
      - strong [ref=e57]: Copyright © 2024 - 2026 Symphony Softtech Ltd.
      - text: All rights reserved.
    - generic [ref=e58]: "Branch Name :"
    - generic [ref=e59]: Version 27.April.2025
```

# Test source

```ts
  1   | const { test, expect } = require('@playwright/test');
  2   | 
  3   | // Shared login helper
  4   | async function login(page) {
  5   |   await page.goto('https://dev-hospital.shampanlab.com');
  6   |   await page.waitForLoadState('networkidle');
  7   | 
  8   |   await page.locator('input[placeholder="User Name"], input[type="text"]').first().fill('ERP');
  9   |   await page.locator('input[placeholder="Password"], input[type="password"]').first().fill('Symphony@2025');
  10  | 
  11  |   await page.locator('button:has-text("Sign In"), input[value="Sign In"]').click();
  12  | 
> 13  |   await page.waitForLoadState('networkidle');
      |              ^ Error: page.waitForLoadState: Test timeout of 30000ms exceeded.
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
  106 | 
  107 | // ══════════════════════════════════
  108 | // 3. NAVIGATION TESTS
  109 | // ══════════════════════════════════
  110 | test.describe('Navigation Tests', () => {
  111 | 
  112 |   test('Menu items are clickable', async ({ page }) => {
  113 |     await login(page);
```