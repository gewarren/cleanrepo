using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Contentment
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Annoying")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Annoying")]
    class Program
    {
        public Program(string arg1)
        {
            throw new InvalidOperationException(nameof(arg1));
        }

        static StringBuilder SimilarFiles = new StringBuilder();
        static StringBuilder ImagesNotInDictionary = new StringBuilder("\nThe following referenced images were not found in our dictionary. " +
            "This can happen if the image is in a parent directory of the input directory:\n");

        static void Main(string[] args)
        {
            // Command line options
            var options = new Options();
            bool parsedArgs = CommandLine.Parser.Default.ParseArguments(args, options);

            if (parsedArgs)
            {
                // Verify that the input directory exists.
                if (!Directory.Exists(options.InputDirectory))
                {
                    Console.WriteLine($"\nDirectory {options.InputDirectory} does not exist.");
                    return;
                }

                // Find orphaned topics
                if (options.FindOrphanedTopics)
                {
                    Console.WriteLine($"\nSearching the {options.InputDirectory} directory and its subdirectories for orphaned topics.");

                    List<FileInfo> tocFiles = GetTocFiles(options.InputDirectory);
                    List<FileInfo> markdownFiles = GetMarkdownFiles(options.InputDirectory, options.SearchRecursively);

                    ListOrphanedTopics(tocFiles, markdownFiles, options.IgnoreRedirects, options.Verbose);
                }
                // Find topics referenced multiple times
                else if (options.FindMultiples)
                {
                    Console.WriteLine($"\nSearching the {options.InputDirectory} directory and its subdirectories for " +
                        $"topics that appear more than once in one or more TOC.md files.");

                    List<FileInfo> tocFiles = GetTocFiles(options.InputDirectory);
                    List<FileInfo> markdownFiles = GetMarkdownFiles(options.InputDirectory, options.SearchRecursively);

                    ListPopularFiles(tocFiles, markdownFiles);
                }
                // Find orphaned images
                else if (options.FindOrphanedImages)
                {
                    Console.WriteLine($"\nSearching the {options.InputDirectory} directory for orphaned images.\n");

                    Dictionary<string, int> imageFiles = GetMediaFiles(options.InputDirectory, options.SearchRecursively);

                    if (imageFiles.Count == 0)
                    {
                        Console.WriteLine("\nNo image files were found!");
                        return;
                    }

                    ListOrphanedImages(options.InputDirectory, imageFiles, options.Verbose, options.Delete);
                }
                // Find links to topics in the central redirect file
                else if (options.FindRedirectedTopicLinks)
                {
                    Console.WriteLine($"\nSearching the {options.InputDirectory} directory for links to redirected topics.\n");

                    // Find the .openpublishing.redirection.json file for the directory
                    FileInfo redirectsFile = GetRedirectsFile(options.InputDirectory);

                    if (redirectsFile == null)
                    {
                        Console.WriteLine($"Could not find redirects file for directory {options.InputDirectory}");
                        return;
                    }

                    // Put all the redirected files in a list
                    List<string> redirectedFiles = new List<string>();
                    GetAllRedirectedFiles(redirectsFile, redirectedFiles);

                    // Get all the markdown and YAML files.
                    List<FileInfo> linkingFiles = GetMarkdownFiles(options.InputDirectory, options.SearchRecursively);
                    linkingFiles.AddRange(GetYAMLFiles(options.InputDirectory, options.SearchRecursively));

                    // Check all links, including in toc.yml, to files in the redirects Dictionary.
                    // Output the files that contain links to redirected topics, as well as the bad links.
                    ListRedirectLinks(redirectedFiles, linkingFiles);

                    Console.WriteLine("\nDONE");
                }

                // Uncomment for debugging to see console output.
                //Console.WriteLine("\nPress any key to continue.");
                //Console.ReadLine();
            }
        }

        private static void ListRedirectLinks(List<string> redirectedFiles, List<FileInfo> linkingFiles)
        {
            foreach (string redirectedFile in redirectedFiles)
            {
                StringBuilder backlinks = new StringBuilder();

                foreach (var linkingFile in linkingFiles)
                {
                    if (IsFileLinkedInFile(redirectedFile, linkingFile))
                    {
                        backlinks.AppendLine(linkingFile.FullName);
                    }
                }

                if (backlinks.Length > 0)
                {
                    Console.WriteLine($"\nRedirected file {redirectedFile} is backlinked from the following files:\n");
                    Console.Write(backlinks.ToString());
                }
            }
        }

        private static void GetAllRedirectedFiles(FileInfo redirectsFile, List<string> redirectedFiles)
        {
            foreach (string line in File.ReadAllLines(redirectsFile.FullName))
            {
                // Example line that we're interested in:
                // "source_path": "docs/extensibility/shell/shell-isolated-or-integrated.md",

                // RegEx pattern to match
                string redirectPattern = @"""source_path"": ""(.*)""";

                // There could be more than one image reference on the line, hence the foreach loop.
                foreach (Match match in Regex.Matches(line, redirectPattern))
                {
                    string relativePath = GetFilePathFromSourcePath(match.Groups[0].Value);

                    if (relativePath != null)
                    {
                        // Construct the full path to the referenced image file
                        string fullPath = Path.Combine(redirectsFile.DirectoryName, relativePath);

                        // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                        fullPath = Path.GetFullPath(fullPath);

                        redirectedFiles.Add(fullPath);
                    }
                }
            }
        }

        /// <summary>
        /// If any of the input image files are not
        /// referenced from a markdown (.md) file anywhere in the directory structure, including up the directory 
        /// until the docfx.json file is found, the file path of those files is written to the console.
        /// </summary>
        private static void ListOrphanedImages(string inputDirectory, Dictionary<string, int> imageFiles, bool verboseOutput, bool deleteOrphanedImages)
        {
            var files = GetAllMarkdownFiles(inputDirectory);

            // Gather up all the image references and increment the count for that image in the Dictionary.
            foreach (var markdownFile in files)
            {
                foreach (string line in File.ReadAllLines(markdownFile.FullName))
                {
                    string mediaDirectoryName = Path.GetFileName(inputDirectory);

                    // Match []() image references where the path to the image file includes the name of the input media directory.
                    // This includes links that don't start with ! for images that are referenced as a hyperlink
                    // instead of an image to display.

                    // RegEx pattern to match
                    string imageLinkPattern = @"\]\(([^\)]*?)" + mediaDirectoryName + @"\/(.*?)\)";

                    // There could be more than one image reference on the line, hence the foreach loop.
                    foreach (Match match in Regex.Matches(line, imageLinkPattern))
                    {
                        string relativePath = GetFilePathFromLink(match.Groups[0].Value);

                        if (relativePath != null)
                        {
                            // Construct the full path to the referenced image file
                            string fullPath = Path.Combine(markdownFile.DirectoryName, relativePath);

                            // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                            fullPath = Path.GetFullPath(fullPath);

                            if (fullPath != null)
                            {
                                // Increment the count for this image file in our dictionary
                                try
                                {
                                    imageFiles[fullPath.ToLower()]++;
                                }
                                catch (KeyNotFoundException)
                                {
                                    ImagesNotInDictionary.AppendLine(fullPath);
                                }
                            }
                        }
                    }

                    // Match "img src=" references
                    if (line.Contains("<img src="))
                    {
                        string relativePath = GetFilePathFromLink(line);

                        if (relativePath != null)
                        {
                            // Construct the full path to the referenced image file
                            string fullPath = Path.Combine(markdownFile.DirectoryName, relativePath);

                            // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                            fullPath = Path.GetFullPath(fullPath);

                            if (fullPath != null)
                            {
                                // Increment the count for this image file in our dictionary
                                try
                                {
                                    imageFiles[fullPath.ToLower()]++;
                                }
                                catch (KeyNotFoundException)
                                {
                                    ImagesNotInDictionary.AppendLine(fullPath);
                                }
                            }
                        }
                    }
                }
            }

            // Print out the image files with zero references.
            Console.WriteLine("The following media files are not referenced from any .md file:\n");
            foreach (var image in imageFiles)
            {
                if (image.Value == 0)
                {
                    Console.WriteLine(Path.GetFullPath(image.Key));
                }
            }

            if (verboseOutput)
            {
                Console.WriteLine(ImagesNotInDictionary.ToString());
            }

            if (deleteOrphanedImages)
            {
                Console.WriteLine("\nDeleting orphaned files...\n");

                // Delete orphaned image files
                foreach (var image in imageFiles)
                {
                    if (image.Value == 0)
                    {
                        Console.WriteLine($"Deleting {image.Key}.");
                        File.Delete(image.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Finds topics that appear more than once, either in one TOC.md file, or multiple TOC.md files.
        /// </summary>
        private static void ListPopularFiles(List<FileInfo> tocFiles, List<FileInfo> markdownFiles)
        {
            // Keep a hash table of each topic path with the number of times it's referenced
            Dictionary<string, int> topics = markdownFiles.ToDictionary<FileInfo, string, int>(mf => mf.FullName, mf => 0);

            foreach (var markdownFile in markdownFiles)
            {
                // If the file is in the Includes directory, ignore it
                if (markdownFile.FullName.Contains("\\includes\\"))
                    continue;

                foreach (var tocFile in tocFiles)
                {
                    if (IsFileLinkedInFile(markdownFile, tocFile))
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
                    Console.WriteLine($"Topic '{topic.Key}' appears more than once in a TOC file.");
                }
            }
        }

        /// <summary>
        /// Lists the files that aren't in a TOC.
        /// Optionally, only list files that don't have a redirect_url metadata tag.
        /// </summary>
        private static void ListOrphanedTopics(List<FileInfo> tocFiles, List<FileInfo> markdownFiles, bool ignoreFilesWithRedirectUrl, bool verboseOutput)
        {
            int countNotFound = 0;

            StringBuilder sb = new StringBuilder("\nTopics not in any TOC file:\n");

            foreach (var markdownFile in markdownFiles)
            {
                bool found = false;

                // If the file is in the Includes directory, or the file is a TOC itself, ignore it
                if (markdownFile.FullName.Contains("\\includes\\") || String.Compare(markdownFile.Name, "TOC.md") == 0 || String.Compare(markdownFile.Name, "TOC.yml") == 0)
                    continue;

                foreach (var tocFile in tocFiles)
                {
                    if (!IsFileLinkedInFile(markdownFile, tocFile))
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    bool redirect = false;

                    // Check if the topic has a redirect_url tag
                    if (ignoreFilesWithRedirectUrl)
                    {
                        redirect = FileContainsRedirectUrl(markdownFile);
                    }

                    // If it's not a redirected topic, or we're not ignoring redirected topic, report this file.
                    if (!redirect)
                    {
                        countNotFound++;
                        sb.AppendLine(markdownFile.FullName);
                    }
                }
            }

            sb.AppendLine($"\nFound {countNotFound} total .md files that are not referenced in a TOC.\n");
            Console.Write(sb.ToString());

            if (verboseOutput)
            {
                Console.WriteLine("Similar file names:\n" + SimilarFiles.ToString());
            }
        }

        /// <summary>
        /// Checks if the specified file path is referenced in the specified file.
        /// </summary>
        private static bool IsFileLinkedInFile(string linkedFile, FileInfo linkingFile)
        {
            FileInfo file = new FileInfo(linkedFile);
            return IsFileLinkedInFile(file, linkingFile);
        }

        /// <summary>
        /// Checks if the specified file path is referenced in the specified file.
        /// </summary>
        private static bool IsFileLinkedInFile(FileInfo linkedFile, FileInfo linkingFile)
        {
            // Read all the .md files listed in the TOC file
            foreach (string line in File.ReadAllLines(linkingFile.FullName))
            {
                string relativePath = null;

                if (line.Contains("](")) // TOC.md style link
                {
                    // If the file name is somewhere in the line of text...
                    if (line.Contains("(" + linkedFile.Name) || line.Contains("/" + linkedFile.Name))
                    {
                        // Now verify the file path to ensure we're talking about the same file
                        relativePath = GetFilePathFromLink(line);
                    }
                }
                else if (line.Contains("href:")) // TOC.yml style link
                {
                    // If the file name is somewhere in the line of text...
                    if (line.Contains(linkedFile.Name))
                    {
                        // Now verify the file path to ensure we're talking about the same file
                        relativePath = GetFilePathFromLink(line);
                    }
                }

                if (relativePath != null)
                {
                    // Construct the full path to the referenced markdown file
                    string fullPath = Path.Combine(linkingFile.DirectoryName, relativePath);

                    // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                    fullPath = Path.GetFullPath(fullPath);
                    if (fullPath != null)
                    {
                        // See if our constructed path matches the actual file we think it is
                        if (String.Compare(fullPath, linkedFile.FullName) == 0)
                        {
                            return true;
                        }
                        else
                        {
                            // We expect a lot of index.md names, so no need to spit out all similarities
                            if (linkedFile.Name != "index.md")
                            {
                                SimilarFiles.AppendLine($"File '{linkedFile.FullName}' has same file name as a file in {linkingFile.FullName}: '{line}'");
                            }
                        }
                    }
                }
            }

            // We did not find this file linked in the specified file.
            return false;
        }

        private static string GetFilePathFromSourcePath(string text)
        {
            // "source_path": "docs/extensibility/shell/shell-isolated-or-integrated.md",

            if (text.Contains("source_path"))
            {
                // Grab the text that starts after "source_path": "
                text = text.Substring(16);

                // Trim the final quotation mark and comma
                text = text.TrimEnd('"', ',');

                return text;
            }
            else
            {
                throw new ArgumentException($"Argument 'line' does not contain an expected redirect source path.");
            }
        }

        /// <summary>
        /// Returns the file path from the specified text that contains 
        /// either the pattern "[text](file path)" or "img src=".
        /// Returns null if the file is in a different repo or is an http URL.
        /// </summary>
        private static string GetFilePathFromLink(string text)
        {
            // Example image references:
            // ![Auto hide](../ide/media/vs2015_auto_hide.png)
            // ![Unit Test Explorer showing Run All button](../test/media/unittestexplorer-beta-.png "UnitTestExplorer(beta)")
            // ![link to video](../data-tools/media/playvideo.gif "PlayVideo")For a video version of this topic, see...
            // <img src="../data-tools/media/logo_azure-datalake.svg" alt=""
            // The Light Bulb icon ![Small Light Bulb Icon](media/vs2015_lightbulbsmall.png "VS2017_LightBulbSmall"),

            // but not:
            // <![CDATA[

            // Example .md file reference in a TOC:
            // ### [Managing External Tools](ide/managing-external-tools.md)

            if (text.Contains("]("))
            {
                text = text.Substring(text.IndexOf("](") + 2);

                if (text.StartsWith("/") || text.StartsWith("http"))
                {
                    // The file is in a different repo, so ignore it.
                    return null;
                }

                // Look for the closing parenthesis.
                string relativePath;
                try
                {
                    relativePath = text.Substring(0, text.IndexOf(')'));
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Image link is likely badly formatted.
                    Console.WriteLine($"Caught ArgumentOutOfRangeException while extracting the image path from the following text: {text}\n");
                    return null;
                }

                // If there is a whitespace character in the string, truncate it there.
                int index = relativePath.IndexOf(' ');
                if (index > 0)
                {
                    relativePath = relativePath.Substring(0, index);
                }

                return relativePath;
            }
            else if (text.Contains("href:"))
            {
                // e.g. href: ../ide/quickstart-python.md
                return text.Substring(text.IndexOf("href:") + 5).Trim();
            }
            else if (text.Contains("img src="))
            {
                text = text.Substring(text.IndexOf("img src=") + 9);

                if (text.StartsWith("/") || text.StartsWith("http"))
                {
                    // The file is in a different repo, so ignore it.
                    return null;
                }

                // Check that the path is valid, i.e. it starts with a letter or a '.'.
                // RegEx pattern to match
                string imageLinkPattern = @"^(\w|\.).*";

                if (Regex.Matches(text, imageLinkPattern).Count > 0)
                {
                    try
                    {
                        return text.Substring(0, text.IndexOf('"'));
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine($"Caught ArgumentException while extracting the image path from the following text: {text}\n");
                        return null;
                    }
                }
                else
                {
                    // Unrecognizable file path.
                    Console.WriteLine($"Unrecognizable file path (ignoring this image link): {text}\n");
                    return null;
                }
            }
            else
            {
                throw new ArgumentException($"Argument 'line' does not contain an expected link pattern.");
            }
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
        private static List<FileInfo> GetAllMarkdownFiles(string directoryPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);

            // Look further up the path until we find docfx.json
            dir = GetDocFxDirectory(dir);

            return dir.EnumerateFiles("*.md", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Returns a dictionary of all files in the directory.
        /// The search includes the specified directory and all its subdirectories.
        /// </summary>
        private static Dictionary<string, int> GetMediaFiles(string mediaDirectory, bool searchRecursively = true)
        {
            DirectoryInfo dir = new DirectoryInfo(mediaDirectory);

            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            Dictionary<string, int> mediaFiles = new Dictionary<string, int>();

            foreach (var file in dir.EnumerateFiles())
            {
                mediaFiles.Add(file.FullName.ToLower(), 0);
            }

            var mediaDirectories = dir.EnumerateDirectories("*", searchOption);

            foreach (var directory in mediaDirectories)
            {
                foreach (var file in directory.EnumerateFiles())
                {
                    mediaFiles.Add(file.FullName.ToLower(), 0);
                }
            }

            return mediaFiles;
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

            return dir.EnumerateFiles("TOC.*", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// </summary>
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

                    if (dir == dir.Root)
                        throw new Exception("Could not find docfx.json file in directory or parent.");
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"Could not find directory {dir.FullName}");
                throw;
            }

            return dir;
        }

        /// <summary>
        /// Returns true if the specified file contains a "redirect_url" metadata tag.
        /// </summary>
        private static bool FileContainsRedirectUrl(FileInfo markdownFile)
        {
            foreach (var line in File.ReadAllLines(markdownFile.FullName))
            {
                // If the file has a redirect_url metadata tag, return true
                if (line.Contains("redirect_url:"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
