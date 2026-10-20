using System;
using ElectricGrid.Core.DataStructures;
using ElectricGrid.Core.IO;
using ElectricGrid.Core.Models;
using ElectricGrid.Core.Reporting;
using ElectricGrid.Core.Services;

namespace ElectricGrid.ConsoleApp.UI
{
    /// <summary>
    /// Interactive console menu. It only collects input, calls <see cref="IPowerGridService"/> and
    /// prints the results; every rule of the system lives in the Core library.
    /// </summary>
    internal sealed class MainMenu
    {
        private const string DefaultSubstationsFile = "substations.csv";
        private const string DefaultLinesFile = "lines.csv";
        private const string DefaultReportFile = "grid_report.txt";

        private readonly IPowerGridService _service;
        private readonly IStructureInspector _inspector;
        private readonly GridDataLoader _loader;
        private readonly ConsoleInput _input;

        public MainMenu(IPowerGridService service, IStructureInspector inspector)
        {
            _service = service;
            _inspector = inspector;
            _loader = new GridDataLoader(service);
            _input = new ConsoleInput(service);
        }

        /// <summary>Runs the menu loop until the operator chooses to exit.</summary>
        public void Run()
        {
            bool running = true;
            while (running)
            {
                PrintMenu();
                string option = _input.ReadText("Select an option");

                try
                {
                    running = Dispatch(option);
                }
                catch (NotImplementedException ex)
                {
                    // Placeholder for the parts still to be written by teammates (Dijkstra, Trie).
                    ConsolePresenter.Warning($"Feature pending implementation: {ex.Message}");
                }
                catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
                {
                    ConsolePresenter.Error($"Unexpected problem: {ex.Message}");
                }

                if (running && option != "0")
                {
                    _input.Pause();
                }
            }

            ConsolePresenter.Info("Goodbye!");
        }

        private bool Dispatch(string option)
        {
            switch (option)
            {
                case "1": LoadSubstationsFromFile(); break;
                case "2": LoadLinesFromFile(); break;
                case "3": LoadSampleData(); break;
                case "4": RegisterSubstation(); break;
                case "5": FindSubstation(); break;
                case "6": RemoveSubstation(); break;
                case "7": Autocomplete(); break;
                case "8": ConsolePresenter.Substations(_service.GetAllSubstations()); break;
                case "9": RegisterLine(); break;
                case "10": ProposeLine(); break;
                case "11": RemoveLine(); break;
                case "12": ConsolePresenter.Lines(_service.GetAllLines()); break;
                case "13": FindRoute(); break;
                case "14": RunTraversal(breadthFirst: true); break;
                case "15": RunTraversal(breadthFirst: false); break;
                case "16": CheckSameZone(); break;
                case "17": ConsolePresenter.Zones(_service.GetZones()); break;
                case "18": ShowReport(); break;
                case "19": Console.WriteLine(_inspector.DescribeHashTable()); break;
                case "20": Console.WriteLine(_inspector.DescribeGraph()); break;
                case "21": Console.WriteLine(_inspector.DescribeTrie()); break;
                case "22": Console.WriteLine(_inspector.DescribeDisjointSets()); break;
                case "23": ClearData(); break;
                case "0": return false;
                default: ConsolePresenter.Warning("Invalid option."); break;
            }

            return true;
        }

        private static void PrintMenu()
        {
            ConsolePresenter.Title("MUNICIPAL ELECTRICAL GRID MANAGEMENT");
            Console.WriteLine(" DATA");
            Console.WriteLine("   1. Load substations from file       2. Load lines from file");
            Console.WriteLine("   3. Load sample data (Data folder)   23. Clear all data");
            Console.WriteLine(" SUBSTATIONS  [Hash Table]");
            Console.WriteLine("   4. Register substation              5. Find substation by code");
            Console.WriteLine("   6. Remove substation                7. Autocomplete names [Trie - PENDING]");
            Console.WriteLine("   8. List all substations");
            Console.WriteLine(" LINES  [Graph]");
            Console.WriteLine("   9. Register line                   10. Propose new line (zone check)");
            Console.WriteLine("  11. Remove line                     12. List all lines");
            Console.WriteLine(" ANALYSIS  [Graph + Union-Find]");
            Console.WriteLine("  13. Optimal route [Dijkstra - PENDING]  14. BFS traversal");
            Console.WriteLine("  15. DFS traversal                   16. Same electrical zone?");
            Console.WriteLine("  17. List electrical zones           18. Grid report (view / export)");
            Console.WriteLine(" STRUCTURE INSPECTION");
            Console.WriteLine("  19. Hash table    20. Graph    21. Trie (pending)    22. Union-Find");
            Console.WriteLine("   0. Exit");
            Console.WriteLine();
        }

        // ------------------------------------------------------------------ Data loading

        private void LoadSubstationsFromFile()
        {
            ConsolePresenter.Title("Load substations");
            Console.WriteLine("Format: code,name,capacity_mw");
            string path = _input.ReadText("File path", DefaultSubstationsFile);
            ConsolePresenter.LoadReport(_loader.LoadSubstations(path));
        }

        private void LoadLinesFromFile()
        {
            ConsolePresenter.Title("Load lines");
            Console.WriteLine("Format: source_code,target_code,length_km,maintenance_cost,bidirectional");
            string path = _input.ReadText("File path", DefaultLinesFile);
            ConsolePresenter.LoadReport(_loader.LoadLines(path));
        }

        private void LoadSampleData()
        {
            ConsolePresenter.Title("Load sample data");
            ConsolePresenter.LoadReport(_loader.LoadAll(DefaultSubstationsFile, DefaultLinesFile));
        }

        private void ClearData()
        {
            if (_input.ReadYesNo("This removes every substation and line. Continue?", false))
            {
                _service.Clear();
                ConsolePresenter.Success("All data cleared.");
            }
        }

        // ------------------------------------------------------------------ Substations

        private void RegisterSubstation()
        {
            ConsolePresenter.Title("Register substation");
            string code = _input.ReadText("Code (e.g. SUB-050)");
            string name = _input.ReadText("Name");
            double capacity = _input.ReadNumber("Capacity in MW", 0.01);

            ConsolePresenter.Result(_service.RegisterSubstation(code, name, capacity));
        }

        private void FindSubstation()
        {
            ConsolePresenter.Title("Find substation by code");
            string code = _input.ReadText("Code");
            Substation? found = _service.FindSubstation(code);

            if (found == null)
            {
                ConsolePresenter.Warning($"No substation with code '{code.ToUpperInvariant()}'.");
                return;
            }

            ConsolePresenter.Substations(new[] { found });
        }

        private void RemoveSubstation()
        {
            ConsolePresenter.Title("Remove substation");
            string? code = _input.ReadSubstation("Substation to remove");
            if (code == null)
            {
                return;
            }

            if (_input.ReadYesNo($"Remove {code} and all its lines?", false))
            {
                ConsolePresenter.Result(_service.RemoveSubstation(code));
            }
        }

        private void Autocomplete()
        {
            ConsolePresenter.Title("Autocomplete substation names");
            if (!_service.SupportsAutocomplete)
            {
                ConsolePresenter.Warning("Autocomplete is pending: the Trie has not been implemented yet (see TODO [TRIE] markers).");
                return;
            }

            string prefix = _input.ReadText("Prefix");
            Substation[] suggestions = _service.SuggestSubstations(prefix, 15);
            ConsolePresenter.Substations(suggestions);
        }

        // ------------------------------------------------------------------ Lines

        private void RegisterLine()
        {
            ConsolePresenter.Title("Register line");
            string? source = _input.ReadSubstation("Origin substation");
            if (source == null)
            {
                return;
            }

            string? target = _input.ReadSubstation("Destination substation");
            if (target == null)
            {
                return;
            }

            double length = _input.ReadNumber("Length in km", 0.01);
            double cost = _input.ReadNumber("Maintenance cost", 0);
            bool bidirectional = _input.ReadYesNo("Two-way line? (n = one-way, origin -> destination)", true);

            ConsolePresenter.Result(_service.RegisterLine(new PowerLine(source, target, length, cost, bidirectional)));
        }

        /// <summary>
        /// Scenario requirement: verify that two substations belong to the same connected zone
        /// BEFORE approving a new line between them.
        /// </summary>
        private void ProposeLine()
        {
            ConsolePresenter.Title("Propose new line (Union-Find analysis)");
            string? source = _input.ReadSubstation("Origin substation");
            if (source == null)
            {
                return;
            }

            string? target = _input.ReadSubstation("Destination substation");
            if (target == null)
            {
                return;
            }

            LineProposalResult proposal = _service.EvaluateProposedLine(source, target);
            if (!proposal.IsValid)
            {
                ConsolePresenter.Error(proposal.Message);
                return;
            }

            ConsolePresenter.Info(proposal.Message);
            if (proposal.DirectLineExists)
            {
                ConsolePresenter.Warning("Note: a direct line between these substations already exists (in some direction).");
            }

            if (!_input.ReadYesNo("Approve and register this line?", false))
            {
                ConsolePresenter.Info("Line not approved.");
                return;
            }

            double length = _input.ReadNumber("Length in km", 0.01);
            double cost = _input.ReadNumber("Maintenance cost", 0);
            bool bidirectional = _input.ReadYesNo("Two-way line?", true);

            ConsolePresenter.Result(_service.RegisterLine(new PowerLine(source, target, length, cost, bidirectional)));
        }

        private void RemoveLine()
        {
            ConsolePresenter.Title("Remove line");
            string? source = _input.ReadSubstation("Origin substation");
            if (source == null)
            {
                return;
            }

            string? target = _input.ReadSubstation("Destination substation");
            if (target == null)
            {
                return;
            }

            bool both = _input.ReadYesNo("Remove the opposite direction too?", true);
            ConsolePresenter.Result(_service.RemoveLine(source, target, both));
        }

        // ------------------------------------------------------------------ Analysis

        private void FindRoute()
        {
            ConsolePresenter.Title("Optimal route (Hash Table -> Union-Find -> Dijkstra [pending])");
            string? source = _input.ReadSubstation("Origin substation");
            if (source == null)
            {
                return;
            }

            string? target = _input.ReadSubstation("Destination substation");
            if (target == null)
            {
                return;
            }

            string choice = _input.ReadText("Optimize: 1 = lowest maintenance cost, 2 = shortest length", "1");
            EdgeWeightKind criterion = choice == "2" ? EdgeWeightKind.Length : EdgeWeightKind.MaintenanceCost;

            ConsolePresenter.Route(_service.FindOptimalRoute(source, target, criterion));
        }

        private void RunTraversal(bool breadthFirst)
        {
            ConsolePresenter.Title(breadthFirst ? "BFS traversal" : "DFS traversal");
            string? start = _input.ReadSubstation("Start substation");
            if (start == null)
            {
                return;
            }

            if (breadthFirst)
            {
                ConsolePresenter.Traversal("BFS", _service.TraverseBreadthFirst(start));
            }
            else
            {
                ConsolePresenter.Traversal("DFS", _service.TraverseDepthFirst(start));
            }
        }

        private void CheckSameZone()
        {
            ConsolePresenter.Title("Same electrical zone?");
            string? a = _input.ReadSubstation("First substation");
            if (a == null)
            {
                return;
            }

            string? b = _input.ReadSubstation("Second substation");
            if (b == null)
            {
                return;
            }

            ConnectivityResult result = _service.CheckSameZone(a, b);
            if (result.Connected)
            {
                ConsolePresenter.Success(result.Message);
            }
            else
            {
                ConsolePresenter.Warning(result.Message);
            }
        }

        private void ShowReport()
        {
            ConsolePresenter.Title("Grid summary");
            ConsolePresenter.Summary(_service.GetSummary());

            if (!_input.ReadYesNo("Export the full report to a text file?", false))
            {
                return;
            }

            string path = _input.ReadText("Output file", DefaultReportFile);
            try
            {
                string fullPath = GridReportBuilder.Export(_service, path);
                ConsolePresenter.Success($"Report written to {fullPath}");
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                ConsolePresenter.Error($"The report could not be written: {ex.Message}");
            }
        }
    }
}
