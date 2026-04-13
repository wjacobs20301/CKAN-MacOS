# CKAN for macOS

> A native macOS build of **[CKAN — the Comprehensive Kerbal Archive Network][upstream]**, the mod manager for Kerbal Space Program.
>
> **Download:** grab `CKAN.dmg` from the [latest release][releases].

---

## Credits and attribution

This project is a **macOS-focused fork** of the original CKAN project:

- **Upstream project:** [KSP-CKAN/CKAN][upstream]
- **Original authors:** the CKAN contributors — see the [full contributor list][contributors]
- **License:** MIT (see [LICENSE.md](LICENSE.md)), copyright the Comprehensive Kerbal Archive Network (CKAN) Authors

Essentially all of the mod manager — the metadata specification, dependency resolver, registry, installer, download manager, and game-integration logic — is the work of the upstream CKAN authors. This fork's contribution is a native macOS application layer on top of that core: an Avalonia-based GUI, a universal (arm64 + x86_64) build, macOS packaging (`.app` / `.dmg`), and migration from the legacy Mono-era install.

If you use this app, please also consider supporting and contributing to the [upstream project][upstream]. Metadata issues (wrong version, bad download URL, missing dependencies, etc.) should go to the [upstream NetKAN repo][netkan-issues]; macOS-specific bugs go to [this repo][issues].

## What is CKAN?

CKAN is a metadata repository and associated tools that let you find, install, and manage mods for Kerbal Space Program. It provides strong assurances that mods are installed the way their metadata prescribes, for the correct KSP version, alongside their dependencies, and without conflicts.

- **Players** can find new content and install it in a few clicks.
- **Modders** don't have to worry about misinstall problems or outdated versions.

The CKAN metadata format was inspired by the proven formats of the Debian project and CPAN.

## What's new in the macOS build

- **Native macOS app** — double-click `CKAN.app`; no Terminal, no Mono.
- **Universal binary** — Apple Silicon (arm64) and Intel (x86_64).
- **Retina / high-DPI** display support.
- **Styled installer DMG** — drag-to-Applications with a KSP background.
- **Feature parity** with the Windows GUI (install, update, remove, dependency resolution, repository management).
- **In-app update checker** — notifies you when a new `.dmg` is published on the [releases page][releases]; no silent auto-installs.
- **Automatic migration** of data from the legacy Mono-era install.

Built on [Avalonia][avalonia] 11. The same Avalonia GUI also runs on Linux via `ckan gui`.

## Installing

1. Download `CKAN.dmg` from the [latest release][releases].
2. Open the DMG and drag `CKAN.app` to your Applications folder.
3. Launch CKAN. On first run, point it at your KSP install directory.

Requires **macOS 11 (Big Sur) or later**. Runs natively on both Apple Silicon and Intel Macs.

## Reporting bugs / asking for help

| Problem | Where to report |
|---|---|
| App crashes on macOS, GUI layout bug, DMG issue, build problem | **[This repo's issue tracker][issues]** |
| Wrong mod metadata (bad download, missing dep, wrong version) | [Upstream NetKAN issues][netkan-issues] |
| Core CKAN behavior (resolver, spec, non-macOS GUI) | [Upstream CKAN issues][upstream-issues] |

If you're not sure, open it [here][issues] and we'll route it.

## The CKAN spec

At the core of CKAN is the **[metadata specification](Spec.md)**, with a corresponding [JSON Schema](CKAN.schema) also mirrored in the [Schema Store][schemastore]. This repository includes a validator you can use to [validate your files][validate].

## For players

See the upstream [User guide][userguide] to get started. Most of it applies verbatim to the macOS build; anything macOS-specific is documented in this repo.

## For modders

While anyone can contribute metadata for your mod, you know your mod best. Contributors endeavor to be accurate, but mod-author involvement helps. If metadata is incorrect, please [open an issue upstream][netkan-issues].

## Contributing

**No technical expertise is required to contribute to CKAN.** See the upstream [CONTRIBUTING][contributing] guide for general CKAN contributions.

For **macOS-specific** contributions (packaging, the Avalonia GUI on macOS, the `.dmg` installer, arm64/x86_64 builds, this app's update checker), open a PR or issue [here][issues].

## Building from source

Requires the [.NET 8 SDK][dotnet8] and macOS 11+. Nothing else is mandatory; `brew install rdfind symlinks` is optional and shrinks the DMG by ~50%.

```bash
# Publish both arch slices for the universal bundle
dotnet publish Cmdline/CKAN-cmdline.csproj -c Release -r osx-arm64 --self-contained true
dotnet publish Cmdline/CKAN-cmdline.csproj -c Release -r osx-x64   --self-contained true

# Bundle CKAN.app + CKAN.dmg
make -C macosx CONFIGURATION=Release
```

Outputs land in `_build/osx/`:
- `_build/osx/dmg/CKAN.app` — the bundle
- `_build/osx/CKAN.dmg` — the styled installer image

To develop without bundling:

```bash
dotnet run --project Cmdline -f net8.0 -- gui
```

See [CLAUDE.md](CLAUDE.md) for a deeper tour of the project structure.

## Releasing

To cut a new version:

1. Add a new `## v1.2.3` section at the top of [`CHANGELOG.md`](CHANGELOG.md) — the version number is parsed from this header and baked into the binary, `Info.plist`, and the About dialog.
2. Rebuild: `make -C macosx clean && make -C macosx CONFIGURATION=Release`.
3. Tag and push: `git tag v1.2.3 && git push origin v1.2.3`.
4. Create a [GitHub release][new-release] for the `v1.2.3` tag and attach `CKAN.dmg`. The in-app update checker looks for a `.dmg` asset on the [`/releases/latest`][releases] endpoint.

---

Note: Looking for the Open Data portal software called CKAN? Their GitHub repository is [here][otherckan].

 [upstream]: https://github.com/KSP-CKAN/CKAN
 [upstream-issues]: https://github.com/KSP-CKAN/CKAN/issues
 [contributors]: https://github.com/KSP-CKAN/CKAN/graphs/contributors
 [issues]: https://github.com/wjacobs20301/CKAN-MacOS/issues
 [new-release]: https://github.com/wjacobs20301/CKAN-MacOS/releases/new
 [releases]: https://github.com/wjacobs20301/CKAN-MacOS/releases/latest
 [avalonia]: https://avaloniaui.net/
 [dotnet8]: https://dotnet.microsoft.com/download/dotnet/8.0
 [schemastore]: https://schemastore.org/
 [validate]: https://github.com/KSP-CKAN/CKAN/wiki/Adding-a-mod-to-the-CKAN#verifying-metadata-files
 [userguide]: https://github.com/KSP-CKAN/CKAN/wiki/User-guide
 [netkan-issues]: https://github.com/KSP-CKAN/NetKAN/issues/new
 [contributing]: https://github.com/KSP-CKAN/.github/blob/master/CONTRIBUTING.md
 [otherckan]: https://github.com/ckan/ckan
