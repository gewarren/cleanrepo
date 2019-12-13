using CommandLine;
using CommandLine.Text;

namespace CleanRepo
{
    // Define a class to receive parsed values
    class Options
    {
        [Option('d', "directory", HelpText = "Top-level directory in which to perform clean up (for example, find orphaned markdown files).")]
        public string InputDirectory { get; set; }

        [Option("orphaned-topics", HelpText = "Use this option to find orphaned topics.")]
        public bool FindOrphanedTopics { get; set; }

        [Option("multiples", HelpText = "Use this option to find topics that appear more than once in one or separate TOC.md files.")]
        public bool FindMultiples { get; set; }

        [Option("orphaned-images", HelpText = "Find orphaned .png, .gif, .jpg, or .svg files.")]
        public bool FindOrphanedImages { get; set; }

        [Option("orphaned-includes", HelpText = "Find orphaned INCLUDE files.")]
        public bool FindOrphanedIncludes { get; set; }

        [Option('g', "delete", DefaultValue = false, Required = false, HelpText = "Delete orphaned markdown or .png/.jpg/.gif/.svg files.")]
        public bool Delete { get; set; }

        [Option("clean-redirects", Required = false, HelpText = "Clean redirection JSON file by replacing targets that are themselves redirected.")]
        public bool CleanRedirectionFile { get; set; }

        [Option("replace-redirects", Required = false, HelpText = "Find backlink to redirected files and replace with new target.")]
        public bool ReplaceRedirectTargets { get; set; }

        [Option("redirects-file", Required = false, HelpText = "Optionally specify a path to a redirect JSON file in a different repo.")]
        public string RedirectsFile { get; set; }

        [Option("relative-links", HelpText = "Replace site-relative links with file-relative links. You must also specify the docset name for the repo.")]
        public bool ReplaceWithRelativeLinks { get; set; }

        [Option("docset-name", Required = false, HelpText = "The docset name that corresponds to the root of this repo in a URL, e.g. 'visualstudio' in 'http://docs.microsoft.com/visualstudio/ide/get-started'.")]
        public string DocsetName { get; set; }

        [Option("docset-root", Required = false, HelpText = "The full path to the root directory for the docset, e.g. 'c:\\users\\gewarren\\dotnet-docs\\docs'.")]
        public string DocsetRoot { get; set; }

        [Option('s', "recursive", DefaultValue = true, Required = false, HelpText = "Search directory and all subdirectories for markdown, yaml, image, and include files (depending on chosen function).")]
        public bool SearchRecursively { get; set; }

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
