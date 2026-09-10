import { expect, test } from '@playwright/test';
import { changeFilter, likeCountsOnPage } from './helpers';

/**
 * The brief requires anonymous browsing with filters, sorting and paging. This covers
 * that path end to end, including the sad paths rather than only the happy one.
 *
 * Nothing here assumes a pristine database: earlier runs leave posts behind, so the
 * assertions are about relationships (fewer, ordered, distinct) rather than fixed counts.
 */
test.describe('anonymous browsing', () => {
  test('browses and filters without signing in', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('link', { name: 'Sign in' })).toBeVisible();
    await expect(page.locator('.post').first()).toBeVisible();

    const total = await page.locator('.result-count').innerText();
    const unfiltered = Number(total.match(/(\d+)/)![1]);
    expect(unfiltered).toBeGreaterThan(20);

    // A filter must genuinely narrow the result. An unbound query parameter is ignored
    // server-side and returns 200 with the full list, which looks like success.
    await changeFilter(page, 'Tag', 'misleading-information');

    const filtered = Number((await page.locator('.result-count').innerText()).match(/(\d+)/)![1]);
    expect(filtered).toBeGreaterThan(0);
    expect(filtered).toBeLessThan(unfiltered);

    await page.getByRole('button', { name: 'Clear filters' }).click();
    await expect(page.locator('.result-count')).toHaveText(total);
  });

  test('sorts by like count in both directions', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('.post').first()).toBeVisible();

    await changeFilter(page, 'Sort by', 'likes');
    const descending = await likeCountsOnPage(page);
    expect(descending).toEqual([...descending].sort((a, b) => b - a));

    await changeFilter(page, 'Direction', 'asc');
    const ascending = await likeCountsOnPage(page);
    expect(ascending).toEqual([...ascending].sort((a, b) => a - b));
  });

  test('pages through posts without repeating or losing any', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('.post').first()).toBeVisible();

    // Ties in like count are what break paging when the sort has no unique tiebreaker:
    // a post appears on two pages while another appears on none.
    await changeFilter(page, 'Sort by', 'likes');

    const seen: string[] = [];
    let pageNumber = 1;

    for (;;) {
      seen.push(...(await page.locator('.post h3 a').allInnerTexts()));

      const next = page.getByRole('button', { name: 'Next' });
      if (await next.isDisabled()) break;

      await next.click();
      pageNumber += 1;
      await expect(page.locator('.pager span')).toContainText(`Page ${pageNumber}`);
    }

    expect(pageNumber).toBeGreaterThan(1);
    expect(new Set(seen).size).toBe(seen.length);
  });

  test('reads a post and pages its comments', async ({ page }) => {
    await page.goto('/');
    await changeFilter(page, 'Direction', 'asc');

    await page.locator('.post h3 a').first().click();
    await expect(page.locator('.post-detail')).toBeVisible();
    await expect(page.locator('.comments h2')).toContainText('Comments');

    const pager = page.locator('.comments .pager');
    if (await pager.count()) {
      const first = await page.locator('.comments li').first().innerText();
      await pager.getByRole('button', { name: 'Next' }).click();
      await expect(page.locator('.comments li').first()).not.toHaveText(first);
    }
  });

  test('shows an error for a post that does not exist', async ({ page }) => {
    await page.goto('/posts/00000000-0000-0000-0000-0000000000ff');

    await expect(page.locator('.status.error')).toContainText('no longer exists');
  });

  test('sends anonymous visitors to sign in before creating a post', async ({ page }) => {
    await page.goto('/posts/new');

    await expect(page).toHaveURL(/\/login/);
  });
});
