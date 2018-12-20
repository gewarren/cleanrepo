using CommandLine;
using CommandLine.Text;

namespace CleanRepo
{
    // Define a class to receive parsed values
    class Options
    {
        [Option('d', "directory", Required = true, HelpText = "Directory to start search for markdown files, or the media directory to search in for orphaned .png files, or the directory to search in for orphaned INCLUDE files.")]
        public string InputDirectory { get; set; }

        [Option('o', "orphaned_topics", HelpText = "Use this option to find orphaned topics.")]
        public bool FindOrphanedTopics { get; set; }

        [Option('m', "multiples", HelpText = "Use this option to find topics that appear more than once in one or separate TOC.md files.")]
        public bool FindMultiples { get; set; }

        [Option('p', "orphaned_images", HelpText = "Use this option to find orphaned .png files.")]
        public bool FindOrphanedImages { get; set; }

        [Option('i', "orphaned_includes", HelpText = "Use this option to find orphaned INCLUDE files.")]
        public bool FindOrphanedIncludes { get; set; }

        [Option('g', "delete", DefaultValue = false, Required = false, HelpText = "Set to true to delete orphaned markdown or .png files.")]
        public bool Delete { get; set; }

        [Option('l', "redirects", Required = false, HelpText = "Finds backlinks to redirected files in the specified directory.")]
        public bool FindRedirectedTopicLinks { get; set; }

        [Option('r', "replace_redirects", DefaultValue = false, Required = false, HelpText = "Set to true to replace links to redirected files with their target URL.")]
        public bool ReplaceLinks { get; set; }

        [Option('s', "recursive", DefaultValue = true, Required = false, HelpText = "Search directory and all subdirectories.")]
        public bool SearchRecursively { get; set; }

        [Option('v', "verbose", DefaultValue = false, Required = false, HelpText = "Output verbose results.")]
        public bool Verbose { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
