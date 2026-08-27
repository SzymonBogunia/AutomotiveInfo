# Backoffice extensions

`Client/AutomotiveInfo` is one Vite package holding **all** of this site's backoffice
extensions, following the official guide: **Umbraco CMS → Extend your project → Backoffice
extensions → Development flow → Vite package setup**.

```
Client/AutomotiveInfo/                 ← source
  src/editorial-stats/                   one folder per feature
    editorial-stats.element.ts
  public/umbraco-package.json            the manifest  ─┐ copied verbatim into
  public/lang/{en,pl}.js                 localizations ─┘ the output by Vite
  vite.config.ts  package.json  tsconfig.json

App_Plugins/AutomotiveInfo/            ← 100% BUILD OUTPUT — never hand-edit
  editorial-stats.js(.map)
  umbraco-package.json
  lang/en.js  lang/pl.js
```

## Working on it

```bash
cd Client/AutomotiveInfo && npm install && npm run watch
```

`npm run watch` rebuilds into `App_Plugins/` on every save; reload the backoffice to pick it up.
`npm run build` is the one-shot production build and also type-checks (`tsc --noEmit`), which
matters because Vite never validates the `@umbraco-cms/*` imports it externalises.

## The folder is a package, not a feature

Umbraco scans `App_Plugins` two levels deep for `umbraco-package.json`, and **one manifest can
declare any number of extensions** — dashboards, property editors, workspace views,
localizations. So adding a feature is three small steps, not a new project:

1. `src/<feature>/<feature>.element.ts`
2. a key in `build.lib.entry` in `vite.config.ts` — each key becomes its own bundle, so the
   backoffice loads only what it needs
3. an object in `extensions` in `public/umbraco-package.json`

Keep the alias convention `AutomotiveInfo.<Feature>.<Type>`, and prefix localization keys per
feature (`editorialStats_label`) so one `lang/en.js` serves every feature. Custom element tag
names are global across all installed packages, so prefix them too: `automotive-<feature>`.

A separate package folder is only warranted for something shipped independently — its own
NuGet package, version and consumers.

## Two rules

1. **Never hand-edit anything under `App_Plugins/AutomotiveInfo/`.** `emptyOutDir: true` wipes
   that folder on every build. Anything that must ship — the manifest, localization files,
   icons — belongs in `public/`, which Vite copies over.
2. **Commit the build output.** It is ~15 KB and it means `git clone && dotnet run` gives a
   working backoffice with no Node installed. The `**/dist/` rule in `.gitignore` does not reach
   it, because the documented layout has no `dist/` folder. Rebuild and commit whenever you
   change `src/` or `public/`.

Two gotchas worth knowing, both found the hard way:

- Multi-entry `es` builds emit `<name>.mjs` unless you pin `fileName` — the manifest's `element`
  path then 404s silently.
- The `.csproj` deliberately contains **no** build wiring: `App_Plugins/**` is globbed into build
  and publish output by Umbraco's own MSBuild targets. Adding an explicit `<Content Include>`
  for a file in there actually *breaks* `dotnet publish` — it overrides that globbing, and
  without `CopyToPublishDirectory` the file is silently dropped.
