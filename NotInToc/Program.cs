using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NotInToc
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
            }

            // Uncomment for debugging to see console output.
            //Console.WriteLine("\nPress any key to continue.");
            //Console.ReadLine();
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
                        string relativePath = GetFilePath(match.Groups[0].Value);

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
                        string relativePath = GetFilePath(line);

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
                    if (IsInToc(markdownFile, tocFile))
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
                if (markdownFile.FullName.Contains("\\includes\\") || String.Compare(markdownFile.Name, "TOC.md") == 0)
                    continue;

                foreach (var tocFile in tocFiles)
                {
                    if (!IsInToc(markdownFile, tocFile))
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
        /// Checks if the specified file path is referenced in a TOC.md file.
        /// </summary>
        private static bool IsInToc(FileInfo markdownFile, FileInfo tocFile)
        {
            // Read all the .md files listed in the TOC file
            foreach (string line in File.ReadAllLines(tocFile.FullName))
            {
                if (line.Contains("](") == false)
                {
                    // line doesn't contain a file reference
                    continue;
                }

                // If the file name is somewhere in the line of text...
                if (line.Contains("(" + markdownFile.Name) || line.Contains("/" + markdownFile.Name))
                {
                    // Now verify the file path to ensure we're talking about the same file
                    string relativePath = GetFilePath(line);
                    if (relativePath != null)
                    {
                        // Construct the full path to the referenced markdown file
                        string fullPath = Path.Combine(tocFile.DirectoryName, relativePath);

                        // This cleans up the path by replacing forward slashes with back slashes, removing extra dots, etc.
                        fullPath = Path.GetFullPath(fullPath);
                        if (fullPath != null)
                        {
                            // See if our constructed path matches the actual file we think it is
                            if (String.Compare(fullPath, markdownFile.FullName) == 0)
                            {
                                return true;
                            }
                            else
                            {
                                // We expect a lot of index.md names, so no need to spit out all similarities
                                if (markdownFile.Name != "index.md")
                                {
                                    SimilarFiles.AppendLine($"File '{markdownFile.FullName}' has same file name as a file in {tocFile.FullName}: '{line}'");
                                }
                            }
                        }
                    }
                }
            }

            // We did not find this markdown file in any TOC file.
            return false;
        }

        /// <summary>
        /// Returns the file path from the specified text that contains 
        /// either the pattern "[text](file path)" or "img src=".
        /// Returns null if the file is in a different repo or is an http URL.
        /// </summary>
        private static string GetFilePath(string text)
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
                string relativePath = text.Substring(0, text.IndexOf(')'));

                // If there is a whitespace character in the string, truncate it there.
                int index = relativePath.IndexOf(' ');
                if (index > 0)
                {
                    relativePath = relativePath.Substring(0, index);
                }

                return relativePath;
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
                    catch (ArgumentException e)
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
                throw new ArgumentException($"Argument 'line' does not contain the pattern '](' or 'img src='.");
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
        /// Gets all TOC.md files recursively, starting in the
        /// specified directory if it contains "docfx.json" file.
        /// Otherwise it looks up the directory path until it finds 
        /// a "docfx.json" file. Then it starts the recursive search
        /// for TOC.md files from that directory.
        /// </summary>
        private static List<FileInfo> GetTocFiles(string directoryPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);

            // Look further up the path until we find docfx.json
            dir = GetDocFxDirectory(dir);

            return dir.EnumerateFiles("TOC.md", SearchOption.AllDirectories).ToList();
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
                        throw new Exception("Could not find docfx.json file in directory structure.");
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
