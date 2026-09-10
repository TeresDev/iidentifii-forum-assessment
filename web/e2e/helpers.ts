import { Page, expect } from '@playwright/test';

/**
 * The filter bar debounces by 250ms, so the list lags the interaction. Waiting for the
 * request the change causes is deterministic; asserting straight after the click reads
 * stale rows and passes or fails on timing.
 */
export async function changeFilter(page: Page, label: string, value: string) {
  const response = page.waitForResponse(
    (r) => r.url().includes('/api/v1/posts?') && r.request().method() === 'GET' && r.ok(),
  );

  await page.getByLabel(label).selectOption(value);
  await response;
}

export async function likeCountsOnPage(page: Page) {
  const rows = await page.locator('.post .meta').allInnerTexts();

  return rows.map((text) => Number(text.match(/(\d+) likes/)![1]));
}

export const CREDENTIALS = {
  alice: { username: 'alice', password: 'Password123!' },
  ben: { username: 'ben', password: 'Password123!' },
  moderator: { username: 'mod.jordan', password: 'Moderator1!' },
};

/**
 * Clears any existing session first, then waits for the header to name this user.
 * Waiting on a generic "Sign out" button races: the previous session already rendered
 * one, so the assertion passes before the new login has landed.
 */
export async function signIn(page: Page, who: keyof typeof CREDENTIALS) {
  const { username, password } = CREDENTIALS[who];

  await page.goto('/');
  await page.evaluate(() => localStorage.clear());
  await page.goto('/login');

  await page.getByLabel('Username').fill(username);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page.locator('.who')).toContainText(username);
}

/** Each test owns the post it acts on, so tests cannot collide over seeded rows. */
export async function createPost(page: Page, title: string) {
  await page.goto('/posts/new');
  await page.getByLabel('Title').fill(title);
  await page.getByLabel('Body').fill('Created by an end-to-end test.');
  await page.getByRole('button', { name: 'Publish' }).click();

  await expect(page.getByRole('heading', { name: title })).toBeVisible();

  return page.url();
}
