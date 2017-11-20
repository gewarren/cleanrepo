# not-in-toc

This tool helps you find TOC-related topic issues.

## Current functionality

The not-in-toc.exe utility accepts a single input, that is a path to a Windows directory. The utility finds all TOC.md files recursively, starting either in the specified directory, if it contains a docfx.json file, or in the first parent directory that contains a docfx.json file. For each Markdown (\*.md) file in the specified directory and all subdirectories, the utility looks for a matching entry in any of the TOC.md files. The full path to the file is compared. The utility ignores \*.md files in directories named *Includes*. It also ignores files that have a *redirect_url* metadata tag. The utility writes all the files that weren't found in a TOC.md file to the console.

## Future functionality

- Show topics that appear in more than 1 TOC file.
- Show topics that appear multiple times in the same TOC.
- Group different types of output, e.g. files not in a TOC, then files with same name but different path in TOC.
- Given a file name, show the TOC file(s) it is referenced in.
- Show image files that aren't linked to anywhere.
- Show include files that aren't linked to anywhere.
- Show files in TOCs that have redirect_urls. (Note: should this handle central redirect files too?)
- Do a friendly diff of two TOC files: number of topics in each, topics in one file but not the other, sub-node comparisons, (nodes that have a link in one file but not the other)
