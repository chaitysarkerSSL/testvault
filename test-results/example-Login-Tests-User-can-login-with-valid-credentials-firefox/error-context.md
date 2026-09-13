# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: example.spec.js >> Login Tests >> User can login with valid credentials
- Location: tests\example.spec.js:22:3

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: page.waitForLoadState: Test timeout of 30000ms exceeded.
=========================== logs ===========================
  "domcontentloaded" event fired
  "load" event fired
============================================================
```

# Page snapshot

```yaml
- generic [active] [ref=e1]:
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
      - generic [ref=e18]:
        - navigation
        - generic [ref=e20]:
          - link "Symphony Logo" [ref=e21] [cursor=pointer]:
            - /url: https://www.symphonysofttech.com/
            - img "Symphony Logo" [ref=e22]
          - paragraph [ref=e23]: .
    - generic [ref=e25]:
      - img "User Image" [ref=e30] [cursor=pointer]
      - generic [ref=e31]:
        - generic [ref=e32]:
          - link "Admissions Admissions" [ref=e33] [cursor=pointer]:
            - /url: /ADM/Admission/Index
            - img "Admissions" [ref=e34]
            - heading [level=3]
            - paragraph [ref=e35]: Admissions
          - link "Prescriptions Prescriptions" [ref=e36] [cursor=pointer]:
            - /url: /PRM/Prescription
            - img "Prescriptions" [ref=e37]
            - heading [level=3]
            - paragraph [ref=e38]: Prescriptions
          - link "Doctors Doctors" [ref=e39] [cursor=pointer]:
            - /url: /DMS/Doctor/Index
            - img "Doctors" [ref=e40]
            - heading [level=3]
            - paragraph [ref=e41]: Doctors
          - link "Patients Patients" [ref=e42] [cursor=pointer]:
            - /url: /PMS/Patient
            - img "Patients" [ref=e43]
            - heading [level=3]
            - paragraph [ref=e44]: Patients
        - generic [ref=e46]:
          - generic [ref=e48]:
            - heading "Employee List" [level=4] [ref=e49]
            - generic [ref=e51]:
              - link "0" [ref=e52] [cursor=pointer]:
                - /url: /EMP/Employees
              - generic [ref=e53]: Total Employees
          - heading "Bed Availability" [level=4] [ref=e56]
        - dialog [ref=e58]:
          - document:
            - generic [ref=e59]:
              - heading "Select Branch" [level=4] [ref=e61]
              - generic [ref=e63]:
                - generic [ref=e64]:
                  - generic [ref=e66]:
                    - text: Show
                    - combobox "Show entries" [ref=e67]:
                      - option "5"
                      - option "10" [selected]
                      - option "25"
                      - option "All"
                    - text: entries
                  - generic [ref=e70]:
                    - text: "Search:"
                    - searchbox "Search:" [ref=e71]
                - table [ref=e74]:
                  - rowgroup [ref=e75]:
                    - 'row "Branch Code: activate to sort column descending Branch Name: activate to sort column ascending" [ref=e76]':
                      - 'columnheader "Branch Code: activate to sort column descending" [ref=e77] [cursor=pointer]': ↑ Branch Code ↓
                      - 'columnheader "Branch Name: activate to sort column ascending" [ref=e78] [cursor=pointer]': ↑ Branch Name ↓
                      - text: ↑ ↓
                  - rowgroup [ref=e79] [cursor=pointer]:
                    - row "BP-00001 Branch 01" [ref=e80]:
                      - cell "BP-00001" [ref=e81]
                      - cell "Branch 01" [ref=e82]
                    - row "BP-00002 Branch 02" [ref=e83]:
                      - cell "BP-00002" [ref=e84]
                      - cell "Branch 02" [ref=e85]
                    - row "BP-00003 Branch 03" [ref=e86]:
                      - cell "BP-00003" [ref=e87]
                      - cell "Branch 03" [ref=e88]
                - generic [ref=e89]:
                  - status [ref=e91]: Showing 1 to 3 of 3 entries
                  - list [ref=e94]:
                    - listitem [ref=e95]:
                      - link "Previous":
                        - /url: "#"
                    - listitem [ref=e96]:
                      - link "1" [ref=e97] [cursor=pointer]:
                        - /url: "#"
                    - listitem [ref=e98]:
                      - link "Next":
                        - /url: "#"
    - contentinfo [ref=e99]:
      - generic [ref=e100]:
        - strong [ref=e101]: Copyright © 2024 - 2026 Symphony Softtech Ltd.
        - text: All rights reserved.
      - generic [ref=e102]: "Branch Name :"
      - generic [ref=e103]: Version 27.April.2025
  - img
  - img [ref=e107]
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
> 31  |     await page.waitForLoadState('networkidle');
      |                ^ Error: page.waitForLoadState: Test timeout of 30000ms exceeded.
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
  114 | 
  115 |     const menuItem = page.locator('a[href], .menu-item, li a, nav a').first();
  116 | 
  117 |     const isVisible = await menuItem.isVisible().catch(() => false);
  118 | 
  119 |     if (isVisible) {
  120 |       await menuItem.click();
  121 |       await page.waitForLoadState('networkidle');
  122 |       await page.waitForTimeout(1000);
  123 |     }
  124 | 
  125 |     await expect(page).not.toHaveURL('about:blank');
  126 |   });
  127 | 
  128 |   test('User can logout', async ({ page }) => {
  129 |     await login(page);
  130 | 
  131 |     const logoutBtn = page.locator(
```