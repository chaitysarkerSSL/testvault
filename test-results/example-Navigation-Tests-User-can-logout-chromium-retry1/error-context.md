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
Error: locator.click: Test timeout of 30000ms exceeded.
Call log:
  - waiting for locator('button:has-text("Logout"), a:has-text("Logout"), button:has-text("Sign Out"), a:has-text("Sign Out"), [title="Logout"]').first()
    - locator resolved to <a data-menu-id="2" href="/Login/LogOff" class="nav-link User Logout ">…</a>
  - attempting click action
    2 × waiting for element to be visible, enabled and stable
      - element is visible, enabled and stable
      - scrolling into view if needed
      - done scrolling
      - <div tabindex="-1" role="dialog" aria-modal="true" id="branchProfiles" data-backdrop="static" class="modal fade show" aria-labelledby="Branch Profiles">…</div> from <div class="content-wrapper">…</div> subtree intercepts pointer events
    - retrying click action
    - waiting 20ms
    2 × waiting for element to be visible, enabled and stable
      - element is visible, enabled and stable
      - scrolling into view if needed
      - done scrolling
      - <div tabindex="-1" role="dialog" aria-modal="true" id="branchProfiles" data-backdrop="static" class="modal fade show" aria-labelledby="Branch Profiles">…</div> from <div class="content-wrapper">…</div> subtree intercepts pointer events
    - retrying click action
      - waiting 100ms
    27 × waiting for element to be visible, enabled and stable
       - element is visible, enabled and stable
       - scrolling into view if needed
       - done scrolling
       - <div tabindex="-1" role="dialog" aria-modal="true" id="branchProfiles" data-backdrop="static" class="modal fade show" aria-labelledby="Branch Profiles">…</div> from <div class="content-wrapper">…</div> subtree intercepts pointer events
     - retrying click action
       - waiting 500ms

```

# Page snapshot

```yaml
- generic [ref=e1]:
  - generic [ref=e2]:
    - navigation [ref=e3]:
      - list [ref=e4]:
        - listitem [ref=e5]:
          - button "" [ref=e6] [cursor=pointer]:
            - generic [ref=e7]: 
        - listitem [ref=e8]:
          - link "Change Branch" [ref=e9] [cursor=pointer]:
            - /url: /Common/Home/Index?branchChange=true
    - complementary [ref=e10]:
      - link "Logo" [ref=e11] [cursor=pointer]:
        - /url: "#"
        - img "Logo" [ref=e13]
      - generic [ref=e17]:
        - navigation [ref=e18]:
          - menu [ref=e20]:
            - listitem [ref=e21]:
              - link " Dashboard" [ref=e22] [cursor=pointer]:
                - /url: /Common/Home?branchChange=False
                - generic [ref=e23]: 
                - paragraph [ref=e24]: Dashboard
            - listitem [ref=e25]:
              - generic [ref=e26]:
                - generic [ref=e27]: 
                - paragraph [ref=e28]:
                  - text: Patient
                  - generic [ref=e29]: 
              - text:   
            - listitem [ref=e30]:
              - generic [ref=e31]:
                - generic [ref=e32]: 
                - paragraph [ref=e33]:
                  - text: Prescription
                  - generic [ref=e34]: 
              - text:      
            - listitem [ref=e35]:
              - generic [ref=e36]:
                - generic [ref=e37]: 
                - paragraph [ref=e38]:
                  - text: Admission
                  - generic [ref=e39]: 
              - text:    
            - listitem [ref=e40]:
              - generic [ref=e41]:
                - generic [ref=e42]: 
                - paragraph [ref=e43]:
                  - text: OT
                  - generic [ref=e44]: 
              - text:     
            - listitem [ref=e45]:
              - generic [ref=e46]:
                - generic [ref=e47]: 
                - paragraph [ref=e48]:
                  - text: Doctors
                  - generic [ref=e49]: 
              - text:  
            - listitem [ref=e50]:
              - generic [ref=e51]:
                - generic [ref=e52]: 
                - paragraph [ref=e53]:
                  - text: Employee
                  - generic [ref=e54]: 
              - text: 
            - listitem [ref=e55]:
              - generic [ref=e56]:
                - generic [ref=e57]: 
                - paragraph [ref=e58]:
                  - text: Diagnostic
                  - generic [ref=e59]: 
              - text:               
            - listitem [ref=e60]:
              - generic [ref=e61]:
                - generic [ref=e62]: 
                - paragraph [ref=e63]:
                  - text: House Keeping
                  - generic [ref=e64]: 
              - text:  
            - listitem [ref=e65]:
              - generic [ref=e66]:
                - generic [ref=e67]: 
                - paragraph [ref=e68]:
                  - text: Ambulances
                  - generic [ref=e69]: 
              - text:    
            - listitem [ref=e70]:
              - generic [ref=e71]:
                - generic [ref=e72]: 
                - paragraph [ref=e73]:
                  - text: Blood Bank
                  - generic [ref=e74]: 
              - text:         
            - listitem [ref=e75]:
              - generic [ref=e76]:
                - generic [ref=e77]: 
                - paragraph [ref=e78]:
                  - text: POS
                  - generic [ref=e79]: 
              - text:            
            - listitem [ref=e80]:
              - generic [ref=e81]:
                - generic [ref=e82]: 
                - paragraph [ref=e83]:
                  - text: Menu Authorization
                  - generic [ref=e84]: 
              - text:   
            - listitem [ref=e85]:
              - generic [ref=e86]:
                - generic [ref=e87]: 
                - paragraph [ref=e88]:
                  - text: Set Up
                  - generic [ref=e89]: 
              - text:                                                                    
            - listitem [ref=e90]:
              - link " User Logout" [ref=e91] [cursor=pointer]:
                - /url: /Login/LogOff
                - generic [ref=e92]: 
                - paragraph [ref=e93]: User Logout
        - generic [ref=e95]:
          - link "Symphony Logo" [ref=e96] [cursor=pointer]:
            - /url: https://www.symphonysofttech.com/
            - img "Symphony Logo" [ref=e97]
          - paragraph [ref=e98]: .
    - generic [ref=e100]:
      - img "User Image" [ref=e105] [cursor=pointer]
      - generic [ref=e106]:
        - generic [ref=e107]:
          - link "Admissions Admissions" [ref=e108] [cursor=pointer]:
            - /url: /ADM/Admission/Index
            - img "Admissions" [ref=e109]
            - heading [level=3]
            - paragraph [ref=e110]: Admissions
          - link "Prescriptions Prescriptions" [ref=e111] [cursor=pointer]:
            - /url: /PRM/Prescription
            - img "Prescriptions" [ref=e112]
            - heading [level=3]
            - paragraph [ref=e113]: Prescriptions
          - link "Doctors Doctors" [ref=e114] [cursor=pointer]:
            - /url: /DMS/Doctor/Index
            - img "Doctors" [ref=e115]
            - heading [level=3]
            - paragraph [ref=e116]: Doctors
          - link "Patients Patients" [ref=e117] [cursor=pointer]:
            - /url: /PMS/Patient
            - img "Patients" [ref=e118]
            - heading [level=3]
            - paragraph [ref=e119]: Patients
        - generic [ref=e121]:
          - generic [ref=e123]:
            - heading "Employee List" [level=4] [ref=e124]
            - generic [ref=e126]:
              - link "0" [ref=e127] [cursor=pointer]:
                - /url: /EMP/Employees
              - generic [ref=e128]: Total Employees
          - heading "Bed Availability" [level=4] [ref=e131]
        - dialog [active] [ref=e133]:
          - document:
            - generic [ref=e134]:
              - heading "Select Branch" [level=4] [ref=e136]
              - generic [ref=e138]:
                - generic [ref=e139]:
                  - generic [ref=e141]:
                    - text: Show
                    - combobox "Show entries" [ref=e142]:
                      - option "5"
                      - option "10" [selected]
                      - option "25"
                      - option "All"
                    - text: entries
                  - generic [ref=e145]:
                    - text: "Search:"
                    - searchbox "Search:" [ref=e146]
                - table [ref=e149]:
                  - rowgroup [ref=e150]:
                    - 'row "Branch Code: activate to sort column descending Branch Name: activate to sort column ascending" [ref=e151]':
                      - 'columnheader "Branch Code: activate to sort column descending" [ref=e152] [cursor=pointer]': ↑ Branch Code ↓
                      - 'columnheader "Branch Name: activate to sort column ascending" [ref=e153] [cursor=pointer]': ↑ Branch Name ↓
                      - text: ↑ ↓
                  - rowgroup [ref=e154] [cursor=pointer]:
                    - row "BP-00001 Branch 01" [ref=e155]:
                      - cell "BP-00001" [ref=e156]
                      - cell "Branch 01" [ref=e157]
                    - row "BP-00002 Branch 02" [ref=e158]:
                      - cell "BP-00002" [ref=e159]
                      - cell "Branch 02" [ref=e160]
                    - row "BP-00003 Branch 03" [ref=e161]:
                      - cell "BP-00003" [ref=e162]
                      - cell "Branch 03" [ref=e163]
                - generic [ref=e164]:
                  - status [ref=e166]: Showing 1 to 3 of 3 entries
                  - list [ref=e169]:
                    - listitem [ref=e170]:
                      - link "Previous":
                        - /url: "#"
                    - listitem [ref=e171]:
                      - link "1" [ref=e172] [cursor=pointer]:
                        - /url: "#"
                    - listitem [ref=e173]:
                      - link "Next":
                        - /url: "#"
    - contentinfo [ref=e174]:
      - generic [ref=e175]:
        - strong [ref=e176]: Copyright © 2024 - 2026 Symphony Softtech Ltd.
        - text: All rights reserved.
      - generic [ref=e177]: "Branch Name :"
      - generic [ref=e178]: Version 27.April.2025
  - img
```

# Test source

```ts
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
  132 |       'button:has-text("Logout"), a:has-text("Logout"), button:has-text("Sign Out"), a:has-text("Sign Out"), [title="Logout"]'
  133 |     ).first();
  134 | 
  135 |     const isVisible = await logoutBtn.isVisible().catch(() => false);
  136 | 
  137 |     if (isVisible) {
> 138 |       await logoutBtn.click();
      |                       ^ Error: locator.click: Test timeout of 30000ms exceeded.
  139 |       await page.waitForTimeout(2000);
  140 | 
  141 |       await expect(
  142 |         page.locator('button:has-text("Sign In"), input[value="Sign In"]')
  143 |       ).toBeVisible({ timeout: 5000 });
  144 |     } else {
  145 |       test.skip();
  146 |     }
  147 |   });
  148 | 
  149 | });
  150 | 
  151 | // ══════════════════════════════════
  152 | // 4. PAGE LOAD TESTS
  153 | // ══════════════════════════════════
  154 | test.describe('Page Load Tests', () => {
  155 | 
  156 |   test('Homepage loads within 10 seconds', async ({ page }) => {
  157 |     const start = Date.now();
  158 | 
  159 |     await page.goto('/');
  160 |     await page.waitForLoadState('domcontentloaded');
  161 | 
  162 |     const duration = Date.now() - start;
  163 | 
  164 |     expect(duration).toBeLessThan(10000);
  165 |   });
  166 | 
  167 |   test('No critical console errors on login page', async ({ page }) => {
  168 |     const errors = [];
  169 | 
  170 |     page.on('console', msg => {
  171 |       if (msg.type() === 'error') errors.push(msg.text());
  172 |     });
  173 | 
  174 |     await page.goto('/');
  175 |     await page.waitForLoadState('networkidle');
  176 | 
  177 |     const criticalErrors = errors.filter(e =>
  178 |       !e.includes('favicon') && !e.includes('warn')
  179 |     );
  180 | 
  181 |     expect(criticalErrors.length).toBeLessThan(5);
  182 |   });
  183 | 
  184 |   test('Login page works on mobile viewport', async ({ page }) => {
  185 |     await page.setViewportSize({ width: 375, height: 667 });
  186 | 
  187 |     await page.goto('https://dev-hospital.shampanlab.com/');
  188 |     await page.waitForLoadState('networkidle');
  189 | 
  190 |     await expect(
  191 |       page.locator('button:has-text("Sign In"), input[value="Sign In"]')
  192 |     ).toBeVisible();
  193 |   });
  194 | 
  195 | });
```