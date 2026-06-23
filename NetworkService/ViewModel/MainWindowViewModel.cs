using NetworkService.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace NetworkService.ViewModel
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly Stack<UndoItem> undoStack;
        private readonly Stack<string> navigationHistory;

        private readonly List<string> terminalCommandHistory;
        private readonly string[] terminalCommandKeywords;
        private int terminalHistoryIndex;

        private BindableBase currentViewModel;
        private string terminalInput;
        private string terminalOutput;
        private bool isTerminalExpanded;
        private bool isShortcutOverlayVisible;

        private int terminalFocusToken;

        private bool isToastVisible;
        private string toastTitle;
        private string toastMessage;
        private string toastType;
        private readonly DispatcherTimer toastTimer;

        private TcpListener tcpListener;

        public MainWindowViewModel()
        {
            undoStack = new Stack<UndoItem>();
            navigationHistory = new Stack<string>();

            terminalCommandHistory = new List<string>();
            terminalHistoryIndex = -1;

            toastTimer = new DispatcherTimer();
            toastTimer.Interval = TimeSpan.FromSeconds(3);
            toastTimer.Tick += ToastTimer_Tick;

            terminalCommandKeywords = new[]
            {
                "help",
                "list",
                "add",
                "delete",
                "search",
                "filter",
                "clear",
                "undo",
                "nav",
                "view"
            };

            AvailableTypes = new ObservableCollection<DEREntityType>();
            AllEntities = new ObservableCollection<DER>();

            LoadPredefinedTypes();
            LoadInitialEntities();

            EntitiesViewModel = new EntitiesViewModel(this, AllEntities, AvailableTypes);
            NetworkDisplayViewModel = new NetworkDisplayViewModel(this, AllEntities, AvailableTypes);
            GraphViewModel = new GraphViewModel(AllEntities);

            DisplayPlaceholderViewModel = new PlaceholderViewModel(
                "Network Display View",
                "Drag & Drop canvas will be implemented in the next step.");

            GraphPlaceholderViewModel = new PlaceholderViewModel(
                "Measurement Graph View",
                "G2 bar chart will be implemented after the display view.");

            NavCommand = new MyICommand<string>(Navigate);
            BackCommand = new MyICommand(GoBack);
            UndoCommand = new MyICommand(Undo);

            ToggleTerminalCommand = new MyICommand(ToggleTerminal);
            OpenTerminalCommand = new MyICommand(OpenTerminalAndRequestFocus);

            ToggleShortcutOverlayCommand = new MyICommand(ToggleShortcutOverlay);
            ExecuteTerminalCommand = new MyICommand(ExecuteTerminalInput);
            DismissToastCommand = new MyICommand(DismissToast);

            UsePreviousTerminalHistoryCommand = new MyICommand(UsePreviousTerminalAndRequestFocus);
            UseNextTerminalHistoryCommand = new MyICommand(UseNextTerminalAndRequestFocus);
            AutocompleteTerminalInputCommand = new MyICommand(AutocompleteTerminalAndRequestFocus);

            TerminalOutput = "NetworkService terminal ready. Type 'help' for commands.";
            IsTerminalExpanded = true;

            CurrentViewModel = EntitiesViewModel;
            navigationHistory.Push("entities");

            CreateListener();
        }

        public NetworkDisplayViewModel NetworkDisplayViewModel { get; private set; }

        public GraphViewModel GraphViewModel { get; private set; }

        public ObservableCollection<DER> AllEntities { get; private set; }

        public ObservableCollection<DEREntityType> AvailableTypes { get; private set; }

        public EntitiesViewModel EntitiesViewModel { get; private set; }

        public PlaceholderViewModel DisplayPlaceholderViewModel { get; private set; }

        public PlaceholderViewModel GraphPlaceholderViewModel { get; private set; }

        public MyICommand<string> NavCommand { get; private set; }

        public MyICommand BackCommand { get; private set; }

        public MyICommand UndoCommand { get; private set; }

        public MyICommand ToggleTerminalCommand { get; private set; }

        public MyICommand OpenTerminalCommand { get; private set; }

        public MyICommand ToggleShortcutOverlayCommand { get; private set; }

        public MyICommand ExecuteTerminalCommand { get; private set; }

        public MyICommand DismissToastCommand { get; private set; }

        public MyICommand UsePreviousTerminalHistoryCommand { get; private set; }

        public MyICommand UseNextTerminalHistoryCommand { get; private set; }

        public MyICommand AutocompleteTerminalInputCommand { get; private set; }

        public BindableBase CurrentViewModel
        {
            get { return currentViewModel; }
            set { SetProperty(ref currentViewModel, value); }
        }

        public string TerminalInput
        {
            get { return terminalInput; }
            set { SetProperty(ref terminalInput, value); }
        }

        public string TerminalOutput
        {
            get { return terminalOutput; }
            set { SetProperty(ref terminalOutput, value); }
        }

        public bool IsTerminalExpanded
        {
            get { return isTerminalExpanded; }
            set { SetProperty(ref isTerminalExpanded, value); }
        }

        public bool IsShortcutOverlayVisible
        {
            get { return isShortcutOverlayVisible; }
            set { SetProperty(ref isShortcutOverlayVisible, value); }
        }

        public int TerminalFocusToken
        {
            get { return terminalFocusToken; }
            private set { SetProperty(ref terminalFocusToken, value); }
        }

        public bool IsToastVisible
        {
            get { return isToastVisible; }
            set { SetProperty(ref isToastVisible, value); }
        }

        public string ToastTitle
        {
            get { return toastTitle; }
            set { SetProperty(ref toastTitle, value); }
        }

        public string ToastMessage
        {
            get { return toastMessage; }
            set { SetProperty(ref toastMessage, value); }
        }

        public string ToastType
        {
            get { return toastType; }
            set { SetProperty(ref toastType, value); }
        }

        private void LoadPredefinedTypes()
        {
            AvailableTypes.Add(new DEREntityType
            {
                TypeName = "Solar Panel",
            });

            AvailableTypes.Add(new DEREntityType
            {
                TypeName = "Wind Turbine",
            });
        }

        private void ToastTimer_Tick(object sender, EventArgs e)
        {
            toastTimer.Stop();
            IsToastVisible = false;
        }

        private void LoadInitialEntities()
        {
            DEREntityType solarPanelType = AvailableTypes[0];
            DEREntityType windTurbineType = AvailableTypes[1];

            AllEntities.Add(new DER(1, "SP-Alpha", solarPanelType, 2.4));
            AllEntities.Add(new DER(2, "WT-Bravo", windTurbineType, 3.7));
            AllEntities.Add(new DER(3, "SP-Charlie", solarPanelType, 1.8));
            AllEntities.Add(new DER(4, "WT-Delta", windTurbineType, 5.8));
        }

        private void Navigate(string destination)
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                return;
            }

            string normalizedDestination = destination.Trim().ToLower();

            switch (normalizedDestination)
            {
                case "entities":
                    CurrentViewModel = EntitiesViewModel;
                    break;

                case "display":
                    CurrentViewModel = NetworkDisplayViewModel;
                    break;

                case "graph":
                    CurrentViewModel = GraphViewModel;
                    GraphViewModel.RefreshChartData();
                    break;

                default:
                    AddTerminalLine("Unknown view: " + destination);
                    return;
            }

            if (navigationHistory.Count == 0 || navigationHistory.Peek() != normalizedDestination)
            {
                navigationHistory.Push(normalizedDestination);
            }

            AddTerminalLine("view: " + normalizedDestination);
        }

        private void GoBack()
        {
            if (IsShortcutOverlayVisible)
            {
                IsShortcutOverlayVisible = false;
                return;
            }

            if (navigationHistory.Count <= 1)
            {
                return;
            }

            navigationHistory.Pop();
            string previousView = navigationHistory.Peek();

            switch (previousView)
            {
                case "entities":
                    CurrentViewModel = EntitiesViewModel;
                    break;

                case "display":
                    CurrentViewModel = NetworkDisplayViewModel;
                    break;

                case "graph":
                    CurrentViewModel = GraphViewModel;
                    GraphViewModel.RefreshChartData();
                    break;
            }

            AddTerminalLine("back: " + previousView);
        }

        public void PushUndo(string label, Action undoAction)
        {
            if (undoAction == null)
            {
                return;
            }

            undoStack.Push(new UndoItem(label, undoAction));
            AddTerminalLine("undo point saved: " + label);
        }

        private void Undo()
        {
            if (undoStack.Count == 0)
            {
                ShowToast("Undo unavailable", "There is no previous command to undo.", "INFO");
                return;
            }

            UndoItem item = undoStack.Pop();
            item.UndoAction();

            RefreshAllViewModels();

            ShowToast("Undo", "Reverted: " + item.Label, "INFO");
            AddTerminalLine("undo: " + item.Label);
        }

        public void RefreshAllViewModels()
        {
            EntitiesViewModel.RefreshDisplayedEntities();
            NetworkDisplayViewModel.RefreshState();
            GraphViewModel.RefreshChartData();
        }

        public void ShowToast(string title, string message, string type)
        {
            ToastTitle = title;
            ToastMessage = message;
            ToastType = type;

            toastTimer.Stop();

            IsToastVisible = false;

            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                IsToastVisible = true;
                toastTimer.Start();
            }));
        }

        private void DismissToast()
        {
            IsToastVisible = false;
        }

        private void ToggleTerminal()
        {
            IsTerminalExpanded = !IsTerminalExpanded;

            if (IsTerminalExpanded)
            {
                RequestTerminalFocus();
            }
        }

        private void OpenTerminalAndRequestFocus()
        {
            IsTerminalExpanded = true;
            RequestTerminalFocus();
        }

        private void RequestTerminalFocus()
        {
            TerminalFocusToken++;
        }

        private void UsePreviousTerminalAndRequestFocus()
        {
            UsePreviousTerminalCommand();
            RequestTerminalFocus();
        }

        private void UseNextTerminalAndRequestFocus()
        {
            UseNextTerminalCommand();
            RequestTerminalFocus();
        }

        private void AutocompleteTerminalAndRequestFocus()
        {
            AutocompleteTerminalCommand();
            RequestTerminalFocus();
        }

        private void ToggleShortcutOverlay()
        {
            IsShortcutOverlayVisible = !IsShortcutOverlayVisible;
        }

        public void AddTerminalLine(string line)
        {
            TerminalOutput += Environment.NewLine + "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line;
        }

        public DEREntityType ResolveEntityType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Trim().ToLower();

            if (normalized == "solar" ||
                normalized == "solar-panel" ||
                normalized == "solarpanel" ||
                normalized == "panel" ||
                normalized == "sp" ||
                normalized == "solar panel")
            {
                return AvailableTypes[0];
            }

            if (normalized == "wind" ||
                normalized == "wind-turbine" ||
                normalized == "windturbine" ||
                normalized == "turbine" ||
                normalized == "wt" ||
                normalized == "wind turbine")
            {
                return AvailableTypes[1];
            }

            return null;
        }

        private void ExecuteTerminalInput()
        {
            string command = TerminalInput;

            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            command = command.Trim();

            AddTerminalCommandToHistory(command);

            TerminalInput = string.Empty;
            AddTerminalLine("$ " + command);

            ExecuteTerminalCommandText(command);

            RequestTerminalFocus();
        }

        private void AddTerminalCommandToHistory(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            if (terminalCommandHistory.Count == 0 ||
                terminalCommandHistory[terminalCommandHistory.Count - 1] != command)
            {
                terminalCommandHistory.Add(command);
            }

            if (terminalCommandHistory.Count > 20)
            {
                terminalCommandHistory.RemoveAt(0);
            }

            terminalHistoryIndex = terminalCommandHistory.Count;
        }

        public void UsePreviousTerminalCommand()
        {
            if (terminalCommandHistory.Count == 0)
            {
                return;
            }

            if (terminalHistoryIndex <= 0 || terminalHistoryIndex > terminalCommandHistory.Count)
            {
                terminalHistoryIndex = terminalCommandHistory.Count - 1;
            }
            else
            {
                terminalHistoryIndex--;
            }

            TerminalInput = terminalCommandHistory[terminalHistoryIndex];
        }

        public void UseNextTerminalCommand()
        {
            if (terminalCommandHistory.Count == 0)
            {
                return;
            }

            if (terminalHistoryIndex < terminalCommandHistory.Count - 1)
            {
                terminalHistoryIndex++;
                TerminalInput = terminalCommandHistory[terminalHistoryIndex];
                return;
            }

            terminalHistoryIndex = terminalCommandHistory.Count;
            TerminalInput = string.Empty;
        }

        public void AutocompleteTerminalCommand()
        {
            string input = TerminalInput;

            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            string trimmedInput = input.Trim();

            string completed = TryCompleteNavigationCommand(trimmedInput);

            if (completed != null)
            {
                TerminalInput = completed;
                return;
            }

            completed = TryCompleteFilterCommand(trimmedInput);

            if (completed != null)
            {
                TerminalInput = completed;
                return;
            }

            completed = TryCompleteBaseCommand(trimmedInput);

            if (completed != null)
            {
                TerminalInput = completed;
            }
        }

        private string TryCompleteBaseCommand(string input)
        {
            if (input.Contains(" "))
            {
                return null;
            }

            List<string> matches = terminalCommandKeywords
                .Where(command => command.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0] + " ";
            }

            if (matches.Count > 1)
            {
                AddTerminalLine("autocomplete: " + string.Join(", ", matches));
            }

            return null;
        }

        private string TryCompleteNavigationCommand(string input)
        {
            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return null;
            }

            string command = parts[0].ToLower();

            if (command != "nav" && command != "view")
            {
                return null;
            }

            string partialView = parts[1];

            string[] views = { "entities", "display", "graph" };

            List<string> matches = views
                .Where(view => view.StartsWith(partialView, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
            {
                return command + " " + matches[0];
            }

            if (matches.Count > 1)
            {
                AddTerminalLine("autocomplete views: " + string.Join(", ", matches));
            }

            return null;
        }

        private string TryCompleteFilterCommand(string input)
        {
            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
            {
                return null;
            }

            if (parts[0].ToLower() != "filter" || parts[1].ToLower() != "type")
            {
                return null;
            }

            string partialType = parts[2];

            string[] types = { "solar", "wind" };

            List<string> matches = types
                .Where(type => type.StartsWith(partialType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
            {
                return "filter type " + matches[0];
            }

            if (matches.Count > 1)
            {
                AddTerminalLine("autocomplete types: " + string.Join(", ", matches));
            }

            return null;
        }

        private void ExecuteTerminalCommandText(string command)
        {
            string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return;
            }

            string head = parts[0].ToLower();

            if (head == "help")
            {
                PrintHelp();
                return;
            }

            if (head == "clear")
            {
                TerminalOutput = "Terminal cleared.";
                return;
            }

            if (head == "list")
            {
                PrintEntities();
                return;
            }

            if (head == "undo")
            {
                Undo();
                return;
            }

            if ((head == "nav" || head == "view") && parts.Length >= 2)
            {
                Navigate(parts[1]);
                return;
            }

            if (head == "search" && parts.Length >= 2)
            {
                Navigate("entities");
                EntitiesViewModel.SearchByName = true;
                EntitiesViewModel.SearchText = command.Substring(command.IndexOf(' ') + 1);
                AddTerminalLine("search applied: " + EntitiesViewModel.SearchText);
                return;
            }

            if (head == "filter" && parts.Length >= 3 && parts[1].ToLower() == "type")
            {
                DEREntityType type = ResolveEntityType(parts[2]);

                if (type == null)
                {
                    AddTerminalLine("Unknown type. Use solar or wind.");
                    return;
                }

                Navigate("entities");
                EntitiesViewModel.FilterType = type;
                AddTerminalLine("filter applied: type=" + type.TypeName);
                return;
            }

            if (head == "add" && parts.Length >= 4)
            {
                ExecuteTerminalAdd(parts);
                return;
            }

            if (head == "delete" && parts.Length >= 2)
            {
                ExecuteTerminalDelete(parts[1]);
                return;
            }

            AddTerminalLine("Unknown command. Type 'help'.");
        }

        private void ExecuteTerminalAdd(string[] parts)
        {
            int id;

            if (!int.TryParse(parts[1], out id))
            {
                AddTerminalLine("Invalid ID. Example: add 10 SP-Delta solar");
                return;
            }

            string name = parts[2];
            DEREntityType type = ResolveEntityType(parts[3]);

            if (type == null)
            {
                AddTerminalLine("Invalid type. Use solar or wind.");
                return;
            }

            EntitiesViewModel.AddEntityFromExternalCommand(id, name, type, 1.0);
        }

        private void ExecuteTerminalDelete(string idText)
        {
            int id;

            if (!int.TryParse(idText, out id))
            {
                AddTerminalLine("Invalid ID. Example: delete 3");
                return;
            }

            EntitiesViewModel.DeleteEntityByIdFromExternalCommand(id);
        }

        private void PrintHelp()
        {
            AddTerminalLine("Available commands:");
            AddTerminalLine("  help");
            AddTerminalLine("  list");
            AddTerminalLine("  add <id> <name> <solar|wind>");
            AddTerminalLine("  delete <id>");
            AddTerminalLine("  search <term>");
            AddTerminalLine("  filter type <solar|wind>");
            AddTerminalLine("  nav entities|display|graph");
            AddTerminalLine("  undo");
            AddTerminalLine("  clear");
            AddTerminalLine("  Tab       -> autocomplete command");
            AddTerminalLine("  Up/Down   -> command history");
        }

        private void PrintEntities()
        {
            if (AllEntities.Count == 0)
            {
                AddTerminalLine("No entities.");
                return;
            }

            foreach (DER entity in AllEntities)
            {
                AddTerminalLine("#" + entity.Id + " | " + entity.Name + " | " +
                                entity.TypeName + " | " + entity.FormattedMeasurement + " | " +
                                entity.StatusText);
            }
        }

        public void RestartMeteringSimulator()
        {
            try
            {
                string simulatorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MeteringSimulator.exe");

                foreach (Process process in Process.GetProcessesByName("MeteringSimulator"))
                {
                    process.Kill();
                }

                if (File.Exists(simulatorPath))
                {
                    Process.Start(simulatorPath);
                    AddTerminalLine("MeteringSimulator restarted.");
                }
                else
                {
                    AddTerminalLine("MeteringSimulator.exe not found in application directory.");
                }
            }
            catch (Exception ex)
            {
                AddTerminalLine("MeteringSimulator restart failed: " + ex.Message);
            }
        }

        private void CreateListener()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 25675);
                tcpListener.Start();

                Thread listeningThread = new Thread(ListenForSimulatorMessages);
                listeningThread.IsBackground = true;
                listeningThread.Start();

                AddTerminalLine("TCP listener started on port 25675.");
            }
            catch (Exception ex)
            {
                AddTerminalLine("TCP listener failed: " + ex.Message);
            }
        }

        private void ListenForSimulatorMessages()
        {
            while (true)
            {
                try
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();

                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        HandleClient(tcpClient);
                    });
                }
                catch
                {
                    return;
                }
            }
        }

        private void HandleClient(TcpClient tcpClient)
        {
            using (tcpClient)
            {
                NetworkStream stream = tcpClient.GetStream();

                byte[] bytes = new byte[1024];
                int length = stream.Read(bytes, 0, bytes.Length);

                if (length <= 0)
                {
                    return;
                }

                string incoming = Encoding.ASCII.GetString(bytes, 0, length);

                if (incoming.Equals("Need object count"))
                {
                    byte[] data = Encoding.ASCII.GetBytes(AllEntities.Count.ToString());
                    stream.Write(data, 0, data.Length);
                    return;
                }

                Application.Current.Dispatcher.Invoke(delegate
                {
                    ProcessMeasurementMessage(incoming);
                });
            }
        }

        private void ProcessMeasurementMessage(string incoming)
        {
            try
            {
                string[] mainParts = incoming.Split(':');

                if (mainParts.Length != 2)
                {
                    AddTerminalLine("Invalid simulator message: " + incoming);
                    return;
                }

                string entityPart = mainParts[0].Trim();
                string valuePart = mainParts[1].Trim();

                int underscoreIndex = entityPart.IndexOf('_');

                if (underscoreIndex < 0)
                {
                    AddTerminalLine("Invalid entity format: " + incoming);
                    return;
                }

                string indexText = entityPart.Substring(underscoreIndex + 1);
                int receivedIndex;

                if (!int.TryParse(indexText, out receivedIndex))
                {
                    AddTerminalLine("Invalid entity index: " + incoming);
                    return;
                }

                double value;

                if (!double.TryParse(valuePart, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    valuePart = valuePart.Replace(',', '.');

                    if (!double.TryParse(valuePart, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    {
                        AddTerminalLine("Invalid measurement value: " + incoming);
                        return;
                    }
                }

                int entityIndex = ResolveSimulatorEntityIndex(receivedIndex);

                if (entityIndex < 0 || entityIndex >= AllEntities.Count)
                {
                    AddTerminalLine("Simulator entity index out of range: " + incoming);
                    return;
                }

                DER entity = AllEntities[entityIndex];
                entity.LastMeasurement = value;

                WriteToLog(entity, value);

                if (!entity.IsMeasurementValid)
                {
                    ShowToast("Alert", entity.Name + " measured " + value.ToString("0.0") + " MW.", "ERROR");
                }

                RefreshAllViewModels();

                AddTerminalLine("measurement received: #" + entity.Id + " = " +
                                value.ToString("0.0") + " MW -> log.txt");
            }
            catch (Exception ex)
            {
                AddTerminalLine("Measurement processing failed: " + ex.Message);
            }
        }

        private int ResolveSimulatorEntityIndex(int receivedIndex)
        {
            if (receivedIndex >= 0 && receivedIndex < AllEntities.Count)
            {
                return receivedIndex;
            }

            if (receivedIndex >= 1 && receivedIndex <= AllEntities.Count)
            {
                return receivedIndex - 1;
            }

            return -1;
        }

        private void WriteToLog(DER entity, double value)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] Entity '" +
                          entity.Name + "' (ID=" + entity.Id + "): " +
                          value.ToString("0.0", CultureInfo.InvariantCulture) + " MW";

            File.AppendAllText(logPath, line + Environment.NewLine);
        }

        private class UndoItem
        {
            public UndoItem(string label, Action undoAction)
            {
                Label = label;
                UndoAction = undoAction;
            }

            public string Label { get; private set; }

            public Action UndoAction { get; private set; }
        }
    }
}