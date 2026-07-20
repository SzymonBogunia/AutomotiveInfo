# Internship Plan — Phase 2: Extending AutomotiveInfo (Umbraco 17 / .NET 10)

> **Phase 1** built the portal (content types, compositions, Block List, strongly-typed Razor views).
> **Phase 2** (this plan) turns you into a developer who *extends* Umbraco with code — headless delivery, custom APIs, backoffice extensions and a professional workflow — on the same AutomotiveInfo project.
> **Phase 3** (next) moves the project onto **Umbraco Cloud**. This phase is designed to make you ready for it.
>
> **Duration:** ~2 weeks (10 working days) · **Core modules:** A, C, D · **Prerequisite:** Phase 1 complete.

---

## Ground rules & how to work

- **Naming — the golden rule stays.** All code and aliases in **English** (`seoTitle`, `frontPage`); everything a Polish editor sees stays **Polish** ("Tytuł SEO", "Strona Główna").
- **Always strongly typed.** ModelsBuilder stays on `SourceCodeManual`; bind views/code to generated models, never magic strings.
- **Git discipline.** One feature branch per module, small commits, open a PR for review before merging. This is the exact habit Phase 3 (Cloud) depends on.
- **uSync discipline.** Structure auto-exports on save (GUID-named files); after any schema change, confirm the `uSync/v17` diff and commit it. Content is exported deliberately from the uSync dashboard.
- **Definition of Done (every task):** builds with 0 errors · works in both cultures (`/pl` and `/en`) · schema committed via uSync · README updated if behaviour changed · reviewed in a PR.
- **When to ask vs. dig.** Stuck on *environment/setup* >30 min → ask. Stuck on a *feature* in a docs-led task → that struggle is the point; read the docs, then ask a specific question.

**Difficulty legend:** 🟢 **Guided** (step-by-step here) · 🟡 **Docs-led** (requirements + doc links, you find the how) · 🔴 **Stretch** (complex, self-directed) · ⏱ rough time.

---

## The professional Umbraco roadmap

Where the whole journey goes, so each task has a place. Phase 2 covers stages 2–6; Phase 3 is stage 7.

| Stage | Skill | Covered by | Docs |
|---|---|---|---|
| 1 | Content modelling & Razor rendering | **Phase 1 ✓** | — |
| 2 | Traversal, search & localisation depth | Module B | `Reference → Querying`, `Searching → Examine` |
| 3 | Headless — Content Delivery API | Module C | `Reference → Content Delivery API` |
| 4 | Custom server code — APIs, notifications, DI | Module D | `Reference → Routing`, `Reference → Notifications` |
| 5 | Extending the backoffice (v14+ TypeScript/Lit) | Module E | `Customizing → Extending the Backoffice` |
| 6 | Performance, caching & publishing pipeline | woven into C/D/E | — |
| 7 | Deployment & environments → **Umbraco Cloud** | **Phase 3** | `docs.umbraco.com/umbraco-cloud` |

> **Using the docs:** everything is at **docs.umbraco.com** — always set the version selector to **17 / "Latest"** (the backoffice extension model changed a lot v13→v14). uSync docs: **docs.jumoo.co.uk/usync**.

---

## Module A — Warm-up: close the Phase 1 gaps `CORE` · ~1.5 days

Guided tasks that also fix the real gaps found in the Phase 1 review.

### A1 — Fix the two Rich Text editors 🟢 ⏱2–3h
**Goal:** "Advanced" should offer headings + tables; "Simple" should be minimal.
1. Settings → Data Types → open **RTE - Advanced**; add **Heading (H2–H4)** and **Table** toolbar buttons + matching extensions (keep media + links).
2. Open **RTE - Simple**: reduce to **Bold, Italic, Link** only — add the missing **Link** extension, remove the rest.
3. Save → confirm the uSync `DataTypes/` diff → commit.
**Done when:** an editor can insert an H2 + table in an article; the accordion body offers only bold/italic/link.

### A2 — Give the CTA colour picker real options 🟢 ⏱1h
The "Kolor tła" picker is empty, so the CTA background does nothing. Add **3 predefined values**. The view uses the value as a CSS class (`@bgColor`), so use Tailwind class names, e.g. `bg-slate-900`, `bg-red-950`, `bg-slate-800`.
**Done when:** picking a colour changes the CTA block background.

### A3 — Editor permissions "News Editors" 🟢 ⏱2h
Finish Phase 1's Zadanie 4.3.
1. Users → Groups → create **"Redaktorzy Aktualności"** (alias `newsEditors`).
2. Set its content **start node** to the news list; grant browse/create/update/publish only there.
3. Remove root access; create a test user and verify they see only the news list.
**Done when:** the test user can edit articles but cannot see/publish the home page.

### A4 — Responsive images with `<picture>` + `GetCropUrl()` 🟡 ⏱3–4h
Define named crops on the image data type (e.g. `card 16:9`, `hero 21:9`). Render hero/article/card images as `<picture>` with a `srcset` from `GetCropUrl()` at 2–3 widths + WebP.
**Docs:** `Reference → Templating → Working with Media / Image Cropper`.
**Done when:** DevTools shows a smaller image on mobile than desktop.

### A5 — Re-shape the content tree *(optional)* 🟡 ⏱2h
Move `contentPage`/`newsListPage`/`error404Page` under `frontPage`; add `Autorzy`/`Tagi` folders; add a 2nd author + 2nd content page. Then move the culture domains onto `frontPage` so a bare `/pl` renders the homepage.
**Done when:** `/pl` and `/en` land directly on the home page; all pages resolve.

---

## Module B — Content modelling & rendering depth `PICK 2+` · ~2 days

Move beyond static fields into **querying the content tree**.

### B1 — Dynamic navigation & breadcrumbs 🟡 ⏱3h
Build the top nav from the home page's children (respect a "hide in nav" toggle you add) and a breadcrumb from `Ancestors()`; highlight the active node.
**Docs:** `Reference → Querying → IPublishedContent` (Children, Ancestors, IsAncestorOrSelf).

### B2 — Add a Block **Grid** layout 🟡 ⏱4–5h
Add a Block Grid property to `frontPage` (or a new landing type) reusing existing element types; configure a 12-column layout with areas; render with the Block Grid partials. Write two sentences on when you'd pick Grid over List.
**Docs:** `Fundamentals → Backoffice → Property Editors → Block Grid`.

### B3 — Site search with Examine 🟡 ⏱4h
Add a search page + form; query the `ExternalIndex` across title/body/tags; culture-aware results with paging. Bonus: boost title matches.
**Docs:** `Reference → Searching → Examine`.

### B4 — Localisation depth: Dictionary + 3rd culture 🟡 ⏱3h
Move fixed view strings ("Zobacz wszystkie", "Brak wyników", buttons) into **Dictionary** items and render with `Umbraco.GetDictionaryValue()`. Optionally add a third language with fallback and confirm the Delivery API returns the right culture.
**Docs:** `Fundamentals → Backoffice → Dictionary Items`; language fallback.

---

## Module C — Headless: the Content Delivery API `CORE` · ~2.5 days

The headline skill of this phase: expose content as JSON and consume it from a decoupled front-end.

### C1 — Turn it on & explore 🟢 ⏱2h
1. `appsettings.json` → `Umbraco:CMS:DeliveryApi:Enabled = true`.
2. Run the site, open `/umbraco/swagger`, select the **Delivery API** definition.
3. Call `GET /umbraco/delivery/api/v2/content` and `.../content/item/{path}`; inspect an article's JSON.
**Done when:** you can fetch the news list and a single article as JSON.

### C2 — Query mastery: filter, sort, expand, fields 🟡 ⏱3h
Use query params to fetch only `newsPage` items, filter by tag, sort by publish date desc, page results, and `expand` the author + tag pickers. Switch culture with the `Accept-Language` header (`pl-PL` vs `en-US`).
**Docs:** `Reference → Content Delivery API → Selecting / Filtering / Sorting / Expanding`.

### C3 — Build a headless consumer 🟡 ⏱5–6h
Create a small standalone front-end — a single `index.html` + `fetch()`, or a minimal React/Vite app — that lists the latest articles from the Delivery API and opens a detail view. Keep it in `/headless-demo` in the repo. Handle loading + empty states.
**Done when:** a page with zero Razor renders AutomotiveInfo articles purely from the API.

### C4 — Protect & shape the output 🔴 ⏱3h
Require an **API key** for the Delivery API; enable output caching; change how one property serialises (e.g. a computed "reading time") via **output expansion** or a custom `IDeliveryApiPropertyValueConverter`.
**Docs:** `Content Delivery API → Protect / Output caching / Property extensions`.

---

## Module D — Custom API endpoints (server-side C#) `CORE` · ~2.5 days

Write your own backend: controllers, DI, event handlers, OpenAPI.

### D1 — Your first custom controller 🟢 ⏱4h
Endpoint `GET /api/news/latest?tag=&count=3`.
1. Add a controller; inject `IPublishedContentQuery` / `IUmbracoContextAccessor`.
2. Query the news list's children, filter by tag, sort desc, take `count`.
3. Return a small **DTO** (title, url, date, tag, image) — never raw `IPublishedContent`.
4. Register routing via a composer; test via Swagger/curl.
**Docs:** `Reference → Routing → Custom/API controllers`; `Composing`.
**Done when:** `/api/news/latest?count=2` returns clean JSON of 2 articles.

### D2 — React to publishing: a notification handler 🟡 ⏱3h
Register an `INotificationHandler<ContentPublishedNotification>` (via a composer) that, on article publish, does something real: invalidate a custom cache, write an audit log line, or POST to a webhook stub. Verify it fires.
**Docs:** `Reference → Notifications`; `Composing`.

### D3 — Document & harden the API 🟡 ⏱3h
Add the controller to **Swagger/OpenAPI** with response types; add API versioning; validate inputs (clamp `count` 1–20); return correct status codes + problem details.
**Docs:** `Reference → Routing / Swagger`; ASP.NET Core routing.

### D4 — Secure it + a first test 🔴 ⏱3h
Add API-key (or member) auth; write one integration test that boots the app and asserts the JSON shape + a 401 without the key.
**Docs:** ASP.NET Core auth/middleware; `WebApplicationFactory` integration tests.

---

## Module E — Extending the backoffice & pro practices `STRETCH · pick 1` · ~2 days

The v14+ backoffice is a TypeScript/Lit web-component app — a real step up.

### E1 — A custom Dashboard 🔴 ⏱6–8h
Build a backoffice dashboard showing editorial stats (article count per tag, recent publishes) reading from your D1 API. Requires the extension manifest + a Lit web component + client build.
**Docs:** `Customizing → Extending the Backoffice → Dashboards`.

### E2 — A custom value converter or property editor 🔴 ⏱6h
Either a server-side `IPropertyValueConverter` (turn a field into a typed value object) **or** a small custom property editor.
**Docs:** `Extending → Property Editors`; `Reference → Property Value Converters`.

### E3 — A scheduled background job 🔴 ⏱4h
Add an "expiry date" to `newsPage` and a recurring job (`IRecurringBackgroundJob` / hosted service) that unpublishes expired articles daily. Log each run.
**Docs:** `Reference → Scheduling / Background jobs`; `IContentService`.

---

## Module F — "Nice to have" complex stretch goals `BONUS · pick 1`

Portfolio-grade and deliberately under-specified.

- **F1 — External API integration + caching 🔴:** pull live data from a public automotive/fuel-price API, cache with `IAppPolicyCache` (TTL), surface in a new block, re-expose via your API. Handle the source being down.
- **F2 — SEO structured data + sitemap 🔴:** emit JSON-LD (`Article`/`BreadcrumbList`) from the SEO composition; generate `sitemap.xml` + `robots.txt` from the tree.
- **F3 — Automated tests + CI 🔴:** integration tests for your custom API + a GitHub Actions workflow (build + test on every PR). Rehearses Phase 3's Cloud CI/CD.
- **F4 — Members & gated content 🔴:** member registration/login + a "premium" flag; protect with Public Access (Razor) and member auth (Delivery API).

---

## Suggested schedule (10 working days)

A guide, not a contract. Core path is **A → C → D → Phase-3 prep**; B/E/F fill remaining capacity.

| Day | Focus | Tasks |
|---|---|---|
| 1 | Read roadmap · warm up | Roadmap + A1, A2 |
| 2 | Finish warm-up | A3, A4 (A5 if quick) |
| 3 | Modelling depth | B — pick 1–2 (e.g. B1 + B3) |
| 4 | Delivery API basics | C1, C2 |
| 5 | Headless consumer | C3 |
| 6 | Delivery API polish · start backend | C4, begin D1 |
| 7 | Custom API + events | D1, D2 |
| 8 | Document & harden | D3 (D4 if time) |
| 9 | One stretch goal | Module E *or* F |
| 10 | Phase-3 prep · polish · demo | Bridge checklist + demo |

---

## Bridge to Phase 3 — Umbraco Cloud

Phase 3 moves AutomotiveInfo onto Umbraco Cloud. What it is and how this phase prepares you:

- **What Cloud adds:** hosted environments (Development → Live, optionally Staging), a git-based deployment flow, and **Umbraco Deploy** — Cloud's engine for moving *schema* (and, on demand, *content*) between environments. Think "uSync, but managed and content-aware."
- **uSync → Deploy:** the schema-as-code discipline you've practised (GUID-named files, structure on startup, commit every change) is exactly the model Deploy formalises.
- **Git flow → Cloud flow:** your branch/PR habit maps onto Cloud's environment-per-branch deployment. Sloppy git = painful Cloud.
- **Structure vs content ownership** (the uSync split you've internalised) is central to how Deploy transfers schema automatically but treats content deliberately.

**Pre-Phase-3 readiness checklist**
- [ ] Build is green & the site runs from a clean clone
- [ ] uSync export is committed and reproduces the schema
- [ ] README is current
- [ ] You work in branches + PRs by reflex
- [ ] You can explain the publish/deploy model
- [ ] You can find an answer in the Umbraco docs unaided

Start reading now: **docs.umbraco.com/umbraco-cloud** and **docs.umbraco.com/umbraco-deploy**.

---

## What success looks like

- **Must ship (core):** Module A done; Delivery API enabled with a working headless consumer (C1–C3); at least one custom API endpoint with a DTO + a notification handler (D1–D2); the Phase-3 checklist green.
- **Strong performance:** also completes C4, D3, two Module B tasks, and one Module E or F stretch.
- **Final demo:** show (1) the headless page rendering live content, (2) your custom endpoint in Swagger, (3) one backoffice/stretch feature — and explain, in your own words, when you'd choose headless vs Razor, and how your work will deploy on Cloud.
- **Judged on:** correctness, strong typing & clean DTOs, git/uSync hygiene, and — increasingly through the phase — how well you used the documentation on your own.
