using CleanRepo.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CleanRepo
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Annoying")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Annoying")]
    class Program
    {
        static void Main(string[] args)
        {
            // Command line options
            var options = new Options();
            var parsedArgs = CommandLine.Parser.Default.ParseArguments(args, options);
            if (parsedArgs)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Find orphaned topics
                if (options.FindOrphanedTopics)
                {
                    if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                    {
                        Console.WriteLine("\nYou must specify a valid top-level directory in which to perform the clean up, e.g. c:\\repos\\dotnet-docs\\docs.");
                        return;
                    }

                    Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory and its subdirectories for orphaned topics...");

                    List<FileInfo> tocFiles = GetTocFiles(options.InputDirectory);
                    List<FileInfo> markdownFiles = GetMarkdownFiles(options.InputDirectory, options.SearchRecursively);

                    if (tocFiles is null || markdownFiles is null)
                    {
                        return;
                    }

                    ListOrphanedTopics(tocFiles, markdownFiles, options.Delete);
                }
                // Find topics referenced multiple times
                else if (options.FindMultiples)
                {
                    if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                    {
                        Console.WriteLine("\nYou must specify a valid top-level directory in which to perform the clean up, e.g. c:\\repos\\dotnet-docs\\docs.");
                        return;
                    }

                    Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory and its subdirectories for " +
                        $"topics that appear more than once in one or more TOC files...\n");

                    List<FileInfo> tocFiles = GetTocFiles(options.InputDirectory);
                    List<FileInfo> markdownFiles = GetMarkdownFiles(options.InputDirectory, options.SearchRecursively);

                    if (tocFiles is null || markdownFiles is null)
                    {
                        return;
                    }

                    ListPopularFiles(tocFiles, markdownFiles);
                }
                // Find orphaned images
                else if (options.FindOrphanedImages)
                {
                    if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                    {
                        Console.WriteLine("\nYou must specify a valid top-level directory in which to perform the clean up, e.g. c:\\repos\\dotnet-docs\\docs.");
                        return;
                    }

                    string recursive = options.SearchRecursively ? "recursively " : "";
                    Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory {recursive}for orphaned .png/.jpg/.gif files...\n");

                    Dictionary<string, int> imageFiles = GetMediaFiles(options.InputDirectory, options.SearchRecursively);

                    if (imageFiles.Count == 0)
                    {
                        Console.WriteLine("\nNo .png/.jpg/.gif files were found!");
                        return;
                    }

                    ListOrphanedImages(options.InputDirectory, imageFiles, options.Delete);
                }
                // Find orphaned include-type files
                else if (options.FindOrphanedIncludes)
                {
                    if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                    {
                        Console.WriteLine("\nYou must specify a valid top-level directory in which to perform the clean up, e.g. c:\\repos\\dotnet-docs\\docs.");
                        return;
                    }

                    string recursive = options.SearchRecursively ? "recursively " : "";
                    Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory {recursive}for orphaned .md files " +
                        $"in directories named 'includes' or '_shared'.");

                    Dictionary<string, int> includeFiles = GetIncludeFiles(options.InputDirectory, options.SearchRecursively);

                    if (includeFiles.Count == 0)
                    {
                        Console.WriteLine("\nNo .md files were found in any directory named 'includes' or '_shared'.");
                        return;
                    }

                    ListOrphanedIncludes(options.InputDirectory, includeFiles, options.Delete);
                }
                else if (options.CleanRedirectionFile)
                {
                    if (String.IsNullOrEmpty(options.DocsetName))
                    {
                        Console.WriteLine("\nYou must specify a docset name, e.g. dotnet.");
                        return;
                    }
                    if (String.IsNullOrEmpty(options.DocsetRoot) || !Directory.Exists(options.DocsetRoot))
                    {
                        Console.WriteLine("\nYou must specify a valid docset root for this repo when replacing site-relative links.");
                        return;
                    }

                    FileInfo redirectsFile = null;
                    if (String.IsNullOrEmpty(options.RedirectsFile))
                    {
                        // Find the .openpublishing.redirection.json file for the directory
                        redirectsFile = GetRedirectsFile(options.DocsetRoot);
                    }
                    else
                    {
                        redirectsFile = new FileInfo(options.RedirectsFile);
                    }

                    if (redirectsFile == null)
                    {
                        Console.WriteLine($"\nCould not find redirects file for directory '{options.DocsetRoot}'.");
                        return;
                    }
                    
                    DirectoryInfo dirInfo = new DirectoryInfo(options.DocsetRoot);
                    string docsetRootFolder = dirInfo.Name;

                    Console.WriteLine($"\nCleaning the '{redirectsFile.FullName}' redirection file.\n");
                    CleanRedirectsFile(redirectsFile, options.DocsetName, docsetRootFolder);
                }
                // Find links to topics in the central redirect file
                else if (options.ReplaceRedirectTargets)
                {
                    if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                    {
                        Console.WriteLine("\nYou must specify a valid top-level directory in which to perform the clean up, e.g. c:\\repos\\dotnet-docs\\docs.");
                        return;
                    }

                    Console.WriteLine("\nIt's highly recommended to run the --clean-redirects option before replacing redirect links. Do you want to continue? y or n");
                    var info = Console.ReadKey();
                    if (info.KeyChar != 'y' && info.KeyChar != 'Y')
                    {
                        return;
                    }

                    Console.WriteLine($"\nSearching the '{options.InputDirectory}' directory for links to redirected topics...\n");

                    FileInfo redirectsFile = null;
                    if (String.IsNullOrEmpty(options.RedirectsFile))
                    {
                        // Find the .openpublishing.redirection.json file for the directory
                        redirectsFile = GetRedirectsFile(options.InputDirectory);
                    }
                    else
                    {
                        redirectsFile = new FileInfo(options.RedirectsFile);
                    }

                    if (redirectsFile == null)
                    {
                        Console.WriteLine($"\nCould not find redirects file for directory '{options.InputDirectory}'.");
                        return;
                    }

                    // Put all the redirected files in a list
                    List<Redirect> redirects = GetAllRedirectedFiles(redirectsFile);
                    if (redirects is null)
                    {
                        Console.WriteLine("\nDid not find any redirects - exiting.");
                        return;
                    }

                    // Get all the markdown and YAML files.
                    List<FileInfo> linkingFiles = GetMarkdownFiles(options.InputDirectory, options.SearchRecursively);
                    linkingFiles.AddRange(GetYAMLFiles(options.InputDirectory, options.SearchRecursively));

                    // Check all links, including in toc.yml, to files in the redirects list.
                    // Replace links to redirected files.
                    FindRedirectLinks(redirects, linkingFiles);

                    Console.WriteLine("DONE");
                }
                // Replace site-relative links to *this* repo with file-relative links
                else if (options.ReplaceWithRelativeLinks)
                {
                    if (String.IsNullOrEmpty(options.InputDirectory) || !Directory.Exists(options.InputDirectory))
                    {
                        Console.WriteLine("\nYou must specify a valid top-level directory in which to perform the clean up, e.g. c:\\repos\\dotnet-docs\\docs.");
                        return;
                    }
                    if (String.IsNullOrEmpty(options.DocsetName))
                    {
                        Console.WriteLine("\nYou must specify the docset name for this repo when replacing site-relative links.");
                        return;
                    }
                    if (String.IsNullOrEmpty(options.DocsetRoot) || !Directory.Exists(options.DocsetRoot))
                    {
                        Console.WriteLine("\nYou must specify a valid docset root for this repo when replacing site-relative links.");
                        return;
                    }

                    Console.WriteLine($"\nReplacing site-relative links to '/{options.DocsetName}/' in " +
                        $"the '{options.InputDirectory}' directory with file-relative links.\n");

                    // Get all the markdown and YAML files.
                    List<FileInfo> linkingFiles = GetMarkdownFiles(options.InputDirectory, options.SearchRecursively);
                    linkingFiles.AddRange(GetYAMLFiles(options.InputDirectory, options.SearchRecursively));

                    // Check all links in these files.
                    ReplaceLinks(linkingFiles, options.DocsetName, options.DocsetRoot);
                }
                else
                {
                    Console.WriteLine("\nYou did not specify which function to perform.");
                    return;
                }

                stopwatch.Stop();
                Console.WriteLine($"\nElapsed time: {stopwatch.Elapsed.ToHumanReadableString()}");

                // Uncomment for debugging to see console output.
                //Console.WriteLine("\nPress any key to continue.");
                //Console.ReadLine();
            }
        }

        #region Replace site-relative links
        private static void ReplaceLinks(List<FileInfo> linkingFiles, string docsetName, string rootDirectory)
        {
            foreach (var linkingFile in linkingFiles)
            {
                // Find links that look like [link text](/docsetName/some other text)
                string pattern = @"\]\(/" + docsetName + @"/([^\)]*)\)";

                // Read the whole file up front because we might change the file mid-flight.
                string originalFileText = File.ReadAllText(linkingFile.FullName);

                foreach (Match match in Regex.Matches(originalFileText, pattern, RegexOptions.IgnoreCase))
                {
                    // Get the first capture group, which is the part of the path after the docset name.
                    string siteRelativePath = match.Groups[1].Value;

                    // If the path contains a ?, ignore this link as replacing it might not be ideal.
                    if (siteRelativePath.IndexOf('?') >= 0)
                        continue;

                    // Build an absolute path to this file.
                    string absolutePath = Path.Combine(rootDirectory, siteRelativePath.Trim() + ".md");

                    // Clean up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                    absolutePath = Path.GetFullPath(absolutePath);

                    // Check that the link is valid in the local repo.
                    if (!File.Exists(absolutePath))
                    {
                        continue;
                    }

                    if (absolutePath != null)
                    {
                        // Determine the file-relative path to absolutePath.
                        string fileRelativePath = GetFileRelativePath(linkingFile.FullName, absolutePath);

                        if (fileRelativePath != null)
                        {
                            // Replace the link.
                            Console.WriteLine($"Replacing '/{docsetName}/{siteRelativePath}' link with '{fileRelativePath}' in file '{linkingFile.FullName}'.");

                            // If a previous link was found and replace, the text may have changed.
                            string currentFileText = File.ReadAllText(linkingFile.FullName);

                            string newText = currentFileText.Replace($"](/{docsetName}/{siteRelativePath})", $"]({fileRelativePath})");
                            File.WriteAllText(linkingFile.FullName, newText);
                        }
                    }
                }
            }
        }

        private static string GetFileRelativePath(string linkingFilePath, string linkedFilePath)
        {
            string[] linkingFileDirs = linkingFilePath.Split('\\');
            string[] linkedFileDirs = linkedFilePath.Split('\\');

            // Get the shortest of the two paths.
            int len = linkingFileDirs.Length < linkedFileDirs.Length ? linkingFileDirs.Length : linkedFileDirs.Length;

            // Variables used to determine where in the loop we exit.
            int lastCommonRoot = -1;
            int index;

            // Find common root of the two file paths.
            for (index = 0; index < len; index++)
            {
                if (linkingFileDirs[index] == linkedFileDirs[index])
                    lastCommonRoot = index;
                else break;
            }

            // If we didn't find a common prefix, return null.
            if (lastCommonRoot == -1)
            {
                return null;
            }

            // Build up the relative path.
            StringBuilder relativePath = new StringBuilder();

            // Add on the ..\
            for (index = lastCommonRoot + 1; index < linkingFileDirs.Length - 1; index++)
            {
                if (linkingFileDirs[index].Length > 0)
                    relativePath.Append("../");
            }

            // Add on the folders.
            for (index = lastCommonRoot + 1; index < linkedFileDirs.Length - 1; index++)
            {
                relativePath.Append(linkedFileDirs[index] + "/");
            }

            // Add the file name.
            relativePath.Append(linkedFileDirs[linkedFileDirs.Length - 1]);

            return relativePath.ToString();
        }
        #endregion

        #region Orphaned includes
        /// TODO: Improve the perf of this method using the following pseudo code:
        /// For each include file
        ///    For each markdown file
        ///       Do a RegEx search for the include file
        ///          If found, BREAK to the next include file
        private static void ListOrphanedIncludes(string inputDirectory, Dictionary<string, int> includeFiles, bool deleteOrphanedIncludes)
        {
            // Get all files that could possibly link to the include files
            var files = GetAllMarkdownFiles(inputDirectory, out DirectoryInfo rootDirectory);

            if (files is null)
            {
                return;
            }

            // Gather up all the include references and increment the count for that include file in the Dictionary.
            //foreach (var markdownFile in files)
            Parallel.ForEach(files, markdownFile =>
            {
                foreach (string line in File.ReadAllLines(markdownFile.FullName))
                {
                    // Example include references:
                    // [!INCLUDE [DotNet Restore Note](../includes/dotnet-restore-note.md)]
                    // [!INCLUDE[DotNet Restore Note](~/includes/dotnet-restore-note.md)]
                    // [!INCLUDE [temp](../_shared/assign-to-sprint.md)]

                    // An include file referenced from another include file won't have "includes" or "_shared" in the path.
                    // E.g. [!INCLUDE [P2S FAQ All](vpn-gateway-faq-p2s-all-include.md)]

                    // RegEx pattern to match
                    string includeLinkPattern = @"\[!INCLUDE[ ]?\[[^\]]*?\]\((.*?\.md)";

                    // There could be more than one INCLUDE reference on the line, hence the foreach loop.
                    foreach (Match match in Regex.Matches(line, includeLinkPattern, RegexOptions.IgnoreCase))
                    {
                        // Get the first capture group, which is the relative path ending in '.md'.
                        string relativePath = match.Groups[1].Value.Trim();

                        if (relativePath != null)
                        {
                            string fullPath;

                            // Path could start with a tilde e.g. ~/includes/dotnet-restore-note.md
                            if (relativePath.StartsWith("~/"))
                            {
                                fullPath = Path.Combine(rootDirectory.FullName, relativePath.TrimStart('~', '/'));
                            }
                            else
                            {
                                // Construct the full path to the referenced INCLUDE file
                                fullPath = Path.Combine(markdownFile.DirectoryName, relativePath);
                            }

                            // Clean up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                            fullPath = Path.GetFullPath(fullPath);

                            if (fullPath != null)
                            {
                                // Increment the count for this INCLUDE file in our dictionary
                                try
                                {
                                    // TODO - is this okay to have in a Parallel.ForEach loop?
                                    includeFiles[fullPath.ToLower()]++;
                                }
                                catch (KeyNotFoundException)
                                {
                                    // No need to do anything.
                                }
                            }
                        }
                    }
                }
            });

            int count = 0;

            // Print out the INCLUDE files that have zero references.
            StringBuilder output = new StringBuilder();
            foreach (var includeFile in includeFiles)
            {
                if (includeFile.Value == 0)
                {
                    count++;
                    output.AppendLine(Path.GetFullPath(includeFile.Key));
                }
            }

            if (deleteOrphanedIncludes)
            {
                // Delete orphaned image files
                foreach (var includeFile in includeFiles)
                {
                    if (includeFile.Value == 0)
                    {
                        File.Delete(includeFile.Key);
                    }
                }
            }

            string deleted = deleteOrphanedIncludes ? "and deleted " : "";

            Console.WriteLine($"\nFound {deleted}{count} orphaned INCLUDE files:\n");
            Console.WriteLine(output.ToString());
            Console.WriteLine("DONE");
        }

        /// <summary>
        /// Returns a collection of *.md files in the current directory, and optionally subdirectories,
        /// if the directory name is 'includes' or '_shared'.
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, int> GetIncludeFiles(string inputDirectory, bool searchRecursively)
        {
            DirectoryInfo dir = new DirectoryInfo(inputDirectory);

            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            Dictionary<string, int> includeFiles = new Dictionary<string, int>();

            if (String.Compare(dir.Name, "includes", true) == 0
                || String.Compare(dir.Name, "_shared", true) == 0)
            {
                // This is a folder that is likely to contain "include"-type files, i.e. files that aren't in the TOC.

                foreach (var file in dir.EnumerateFiles("*.md"))
                {
                    includeFiles.Add(file.FullName.ToLower(), 0);
                }
            }

            if (searchOption == SearchOption.AllDirectories)
            {
                foreach (var subDirectory in dir.EnumerateDirectories("*", SearchOption.AllDirectories))
                {
                    if (String.Compare(subDirectory.Name, "includes", true) == 0
                        || String.Compare(subDirectory.Name, "_shared", true) == 0)
                    {
                        // This is a folder that is likely to contain "include"-type files, i.e. files that aren't in the TOC.

                        foreach (var file in subDirectory.EnumerateFiles("*.md"))
                        {
                            includeFiles.Add(file.FullName.ToLower(), 0);
                        }
                    }
                }
            }

            return includeFiles;
        }
        #endregion

        #region Orphaned images
        /// <summary>
        /// If any of the input image files are not
        /// referenced from a markdown (.md) file anywhere in the docset, including up the directory 
        /// until the docfx.json file is found, the file path of those files is written to the console.
        /// </summary>
        /// TODO: Improve the perf of this method using the following pseudo code:
        /// For each image
        ///    For each markdown file
        ///       Do a RegEx search for the image
        ///          If found, BREAK to the next image
        private static void ListOrphanedImages(string inputDirectory, Dictionary<string, int> imageFiles, bool deleteOrphanedImages)
        {
            var files = GetAllMarkdownFiles(inputDirectory, out DirectoryInfo rootDirectory);

            if (files is null)
            {
                return;
            }

            void TryIncrementFile(string key, Dictionary<string, int> fileMap)
            {
                if (fileMap.ContainsKey(key))
                {
                    ++fileMap[key];
                }
            }

            // Gather up all the image references and increment the count for that image in the Dictionary.
            //foreach (var markdownFile in files)
            Parallel.ForEach(files, markdownFile =>
            {
                foreach (string line in File.ReadAllLines(markdownFile.FullName))
                {
                    /* Support all of the following variations:
                    *
                    [hello](media/how-to-use-lightboxes/xamarin.png#lightbox)
                    ![Auto hide](../ide/media/vs2015_auto_hide.png)
                    ![Unit Test Explorer showing Run All button](../test/media/unittestexplorer-beta-.png "UnitTestExplorer(beta)")
                    ![Architecture](./media/ci-cd-flask/Architecture.PNG?raw=true)
                    The Light Bulb icon ![Small Light Bulb Icon](media/vs2015_lightbulbsmall.png "VS2017_LightBulbSmall")
                    *
                    * Does not currently support file names that contain parentheses:
                    * [VS image](../media/pic(azure)_1.png)
                    */

                    // RegEx pattern to match
                    string mdImageRegEx = @"\]\(([^\)]*?\.(png|jpg|gif))";

                    // There could be more than one image reference on the line, hence the foreach loop.
                    foreach (Match match in Regex.Matches(line, mdImageRegEx, RegexOptions.IgnoreCase))
                    {
                        string relativePath = match.Groups[1].Value.Trim();

                        if (relativePath.StartsWith("/") || relativePath.StartsWith("http"))
                        {
                            // The file is in a different repo, so ignore it.
                            continue;

                            // TODO - For links that start with "/", check if they are in the same repo.
                        }

                        if (relativePath != null)
                        {
                            // Construct the full path to the referenced image file
                            string fullPath = null;
                            try
                            {
                                // Path could start with a tilde e.g. ~/media/pic1.png
                                if (relativePath.StartsWith("~/"))
                                {
                                    fullPath = Path.Combine(rootDirectory.FullName, relativePath.TrimStart('~', '/'));
                                }
                                else
                                {
                                    fullPath = Path.Combine(markdownFile.DirectoryName, relativePath);
                                }
                            }
                            catch (ArgumentException)
                            {
                                Console.WriteLine($"Possible bad image link '{match.Groups[0].Value}' in file '{markdownFile.FullName}'.\n");
                                continue;
                            }

                            // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                            try
                            {
                                fullPath = Path.GetFullPath(fullPath);
                            }
                            catch (ArgumentException)
                            {
                                Console.WriteLine($"Possible bad image link '{match.Groups[0].Value}' in file '{markdownFile.FullName}'.\n");
                                continue;
                            }

                            if (fullPath != null)
                            {
                                TryIncrementFile(fullPath, imageFiles);
                            }
                        }
                    }

                    // Match "img src=" references
                    // Example: <img data-hoverimage="./images/getstarted.svg" src="./images/getstarted.png" alt="Get started icon" />
                    // Example: <img style="display: none;" alt="Anaconda" src="_img/index/logo_anaconda.png" data-linktype="external" data-hoverimage="_img/index/logo_anaconda.png">

                    string htmlImageRegEx = "<img[^>]*?src[ ]*=[ ]*\"([^>]*?.(png|gif|jpg))[ ]*\"";
                    foreach (Match match in Regex.Matches(line, htmlImageRegEx, RegexOptions.IgnoreCase))
                    {
                        string relativePath = match.Groups[1].Value.Trim();

                        if (relativePath.StartsWith("/") || relativePath.StartsWith("http"))
                        {
                            // The file is in a different repo, so ignore it.
                            continue;

                            // TODO - check if link is site-relative to the same docset
                        }

                        if (relativePath != null)
                        {
                            string fullPath;

                            // Path could start with a tilde e.g. ~/media/pic1.png
                            if (relativePath.StartsWith("~/"))
                            {
                                // Construct the full path to the referenced image file
                                fullPath = Path.Combine(rootDirectory.FullName, relativePath.TrimStart('~', '/'));
                            }
                            else
                            {
                                // Construct the full path to the referenced image file
                                fullPath = Path.Combine(markdownFile.DirectoryName, relativePath);
                            }

                            // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                            fullPath = TryGetFullPath(fullPath);

                            if (fullPath != null)
                            {
                                TryIncrementFile(fullPath, imageFiles);
                            }
                        }
                    }

                    // Match reference-style image links
                    // Example: [0]: ../../media/vs-acr-provisioning-dialog-2019.png

                    string referenceLinkRegEx = @"\[.*\]:(.*\.(png|gif|jpg))";
                    foreach (Match match in Regex.Matches(line, referenceLinkRegEx, RegexOptions.IgnoreCase))
                    {
                        string relativePath = match.Groups[1].Value.Trim();

                        if (relativePath.StartsWith("/") || relativePath.StartsWith("http"))
                        {
                            // The file is in a different repo, so ignore it.
                            continue;

                            // TODO - For links that start with "/", check if they are in the same repo.
                        }

                        if (relativePath != null)
                        {
                            // Construct the full path to the referenced image file
                            string fullPath = Path.Combine(markdownFile.DirectoryName, relativePath);

                            // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                            fullPath = TryGetFullPath(fullPath);

                            if (fullPath != null)
                            {
                                TryIncrementFile(fullPath, imageFiles);
                            }
                        }
                    }
                }
            });

            int count = 0;

            // Print out the image files with zero references.
            StringBuilder output = new StringBuilder();
            foreach (var image in imageFiles)
            {
                if (image.Value == 0)
                {
                    count++;
                    output.AppendLine(Path.GetFullPath(image.Key));
                }
            }

            if (deleteOrphanedImages)
            {
                // Delete orphaned image files
                foreach (var image in imageFiles)
                {
                    if (image.Value == 0)
                    {
                        try
                        {
                            File.Delete(image.Key);
                        }
                        catch (PathTooLongException)
                        {
                            output.AppendLine($"Unable to delete {image.Key} because its path is too long.");
                        }
                    }
                }
            }

            string deleted = deleteOrphanedImages ? "and deleted " : "";

            Console.WriteLine($"\nFound {deleted}{count} orphaned .png/.jpg/.gif files:\n");
            Console.WriteLine(output.ToString());
            Console.WriteLine("DONE");
        }

        private static string TryGetFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch (PathTooLongException)
            {
                Console.WriteLine($"Unable to get path because it's too long: {path}\n");
                return null;
            }
        }

        /// <summary>
        /// Returns a dictionary of all .png/.jpg/.gif files in the directory.
        /// The search includes the specified directory and (optionally) all its subdirectories.
        /// </summary>
        private static Dictionary<string, int> GetMediaFiles(string mediaDirectory, bool searchRecursively = true)
        {
            DirectoryInfo dir = new DirectoryInfo(mediaDirectory);

            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            Dictionary<string, int> mediaFiles = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in dir.EnumerateFiles("*.png", searchOption))
            {
                mediaFiles.Add(file.FullName.ToLower(), 0);
            }

            foreach (var file in dir.EnumerateFiles("*.jpg", searchOption))
            {
                mediaFiles.Add(file.FullName.ToLower(), 0);
            }

            foreach (var file in dir.EnumerateFiles("*.gif", searchOption))
            {
                mediaFiles.Add(file.FullName.ToLower(), 0);
            }

            return mediaFiles;
        }
        #endregion

        #region Orphaned topics
        /// <summary>
        /// Lists the markdown files that aren't referenced from a TOC file.
        /// </summary>
        private static void ListOrphanedTopics(List<FileInfo> tocFiles, List<FileInfo> markdownFiles, bool deleteOrphanedTopics)
        {
            Dictionary<string, int> filesToKeep = new Dictionary<string, int>();

            Console.WriteLine("\nTopics not in any TOC file (that are also not includes or shared):\n");

            bool IsTopicFile(FileInfo file) =>
                !file.FullName.Contains("\\includes\\") &&
                !file.FullName.Contains("\\_shared\\") &&
                String.Compare(file.Name, "TOC.md", true) != 0 &&
                String.Compare(file.Name, "index.md", true) != 0;

            List<FileInfo> orphanedFiles = new List<FileInfo>();

            Parallel.ForEach(markdownFiles.Where(IsTopicFile), markdownFile =>
            {
                var found = tocFiles.Any(tocFile => IsFileLinkedFromTocFile(markdownFile, tocFile));
                if (!found)
                {
                    orphanedFiles.Add(markdownFile);
                    Console.WriteLine(markdownFile.FullName);
                }
            });

            Console.WriteLine($"\nFound {orphanedFiles.Count} .md files that aren't referenced in a TOC.");

            // Delete files if the option is set.
            if (deleteOrphanedTopics)
            {
                Parallel.ForEach(markdownFiles, linkingFile =>
                {
                    CheckFileLinks(orphanedFiles, linkingFile, ref filesToKeep);
                });

                // Delete files that aren't linked to.
                foreach (var orphanedFile in orphanedFiles)
                {
                    if (!filesToKeep.ContainsKey(orphanedFile.FullName))
                        File.Delete(orphanedFile.FullName);
                }

                if (filesToKeep.Count > 0)
                {
                    Console.Write($"\nThe following {filesToKeep.Count} files *were not deleted* " +
                        $"because they're referenced in one or more files:\n\n");
                    foreach (var fileName in filesToKeep)
                    {
                        Console.WriteLine(fileName);
                    }
                }
                else
                {
                    Console.WriteLine($"\nDeleted {orphanedFiles.Count} files.");
                }
            }
        }

        private static bool IsFileLinkedFromTocFile(FileInfo linkedFile, FileInfo tocFile)
        {
            string text = File.ReadAllText(tocFile.FullName);

            // Example links .yml/.md:
            // href: ide/managing-external-tools.md
            // # [Managing External Tools](ide/managing-external-tools.md)

            string linkRegEx = tocFile.Extension.ToLower() == ".yml" ?
                @"href:(.*?" + linkedFile.Name + ")" :
                @"\]\((?!http)(([^\)])*?" + linkedFile.Name + @")";

            // For each link that contains the file name...
            foreach (Match match in Regex.Matches(text, linkRegEx, RegexOptions.IgnoreCase))
            {
                // Get the file-relative path to the linked file.
                string relativePath = match.Groups[1].Value.Trim();

                // Remove any quotation marks
                relativePath = relativePath.Replace("\"", "");

                if (relativePath.StartsWith("/") || relativePath.StartsWith("http"))
                {
                    // The file is in a different repo, so ignore it.
                    continue;

                    // TODO - For links that start with "/", check if they are in the same repo.
                }

                if (relativePath != null)
                {
                    // Construct the full path to the referenced file
                    string fullPath = Path.Combine(tocFile.DirectoryName, relativePath);

                    // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                    fullPath = Path.GetFullPath(fullPath);

                    if (fullPath != null)
                    {
                        // See if our constructed path matches the actual file we think it is
                        if (String.Compare(fullPath, linkedFile.FullName, true) == 0)
                        {
                            return true;
                        }
                        else
                        {
                            // If we get here, the file name matched but the full path did not.
                        }
                    }
                }
            }

            // We did not find this file linked in the specified file.
            return false;
        }

        /// <summary>
        /// If linkingFile contains a link to any file in linkedFiles, add the file to filesToKeep.
        /// </summary>
        private static void CheckFileLinks(List<FileInfo> linkedFiles, FileInfo linkingFile, ref Dictionary<string, int> filesToKeep)
        {
            if (!File.Exists(linkingFile.FullName))
            {
                return;
            }

            string fileContents = File.ReadAllText(linkingFile.FullName);

            // Example links .yml/.md:
            // href: ide/managing-external-tools.md
            // [Managing External Tools](ide/managing-external-tools.md)

            foreach (var linkedFile in linkedFiles)
            {
                string linkRegEx = linkingFile.Extension.ToLower() == ".yml" ?
                        @"href:(.*?" + linkedFile.Name + ")" :
                        @"\]\((?!http)(([^\)])*?" + linkedFile.Name + @")";

                // For each link that contains the file name...
                foreach (Match match in Regex.Matches(fileContents, linkRegEx, RegexOptions.IgnoreCase))
                {
                    // Get the file-relative path to the linked file.
                    string relativePath = match.Groups[1].Value.Trim();

                    // Remove any quotation marks
                    relativePath = relativePath.Replace("\"", "");

                    if (relativePath != null)
                    {
                        string fullPath;
                        try
                        {
                            // Construct the full path to the referenced file
                            fullPath = Path.Combine(linkingFile.DirectoryName, relativePath);
                        }
                        catch (ArgumentException e)
                        {
                            Console.WriteLine($"\nCaught exception while constructing full path " +
                                $"for '{relativePath}' in '{linkingFile.FullName}': {e.Message}");
                            throw;
                        }

                        // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                        fullPath = Path.GetFullPath(fullPath);
                        if (fullPath != null)
                        {
                            // See if our constructed path matches the actual file we think it is
                            if (String.Compare(fullPath, linkedFile.FullName, true) == 0)
                            {
                                // File is linked from another file.
                                if (filesToKeep.ContainsKey(linkedFile.FullName))
                                {
                                    // Increment the count of links to this file.
                                    filesToKeep[linkedFile.FullName]++;
                                }
                                else
                                {
                                    filesToKeep.Add(linkedFile.FullName, 1);
                                }
                            }
                            else
                            {
                                // This link did not match the full file name.
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Redirected files
        // TODO: Use a tuple instead
        private class Redirect
        {
            public string source_path;
            public string redirect_url;
            public bool redirect_document_id;
        }

        private static FileInfo GetRedirectsFile(string inputDirectory)
        {
            DirectoryInfo dir = new DirectoryInfo(inputDirectory);

            try
            {
                FileInfo[] files = dir.GetFiles(".openpublishing.redirection.json", SearchOption.TopDirectoryOnly);
                while (dir.GetFiles(".openpublishing.redirection.json", SearchOption.TopDirectoryOnly).Length == 0)
                {
                    dir = dir.Parent;

                    // Loop exit condition.
                    if (dir == dir.Root)
                        return null;
                }

                return dir.GetFiles(".openpublishing.redirection.json", SearchOption.TopDirectoryOnly)[0];
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"Could not find directory {dir.FullName}");
                throw;
            }
        }

        private static void WriteRedirectJson(FileInfo redirectsFile, List<Redirect> redirects)
        {
            string json = JsonConvert.SerializeObject(redirects);
            File.WriteAllText(redirectsFile.FullName, json);
        }

        private static List<Redirect> LoadRedirectJson(FileInfo redirectsFile)
        {
            using (StreamReader reader = new StreamReader(redirectsFile.FullName))
            {
                string json = reader.ReadToEnd();

                // Trim the string so we're just left with an array of redirect objects
                json = json.Trim();
                json = json.Substring(json.IndexOf('['));
                json = json.TrimEnd('}');

                try
                {
                    return JsonConvert.DeserializeObject<List<Redirect>>(json);
                }
                catch (JsonReaderException e)
                {
                    Console.WriteLine($"Caught exception while reading JSON file: {e.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// For each target URL, see if it's a source_path somewhere else.
        /// If so, replace the original target URL with the new target URL.
        /// </summary>
        private static void CleanRedirectsFile(FileInfo redirectsFile, string docsetName, string docsetRootFolderName)
        {
            List<Redirect> redirects = LoadRedirectJson(redirectsFile);
            string fileText = File.ReadAllText(redirectsFile.FullName);

            if (redirects is null)
            {
                return;
            }

            // Load the sources and targets into a dictionary for easier look up.
            Dictionary<string, string> redirectsLookup = new Dictionary<string, string>(redirects.Count);
            foreach (Redirect redirect in redirects)
            {
                redirectsLookup.Add(redirect.source_path, redirect.redirect_url);
            }

            foreach (var redirectPair in redirectsLookup)
            {
                string currentTarget = redirectPair.Value;

                if (!redirectPair.Value.StartsWith("/" + docsetName + "/"))
                {
                    // Redirect URL is in a different docset/repo, so ignore it.
                    continue;
                }

                // Put the URL into the same format as source_path
                string normalizedTargetPath = docsetRootFolderName + redirectPair.Value.Remove(0, docsetName.Length + 1) + ".md";

                while (redirectsLookup.ContainsKey(normalizedTargetPath))
                {
                    // Avoid an infinite loop by checking that this isn't the same key/value pair.
                    if (String.Equals(redirectsLookup[normalizedTargetPath], currentTarget))
                    {
                        Console.WriteLine($"\nWARNING: {normalizedTargetPath} REDIRECTS TO ITSELF. PLEASE FIND A DIFFERENT REDIRECT URL.\n");
                        break;
                    }

                    currentTarget = redirectsLookup[normalizedTargetPath];

                    if (!currentTarget.StartsWith("/" + docsetName + "/"))
                    {
                        // Redirect URL is in a different docset/repo, so stop looking for further targets.
                        break;
                    }

                    normalizedTargetPath = docsetRootFolderName + currentTarget.Remove(0, docsetName.Length + 1) + ".md";
                }

                if (currentTarget != redirectPair.Value)
                {
                    Console.WriteLine($"Replacing target URL '{redirectPair.Value}' with '{currentTarget}'.");

                    fileText = fileText.Replace($"\"redirect_url\": \"{redirectPair.Value}\"", $"\"redirect_url\": \"{currentTarget}\"");
                }
            }

            // Write the redirects back to the file.
            File.WriteAllText(redirectsFile.FullName, fileText);
        }

        private static List<Redirect> GetAllRedirectedFiles(FileInfo redirectsFile)
        {
            List<Redirect> redirects = LoadRedirectJson(redirectsFile);

            if (redirects is null)
            {
                return null;
            }

            foreach (Redirect redirect in redirects)
            {
                if (redirect.source_path != null)
                {
                    // Construct the full path to the redirected file
                    string fullPath = Path.Combine(redirectsFile.DirectoryName, redirect.source_path);

                    // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                    fullPath = Path.GetFullPath(fullPath);

                    redirect.source_path = fullPath;
                }
            }

            return redirects;
        }

        private static void FindRedirectLinks(List<Redirect> redirects, List<FileInfo> linkingFiles)
        {
            Dictionary<string, Redirect> redirectLookup = Enumerable.ToDictionary<Redirect, string>(redirects, r => r.source_path);

            // For each file...
            //foreach (var linkingFile in linkingFiles)
            Parallel.ForEach(linkingFiles, linkingFile =>
            {
                bool foundOldLink = false;
                StringBuilder output = new StringBuilder($"FILE '{linkingFile.FullName}' contains the following link(s) to redirected files:\n\n");

                string text = File.ReadAllText(linkingFile.FullName);

                string linkRegEx = linkingFile.Extension.ToLower() == ".yml" ?
                    @"href:(.*\.md)" :
                    @"\]\((?!http)([^\)]*\.md)\)";

            // For each link in the file...
            foreach (Match match in Regex.Matches(text, linkRegEx, RegexOptions.IgnoreCase))
                {
                // Get the file-relative path to the linked file.
                string relativePath = match.Groups[1].Value.Trim();

                // Remove any quotation marks
                relativePath = relativePath.Replace("\"", "");

                    string fullPath = null;
                    try
                    {
                    // Construct the full path to the linked file.
                    fullPath = Path.Combine(linkingFile.DirectoryName, relativePath);
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine($"Ignoring the link {relativePath} due to possibly invalid format.\n");
                        continue;
                    }

                // Clean up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                try
                    {
                        fullPath = Path.GetFullPath(fullPath);
                    }
                    catch (NotSupportedException)
                    {
                        Console.WriteLine($"Found a possibly malformed link '{match.Groups[0].Value}' in '{linkingFile.FullName}'.\n");
                        break;
                    }

                    if (fullPath != null)
                    {
                    // See if our constructed path matches a source file in the dictionary of redirects.
                    if (redirectLookup.ContainsKey(fullPath))
                        {
                            foundOldLink = true;
                            output.AppendLine($"'{relativePath}'");

                        // Replace the link.
                        string redirectURL = redirectLookup[fullPath].redirect_url;

                            output.AppendLine($"REPLACING '({relativePath})' with '({redirectURL})'.");

                            string newText = text.Replace($"]({relativePath})", $"]({redirectURL})");
                            File.WriteAllText(linkingFile.FullName, newText);
                        }
                    }
                }

                if (foundOldLink)
                {
                    Console.WriteLine(output.ToString());
                }
            });
        }
        #endregion

        #region Popular files
        /// <summary>
        /// Finds topics that appear more than once, either in one TOC.md file, or multiple TOC.md files.
        /// </summary>
        private static void ListPopularFiles(List<FileInfo> tocFiles, List<FileInfo> markdownFiles)
        {
            bool found = false;
            StringBuilder output = new StringBuilder("The following files appear in more than one TOC file:\n\n");

            // Keep a hash table of each topic path with the number of times it's referenced
            Dictionary<string, int> topics = markdownFiles.ToDictionary<FileInfo, string, int>(mf => mf.FullName, mf => 0);

            foreach (var markdownFile in markdownFiles)
            {
                // If the file is in the Includes directory, ignore it
                if (markdownFile.FullName.Contains("\\includes\\"))
                    continue;

                foreach (var tocFile in tocFiles)
                {
                    if (IsFileLinkedFromFile(markdownFile, tocFile))
                    {
                        topics[markdownFile.FullName]++;
                    }
                }
            }

            // Now spit out the topics that appear more than once.
            foreach (var topic in topics)
            {
                if (topic.Value > 1)
                {
                    found = true;
                    output.AppendLine(topic.Key);
                }
            }

            // Only write the StringBuilder to the console if we found a topic referenced from more than one TOC file.
            if (found)
            {
                Console.Write(output.ToString());
            }
        }
        #endregion

        #region Generic helper methods
        /// <summary>
        /// Checks if the specified file path is referenced in the specified file.
        /// </summary>
        private static bool IsFileLinkedFromFile(FileInfo linkedFile, FileInfo linkingFile)
        {
            if (!File.Exists(linkingFile.FullName))
            {
                return false;
            }

            foreach (var line in File.ReadAllLines(linkingFile.FullName))
            {
                // Example links .yml/.md:
                // href: ide/managing-external-tools.md
                // [Managing External Tools](ide/managing-external-tools.md)

                string linkRegEx = linkingFile.Extension.ToLower() == ".yml" ?
                    @"href:(.*?" + linkedFile.Name + ")" :
                    @"\]\((?!http)(([^\)])*?" + linkedFile.Name + @")";

                // For each link that contains the file name...
                foreach (Match match in Regex.Matches(line, linkRegEx, RegexOptions.IgnoreCase))
                {
                    // Get the file-relative path to the linked file.
                    string relativePath = match.Groups[1].Value.Trim();

                    // Remove any quotation marks
                    relativePath = relativePath.Replace("\"", "");

                    if (relativePath != null)
                    {
                        string fullPath;
                        try
                        {
                            // Construct the full path to the referenced file
                            fullPath = Path.Combine(linkingFile.DirectoryName, relativePath);
                        }
                        catch (ArgumentException e)
                        {
                            Console.WriteLine($"\nCaught exception while constructing full path " +
                                $"for '{relativePath}' in '{linkingFile.FullName}': {e.Message}");
                            throw;
                        }

                        // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                        fullPath = Path.GetFullPath(fullPath);
                        if (fullPath != null)
                        {
                            // See if our constructed path matches the actual file we think it is
                            if (String.Compare(fullPath, linkedFile.FullName, true) == 0)
                            {
                                return true;
                            }
                            else
                            {
                                // If we get here, the file name matched but the full path did not.
                            }
                        }
                    }
                }
            }

            // We did not find this file linked in the specified file.
            return false;
        }

        /// <summary>
        /// Gets all *.md files recursively, starting in the specified directory.
        /// </summary>
        private static List<FileInfo> GetMarkdownFiles(string directoryPath, bool searchRecursively)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return dir.EnumerateFiles("*.md", searchOption).ToList();
        }

        /// <summary>
        /// Gets all *.yml files recursively, starting in the specified directory.
        /// </summary>
        private static List<FileInfo> GetYAMLFiles(string directoryPath, bool searchRecursively)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return dir.EnumerateFiles("*.yml", searchOption).ToList();
        }

        /// <summary>
        /// Gets all *.md files recursively, starting in the ancestor directory that contains docfx.json.
        /// </summary>
        private static List<FileInfo> GetAllMarkdownFiles(string directoryPath, out DirectoryInfo rootDirectory)
        {
            // Look further up the path until we find docfx.json
            rootDirectory = GetDocFxDirectory(new DirectoryInfo(directoryPath));

            if (rootDirectory is null)
                return null;

            return rootDirectory.EnumerateFiles("*.md", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Gets all TOC.* files recursively, starting in the specified directory if it contains "docfx.json" file.
        /// Otherwise it looks up the directory path until it finds a "docfx.json" file. Then it starts the recursive search
        /// for TOC.* files from that directory.
        /// </summary>
        private static List<FileInfo> GetTocFiles(string directoryPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);

            // Look further up the path until we find docfx.json
            dir = GetDocFxDirectory(dir);

            if (dir is null)
                return null;

            return dir.EnumerateFiles("TOC.*", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Returns the specified directory if it contains a file named "docfx.json".
        /// Otherwise returns the nearest parent directory that contains a file named "docfx.json".
        /// </summary>
        private static DirectoryInfo GetDocFxDirectory(DirectoryInfo dir)
        {
            try
            {
                while (dir.GetFiles("docfx.json", SearchOption.TopDirectoryOnly).Length == 0)
                {
                    dir = dir.Parent;

                    if (dir == dir?.Root)
                    {
                        Console.WriteLine($"\nCould not find a directory containing docfx.json.");
                        return null;
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"\nCould not find directory {dir.FullName}");
                return null;
            }

            return dir;
        }
        #endregion
    }
}