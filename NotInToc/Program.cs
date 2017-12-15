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

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Find orphaned topics
                if (options.FindOrphans)
                {
                    Console.WriteLine($"\nSearching the {options.InputDirectory} directory and its subdirectories for orphaned topics.\n");

                    List<FileInfo> tocFiles = GetTocFiles(options.InputDirectory);
                    List<FileInfo> markdownFiles = GetMarkdownFiles(options.InputDirectory);

                    ListFilesNotInToc(tocFiles, markdownFiles);
                }
                else if (options.FindMultiples)
                {
                    Console.WriteLine($"\nSearching the {options.InputDirectory} directory and its subdirectories for " +
                        $"topics that appear more than once in one or more TOC.md files.\n");

                    List<FileInfo> tocFiles = GetTocFiles(options.InputDirectory);
                    List<FileInfo> markdownFiles = GetMarkdownFiles(options.InputDirectory);

                    ListPopularFiles(tocFiles, markdownFiles);
                }
            }
            else
            {
                Console.WriteLine(options.GetUsage());
            }

            // Uncomment for debugging to see console output.
            //Console.WriteLine("\nPress any key to continue.");
            //Console.ReadLine();
        }

        /// <summary>
        /// Finds topics that appear more than once, either in one TOC.md file, or multiple TOC.md files.
        /// </summary>
        private static void ListPopularFiles(List<FileInfo> tocFiles, List<FileInfo> markdownFiles)
        {
            // Keep a hash table of each topic path with the number of times it's referenced
            Dictionary<FileInfo, int> topics = new Dictionary<FileInfo, int>(markdownFiles.Count);

            foreach (var markdownFile in markdownFiles)
            {
                // If the file is in the Includes directory, ignore it
                if (markdownFile.FullName.Contains("\\includes\\"))
                    continue;

                foreach (var tocFile in tocFiles)
                {
                    if (IsInToc(markdownFile, tocFile))
                    {
                        topics[tocFile]++;
                    }
                }
            }
        }

        /// <summary>
        /// Lists the files that aren't in a TOC.
        /// Optionally, only list files that don't have a redirect_url metadata tag.
        /// </summary>
        private static void ListFilesNotInToc(List<FileInfo> tocFiles, List<FileInfo> markdownFiles, bool ignoreFilesWithRedirectUrl = true)
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
                    // line doesn't contain a file reference
                    continue;

                int startOfPath = line.IndexOf("](") + 2;
                int startOfFileName = line.LastIndexOf('/') + 1;
                if (startOfFileName == 0)
                {
                    // There's no '/' in the path to the file
                    startOfFileName = startOfPath;
                }

                string fileNameInToc = line.Substring(startOfFileName, line.LastIndexOf(')') - startOfFileName);

                // If the file name is somewhere in the line of text...
                if (String.Compare(markdownFile.Name, fileNameInToc) == 0)
                {
                    // Now verify the file path to ensure we're talking about the same file
                    string relativePath = line.Substring(startOfPath, line.LastIndexOf(')') - startOfPath).Replace('/', '\\');

                    DirectoryInfo rootPath = tocFile.Directory;
                    while (relativePath.StartsWith(".."))
                    {
                        // Go up one level in the root path.
                        rootPath = rootPath.Parent;

                        // Remove "..\" from relative path.
                        relativePath = relativePath.Substring(3);
                    }

                    string fullPath = String.Concat(rootPath.FullName, "\\", relativePath);

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

        /// <summary>
        /// Checks if the specified file NAME is referenced in a TOC.md file.
        /// </summary>
        private static bool IsInToc(string markdownFile, List<FileInfo> tocFiles)
        {
            foreach (var tocFile in tocFiles)
            {
                // Read all the .md files listed in the TOC file
                foreach (string line in File.ReadAllLines(tocFile.FullName))
                {
                    if (line.Contains(markdownFile))
                        return true;
                }
            }

            // We did not find this file in any TOC file.
            return false;
        }

        /// <summary>
        /// Gets all *.md files recursively, starting in the
        /// specified directory.
        /// </summary>
        private static List<FileInfo> GetMarkdownFiles(string directoryPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            return dir.EnumerateFiles("*.md", SearchOption.AllDirectories).ToList();
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
            while (dir.GetFiles("docfx.json", SearchOption.TopDirectoryOnly).Length == 0)
            {
                dir = dir.Parent;

                if (dir == dir.Root)
                    throw new Exception("Could not find docfx.json file in directory structure.");
            }

            return dir.EnumerateFiles("TOC.md", SearchOption.AllDirectories).ToList();
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
