.PHONY: clean app verify-universal

CONFIGURATION?=Debug
NAME:=CKAN
APPNAME:=$(NAME).app
OUTDIR:=../_build/osx
DMGROOT:=$(OUTDIR)/dmg
APPROOT=$(DMGROOT)/$(APPNAME)
DMG:=$(OUTDIR)/$(NAME).dmg
SCRIPTSRC:=CKAN
SCRIPTDEST:=$(APPROOT)/Contents/MacOS/CKAN
BUILDDIR_x86_64_SRC:=../_build/out/CKAN-CmdLine/$(CONFIGURATION)/bin/net8.0/osx-x64/publish
BUILDDIR_x86_64_DEST:=$(APPROOT)/Contents/MacOS/x86_64
BUILDDIR_arm64_SRC:=../_build/out/CKAN-CmdLine/$(CONFIGURATION)/bin/net8.0/osx-arm64/publish
BUILDDIR_arm64_DEST:=$(APPROOT)/Contents/MacOS/arm64
ICNSSRC:=../assets/ckan.icns
ICNSDEST:=$(APPROOT)/Contents/Resources/CKAN.icns
PLISTSRC:=Info.plist.in
PLISTTMP:=$(OUTDIR)/Info.plist
PLISTDEST:=$(APPROOT)/Contents/Info.plist
CHANGELOGSRC:=../CHANGELOG.md
VERSION:=$(shell egrep '^\s*\#\#\s+v.*$$' $(CHANGELOGSRC) | head -1 | sed -e 's/^\s*\#\#\s\+v//' )

$(DMG): app
	mkdir -p $(OUTDIR)
	rm -f $@
	@sh ./build-dmg.sh "$(NAME)" "$(DMGROOT)" "$@" "dmg-background.jpg"

app: $(BUILDDIR_x86_64_DEST) $(BUILDDIR_arm64_DEST) $(SCRIPTDEST) $(ICNSDEST) $(PLISTDEST) verify-universal
	@# rdfind + symlinks are optional disk-space optimization. Skip if not installed.
	@if command -v rdfind >/dev/null 2>&1 && command -v symlinks >/dev/null 2>&1; then \
	  echo "Deduplicating identical files across arch slices..."; \
	  rdfind -minsize 10240 -makeresultsfile false -makesymlinks true $(BUILDDIR_x86_64_DEST) $(BUILDDIR_arm64_DEST); \
	  symlinks -rc $(BUILDDIR_arm64_DEST); \
	else \
	  echo "Note: install rdfind + symlinks (\`brew install rdfind symlinks\`) to shrink the bundle."; \
	fi

# Sanity-check that both runtime slices were actually produced with the
# expected Mach-O architecture. A missing slice = a broken universal build.
verify-universal: $(BUILDDIR_x86_64_DEST) $(BUILDDIR_arm64_DEST)
	@echo "Verifying x86_64 slice..."
	@file $(BUILDDIR_x86_64_DEST)/CKAN-CmdLine | grep -q 'Mach-O.*x86_64' \
	  || (echo "ERROR: x86_64 slice is missing or wrong arch" && exit 1)
	@echo "Verifying arm64 slice..."
	@file $(BUILDDIR_arm64_DEST)/CKAN-CmdLine | grep -q 'Mach-O.*arm64' \
	  || (echo "ERROR: arm64 slice is missing or wrong arch" && exit 1)
	@echo "Both architectures verified."

$(BUILDDIR_x86_64_DEST): $(BUILDDIR_x86_64_SRC)
	mkdir -p $(shell dirname $@)
	ditto $< $@

$(BUILDDIR_arm64_DEST): $(BUILDDIR_arm64_SRC)
	mkdir -p $(shell dirname $@)
	ditto $< $@

$(SCRIPTDEST): $(SCRIPTSRC)
	mkdir -p $(shell dirname $@)
	ln -f $< $@

$(ICNSDEST): $(ICNSSRC)
	mkdir -p $(shell dirname $@)
	ln -f $< $@

$(PLISTDEST): $(PLISTSRC) $(CHANGELOGSRC)
	mkdir -p $(shell dirname $@)
	sed -e 's/@VERSION@/$(VERSION)/' $< > $(PLISTTMP)
	# plutil is stock on macOS; converts text plist → binary and validates.
	plutil -convert binary1 -o $@ $(PLISTTMP)

clean:
	rm -rf $(OUTDIR)
