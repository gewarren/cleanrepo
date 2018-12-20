# CleanRepo

This command-line tool helps you find topics that aren't linked from a TOC file. It can also find, and optionally delete, orphaned .png files and orphaned .md files in an 'includes' directory. It can also find, and optionally replace, links to redirected files.

## Usage

  -d, --directory            Required. Directory to start search for markdown files, or the media directory to search in for orphaned
                              .png files, or the directory to search in for orphaned INCLUDE files.

  -o, --orphaned_topics      Use this option to find orphaned topics.

  -m, --multiples            Use this option to find topics that appear more than once in one or separate TOC.md files.

  -p, --orphaned_images      Use this option to find orphaned .png files.

  -i, --orphaned_includes    Use this option to find orphaned INCLUDE files.

  -g, --delete               (Default: False) Set to true to delete orphaned markdown or .png files.

  -l, --redirects            Finds backlinks to redirected files in the
                             specified directory.

  -r, --replace_redirects    (Default: False) Set to true to replace links to redirected files with their target URL.

  -s, --recursive            (Default: True) Search directory and all subdirectories.

  -v, --verbose              (Default: False) Output verbose results.

  --help                     Display this help screen.

## Usage examples

Find orphaned topics recursively:

```
CleanRepo.exe -o -d c:\repos\visualstudio-docs-pr\docs\ide
```

Find orphaned topics non-recursively:

```
CleanRepo.exe -o -s false -d c:\repos\visualstudio-docs-pr\docs\ide
```

Find orphaned .png files (recursive):

```
CleanRepo.exe -p -d c:\repos\visualstudio-docs-pr\docs\ide\media
```

Find and delete orphaned INCLUDE files (recursive):

```
CleanRepo.exe -i -g -d c:\repos\visualstudio-docs-pr\docs\ide\includes
```

Find topics with backlinks to redirected topics:

```
CleanRepo.exe -l -d c:\repos\visualstudio-docs-pr\docs\ide
```

Search recursively for topics that appear more than once in one or more TOC files:

```
CleanRepo.exe -m -d c:\repos\visualstudio-docs-pr\docs\ide
```

## Future functionality ideas

- Given a file name, show the TOC files it is referenced in (although this type of search can easily be done in e.g. VS Code,
  with a search scope of TOC.md files).
- Find orphaned code snippets.
- Do a friendly diff of two TOC files: number of topics in each, topics in one file but not the other,
  sub-node comparisons, (nodes that have a link in one file but not the other)
