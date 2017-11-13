# not-in-toc

This tool helps you find TOC-related topic issues.

## Current functionality

The not-in-toc.exe utility accepts a single input, that is a path to a Windows directory. The utility finds all TOC.md files recursively, starting either in the specified directory, if it contains a docfx.json file, or in the first parent directory that contains a docfx.json file. For each Markdown (\*.md) file in the specified directory and all subdirectories, the utility looks for a matching entry in any of the TOC.md files. The full path to the file is compared. The utility ignores \*.md files in directories named *Includes*. It also ignores files that have a *redirect_url* metadata tag. The utility writes all the files that weren't found in a TOC.md file to the console.

## Future functionality

- Show topics that appear in more than 1 TOC file.
- Show topics that appear multiple times in the same TOC.
- Group different types of output, e.g. files not in a TOC, then files with same name but different path in TOC.
