using CommandLine;
using CommandLine.Text;

namespace NotInToc
{
    // Define a class to receive parsed values
    class Options
    {
        [Option('d', "directory", Required = true, HelpText = "Directory to search for .md files.")]
        public string InputDirectory { get; set; }

        [Option('o', null, HelpText = "Use this option to find orphaned topics.")]
        public bool FindOrphans { get; set; }

        [Option('m', null, HelpText = "Use this option to find topics that appear more than once in one or multiple TOC.md file.")]
        public bool FindMultiples { get; set; }

        [Option('i', "ignore_redirects", DefaultValue = true, Required = false, HelpText = "Ignore .md files that have a redirect_url tag when looking for orphans.")]
        public bool IgnoreRedirects { get; set; }

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
