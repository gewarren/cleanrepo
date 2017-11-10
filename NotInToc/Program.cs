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
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: NotInToc.exe <directory path>");
                return;
            }

            string directory = args[0];

            List<FileInfo> tocFiles = GetTocFiles(directory);
            List<FileInfo> markdownFiles = GetMarkdownFiles(directory);

            ListFilesNotInToc(tocFiles, markdownFiles);


            // Uncomment for debugging to see console output.
            //Console.ReadLine();
        }

        /// <summary>
        /// Lists the files that aren't in a TOC.
        /// Optionally, only list files that don't have a redirect_url metadata tag.
        /// </summary>
        private static void ListFilesNotInToc(List<FileInfo> tocFiles, List<FileInfo> markdownFiles, bool ignoreFilesWithRedirectUrl = true)
        {
            foreach (var markdownFile in markdownFiles)
            {
                // If the file is in the Includes directory, ignore it
                if (markdownFile.FullName.Contains("\\includes\\"))
                    continue;

                if (!IsInToc(markdownFile, tocFiles))
                {
                    // If we're ignoring files with redirect_url metadata tags,
                    // see if this file has a redirect_url tag.
                    if (ignoreFilesWithRedirectUrl)
                    {
                        bool redirect = false;

                        foreach (var line in File.ReadAllLines(markdownFile.FullName))
                        {
                            // If the file has a redirect_url metadata tag, set a flag
                            if (line.Contains("redirect_url:"))
                            {
                                redirect = true;
                                break;
                            }
                        }

                        // If the file doesn't have a redirect_url tag, report it
                        if (!redirect)
                        {
                            Console.WriteLine(String.Format("File '{0}' is not in any TOC file", markdownFile.FullName));
                        }
                    }
                    else
                    {
                        Console.WriteLine(String.Format("File '{0}' is not in any TOC file", markdownFile.FullName));
                    }
                }
            }
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
        /// Checks if the specified file PATH is referenced in a TOC.md file.
        /// </summary>
        private static bool IsInToc(FileInfo markdownFile, List<FileInfo> tocFiles)
        {
            foreach (var tocFile in tocFiles)
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
                            // We expect a lot of TOC.md names, so no need to spit out all similarities
                            if (markdownFile.Name != "TOC.md" && markdownFile.Name != "index.md")
                            {
                                Console.WriteLine(String.Format("File '{0}' has same file name as a file in {1}: '{2}'", markdownFile.FullName, tocFile.FullName, line));
                            }
                        }
                    }
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
    }
}
