# CleanRepo

This command-line tool helps you clean up a content repo. It can:

- find topics that aren't linked from a TOC file
- find, and optionally delete, orphaned image (.png) files
- find, and optionally delete, orphaned "shared" markdown files
- find, and optionally replace, links to redirected files
- find topics that appear more than once in a TOC file

## Usage

  -d, --directory            Required. Directory to start search for markdown files, or the media directory to search the search for                                  orphaned .png files, or the directory to start the search for orphaned INCLUDE files.

  -o, --orphaned_topics      Use this option to find orphaned topics.

  -m, --multiples            Use this option to find topics that appear more than once in one or separate TOC.md files.

  -p, --orphaned_images      Use this option to find orphaned .png files.

  -i, --orphaned_includes    Use this option to find orphaned INCLUDE files (looks in folders named 'includes' or '\_shared').

  -g, --delete               (Default: False) Set to true to delete orphaned markdown or .png files.

  -l, --redirects            Finds backlinks to redirected files in the specified directory.

  -r, --replace_redirects    (Default: False) Set to true to replace links to redirected files with their target URL.

  -s, --recursive            (Default: True) Search directory and all subdirectories.

  --help                     Display this help screen.

## Usage examples

Find orphaned topics recursively (that is, in the specified directory and any subdirectories):

```
CleanRepo.exe -o -d c:\repos\visualstudio-docs-pr\docs\ide
```

Find orphaned topics non-recursively (that is, only in the specified directory):

```
CleanRepo.exe -o -s false -d c:\repos\visualstudio-docs-pr\docs\ide
```

Find orphaned .png files (recursive):

```
CleanRepo.exe -p -d c:\repos\visualstudio-docs-pr\docs\ide
```

Find and delete shared markdown files that are orphaned (recursive):

```
CleanRepo.exe -i -g -d c:\repos\visualstudio-docs-pr\docs\ide
```

Find topics with backlinks to redirected topics and replace the links with their target URL:

```
CleanRepo.exe -l -r -d c:\repos\visualstudio-docs-pr\docs\ide
```

> [!TIP]
> Some redirect targets are themselves redirected to yet another target. For this reason, it's recommended to run the `CleanRepo.exe -l -r` command repeatedly until it no longer finds any links to redirected topics.

Search recursively for topics that appear more than once in a TOC file:

```
CleanRepo.exe -m -d c:\repos\visualstudio-docs-pr\docs\ide
```

## Future functionality ideas...

- Find orphaned code snippets
- Replace site-relative links with file-relative links (for files in the same docset)
