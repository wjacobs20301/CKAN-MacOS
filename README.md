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

If you use this app, please also consider supporting and contributing to the [upstream project][upstream]. Metadata issues, resolver bugs, and feature requests that aren't macOS-specific should go to the [upstream issue tracker][upstream-issues].

## What is CKAN?

CKAN is a metadata repository and associated tools that let you find, install, and manage mods for Kerbal Space Program. It provides strong assurances that mods are installed the way their metadata prescribes, for the correct KSP version, alongside their dependencies, and without conflicts.

- **Players** can find new content and install it in a few clicks.
- **Modders** don't have to worry about misinstall problems or outdated versions.

The CKAN metadata format was inspired by the proven formats of the Debian project and CPAN.

## What's new in the macOS build

- **Native macOS app** — double-click `CKAN.app`; no Terminal, no Mono.
- **Universal binary** — Apple Silicon (arm64) and Intel (x86_64).
- **Retina / high-DPI** display support.
- **Feature parity** with the Windows GUI (install, update, remove, dependency resolution, repository management).
- **Automatic migration** of data from the legacy Mono-era install.

Built on [Avalonia][avalonia] 11. The same Avalonia GUI also runs on Linux via `ckan gui`.

## The CKAN spec

At the core of CKAN is the **[metadata specification](Spec.md)**, with a corresponding [JSON Schema](CKAN.schema) also mirrored in the [Schema Store][schemastore]. This repository includes a validator you can use to [validate your files][validate].

## For players

See the upstream [User guide][userguide] to get started.

## For modders

While anyone can contribute metadata for your mod, you know your mod best. Contributors endeavor to be accurate, but mod-author involvement helps. If metadata is incorrect, please [open an issue upstream][netkan-issues].

## Contributing

**No technical expertise is required to contribute to CKAN.** See the upstream [CONTRIBUTING][contributing] guide.

For **macOS-specific** issues (packaging, the Avalonia GUI on macOS, the `.dmg` installer, arm64/x86_64 builds), open an issue in this repository. Everything else is best reported upstream.

## Building

See [CLAUDE.md](CLAUDE.md) for build instructions. Briefly:

```bash
./build.sh                      # build ckan.exe + netkan.exe
./build.sh --target=Test        # run tests
dotnet run --project Cmdline -f net8.0 -- gui   # run the Avalonia GUI
```

---

Note: Looking for the Open Data portal software called CKAN? Their GitHub repository is [here][otherckan].

 [upstream]: https://github.com/KSP-CKAN/CKAN
 [upstream-issues]: https://github.com/KSP-CKAN/CKAN/issues
 [contributors]: https://github.com/KSP-CKAN/CKAN/graphs/contributors
 [releases]: https://github.com/wjacobs20301/CKAN-MacOS/releases/latest
 [avalonia]: https://avaloniaui.net/
 [schemastore]: https://schemastore.org/
 [validate]: https://github.com/KSP-CKAN/CKAN/wiki/Adding-a-mod-to-the-CKAN#verifying-metadata-files
 [userguide]: https://github.com/KSP-CKAN/CKAN/wiki/User-guide
 [netkan-issues]: https://github.com/KSP-CKAN/NetKAN/issues/new
 [contributing]: https://github.com/KSP-CKAN/.github/blob/master/CONTRIBUTING.md
 [otherckan]: https://github.com/ckan/ckan
