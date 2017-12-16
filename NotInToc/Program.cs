using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NotInToc
{
    class Program
    {
        static void Main(string[] args)
        {
            // Command line options
            var options = new Options();

            bool parsedArgs = CommandLine.Parser.Default.ParseArguments(args, options);

            if (parsedArgs)
            {
                // Find orphaned topics
                if (options.FindOrphanedTopics)
                {
                    Console.WriteLine($"\nSearching the {options.InputDirectory} directory and its subdirectories for orphaned topics.\n");

                    List<FileInfo> tocFiles = GetTocFiles(options.InputDirectory, options.SearchRecursively);
                    List<FileInfo> markdownFiles = GetMarkdownFiles(options.InputDirectory, options.SearchRecursively);

                    ListOrphanedTopics(tocFiles, markdownFiles, options.IgnoreRedirects);
                }
                else if (options.FindMultiples)
                {
                    Console.WriteLine($"\nSearching the {options.InputDirectory} directory and its subdirectories for " +
                        $"topics that appear more than once in one or more TOC.md files.\n");

                    List<FileInfo> tocFiles = GetTocFiles(options.InputDirectory, options.SearchRecursively);
                    List<FileInfo> markdownFiles = GetMarkdownFiles(options.InputDirectory, options.SearchRecursively);

                    ListPopularFiles(tocFiles, markdownFiles);
                }
                else if (options.FindOrphanedImages)
                {
                    Console.WriteLine($"\nSearching the {options.InputDirectory} directory and its subdirectories for orphaned images.\n");

                    Dictionary<string, int> imageFiles = GetMediaFiles(options.InputDirectory, options.SearchRecursively);

                    ListOrphanedImages(options.InputDirectory, imageFiles, options.SearchRecursively);
                }
            }

            // Uncomment for debugging to see console output.
            //Console.WriteLine("\nPress any key to continue.");
            //Console.ReadLine();
        }

        private static void ListOrphanedImages(string inputDirectory, Dictionary<string, int> imageFiles, bool searchRecursively)
        {
            DirectoryInfo dir = new DirectoryInfo(inputDirectory);
            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var markdownFile in dir.EnumerateFiles("*.md", searchOption))
            {
                foreach (string line in File.ReadAllLines(markdownFile.FullName))
                {
                    // Image references start with "!["
                    if (line.Trim().StartsWith("!["))
                    {
                        int startOfPath = line.IndexOf("](") + 2;

                        // Example image reference:
                        //  ![Auto hide](../ide/media/vs2015_auto_hide.png "vs2017_auto_hide") 

                        // Construct the full path to the referenced image file
                        string fullPath = ConstructFullPath(markdownFile, line, startOfPath);

                        // If there's a nickname on the end, remove it
                        if (fullPath.EndsWith("\""))
                        {
                            int nicknameLength = fullPath.LastIndexOf('"') - fullPath.IndexOf('"');

                            // Trim nickname + 3 characters from the end
                            fullPath = fullPath.Substring(0, fullPath.Length - (nicknameLength + 2));
                        }

                        // Increment the count for this image file in our dictionary
                        imageFiles[fullPath.ToLower()]++;
                    }
                }
            }

            // Now print out the image files with 0 references.
            foreach (var image in imageFiles)
            {
                if (image.Value == 0)
                {
                    Console.WriteLine($"Image '{image.Key}' is not referenced in any markdown files.");
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
        private static void ListOrphanedTopics(List<FileInfo> tocFiles, List<FileInfo> markdownFiles, bool ignoreFilesWithRedirectUrl)
        {
            int countNotFound = 0;

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
                        Console.WriteLine($"File '{markdownFile.FullName}' is not in any TOC file");
                    }
                }
            }

            Console.WriteLine($"\nFound {countNotFound} total .md files that are not in a TOC.");
        }

        /// <summary>
        /// Checks if the specified file PATH is referenced in a TOC.md file.
        /// </summary>
        private static bool IsInToc(FileInfo markdownFile, FileInfo tocFile, bool outputSimilarities = false)
        {
            // Read all the .md files listed in the TOC file
            foreach (string line in File.ReadAllLines(tocFile.FullName))
            {
                if (line.Contains("](") == false)
                {
                    // line doesn't contain a file reference
                    continue;
                }

                int startOfPath;
                string fileNameInToc;

                GetFileName(line, out startOfPath, out fileNameInToc);

                // If the file name is somewhere in the line of text...
                if (String.Compare(markdownFile.Name, fileNameInToc) == 0)
                {
                    // Now verify the file path to ensure we're talking about the same file
                    string fullPath = ConstructFullPath(tocFile, line, startOfPath);

                    // See if our constructed path matches the actual file we think it is
                    if (String.Compare(fullPath, markdownFile.FullName) == 0)
                    {
                        return true;
                    }
                    else
                    {
                        if (outputSimilarities)
                        {
                            // We expect a lot of index.md names, so no need to spit out all similarities
                            if (markdownFile.Name != "index.md")
                            {
                                Console.WriteLine($"File '{markdownFile.FullName}' has same file name as a file in {tocFile.FullName}: '{line}'");
                            }
                        }
                    }
                }
            }

            // We did not find this file in any TOC file.
            return false;
        }

        private static string ConstructFullPath(FileInfo referencingFile, string line, int startOfPath)
        {
            // TODO: Handle paths that have text after them, for example:
            // ![link to video](../data-tools/media/playvideo.gif "PlayVideo")For a video version of this topic, see...

            string relativePath = line.Substring(startOfPath, line.LastIndexOf(')') - startOfPath);

            // Handle paths that start with "./"
            if (relativePath.StartsWith("./"))
            {
                relativePath = relativePath.Substring(2);
            }

            relativePath = relativePath.Replace('/', '\\');

            DirectoryInfo rootPath = referencingFile.Directory;
            while (relativePath.StartsWith(".."))
            {
                // Go up one level in the root path.
                rootPath = rootPath.Parent;

                // Remove "..\" from relative path.
                relativePath = relativePath.Substring(3);
            }

            string fullPath = String.Concat(rootPath.FullName, "\\", relativePath);
            return fullPath;
        }

        private static void GetFileName(string line, out int startOfPath, out string fileNameInReferencingFile)
        {
            startOfPath = line.IndexOf("](") + 2;
            int startOfFileName = line.LastIndexOf('/') + 1;
            if (startOfFileName == 0)
            {
                // There's no '/' in the path to the file
                startOfFileName = startOfPath;
            }

            fileNameInReferencingFile = line.Substring(startOfFileName, line.LastIndexOf(')') - startOfFileName);
        }

        /// <summary>
        /// Gets all *.md files recursively, starting in the
        /// specified directory.
        /// </summary>
        private static List<FileInfo> GetMarkdownFiles(string directoryPath, bool searchRecursively)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return dir.EnumerateFiles("*.md", searchOption).ToList();
        }

        /// <summary>
        /// Returns a dictionary of all files in all directories that contains the word "media".
        /// </summary>
        private static Dictionary<string, int> GetMediaFiles(string inputDirectory, bool searchRecursively)
        {
            DirectoryInfo dir = new DirectoryInfo(inputDirectory);

            // We need to search up the directory tree for image files, because 
            // markdown files can reference image files higher up in the tree.
            dir = GetDocFxDirectory(dir);

            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            Dictionary<string, int> mediaFiles = new Dictionary<string, int>();

            foreach (var directory in dir.EnumerateDirectories("media", searchOption))
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
        private static List<FileInfo> GetTocFiles(string directoryPath, bool searchRecursively)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);

            // Look further up the path until we find docfx.json
            dir = GetDocFxDirectory(dir);

            SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return dir.EnumerateFiles("TOC.md", searchOption).ToList();
        }

        private static DirectoryInfo GetDocFxDirectory(DirectoryInfo dir)
        {
            while (dir.GetFiles("docfx.json", SearchOption.TopDirectoryOnly).Length == 0)
            {
                dir = dir.Parent;

                if (dir == dir.Root)
                    throw new Exception("Could not find docfx.json file in directory structure.");
            }

            return dir;
        }

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
