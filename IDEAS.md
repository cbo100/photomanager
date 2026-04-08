# Ideas for Future Enhancements

## Organisation

- **Sidecar files** — detect and move/copy associated sidecar files alongside their source image (`.xmp`, `.pp3`, `.dop`, `.cos`, `.nksc`, `.arp`). Match by base filename.
- **Event-based folders** — cluster photos into events using date/time gaps (e.g. photos more than N hours apart = different event). Could use `{Event}` pattern token. Events could be auto-named (e.g. "2024-06-15 Morning") or interactively tagged via a TUI.
- **Interactive event tagging** — TUI interface for reviewing auto-detected events and renaming them before organising
- **Smart duplicate handling** — when a duplicate is found, offer to keep the higher-resolution or better-quality version rather than just skipping
- **Rename files** — option to rename files as part of organisation (e.g. `{Year}{Month}{Day}_{Hour}{Minute}{Second}{Ext}`) to avoid collisions and improve chronological sorting

## Import / Ingest

- **Watch mode** — monitor a folder (e.g. SD card mount point or camera import folder) and auto-organise new files as they arrive
- **Import from device** — detect connected cameras/phones via MTP or DCIM folder patterns and import directly

## Metadata

- **Write back metadata** — optionally embed resolved location name, event name, or other enriched data back into EXIF/XMP
- **Tag-based organisation** — use IPTC keywords or XMP subjects as pattern tokens (e.g. `{Tags}`)
- **Face detection hints** — flag photos containing faces for manual review or separate folder

## Safety & Recovery

- **Undo / operation log** — record every file operation to a log file; support `photomanager undo` to reverse the last run
- **Verify after copy** — SHA256 checksum comparison between source and destination after copy/move to confirm integrity
- **Dry-run diff** — show what changed since the last run (new files, already organised, duplicates)

## Reporting

- **Statistics and reports** — summary after a run: photos processed, duplicates skipped, locations found, events detected, errors
- **Missing metadata report** — flag photos with no date taken, no GPS, or no camera info
- **Timeline view** — visualise photo distribution across years/months in the terminal

## Quality of Life

- **Config file** — persist preferred pattern, mode, extensions etc. in `~/.photomanager/config.toml` so you don't repeat flags every run
- **Shell completions** — generate bash/zsh/fish completion scripts
- **Publish as a single-file tool** — `dotnet publish` as a self-contained native AOT binary for easy distribution
