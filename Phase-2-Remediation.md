# Phase 2 — Remediation & Handover

> Companion to **[Phase-2-Review.md](Phase-2-Review.md)** (the assessment). That document said *what was wrong*; this one says **what was fixed, what you should learn from it, and what is still yours to do**.
>
> Everything here was applied to the working tree and **verified at runtime** — nothing is committed, so you can read every change as a diff before accepting it.
>
> **Scope:** uSync import repaired · 9 shipped bugs fixed · 7 architecture steps completed · 6 review findings corrected.
> **Delta:** 98 files modified · 27 added · 33 deleted (of which uSync: 42 changed / 10 added / 29 deleted).

---

## 1. Start here — get it running and see it working

```bash
dotnet build
dotnet run
```

No Node needed to *run* the site: the backoffice dashboard's built output is committed. You only
need `npm` when you change the dashboard itself — see [`Client/README.md`](Client/README.md).

Then check these URLs — each one exercises something that was broken before:

| URL | What it proves |
|---|---|
| `/` | 301 → `/pl` (domain root redirect) |
| `/pl/` and `/en/` | culture routing; `<title>` differs per language (per-culture SEO) |
| `/pl/nieistnieje/` | the **custom** 404 page, in the right language |
| `/pl/strona-wyszukiwania/?tag=Premiera` | tag facet + tag-only browsing |
| `/api/v1/news/latest?culture=en` | public API returns **English** titles and `/en/` URLs |
| `/umbraco/management/api/v1/editorial-stats` | 401 without a token (backoffice-only API) |
| Backoffice → Content → *Statystyki redakcyjne* | dashboard renders live stats |

> **First-time note:** the Delivery API tag filter needs an index build — Settings → **Examine Management** → rebuild `DeliveryApiContentIndex`. That step is *required*, not optional (see Lesson 3).

---

## 2. What was fixed

### 2.1 The blocker: uSync could not import

`SyncFileService.MergeFoldersAsync` threw `Duplicate: Item key …` and **both Import and Report 500'd**. Cause: three duplicate-key files in `uSync/v17/Domains/`, because `GuidNames: true` is unsafe for the Domain handler — Umbraco domains have no persisted key, so every export wrote a *new random filename* instead of overwriting.

Fixed by deleting the three stale files and adding a per-handler override:

```jsonc
"Handlers": { "MediaHandler": { "Enabled": false }, "DomainHandler": { "GuidNames": false } }
```

The import then restored the 7 domains the database had lost — which is why the whole site had been 404ing.

### 2.2 The nine shipped bugs

| # | Bug | Fix |
|---|---|---|
| 1 | **Culture cache bleed** — one global cache key held culture-dependent data; the first visitor's language was served to everyone for 10 min | per-culture keys + a `NewsCacheSignal` (one `CancellationChangeToken` expires every per-culture entry at once) |
| 2 | **API was Polish-only** — the news list was found by the hardcoded URL segment `strona-aktualnosci` | typed `DescendantsOrSelf<NewsListPage>()` — culture-independent, rename-proof, short-circuits |
| 3 | **Search was Polish-only** — hardcoded `articleTitle_pl-pl` fields; `Console.WriteLine` debug shipped | culture-built field names + `__Published_{culture}` guard; `ILogger`; **plus a full refactor** (§2.4) |
| 4 | **Tag filter never worked** | the `DeliveryApiContentIndex` had never been rebuilt; values are now normalized lower-case both sides; `IContentService` (SQL, N+1) → published cache |
| 5 | **Reading-time converter broke rich text** — hijacked by property alias, returned raw markup (killing local links, media, RTE blocks) | rewritten as a **decorator** over the core `RteBlockRenderingValueConverter` — Razor delegates 100%, only the Delivery API output is extended |
| 6 | **Custom 404 never worked** | the appsettings key was `Error404Page`; the valid key is **`Error404Collection`** — an invalid key fails *silently*. Node also republished, `chatgpt.com` link replaced with an internal link, `umbracoNaviHide` set |
| 7 | **Schema forbade its own content tree** | `accordion` (an element type!) removed from `websiteContainer`; `newsListPage`+`error404Page` allowed under `frontPage`; `frontPage.AllowAtRoot=false` |
| 8 | **`<title> - AutomotiveInfo</title>` on every article; SEO composition unused** | `seoSettings` wired into `_Layout` (title chain, meta description, canonical, hreflang + x-default, robots); `Artykul` sets its title |
| 9 | **Dashboard 404'd on every clean clone; stats were wrong; and the manifest was missing from `dotnet publish`** | Client project restructured to the layout in Umbraco's *Vite package setup* guide (manifest + `lang/` moved into `public/`, output straight into `App_Plugins/…`, build output committed); dedicated stats endpoint replaces client-side aggregation of a capped list; **the explicit `<Content Include>` for `umbraco-package.json` was deleted** — see §3, Lessons 11–13 |

### 2.3 Seven architecture steps

| Step | Change |
|---|---|
| **Compositions** | new `navigationSettings` (one **invariant** `umbracoNaviHide`) + `basePage` (`heading` mandatory + `lead`, per culture); `seoSettings` made **culture-variant** so `/en` gets English SEO |
| **Mandatory fields** | `articleTitle`, `publishDate`, `heroTitle`, `newsListPage.title`, `recentNewsBlock.source`, `callToActionBlock.link` — then the fallback ternaries were deleted from views |
| **Shared partials** | 5 components existed twice (and had drifted) → `Views/Partials/components/_*.cshtml` + view models; 9 wrappers are now ~8 lines each; article card deduplicated 3× → 1 |
| **Invariant keys** | `imagePosition` `Lewo/Centruj/Prawo` → `left/center/right`; CTA colour now driven by the picker's **hex value**, not its label |
| **Extended `blockSettings`** | `spacingTop`/`spacingBottom`/`anchorId`/`backgroundColor` + built-in True/false; one `BlockChrome` record computes it for every block — **editors now control vertical rhythm** |
| **searchPage + facet** | `resultsPerPage` property; **tag facet chips with counts**, tag-only browsing, pager preserving `q` *and* `tag` |
| **Data types** | **67 → 54** live: 7 properties repointed to canonical types, 13 orphans/duplicates deleted, misleading names fixed, project types foldered (38 Umbraco stock types deliberately left alone) |

### 2.4 Search, rebuilt properly

Beyond the culture bug, the whole search stack was restructured — worth reading as a reference implementation:

- `SearchPageController` (a **route-hijacking `RenderController`**) owns HTTP concerns; the view has no service calls or query-string parsing.
- `IArticleSearchService` returns a **`SearchHit` DTO** — no `IPublishedContent` leaks into views.
- **Relevance boosting**: title ×3, tag ×2.5, node name ×2, plus a managed query for recall.
- **Unified search**: typing `premiera` finds articles *tagged* Premiera too — no `#` syntax required (`#tag` remains as an explicit filter, and the facet chips make tags discoverable).
- Tag matching uses the **guid token** inside the stored UDI — exact, single-term, no hand-written Lucene.

---

## 3. Thirteen lessons — the real value of this round

**1. A green page proves nothing if the build was red.**
Twice, a verification "passed" against a **stale binary** (the app was running, so the build failed on a file lock, and `--no-build` booted the old code). The tell the second time: a redirect pointed at `/pl` *without* a trailing slash — literally the output of the code I thought I had disabled.
→ *Rule: confirm `0 errors` (and, when in doubt, the binary timestamp) before trusting any runtime observation.*

**2. Invalid configuration keys fail silently.**
`Error404Page` isn't a real key — .NET configuration binding ignores unknown keys, so the custom 404 was never configured, in Phase 1 either. Nothing warned, because `appsettings.json`'s `$schema` points at a stub file instead of `appsettings-schema.Umbraco.Cms.json`.
→ *Rule: point `$schema` at the real schema and let the IDE validate; treat "the feature just doesn't happen" as a config-key suspect.*

**3. Some features need an operational step, not code.**
The tag filter was fully implemented and completely inert because the **Delivery API index had never been rebuilt**. Code + config + *index state* all have to line up.
→ *Rule: if you add an `IContentIndexHandler`, rebuilding the index is part of the feature — and belongs in the README.*

**4. One variable per experiment.**
My first fix for the tag filter bundled a composer registration *and* an index rebuild. You challenged the registration; isolating it proved Umbraco **auto-discovers** those handlers and the composer was pure noise. I deleted it.
→ *Rule: when two changes could explain a result, test them separately.*

**5. Editor-facing text must never drive logic.**
Razor branched on Polish dropdown labels (`"Prawo"`) and on a colour picker's **label** used as a CSS class. Rename either in the backoffice → silent breakage, no compile error.
→ *Rule: store invariant keys (or stable values like a hex code); labels are for humans only.*

**6. Moving a property into a composition destroys its values.**
A property gains a new property-type id, so content is orphaned. Values for `heading`/`lead`/`umbracoNaviHide` were **captured first**, then re-applied and republished.
→ *Rule: for schema surgery — capture → migrate → verify. Never "just try it" on content you care about.*

**7. Compositions have structural rules.**
`InvalidCompositionException: property groups with the same alias must also have the same type` — `basePage` had a **Group** named "Treść" colliding with the pages' **Tab** "Treść". Making it a Tab let it merge cleanly.
→ *Rule: match container types across compositions; think about where fields will appear for the editor.*

**8. Property editors have storage formats you must respect.**
`Umbraco.DropDown.Flexible` stores an **array** (`["large"]`). Setting the plain string `"large"` silently did nothing — caught only because the rendered spacing didn't change. Similarly `PickedColor` exposes `.Color`, not `.Value`.
→ *Rule: read a real stored value before writing one programmatically.*

**9. Umbraco auto-discovers more than you think.**
`IFilterHandler`, `IContentIndexHandler` and property value converters are all type-scanned — no composer needed. Services, notification handlers and collection *overrides* do need registration.
→ *Rule: don't add ceremony you haven't verified is required.*

**10. Mandatory fields replace defensive code.**
Every view had `IsNullOrEmpty(x) ? Name : x` — defensive rendering standing in for missing validation. With the schema locked, those ternaries were deleted; publishing an empty title now fails with `400 ContentInvalid`. Note the deliberate exception: **service-level** fallbacks stayed, because uSync imports and API writes bypass validation.
→ *Rule: views may trust the schema; services trust nothing.*

**11. Declaring an MSBuild item can *remove* it from the output.**
`App_Plugins/**` is globbed into build and publish output by **Umbraco's own MSBuild targets** — you don't need to declare anything. The project had `<Content Include="App_Plugins\…\umbraco-package.json" />` (no `CopyToPublishDirectory`), which *overrode* that globbing: the manifest was in neither `bin/` nor `dotnet publish` output. Locally the dashboard still worked, because in development Umbraco reads `App_Plugins` from the project folder — so this would only have appeared **after deploying**. Proved by deleting the line: all four plugin files then published correctly.
→ *Rule: don't hand-declare files a framework already handles; if you must, always set the copy semantics — and verify with `dotnet publish`, not just `dotnet build`.*

**12. Read the framework's own layout before inventing build wiring.**
My first fix for the dashboard was ~20 lines of MSBuild in the `.csproj` — a target running `npm ci && vite build`, made incremental with `Inputs`/`Outputs`, plus a `SkipClientBuild` escape hatch. It worked. It was also all avoidable. Umbraco documents the layout (*Extend your project → Backoffice extensions → Development flow → Vite package setup*), and the documented shape removes the problem instead of automating around it:

| | Before | Docs-aligned |
|---|---|---|
| Manifest lives in | `App_Plugins/…/` (hand-written) | `Client/…/public/` (Vite **copies** it out) |
| Vite `outDir` | `App_Plugins/…/dist` | `App_Plugins/…` — the whole folder is output |
| `App_Plugins` folder is | half source, half generated | **100% generated** (so `emptyOutDir` is safe) |
| Build | MSBuild target, `npm` required to run the site | `npm run build`/`watch`; output committed, no Node to run |
| `.csproj` | ~20 lines | 2 lines, neither about the dashboard's build |

Two details make it click. First, `public/` — putting the manifest and `lang/*.js` there is what lets the whole output folder be disposable. Second, the `.gitignore` rule was `**/dist/`; with no `dist/` folder in the documented layout, the output is committable **without touching `.gitignore`**. The remaining two `.csproj` lines are unrelated to building: `<Content Remove="Client\**" />` (the Web SDK's `**/*.json` glob was publishing `package-lock.json` and a dead second manifest — caught by inspecting `dotnet publish` output) and `TypeScriptCompileBlocked`, which is the guide's own warning about Visual Studio compiling the `.ts` files alongside Vite.
→ *Rule: when your fix is clever, check whether the docs describe a shape where the problem doesn't exist. Automating around a bad layout is the second-best answer.*

**13. An `App_Plugins` folder is a *package*, not a feature.**
The folder was called `EditorialStatsDashboard` — named after one feature, and after the extension *type* the manifest already declares (`"type": "dashboard"`). The docs settle how this should be scoped: Umbraco scans `App_Plugins` **two levels deep** for `umbraco-package.json`, the folder name is free-form, and one manifest holds *"an array of Extension Manifest objects"* — any number, of any mix of types. So a per-feature folder buys nothing and costs a duplicate `node_modules`, `vite.config.ts`, `tsconfig.json` and build step to keep in sync. Renamed to `Client/AutomotiveInfo` → `App_Plugins/AutomotiveInfo`, with the feature nested at `src/editorial-stats/` and Vite's **object-form `entry`** giving each feature its own bundle — so adding a property editor later is a folder, an entry key and a manifest object, not a second project. Your own alias convention was already right (`AutomotiveInfo.EditorialStats.Dashboard` = `<Package>.<Feature>.<Type>`); only the folder disagreed with it. Two things the rename exposed: multi-entry `es` builds emit `<name>.mjs` unless you pin `fileName`, which would have 404'd the manifest's `element` path silently; and custom element tags are global across every installed package, so they want a product prefix (`automotive-editorial-stats`).
→ *Rule: name the folder after what ships, not after what's in it today. Per-feature packages are for things with their own version number and consumers.*

---

## 4. Corrections to the Phase-2 Review

The review was written from static analysis; runtime testing corrected six points. Trust this list over the earlier document:

1. **`TagFilterHandler` didn't need registration** — handlers are auto-discovered (proved by disabling my composer). The real causes were the missing index rebuild and case-sensitivity.
2. **Property value converters are also auto-discovered** — the old `RegisterCustomConvertersComposer` was deleted, not replaced.
3. **The 404 root cause was deeper** than "unpublished node": the config key itself was invalid (`Error404Page` vs `Error404Collection`), so Phase 1's "content 404 ✓" was never true.
4. **`singleblock/default.cshtml` is not dead code** — it's Umbraco's stock `SingleBlockPartialWithFallback` scaffold. Kept; only its `singleBlock/` → `singleblock/` casing was fixed (a real Linux/Cloud hazard).
5. **The `Textarea` data type is actually an `Umbraco.TextBox`** — worse than "misleading name"; renamed to `TextBox [512 znaków]` and made canonical.
6. **`Umbraco.DropDown.Flexible` cannot separate value from label**, so invariant keys are visible to editors. Compensated with Polish property descriptions rather than pretending otherwise.

---

## 5. Patterns now in the codebase worth studying

| Pattern | Where | Why it matters |
|---|---|---|
| Service owns query + cache + culture; controllers stay thin | `News/NewsArticleService.cs` → `Controllers/NewsApiController.cs` | one source of truth for "the articles"; the list page, the block and the API cannot drift |
| DTO boundary | `NewsArticleDto`, `SearchHit`, `NewsStatsDto` | views and API consumers never touch `IPublishedContent` |
| Route-hijacking controller + view model | `SearchPageController` + `SearchViewModel` | HTTP concerns out of Razor; testable |
| Decorator over a core converter | `PropertyValueConverters/ReadingTimeRichTextValueConverter.cs` | extend framework behaviour **without** reimplementing it (core ctor churns between versions — composition survives upgrades) |
| Shared partials + view-model factory | `Views/Partials/components/` + `Models/Blocks/BlockViewModels.cs` | markup exists once; Block List and Block Grid share it |
| Chrome from shared settings | `BlockChrome` + `ToChrome(settings)` | one place turns editor settings into classes/styles |
| Backoffice-only API + auth | `Controllers/EditorialStatsController.cs` (`ManagementApiControllerBase`) + `UMB_AUTH_CONTEXT` in the Lit element | editorial data is not public; this is the v14+ pattern |
| Cache invalidation by signal | `Caching/NewsCacheSignal.cs` + `NewsPublishedCacheInvalidationHandler` | invalidate N keys without enumerating them |

---

## 6. Your worklist — what is still yours to do

Ordered by leverage. Acceptance criteria included so "done" is unambiguous.

### ① Tests + CI — do this first
The highest-value thing missing. **One integration test would have caught bugs 1, 2 and 3 on the day they were written.**
- Add a test project (`WebApplicationFactory<Program>`); `Program.cs` needs `public partial class Program { }` for the factory to reference.
- Boot against **SQLite** with `uSync ImportAtStartup: All` so your committed uSync files *are* the fixtures — schema-as-code paying off.
- Assertions to write: `/api/v1/news/latest?culture=en` → every URL starts `/en/` · `?culture=xx` → 400 · `/api/v1/news/stats` → `totalArticles == Σ tagCounts` · Delivery API `?filter=tag:premiera` == `tag:PREMIERA` · `componentText` contains both `readingTimeMinutes` **and** `blocks` · editorial-stats → 401 without a token.
- GitHub Actions: `setup-dotnet` + `setup-node`, `dotnet build` (builds the dashboard too), `dotnet test`.
- **Done when:** a PR shows a green check, and deliberately breaking a culture makes it red.

### ② Secrets — move and rotate
`appsettings.json` still contains the sa password, the unattended admin password, the Delivery API key and the imaging HMAC key; the admin password is also printed in `README.md`.
- Move to user-secrets / environment variables (the project already has a `UserSecretsId`), commit placeholders.
- **Rotate them** — they are in git history, so deleting is not enough.
- Also set `DeliveryApi:PublicAccess = false` (your own C4 deliverable, silently reverted in commit `66585e2`) and re-enable `RedirectUrlTracking` (currently disabled, so page renames lose their 301s).
- **Done when:** a fresh clone needs local secrets to run, and no credential appears in `git grep`.

### ③ Tailwind: real build instead of the CDN
`_Layout.cshtml` loads `cdn.tailwindcss.com` — a dev-only build: no purge, no SRI, breaks CSP, and one outage from an unstyled site. Your `Client/` Vite setup can produce a real stylesheet.
- **Watch out:** dynamically-built classes (`md:col-span-@Model.ColumnSpan`) get purged. Use a safelist or a key→literal-class map — the spacing classes in `BlockChrome` are already written as literals for exactly this reason.
- **Done when:** no CDN script tag, and grid spans + block spacing still render.

### ④ README rewrite
Untouched all phase: wrong port (44309 vs **44328**), plaintext credentials, and none of the new features documented.
- Must cover: Delivery API + key, `/api/v1/news/*`, the backoffice stats endpoint, Swagger, the headless demo, **`npm` as a build prerequisite**, the search page, three cultures + Dictionary, and **the Examine/Delivery index rebuild step**.
- **Done when:** someone clones the repo and gets a working site from the README alone.

### ⑤ Finish the deferred items
- **Decide `es`**: the language, a domain and 20 dictionary translations exist, but **no content has an `es` variant**, so `/es` cannot route. Either publish Spanish content or remove the language + domain.
- **B2 grid areas**: `Main Block Grid` defines no `areas`, so the area partials are unreachable and the plan's "12-column layout with areas" is unmet. Also write the two sentences on *Grid vs List* the plan asked for.
- **`newsEditors` group**: there is still no repo evidence of it. Verify in the backoffice (start node = news list, no top-level publish) and document it in the README — user groups are not uSync-serialized, so documentation *is* the artefact.
- **Search index quality**: search still matches raw Block-List JSON. Add a computed `searchableText` (and indexed tag names) via `TransformingIndexValues`; that also removes the per-search tag lookup.
- **Small ones**: `author.secondName` → `lastName`; delete the unused `websiteContainer.navLinks`; give the 22 document types one-line Descriptions; add Descriptions/labels where editors are guessing; `Program.cs` hardcodes `"pl"` in the root redirect — derive it from `IDefaultCultureAccessor`.

### ⑥ Content hygiene
Two junk articles (`/test`, `/testowy-artykul-zmiana`) are committed, and **no article has an image**, so the responsive-image and `card` crop work can't be seen. Add one real image, delete the junk.

---

## 7. Phase-3 (Umbraco Cloud) readiness

| Checklist item | Before | Now |
|---|---|---|
| Build green & site runs from a clean clone | ✗ (dashboard 404, site was fully down) | **✓** (`dotnet run` only — no Node prerequisite; dashboard output committed) |
| uSync reproduces the schema | ✗ (import crashed) | **✓** (Report: 160 actions, 0 failures) |
| README current | ✗ | ✗ — worklist ④ |
| Branch + PR by reflex | ✗ (12 of 25 commits direct to main) | your habit to fix |
| Can explain the publish/deploy model | ✗ | partly — note that `IMemoryCache` invalidation is **per-process**; load-balanced Cloud needs `ICacheRefresher` |
| Secrets out of the repo | ✗ | ✗ — worklist ② |

**2 of 6 → 4 of 6 after worklist ② and ④.**

---

## 8. Working notes

- **Nothing is committed.** Review with `git diff` / `git status`; the uSync folder changes are schema + content exports that accompany the code.
- **Domains are relative** (`/pl`, `/en`) and therefore portable — no hostname is baked into the repo any more. Keep it that way; hostname domains are environment configuration (Umbraco Deploy never transfers them).
- **After any schema change:** confirm the `uSync/v17` diff and commit it with the code. After an export, run `git status -- uSync/` and look for an *added* Domains file with no matching deletion — that is always a duplicate (the bug that started all this).
- The app is currently **stopped** and port 44328 is free.
