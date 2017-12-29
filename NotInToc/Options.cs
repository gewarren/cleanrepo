using CommandLine;
using CommandLine.Text;

namespace NotInToc
{
    // Define a class to receive parsed values
    class Options
    {
        [Option('d', "directory", Required = true, HelpText = "Directory to start search for markdown files or media directories.")]
        public string InputDirectory { get; set; }

        [Option('r', "recursive", DefaultValue = true, Required = false, HelpText = "Search directory and all subdirectories.")]
        public bool SearchRecursively { get; set; }

        [Option('o', "orphaned_topics", HelpText = "Use this option to find orphaned topics.")]
        public bool FindOrphanedTopics { get; set; }

        [Option('m', "multiples", HelpText = "Use this option to find topics that appear more than once in one or separate TOC.md files.")]
        public bool FindMultiples { get; set; }

        [Option('p', "orphaned_images", HelpText = "Use this option to find orphaned images.")]
        public bool FindOrphanedImages { get; set; }

        [Option('i', "ignore_redirects", DefaultValue = true, Required = false, HelpText = "Ignore .md files that have a redirect_url tag when looking for orphans.")]
        public bool IgnoreRedirects { get; set; }

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
