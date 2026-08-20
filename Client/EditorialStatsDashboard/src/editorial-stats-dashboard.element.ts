import { css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

interface NewsArticleDto {
    title: string;
    url: string;
    date: string;
    tag: string | null;
    imageUrl: string | null;
}

interface TagCount {
    tag: string;
    count: number;
}

@customElement('editorial-stats-dashboard')
export class EditorialStatsDashboardElement extends UmbLitElement {
    @state()
    private _tagStats: TagCount[] = [];

    @state()
    private _recentArticles: NewsArticleDto[] = [];

    @state()
    private _loading = true;

    @state()
    private _error: string | null = null;

    constructor() {
        super();
        this.#fetchStats();
    }

    async #fetchStats() {
        this._loading = true;
        this._error = null;
        try {
            const res = await fetch('/api/v1/news/latest?count=100');
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const articles: NewsArticleDto[] = await res.json();

            const counts = new Map<string, number>();
            for (const a of articles) {
                const key = a.tag ?? 'Bez tagu';
                counts.set(key, (counts.get(key) ?? 0) + 1);
            }

            this._tagStats = [...counts.entries()]
                .map(([tag, count]) => ({ tag, count }))
                .sort((a, b) => b.count - a.count);

            this._recentArticles = articles
                .slice()
                .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
                .slice(0, 5);
        } catch (e) {
            this._error = e instanceof Error ? e.message : 'Nieznany błąd';
        } finally {
            this._loading = false;
        }
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader></uui-loader>`;
        }

        if (this._error) {
            return html`
        <uui-box headline="Błąd">
          <p>Nie udało się pobrać danych: ${this._error}</p>
          <uui-button label="Spróbuj ponownie" @click=${this.#fetchStats}></uui-button>
        </uui-box>
      `;
        }

        return html`
      <div class="grid">
        <uui-box headline="Artykuły wg tagów">
          <table>
            ${this._tagStats.map(
            (t) => html`
                <tr>
                  <td>${t.tag}</td>
                  <td class="count">${t.count}</td>
                </tr>
              `
        )}
          </table>
        </uui-box>

        <uui-box headline="Ostatnio opublikowane">
          <ul class="recent-list">
            ${this._recentArticles.map(
            (a) => html`
                <li>
                  <a href=${a.url} target="_blank" rel="noopener">${a.title}</a>
                  <span class="meta">${new Date(a.date).toLocaleDateString('pl-PL')} · ${a.tag ?? 'Bez tagu'}</span>
                </li>
              `
        )}
          </ul>
        </uui-box>
      </div>
    `;
    }

    static override styles = css`
    :host {
      display: block;
      padding: 24px;
    }
    .grid {
      display: grid;
      grid-template-columns: 1fr 2fr;
      gap: 16px;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    td {
      padding: 6px 0;
      border-bottom: 1px solid #eee;
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
      border-bottom: 1px solid #eee;
    }
    .meta {
      font-size: 0.85em;
      color: #888;
    }
  `;
}

export default EditorialStatsDashboardElement;

declare global {
    interface HTMLElementTagNameMap {
        'editorial-stats-dashboard': EditorialStatsDashboardElement;
    }
}