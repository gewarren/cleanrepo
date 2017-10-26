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

        private static void ListFilesNotInToc(List<FileInfo> tocFiles, List<FileInfo> markdownFiles, bool ignoreFilesWithRedirectUrl = true)
        {
            foreach (var markdownFile in markdownFiles)
            {
                // If the file is in the Includes directory, ignore it
                if (markdownFile.FullName.Contains("\\includes\\"))
                    continue;

                if (!IsInToc(markdownFile.Name, tocFiles))
                {
                    if (ignoreFilesWithRedirectUrl)
                    {
                        bool redirect = false;

                        foreach (var line in File.ReadAllLines(markdownFile.FullName))
                        {
                            if (line.Contains("redirect_url:"))
                            {
                                redirect = true;
                                break;
                            }
                        }

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
