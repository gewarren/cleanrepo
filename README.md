# CleanRepo

This command-line tool helps you clean up a DocFx-based content repo. It can:

- Find and delete markdown files that aren't linked from a TOC file.
- Find and delete orphaned image (.png, .jpg, .gif, .svg) files.
- Find and delete orphaned "shared" markdown files (includes).
- Trim unused redirect entries from an .openpublishing.redirection.json file.
- Find and replace links to redirected files.
- Replace site-relative links with file-relative links (includes image links).

## Usage

| Command | Description |
| - | - |
| --start-directory | Top-level directory in which to perform clean up (for example, find orphaned markdown files). |
| --docset-root | The full path to the root directory for the docset, e.g. 'c:\users\gewarren\dotnet-docs\docs'. |
| --repo-root | The full path to the local root directory for the repository, e.g. 'c:\users\gewarren\dotnet-docs'. |
| --delete | True to delete orphaned files. |
| --orphaned-topics | Use this option to find orphaned articles. |
| --orphaned-images | Find orphaned .png, .gif, .svg, or .jpg files. |
| --orphaned-snippets | Find orphaned .cs and .vb files. |
| --orphaned-includes | Find orphaned INCLUDE files. |
| --format-redirects | Format the redirection JSON file by deserializing and then serializing with pretty printing. （Recommend doing this before --trim-redirects in a separate commit to make the trimming diff useful.) |
| --trim-redirects | Remove redirect entries for pages that haven't been viewed in a specified number of days. |
| --lookback-days | (Default: 180) The number of history days to check for page-view activity. |
| --output-file | The file to write the redirect page view output to. |
| --replace-redirects | Find backlinks to redirected files and replace with new target. |
| --relative-links | Replace site-relative links with file-relative links.  You must also specify the docset name for the repo. |

## Usage examples

- Find orphaned articles recursively (that is, in the specified directory and any subdirectories):

  ```
  CleanRepo.exe --orphaned-topics --start-directory c:\repos\visualstudio-docs-pr\docs\ide
  ```

- Find and delete orphaned .png/.gif/.jpg/.svg files (recursive):

  ```
  CleanRepo.exe --orphaned-images --start-directory c:\repos\visualstudio-docs-pr\docs\ide
  ```

- Find and delete shared markdown files that are orphaned (recursive):

  ```
  CleanRepo.exe --orphaned-includes --start-directory c:\repos\visualstudio-docs-pr\docs\ide
  ```

- Remove entries in the .openpublishing.redirection.json file that haven't been clicked in the last 180 days:

  ```
  CleanRepo.exe --trim-redirects --docset-root c:\users\gewarren\dotnet-docs\docs
  ```
  
  This option generates an output file that includes tab-separated data showing the number of page views for each redirected URL in the specified time frame. A value of -1 means that the URL was invalid for some reason, and these redirects are NOT removed from the redirection file.

- Find articles with backlinks to redirected topics, and replace the links with their target URL:

  ```
  CleanRepo.exe --replace-redirects --start-directory c:\repos\visualstudio-docs-pr\docs\ide
  ```

- Replace site-relative links with file-relative links, when the file exists (includes image links):

  ```
  CleanRepo.exe --relative-links -start-directory c:\repos\visualstudio-docs-pr\docs\ide
 ```
