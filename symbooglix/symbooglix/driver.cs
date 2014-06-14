using CommandLine;
using CommandLine.Text;
using System;
using System.IO;
using Microsoft;
using System.Linq;
using Microsoft.Boogie;
using System.Diagnostics;
using System.Collections.Generic;



namespace symbooglix
{
    public class driver
    {
        class CmdLineOpts
        {
            [Option('e', "entry-point", DefaultValue = "main", HelpText = "Use Constant folding during execution")]
            public string entryPoint { get; set; }

            // FIXME: Booleans can't be disabled in the CommandLine library so use ints instead
            [Option("fold-constants", DefaultValue = 1, HelpText = "Use Constant folding during execution")]
            public int useConstantFolding { get; set; }

            [Option("print-instr", DefaultValue = false, HelpText = "Print instructions during execution")]
            public bool useInstructionPrinter { get; set; }

            [Option("print-stack-enter-leave", DefaultValue = false, HelpText = "Print stackframe when entering/leaving procedures")]
            public bool useEnterLeaveStackPrinter { get; set; }

            [Option("print-call-seq", DefaultValue = false, HelpText = "Print call sequence during execution")]
            public bool useCallSequencePrinter { get; set; }

            [Option("verify-unmodified-impl", DefaultValue = true, HelpText = "Verify that implementation commands aren't accidently modified during execution")]
            public bool useVerifyUnmodifiedProcedureHandler { get; set; }

            // Positional args
            [ValueOption(0)]
            public string boogieProgramPath { get; set; }

            // For printing parser error messages
            [ParserState]
            public IParserState LastParserState { get; set; }

           
            [HelpOption]
            public string GetUsage()
            {
                var help = new HelpText {
                    Heading = new HeadingInfo("Symbooglix", "The symbolic execution engine for boogie programs"),
                    Copyright = new CopyrightInfo("Dan Liew", 2014),
                    AdditionalNewLineAfterOption = true,
                    AddDashesToOption = true
                };

                // FIXME: Printing parser errors is totally broken.
                if (LastParserState == null)
                    Console.WriteLine("FIXME: CommandLine parser did not give state");

                if (LastParserState != null && LastParserState.Errors.Any())
                {
                    var errors = help.RenderParsingErrorsText(this, 2);
                    help.AddPostOptionsLine("Error: Failed to parse command line options");
                    help.AddPostOptionsLine(errors);
                }
                else
                {

                    help.AddPreOptionsLine("Usage: symbooglix [options] <boogie program>");
                    help.AddOptions(this);
                }

                return help;
            }
        }

        public static int Main(String[] args)
        {
            // Debug log output goes to standard error.
            // FIXME: We should throw exceptions when assertions fail.
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Error));

            // FIXME: Urgh... we are forced to use Boogie's command line
            // parser becaue the Boogie program resolver/type checker
            // is dependent on the parser being used...EURGH!
            CommandLineOptions.Install(new Microsoft.Boogie.CommandLineOptions());


            var options = new CmdLineOpts();
            if (! CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine("Failed to parse args");
                return 1;
            }

            if (options.boogieProgramPath == null)
            {
                Console.WriteLine("A boogie program must be specified. See --help");
                return 1;
            }

            if (!File.Exists(options.boogieProgramPath))
            {
                Console.WriteLine("Boogie program \"" + options.boogieProgramPath + "\" does not exist");
                return 1;
            }

           
            Program p = null;

            // FIXME: Provide command line option for this
            var defines = new List<String> { "FILE_0" }; // WTF??
            int errors = Parser.Parse (options.boogieProgramPath, defines, out p);

            if (errors != 0)
            {
                Console.WriteLine("Failed to parse");
                return 1;
            }

            errors = p.Resolve();

            if (errors != 0)
            {
                Console.WriteLine("Failed to resolve.");
                return 1;
            }

            errors = p.Typecheck();

            if (errors != 0)
            {
                Console.WriteLine("Failed to Typecheck.");
                return 1;
            }


            IStateScheduler scheduler = new DFSStateScheduler();
            // HACK: Get rid of this ASAP!
            /*
            Solver.ISolver solver = new Solver.SMTLIBQueryLoggingSolver(new Solver.DummySolver(), 
                                                                        new StreamWriter(Console.OpenStandardOutput()),
                                                                        true); // FIXME: Use a real solver
            */
            Solver.ISolver solver = new Solver.DummySolver(); // But back so tests can pass.

            Executor e = new Executor(p, scheduler, solver);


            Implementation entry = p.TopLevelDeclarations.OfType<Implementation>().Where(i => i.Name == options.entryPoint).FirstOrDefault();
            if (entry == null)
            {
                Console.WriteLine("Could not find implementation \"" + options.entryPoint + "\" to use as entry point");
                return 1;
            }

            // This debugging handler should be registered first
            IExecutorHandler verifyUnmodified = null;
            if (options.useVerifyUnmodifiedProcedureHandler)
            {
                verifyUnmodified = new VerifyUnmodifiedProcedureHandler();
                e.registerPreEventHandler(verifyUnmodified);
            }

            if (options.useInstructionPrinter)
            {
                Console.WriteLine("Installing instruction printer");
                e.registerPreEventHandler(new InstructionPrinter());
            }

            if (options.useEnterLeaveStackPrinter)
            {
                Console.WriteLine("Installing Entering and Leaving stack printer");
                e.registerPreEventHandler(new EnterAndLeaveStackPrinter());
            }

            if (options.useCallSequencePrinter)
            {
                Console.WriteLine("Installing call sequence printer");
                e.registerPreEventHandler(new CallSequencePrinter());
            }

            if (options.useVerifyUnmodifiedProcedureHandler)
            {
                // This debugging handler should be registered last
                e.registerPostEventHandler(verifyUnmodified);
            }

            if (options.useConstantFolding > 0)
            {
                e.UseConstantFolding = true;
            }
            else
            {
                e.UseConstantFolding = false;
            }

            // Just print a message about break points for now.
            e.registerBreakPointHandler(new BreakPointPrinter());

            e.registerTerminationHandler(new TerminationConsoleReporter());

            e.run(entry);
            return 0;
        }
    }
}

