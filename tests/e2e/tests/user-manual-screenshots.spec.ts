// elbruno.Doc2Code — Playwright screenshot capture for user manual documentation.
import { test, expect } from '@playwright/test';
import path from 'path';

const screenshotDir = path.resolve(__dirname, '..', '..', '..', 'docs', 'screenshots');

test.describe('Doc2Code User Manual Screenshots', () => {
  test('01 — capture idle home screen', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const captureFilePath = path.join(screenshotDir, '01-home-idle.png');
    await page.screenshot({ path: captureFilePath, fullPage: true });
  });

  test('02 — capture file selection state', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const sampleFilePath = path.resolve(
      __dirname,
      '..',
      '..',
      '..',
      'samples',
      'sample-task-tracker.txt',
    );
    const uploadTrigger = page.locator('input[type="file"]');
    await uploadTrigger.setInputFiles(sampleFilePath);
    await expect(page.getByText('sample-task-tracker')).toBeVisible({ timeout: 10_000 });

    const captureFilePath = path.join(screenshotDir, '02-file-selected.png');
    await page.screenshot({ path: captureFilePath, fullPage: true });
  });

  test('03 — capture pipeline diagram layout', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const nodeLabels = ['Analyst', 'Architect', 'Developer', 'Reviewer', 'Testing', 'Documentation'];
    for (const label of nodeLabels) {
      await expect(page.getByText(label, { exact: false }).first()).toBeVisible({ timeout: 15_000 });
    }

    const captureFilePath = path.join(screenshotDir, '03-pipeline-diagram.png');
    await page.screenshot({ path: captureFilePath, fullPage: true });
  });

  test('04 — capture console panel', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const consoleArea = page.locator('[class*="console"], [class*="Console"], [class*="log"], [class*="Log"], [role="log"]').first();
    await expect(consoleArea).toBeVisible({ timeout: 15_000 });

    const captureFilePath = path.join(screenshotDir, '04-console-panel.png');
    await page.screenshot({ path: captureFilePath, fullPage: true });
  });

  test('05 — capture settings page', async ({ page }) => {
    await page.goto('/settings');
    await page.waitForLoadState('networkidle');

    const settingsHeading = page.getByRole('heading', { name: /settings/i }).first();
    await expect(settingsHeading).toBeVisible({ timeout: 15_000 });

    const captureFilePath = path.join(screenshotDir, '05-settings-page.png');
    await page.screenshot({ path: captureFilePath, fullPage: true });
  });

  test('06 — capture settings agent profiles', async ({ page }) => {
    await page.goto('/settings');
    await page.waitForLoadState('networkidle');

    const accordionTrigger = page
      .locator('button[class*="accordion"], [class*="Accordion"] button, details summary')
      .first();
    await accordionTrigger.click();
    await page.waitForTimeout(500);

    const captureFilePath = path.join(screenshotDir, '06-agent-profiles.png');
    await page.screenshot({ path: captureFilePath, fullPage: true });
  });
});
