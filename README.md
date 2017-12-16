# not-in-toc

This tool helps you find TOC-related topic issues.

## Usage

  -d, --directory           Required. Directory to search for .md files.

  -r, --recursive           (Default: False) Search directory and all
                            subdirectories.

  -o, --orphans             Use this option to find orphaned topics.

  -m, --multiples           Use this option to find topics that appear more
                            than once in one or multiple TOC.md file.

  -i, --ignore_redirects    (Default: True) Ignore .md files that have a
                            redirect_url tag when looking for orphans.

  --help                    Display this help screen.

## Usage examples

Search for orphaned topics non-recursively:
NotInToc.exe -o -d c:\Users\gewarren\visualstudio-docs-pr\docs\ide

Search for orphaned topics recursively, ignoring topics that have a redirect_url tag:
NotInToc.exe -o -r -i false -d c:\Users\gewarren\visualstudio-docs-pr\docs\ide

Search for topics that appear more than once in one or more TOC files, recursively:
NotInToc.exe -m -r -d c:\Users\gewarren\visualstudio-docs-pr\docs\extensibility

## Future functionality ideas

- Show topics that appear multiple times in the same TOC.
- Given a file name, show the TOC files it is referenced in (although this type of search can easily be done in VS Code, with a search scope of TOC.md files).
- Group different types of output, e.g. files not in a TOC, then files with same name but different path in TOC.
- Show image files that aren't linked to anywhere.
- Show include files that aren't linked to anywhere.
- Show files in TOCs that have redirect_urls. (Note: should this handle central redirect files too?)
- Do a friendly diff of two TOC files: number of topics in each, topics in one file but not the other, sub-node comparisons, (nodes that have a link in one file but not the other)
