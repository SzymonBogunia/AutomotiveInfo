import { css as b, state as _, customElement as x, html as l } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as S } from "@umbraco-cms/backoffice/lit-element";
import { UMB_AUTH_CONTEXT as w } from "@umbraco-cms/backoffice/auth";
var E = Object.defineProperty, $ = Object.getOwnPropertyDescriptor, v = (e) => {
  throw TypeError(e);
}, c = (e, t, a, r) => {
  for (var i = r > 1 ? void 0 : r ? $(t, a) : t, d = e.length - 1, u; d >= 0; d--)
    (u = e[d]) && (i = (r ? u(t, a, i) : u(i)) || i);
  return r && i && E(t, a, i), i;
}, g = (e, t, a) => t.has(e) || v("Cannot " + a), p = (e, t, a) => (g(e, t, "read from private field"), t.get(e)), m = (e, t, a) => t.has(e) ? v("Cannot add the same private member more than once") : t instanceof WeakSet ? t.add(e) : t.set(e, a), y = (e, t, a, r) => (g(e, t, "write to private field"), t.set(e, a), a), f = (e, t, a) => (g(e, t, "access private method"), a), o, n, h;
const C = "/umbraco/management/api/v1/editorial-stats";
let s = class extends S {
  constructor() {
    super(...arguments), m(this, n), this._stats = null, this._loading = !0, this._error = null, m(this, o);
  }
  // Side effects belong in the lifecycle, not the constructor: a constructor fetch
  // fires even for elements that are never attached and cannot be cancelled.
  connectedCallback() {
    super.connectedCallback(), f(this, n, h).call(this);
  }
  disconnectedCallback() {
    var e;
    super.disconnectedCallback(), (e = p(this, o)) == null || e.abort();
  }
  render() {
    if (this._loading)
      return l`<uui-loader></uui-loader>`;
    if (this._error || !this._stats)
      return l`
        <uui-box headline=${this.localize.term("editorialStats_errorHeadline")}>
          <p>${this.localize.term("editorialStats_errorMessage", this._error)}</p>
          <uui-button
            label=${this.localize.term("editorialStats_retry")}
            @click=${() => f(this, n, h).call(this)}></uui-button>
        </uui-box>
      `;
    const e = this.localize.term("editorialStats_untagged");
    return l`
      <div class="grid">
        <uui-box headline=${this.localize.term("editorialStats_tagsHeadline", this._stats.totalArticles)}>
          <table>
            ${this._stats.tagCounts.map(
      (t) => l`
                <tr>
                  <td>${t.tag || e}</td>
                  <td class="count">${t.count}</td>
                </tr>
              `
    )}
          </table>
        </uui-box>

        <uui-box headline=${this.localize.term("editorialStats_recentHeadline")}>
          <ul class="recent-list">
            ${this._stats.recentArticles.map(
      (t) => l`
                <li>
                  <a href=${t.url} target="_blank" rel="noopener">${t.title}</a>
                  <span class="meta">${new Date(t.date).toLocaleDateString()} · ${t.tag ?? e}</span>
                </li>
              `
    )}
          </ul>
        </uui-box>
      </div>
    `;
  }
};
o = /* @__PURE__ */ new WeakMap();
n = /* @__PURE__ */ new WeakSet();
h = async function() {
  var e;
  (e = p(this, o)) == null || e.abort(), y(this, o, new AbortController()), this._loading = !0, this._error = null;
  try {
    const t = await this.getContext(w), a = await (t == null ? void 0 : t.getLatestToken()), r = await fetch(C, {
      headers: a ? { Authorization: `Bearer ${a}` } : void 0,
      signal: p(this, o).signal
    });
    if (!r.ok) throw new Error(`HTTP ${r.status}`);
    this._stats = await r.json();
  } catch (t) {
    if (t instanceof DOMException && t.name === "AbortError") return;
    this._error = t instanceof Error ? t.message : String(t);
  } finally {
    this._loading = !1;
  }
};
s.styles = b`
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
c([
  _()
], s.prototype, "_stats", 2);
c([
  _()
], s.prototype, "_loading", 2);
c([
  _()
], s.prototype, "_error", 2);
s = c([
  x("automotive-editorial-stats")
], s);
const A = s;
export {
  s as EditorialStatsElement,
  A as default
};
//# sourceMappingURL=editorial-stats.js.map
