import { expect, test } from '@playwright/test';
import { CREDENTIALS, createPost, signIn } from './helpers';

test.describe('authenticated actions', () => {
  test('signs in, creates a post, comments on it, and keeps the session across a reload', async ({
    page,
  }) => {
    await signIn(page, 'alice');

    const title = `E2E post ${Date.now()}`;
    const url = await createPost(page, title);

    await expect(page.locator('.post-detail h1')).toHaveText(title);

    // Own post: the like control is offered but disabled rather than failing on click.
    await expect(page.locator('.actions-row button')).toBeDisabled();

    await page.getByLabel('Add a comment').fill('First comment from the end-to-end suite.');
    await page.getByRole('button', { name: 'Post comment' }).click();
    await expect(page.locator('.comments h2')).toContainText('Comments (1)');

    // localStorage-backed session must survive a refresh, which is the whole reason it
    // is not held in memory only.
    await page.reload();
    await expect(page.locator('.who')).toContainText('alice');
    await expect(page).toHaveURL(url);
  });

  test('likes and unlikes another user’s post, and refuses a second like', async ({ page }) => {
    await signIn(page, 'alice');
    const url = await createPost(page, `Likeable post ${Date.now()}`);

    await signIn(page, 'ben');
    await page.goto(url);

    const like = page.locator('.actions-row button');
    await expect(like).toContainText('Like · 0');

    await like.click();
    await expect(like).toContainText('Unlike · 1');

    await page.reload();
    await expect(page.locator('.actions-row button')).toContainText('Unlike · 1');

    await page.locator('.actions-row button').click();
    await expect(page.locator('.actions-row button')).toContainText('Like · 0');
  });

  test('hides moderation from regular users while the server enforces it', async ({
    page,
    request,
  }) => {
    await signIn(page, 'alice');
    const url = await createPost(page, `Moderatable post ${Date.now()}`);
    const postId = url.split('/').pop()!;

    await signIn(page, 'ben');
    await page.goto(url);

    await expect(page.locator('.moderation')).toHaveCount(0);

    // The control that actually matters: a regular user with a perfectly valid token,
    // calling the endpoint directly and bypassing the browser entirely.
    const login = await request.post('http://localhost:5080/api/v1/auth/login', {
      data: CREDENTIALS.ben,
    });
    const { accessToken } = await login.json();

    const refused = await request.post(`http://localhost:5080/api/v1/posts/${postId}/tags`, {
      headers: { Authorization: `Bearer ${accessToken}` },
      data: { slug: 'misleading-information' },
    });

    expect(refused.status()).toBe(403);
  });

  test('lets a moderator tag a post as misleading and remove the tag', async ({ page }) => {
    await signIn(page, 'alice');
    const url = await createPost(page, `Taggable post ${Date.now()}`);

    await signIn(page, 'moderator');
    await expect(page.locator('.badge')).toHaveText('Moderator');

    await page.goto(url);
    await page.locator('.moderation select').selectOption('misleading-information');
    await page.getByRole('button', { name: 'Apply tag' }).click();

    await expect(page.locator('.tag.moderation')).toContainText('Misleading or false information');

    await page.locator('.tag-remove').click();
    await expect(page.locator('.tag.moderation')).toHaveCount(0);
  });

  test('rejects bad credentials without revealing whether the account exists', async ({ page }) => {
    await page.goto('/login');

    await page.getByLabel('Username').fill('alice');
    await page.getByLabel('Password').fill('definitely-wrong');
    await page.getByRole('button', { name: 'Sign in' }).click();
    const wrongPassword = await page.locator('.status.error').innerText();

    await page.getByLabel('Username').fill('no-such-person-at-all');
    await page.getByLabel('Password').fill('definitely-wrong');
    await page.getByRole('button', { name: 'Sign in' }).click();
    const unknownUser = await page.locator('.status.error').innerText();

    expect(unknownUser).toBe(wrongPassword);
  });

  test('signs out and clears the stored session', async ({ page }) => {
    await signIn(page, 'alice');

    await page.getByRole('button', { name: 'Sign out' }).click();

    await expect(page.getByRole('link', { name: 'Sign in' })).toBeVisible();
    expect(await page.evaluate(() => localStorage.getItem('forum.session'))).toBeNull();
  });
});
