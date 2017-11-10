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
        }

        /// <summary>
        /// Lists the file that aren't in a TOC, optionally only those files that don't have a redirect_url metadata tag.
        /// </summary>
        private static void ListFilesNotInToc(List<FileInfo> tocFiles, List<FileInfo> markdownFiles, bool ignoreFilesWithRedirectUrl = true)
        {
            foreach (var markdownFile in markdownFiles)
            {
                // If the file is in the Includes directory, ignore it
                if (markdownFile.FullName.Contains("\\includes\\"))
                    continue;

                //if (!IsInToc(markdownFile.Name, tocFiles))
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
        /// Checks if the specified file is referenced in a TOC.md file.
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
        /// Checks if the specified file is referenced in a TOC.md file.
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
                    int startOfFileName = line.LastIndexOf('/');
                    if (startOfFileName == -1)
                    {
                        // There's no '/' in the path to the file
                        startOfFileName = startOfPath;
                    }

                    string fileNameInToc = line.Substring(startOfFileName, line.LastIndexOf(')') - startOfFileName);

                    //if (line.Contains(markdownFile.Name))
                    if (String.Compare(markdownFile.Name, fileNameInToc) == 0)
                    {
                        // Now verify the path to ensure we're talking about the same file
                        string relativePath = line.Substring(startOfPath, line.LastIndexOf(')') - startOfPath).Replace('/', '\\');
                        string fullPath = String.Concat(tocFile.DirectoryName, "\\", relativePath);

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

        private static List<FileInfo> GetMarkdownFiles(string directoryPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            return dir.EnumerateFiles("*.md", SearchOption.AllDirectories).ToList();
        }

        private static List<FileInfo> GetTocFiles(string directoryPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            return dir.EnumerateFiles("toc.md", SearchOption.AllDirectories).ToList();
        }
    }
}
