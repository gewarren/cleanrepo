# CleanRepo

This command-line tool helps you clean up a DocFx-based content repo. It can:

- find and delete markdown files that aren't linked from a TOC file
- find and delete orphaned image (.png, .jpg, .gif) files
- find and delete orphaned "shared" markdown files
- find and replace links to redirected files
- replace site-relative links with file-relative links
- find topics that appear more than once in a TOC file

## Usage

  -d, --directory            Required. Directory to start search for markdown files, or the media directory to search in for
                             orphaned .png/.gif/.jpg files, or the directory to search in for orphaned INCLUDE files.

  -o, --orphaned-topics      Use this option to find orphaned topics.

  -m, --multiples            Use this option to find topics that appear more than once in one or separate TOC.md files.

  -p, --orphaned-images      Find orphaned .png, .gif, or .jpg files.

  -i, --orphaned-includes    Find orphaned INCLUDE files.

  -g, --delete               (Default: False) Delete orphaned markdown or .png/.jpg/.gif files.

  -l, --redirects            Find backlink to redirected files in the specified directory.

  -r, --replace-redirects    (Default: False) Replace links to redirected files with their target URL.

  -f, --redirects-file       Optionally specify a path to a redirect JSON file in a different repo.

  --relative-links           Replace site-relative links with file-relative links. You must also specify the docset name for
                             the repo.

  --docset-name              The docset name that corresponds to the root of this repo in a URL, e.g. 'visualstudio' in
                             'http://docs.microsoft.com/visualstudio/ide/get-started'.

  --docset-root              The full path to the root directory for the docset, e.g. 'c:\users\gewarren\dotnet-docs\docs'.

  -s, --recursive            (Default: True) Search directory and all subdirectories for markdown, yaml, image, and
                             include files (depending on chosen function).

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

Find and delete orphaned .png/.gif/.jpg files (recursive):

```
CleanRepo.exe -p -g -d c:\repos\visualstudio-docs-pr\docs\ide
```

Find and delete shared markdown files that are orphaned (recursive):

```
CleanRepo.exe -i -g -d c:\repos\visualstudio-docs-pr\docs\ide
```

Find topics with backlinks to redirected topics and replace the links with their target URL:

```
CleanRepo.exe -l -r -d c:\repos\visualstudio-docs-pr\docs\ide
```

Replace site-relative links to the specified docset with file-relative links, when the file exists:

```
CleanRepo.exe --relative-links --docset-name visualstudio --docset-root c:\repos\visualstudio-docs-pr\docs -d c:\repos\visualstudio-docs-pr\docs\ide
```

> [!TIP]
> Some redirect targets are themselves redirected to yet another target. For this reason, it's recommended to run the `CleanRepo.exe -l -r` command repeatedly until it no longer finds any links to redirected topics.

Search recursively for topics that appear more than once in a TOC file:

```
CleanRepo.exe -m -d c:\repos\visualstudio-docs-pr\docs\ide
```

## Future functionality ideas...

- Find orphaned code snippets
- Consolidate links in a redirection JSON file (i.e. where a target is itself redirected)
