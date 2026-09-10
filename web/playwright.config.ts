import { defineConfig, devices } from '@playwright/test';

/**
 * Starts both halves of the stack, so `npx playwright test` needs nothing running first.
 * Uses Playwright's bundled Chromium rather than a system browser, so the suite does not
 * depend on what happens to be installed on the machine.
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: process.env.CI ? 'line' : [['list']],

  use: {
    baseURL: 'http://localhost:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'], channel: undefined } }],

  webServer: [
    {
      command: 'dotnet run --project ../api/src/Forum.Api --launch-profile http',
      url: 'http://localhost:5080/api/v1/tags',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      stdout: 'ignore',
    },
    {
      command: 'npm start',
      url: 'http://localhost:4200',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      stdout: 'ignore',
    },
  ],
});
