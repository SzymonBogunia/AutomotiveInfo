import { css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

interface NewsArticleDto {
    title: string;
    url: string;
    date: string;
    tag: string | null;
    imageUrl: string | null;
}

interface NewsTagCountDto {
    tag: string;
    count: number;
}

interface NewsStatsDto {
    totalArticles: number;
    tagCounts: NewsTagCountDto[];
    recentArticles: NewsArticleDto[];
}

// Backoffice-only Management API endpoint (backoffice-authenticated).
const STATS_ENDPOINT = '/umbraco/management/api/v1/editorial-stats';

@customElement('automotive-editorial-stats')
export class EditorialStatsElement extends UmbLitElement {
    @state()
    private _stats: NewsStatsDto | null = null;

    @state()
    private _loading = true;

    @state()
    private _error: string | null = null;

    #abortController?: AbortController;

    // Side effects belong in the lifecycle, not the constructor: a constructor fetch
    // fires even for elements that are never attached and cannot be cancelled.
    override connectedCallback() {
        super.connectedCallback();
        this.#fetchStats();
    }

    override disconnectedCallback() {
        super.disconnectedCallback();
        this.#abortController?.abort();
    }

    async #fetchStats() {
        this.#abortController?.abort();
        this.#abortController = new AbortController();

        this._loading = true;
        this._error = null;

        try {
            // The Management API requires backoffice authentication — attach the
            // current user's token from the auth context.
            const authContext = await this.getContext(UMB_AUTH_CONTEXT);
            const token = await authContext?.getLatestToken();

            const res = await fetch(STATS_ENDPOINT, {
                headers: token ? { Authorization: `Bearer ${token}` } : undefined,
                signal: this.#abortController.signal,
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            this._stats = (await res.json()) as NewsStatsDto;
        } catch (e) {
            if (e instanceof DOMException && e.name === 'AbortError') return;
            this._error = e instanceof Error ? e.message : String(e);
        } finally {
            this._loading = false;
        }
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader></uui-loader>`;
        }

        if (this._error || !this._stats) {
            return html`
        <uui-box headline=${this.localize.term('editorialStats_errorHeadline')}>
          <p>${this.localize.term('editorialStats_errorMessage', this._error)}</p>
          <uui-button
            label=${this.localize.term('editorialStats_retry')}
            @click=${() => this.#fetchStats()}></uui-button>
        </uui-box>
      `;
        }

        const untagged = this.localize.term('editorialStats_untagged');

        return html`
      <div class="grid">
        <uui-box headline=${this.localize.term('editorialStats_tagsHeadline', this._stats.totalArticles)}>
          <table>
            ${this._stats.tagCounts.map(
            (t) => html`
                <tr>
                  <td>${t.tag || untagged}</td>
                  <td class="count">${t.count}</td>
                </tr>
              `
        )}
          </table>
        </uui-box>

        <uui-box headline=${this.localize.term('editorialStats_recentHeadline')}>
          <ul class="recent-list">
            ${this._stats.recentArticles.map(
            (a) => html`
                <li>
                  <a href=${a.url} target="_blank" rel="noopener">${a.title}</a>
                  <span class="meta">${new Date(a.date).toLocaleDateString()} · ${a.tag ?? untagged}</span>
                </li>
              `
        )}
          </ul>
        </uui-box>
      </div>
    `;
    }

    // Umbraco design tokens instead of hardcoded colours, so the dashboard
    // follows the backoffice theme (including dark mode).
    static override styles = css`
    :host {
      display: block;
      padding: var(--uui-size-space-6, 24px);
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: var(--uui-size-space-4, 16px);
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    td {
      padding: 6px 0;
      border-bottom: 1px solid var(--uui-color-border, #eee);
    }
    .count {
      text-align: right;
      font-weight: bold;
    }
    .recent-list {
      list-style: none;
      margin: 0;
      padding: 0;
    }
    .recent-list li {
      display: flex;
      flex-direction: column;
      padding: 8px 0;
      border-bottom: 1px solid var(--uui-color-border, #eee);
    }
    .meta {
      font-size: 0.85em;
      color: var(--uui-color-text-alt, #888);
    }
  `;
}

export default EditorialStatsElement;

declare global {
    interface HTMLElementTagNameMap {
        'automotive-editorial-stats': EditorialStatsElement;
    }
}
