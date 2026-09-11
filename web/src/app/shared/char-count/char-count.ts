import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Live character count for a text field.
 *
 * The limit is shown rather than enforced by a `maxlength` attribute: typing past it is
 * allowed so the overflow stays visible and can be edited down. Silently swallowing the
 * extra keystrokes hides the fact that text was lost.
 */
@Component({
  selector: 'app-char-count',
  imports: [DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (visible()) {
      <p class="char-count" [class.over]="overBy() > 0">
        {{ length() | number }} / {{ max() | number }}
        @if (overBy() > 0) {
          <span aria-live="polite">· {{ overBy() | number }} over the limit</span>
        }
      </p>
    }
  `,
  styles: `
    .char-count {
      margin: 0.2rem 0 0;
      font-size: 0.75rem;
      color: var(--muted);
      text-align: right;
    }

    .char-count.over {
      color: var(--danger);
      font-weight: 600;
    }
  `,
})
export class CharCount {
  readonly length = input.required<number>();
  readonly max = input.required<number>();

  /**
   * Stay hidden until the limit is passed. Used for the password field, where a running
   * count of what someone is typing is both unusual and needlessly revealing.
   */
  readonly onlyWhenOver = input(false);

  protected readonly overBy = computed(() => Math.max(0, this.length() - this.max()));
  protected readonly visible = computed(() => !this.onlyWhenOver() || this.overBy() > 0);
}
