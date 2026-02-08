// elbruno.Doc2Code — Playwright configuration for end-to-end and screenshot tests.
import { defineConfig, devices } from '@playwright/test';

const appBaseUrl = process.env.BASE_URL ?? 'https://localhost:7275';
const pipelineTimeoutMs = 120_000;

export default defineConfig({
  testDir: './tests',
  timeout: pipelineTimeoutMs,
  retries: 1,
  reporter: 'html',

  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: appBaseUrl,
        screenshot: 'only-on-failure',
        video: 'retain-on-failure',
        ignoreHTTPSErrors: true,
      },
    },
  ],
});
