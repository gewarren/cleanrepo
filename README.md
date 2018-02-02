# not-in-toc

This tool helps you find TOC-related topic issues.

## Usage

  -d, --directory           Required. Directory to start search for markdown
                            files or media directories.

  -r, --recursive           (Default: True) Search directory and all
                            subdirectories.

  -o, --orphaned_topics     Use this option to find orphaned topics.

  -m, --multiples           Use this option to find topics that appear more
                            than once in one or separate TOC.md files.

  -p, --orphaned_images     Use this option to find orphaned images.

  -i, --ignore_redirects    (Default: True) Ignore .md files that have a
                            redirect_url tag when looking for orphans.

  -v, --verbose             (Default: False) Output verbose results.

  --help                    Display the help screen.

## Usage examples

Search for orphaned topics recursively:
NotInToc.exe -o -d c:\Users\gewarren\visualstudio-docs-pr\docs\ide

Search for orphaned topics non-recursively, ignoring topics that have a redirect_url tag:
NotInToc.exe -o -r false -i false -d c:\Users\gewarren\visualstudio-docs-pr\docs\ide

Search recursively for topics that appear more than once in one or more TOC files:
NotInToc.exe -m -d c:\Users\gewarren\visualstudio-docs-pr\docs\extensibility

## Future functionality ideas

- Given a file name, show the TOC files it is referenced in (although this type of search can easily be done in e.g. VS Code,
  with a search scope of TOC.md files).
- Show orphaned include files.
- Show orphaned code snippets.
- Show files in TOCs that have redirect_urls. (Note: should this handle central redirect files too?)
- Do a friendly diff of two TOC files: number of topics in each, topics in one file but not the other,
  sub-node comparisons, (nodes that have a link in one file but not the other)
