# Phase 2 — Senior Review (basis for the gap-filling round)

> **Scope:** commits `a92e45b…12a4b53` (40 commits, 10 PRs) reviewed against `Internship-Phase-2-Plan.md`.
> **Reviewed:** 14 custom C# files, 25 views, the dashboard, the headless demo, 160 uSync files, git history, runtime logs.
> **Verdict:** strong breadth — every core module shipped, B over-delivered — but almost nothing finished to its *"Done when"* bar. Four systemic habits (culture-correctness, magic strings, copy-paste, config hygiene) turned good features into shipped bugs. Findings: **12 high / 26 medium / 21 low**.

---

## 00 · uSync incident postmortem (FIXED in this review)

- **Symptom:** dashboard Import *and* Report crashed with HTTP 500: `Duplicate: Item key bc3217c8-… already exists for \4732fe99-….config` (log `umbraco/Logs/…20260823.json`, 12:04–12:05). Startup import kept working (Settings group only; `DomainHandler` is in Content). Meanwhile the DB had **lost its domains**, so the whole site 404'd — the import you attempted was the right fix; it just couldn't run.
- **Root cause:** three duplicate-Key pairs in `uSync/v17/Domains/`, left by commit `54c6cf0` (re-export added files without deleting originals). One pair had **conflicting languages** for `localhost:44328/en` (en vs pl-PL).
- **Underlying defect:** `GuidNames: true` is unsafe for the Domain handler — Umbraco domains have no persisted key, so every export wrote a *new random filename*. Commit `8287b59` already deleted 39 such duplicates once; they came back.
- **Fix applied:** deleted the 3 stale files; added `"DomainHandler": { "GuidNames": false }` to `appsettings.json`; verified Export is now in-place (0 new files); full **Report = 157 actions / 0 failures**; **Import restored all 7 domains** → `/pl/` and `/en/` route again (200).
- **Lesson:** after every uSync export run `git status -- uSync/`. A file *added* in `Domains/` with no matching deletion is always a duplicate. uSync → Health Check reports these.
- **Still yours:** prune orphan sub-page domains (`/faq`, `https://…/strona-informacyjna`, relative `/strona-aktualnosci`) down to the culture roots; all domains hardcode `localhost:44328` — dead on any other machine and a Phase-3 blocker.

---

## 01 · Module conformance

| Task | Status | Gap vs "Done when" |
|---|---|---|
| A1 RTE editors | ✅ | Advanced has H2–H4 + Table; Simple = Bold/Italic/Link. Nit: `Heading3` button duplicated. |
| A2 CTA colours | ✅ | Works; label-as-CSS-class is fragile (see §04). |
| A3 newsEditors group | ❌ | **No evidence in repo** (groups aren't uSync-serialized). Verify in backoffice + document in README. |
| A4 responsive images | 🟡 | Real `<picture>`/srcset/WebP in `Artykul.cshtml` ✓ — but named crops `card`/`hero` **never used**; news-list cards still serve full-size `.Url()`. |
| A5 tree re-shape | 🟡 | Done ✓, but `frontPage` allowed-children never updated — schema forbids two of its own live children. |
| B1 nav + breadcrumbs | ✅ | Best work of the phase. |
| B2 Block Grid | 🟡 | **No areas defined** (explicit requirement); spans render for only 2 of 4 blocks; no Grid-vs-List rationale written. |
| B3 Examine search | 🟡 | Hardcoded `_pl-pl` fields → **Polish-only search**; `Console.WriteLine("[DEBUG]…")` shipped. |
| B4 Dictionary + es | 🟡 | 16 keys × 3 cultures ✓ — but only blockGRID partials migrated; blockLIST copies (most of the site) still hardcode Polish. `es` has zero content → `/es` unroutable. |
| C1–C3 Delivery API + demo | ✅ | Custom `TagFilterHandler` beyond spec ✓. Demo: XSS via innerHTML, API key in client JS, absolute localhost URL, `Accept-Language` pinned pl-PL. |
| C4 protect & shape | ❌ | Output cache + reading-time converter built — then commit `66585e2` set `PublicAccess: true`, **undoing your own protection**. |
| D1–D2 controller + handler | ✅ | Clean DTO; cleanest code (handler). But carries Bugs 1–2 below. |
| D3 document & harden | 🟡 | Swagger doc ✓; "versioning" is a route literal; Swagger UI listing unverified. |
| D4 auth + test | ❌ | No auth; no test project (single-project .sln). |
| E1 dashboard | 🟡 | Real Lit/Vite ✓ — but `dist/` gitignored + never built → **404 on clean clone**; `count=100` clamped to 20 → wrong stats. |
| E2 value converter | 🟡 | Real converter, wrong matching + registration (Bug 5). |
| E3 · F1–F4 | ❌ | Not attempted. |

---

## 02 · Shipped bugs — fix these first (each would block a PR)

1. **Culture cache bleed** — `NewsApiController` caches culture-dependent values under culture-less `news:all-articles` (`Caching/NewsCacheKeys.cs`). First visitor's language poisons all cultures for 10 min. → per-culture key from `IVariationContextAccessor` + explicit culture in `Value<T>()`/`Url()`.
2. **Endpoint is Polish-only** — hardcoded segment `"strona-aktualnosci"` (`NewsApiController.cs:20`); the en segment is `news-page` → 404 under en/es. Also walks the whole tree. → `DescendantsOrSelf<NewsListPage>()` or configured key.
3. **Search is Polish-only** — hardcoded `articleTitle_pl-pl`/`components_pl-pl` (`ArticleSearchService.cs:31`). Also: unvalidated `pageSize` (0 ⇒ blow-up), searches raw Block-List JSON, `Console.WriteLine` debug.
4. **`TagFilterHandler` was never activated** *(corrected after empirical verification)* — registration is **not** the issue: Umbraco auto-discovers `IFilterHandler`/`IContentIndexHandler` by type scanning (verified by disabling explicit registration and retesting — the filter still works). The real defects: (a) the **`DeliveryApiContentIndex` rebuild was never run or documented** — the `tagName` field doesn't exist until then (the official docs call this step out); (b) **case-sensitive matching** (`tag:premiera` found nothing); (c) `IContentService.GetById` (SQL) per tag per article inside the index build → use the published cache.
5. **Reading-time converter mis-aimed** — matches property alias `componentText` (hijacks any same-named property; misses `imageWithTextBlock.text` on the same RTE); `.Append()` likely loses to the core converter; if it wins, Razor loses localLinks/media/RTE-blocks. → dedicated data type + match on EditorAlias + verify at runtime.
6. **404 page can't work** *(deeper root cause found during the fix)* — the appsettings key was **`Error404Page`, but the valid key is `Error404Collection`** (verify against `appsettings-schema.Umbraco.Cms.json`) → the custom 404 was *never configured at all*, in Phase 1 either. The IDE never flagged it because the file's `$schema` points at the stub `appsettings-schema.json` instead of the Umbraco schema. On top of that: node `4c1db88b-…` was **unpublished**, its link URL was literally `chatgpt.com`, lead was "Niestety błąd :D", no en variant, no naviHide. Lesson: an invalid config key fails *silently* — .NET configuration binding ignores unknown keys.
7. **Schema forbids its own tree** — `websiteContainer` allows the `accordion` *element type* as a child; `frontPage` doesn't allow its live children `newsListPage`/`error404Page`. Repair; set `frontPage.AllowAtRoot=false`.
8. **`<title> - AutomotiveInfo</title>` on every article** — `Artykul.cshtml` never sets a title; and the entire `seoSettings` composition is referenced by **zero** views.
9. **Dashboard broken on clean clone + wrong numbers** — `dist/` gitignored, no build step; `?count=100` silently clamped to 20. → MSBuild/CI runs `vite build`; add a real stats endpoint.

---

## 03 · Refactoring themes

**Culture-correctness** — derive everything from the variation context (cache keys, lookups, Examine fields, headers); replace `Thread.CurrentThread.CurrentCulture` (`_Layout`, `StronaBledu404`); kill 4× `ToString("dd.MM.yyyy")`; decide `es` (publish variants or remove the language+domain).

**Magic strings** — use generated models (`NewsPage.ModelTypeAlias`, typed properties) — deletes `"newsPage"`×3, the controller's alias digging and `GetPickerItems`, `"NewsApi"` in Swagger (→ `nameof`). Worst: Razor branches on Polish dropdown labels `"Prawo"/"Centruj"` (`imageWithTextBlock.cshtml`) and CTA colours where the *label* is a Tailwind class → store invariant keys (`left/center/right`), map in one place. `StronaWyszukiwania.cshtml` digs `HasValue("summary")` — an alias that exists nowhere. Regenerate stale models (`ood.flag` set; `FolderAuthor`/`FolderTag` missing).

**Duplication** — 5 block partials ×2 folders, already drifted (B4 landed in one copy only: "Click here" in Grid vs "Kliknij tutaj" in List) → shared partials + view models. "Latest articles" logic ×3 with 3 sort rules → one `INewsQueryService`. Article card ×3 → `_articleCard.cshtml`. Delete dead files: `blockgrid/Components/accordion.cshtml`, stray `blockgrid/Components/area.cshtml`, area partials (or actually configure B2 areas), `singleblock/` (case-broken on Linux = Cloud). Nav/breadcrumbs → cached ViewComponent; search page → RenderController + `SearchHit` DTO.

**Secrets & config** — 4 committed secrets in `appsettings.json` (sa password, admin password — also in README!, Delivery API key — also in public demo JS!, HMAC key) → user-secrets + **rotate** (git history keeps them). `PublicAccess=false`. Re-enable `RedirectUrlTracking` (renames currently lose their 301s). Tailwind CDN dev build → real build (also un-breaks dynamic `md:col-span-@…` classes). Gitignore generated schema files (+3,452-line churn buried a PR). **Highest leverage:** one `WebApplicationFactory` integration test with `Accept-Language: en` (catches Bugs 1–3) + GitHub Actions (build, test, dashboard `vite build`) = D4 + F3 + Phase-3 rehearsal.

---

## 04 · Content-type architecture

- **`seoSettings` is composed into 6 types and used by zero views** — wire it into `_Layout` (title/meta description/canonical/hreflang/noindex; `ISeoSettings` already generated) or delete it.
- **Missing compositions:** `navigationSettings` (ONE invariant `umbracoNaviHide` on built-in True/false, all 7 page types — kills 3 duplicate toggle data types, fixes per-culture-hide weirdness, covers `searchPage`/`frontPage` which can't be hidden today) and `basePage` (heading+lead; today "page heading" exists under 5 aliases).
- **Zero mandatory fields on page types** → every view compensates with fallback ternaries. Make mandatory: `articleTitle`, `publishDate`, `heading`, `heroTitle`, `title`, `recentNewsBlock.source`; then delete the fallbacks. (You know the pattern: `accordion.items` has `min:1`.)
- **`searchPage` has zero properties** — add `heading`/`lead`/`resultsPerPage`, compose nav+seo settings, add `noindex`.
- **`blockSettings` is one bit wide** — extend with spacing (invariant-key dropdown), `anchorId`, background theme; move CTA `bgColor` there.
- **Variance fixes:** `umbracoNaviHide` should be invariant; `dataRepository` should be invariant (culture-variant with zero properties = publish 3× for nothing); 404 `link` vs `heroCta` variance inconsistent. (Your invariant `heroImage`/`mainImage`/`publishDate`/`author`/`tag` choices are correct — and `heroImage` documents the decision. Do that everywhere.)
- **Data types 70 → ~55:** 4 identical one-off TextBoxes → 1; 3 naviHide toggles → built-in; delete orphans (3 superseded BlockList drafts, 2 FAQ TextAreas incl. "…(1)", misspelled toggle twin); 3 near-identical MediaPicker3s → 1. Use the `card`/`hero` crops (`GetCropUrl(cropAlias: …)`) or delete them.
- **Editor-visible polish:** "Ustawienia **blocku**"→bloku, "(**CTE**)"→(CTA), "**Nagłowek**"→Nagłówek; "TextArea" types that are TextBoxes; `secondName`→`lastName`; "Element Aktualności" renders a list; delete unused `navLinks`; add one-line Descriptions to all 22 types (all empty today).

---

## 05 · What you did well — keep doing these

Flawless constructor DI (zero service-locator anywhere) · DTO boundary on the API · composer-per-concern · Swagger `DocInclusionPredicate` chaining (senior-level touch) · notification handler with early-return guard · `<picture>`/WebP execution better than most production sites · dynamic-root MNTP pickers (textbook) · Dictionary discipline (16 keys × 3 cultures) · `Layout=null` self-contained 404 · honest comments flagging known limits.

---

## 06 · Process

- **12 of 25 work commits bypassed branch+PR** (worsening over time; last four straight to main). On Cloud, git flow *is* deployment — this habit is Phase 3's foundation.
- Bare commit messages; one accidental pull-merge commit; two git identities; 11 stale merged branches.
- **README untouched all phase** — wrong port, plaintext admin password, and none of the new features documented (Delivery API, endpoint, Swagger, demo, dashboard npm build, search, cultures, index rebuild).

---

## 07 · Prioritized worklist

1. **Correctness:** Bugs 1–9 above (each PR-sized).
2. **Architecture:** compositions (`navigationSettings`, `basePage`) → mandatory fields → shared partials + `INewsQueryService` + `_articleCard` → invariant keys for positions/colours → extend `blockSettings` → wire/delete `seoSettings` → `searchPage` properties → data-type consolidation → regenerate models.
3. **Hygiene / Phase-3:** secrets out + rotate → Tailwind build → integration test + GitHub Actions (incl. dashboard build) → domain cleanup (culture roots, no hardcoded hosts) → decide `es` → README rewrite → gitignore generated schemas → finish B2 areas + write the Grid-vs-List rationale → verify + document newsEditors.

**Phase-3 readiness today: 1 of 6.** ✗ clean clone runs · ✓ uSync reproduces schema (import fixed) · ✗ README current · ✗ branch+PR reflex · ✗ can explain publish/deploy (note: `IMemoryCache` invalidation is per-process — Cloud load balancing needs `ICacheRefresher`) · ✗ secrets out of repo. Completing worklist 3 turns every ✗ into ✓.
