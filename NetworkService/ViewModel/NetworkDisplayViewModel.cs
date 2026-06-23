using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class NetworkDisplayViewModel : BindableBase
    {
        private readonly MainWindowViewModel mainWindowViewModel;
        private readonly ObservableCollection<DER> allEntities;

        private bool isConnectionMode;

        // Čuvamo ID entiteta, ne slot index.
        // Tako veze i selection imaju smisla i kada pomeraš entitete.
        private int? selectedConnectionStartEntityId;

        public NetworkDisplayViewModel(
            MainWindowViewModel mainWindowViewModel,
            ObservableCollection<DER> allEntities,
            ObservableCollection<DEREntityType> availableTypes)
        {
            this.mainWindowViewModel = mainWindowViewModel;
            this.allEntities = allEntities;

            CanvasSlots = new ObservableCollection<CanvasSlot>();
            TreeViewGroups = new ObservableCollection<DERGroup>();
            Connections = new ObservableCollection<Connection>();

            InitializeSlots();
            InitializeTreeViewGroups(availableTypes);
            InitializeCommands();

            this.allEntities.CollectionChanged += AllEntitiesCollectionChanged;

            foreach (DER entity in this.allEntities)
            {
                entity.PropertyChanged += EntityPropertyChanged;
            }

            RefreshState();
        }

        // =========================
        // Collections
        // =========================

        public ObservableCollection<CanvasSlot> CanvasSlots { get; private set; }

        public ObservableCollection<DERGroup> TreeViewGroups { get; private set; }

        public ObservableCollection<Connection> Connections { get; private set; }

        // =========================
        // Commands
        // =========================

        public MyICommand AutoPlaceAllCommand { get; private set; }

        public MyICommand ToggleConnectionModeCommand { get; private set; }

        public MyICommand ClearConnectionSelectionCommand { get; private set; }

        public MyICommand ClearCanvasCommand { get; private set; }

        public MyICommand<int> SelectSlotForConnectionCommand { get; private set; }

        public MyICommand<CanvasSlot> ClearSlotCommand { get; private set; }

        public MyICommand<EntityDropRequest> PlaceEntityCommand { get; private set; }

        // =========================
        // State
        // =========================

        public bool IsConnectionMode
        {
            get { return isConnectionMode; }
            set
            {
                if (SetProperty(ref isConnectionMode, value))
                {
                    if (!value)
                    {
                        selectedConnectionStartEntityId = null;
                    }

                    RaiseConnectionUiChanges();
                }
            }
        }

        public string ConnectionModeText
        {
            get { return IsConnectionMode ? "Connect: ON" : "Connect: OFF"; }
        }

        public string ConnectionStatusText
        {
            get
            {
                if (!IsConnectionMode)
                {
                    return "Connection mode is disabled.";
                }

                if (!selectedConnectionStartEntityId.HasValue)
                {
                    return "Select first occupied slot.";
                }

                DER selectedEntity = FindEntityById(selectedConnectionStartEntityId.Value);
                int selectedSlotIndex = FindSlotIndexForEntityId(selectedConnectionStartEntityId.Value);

                if (selectedEntity == null || selectedSlotIndex < 0)
                {
                    return "Select first occupied slot.";
                }

                return "First slot selected: " + (selectedSlotIndex + 1) +
                       " (#" + selectedEntity.Id + " " + selectedEntity.Name + "). Select second slot.";
            }
        }

        // =========================
        // Initialization
        // =========================

        private void InitializeSlots()
        {
            for (int i = 0; i < 12; i++)
            {
                CanvasSlots.Add(new CanvasSlot(i));
            }
        }

        private void InitializeTreeViewGroups(ObservableCollection<DEREntityType> availableTypes)
        {
            foreach (DEREntityType type in availableTypes)
            {
                TreeViewGroups.Add(new DERGroup(type.TypeName));
            }
        }

        private void InitializeCommands()
        {
            AutoPlaceAllCommand = new MyICommand(AutoPlaceAll);
            ToggleConnectionModeCommand = new MyICommand(ToggleConnectionMode);

            ClearConnectionSelectionCommand = new MyICommand(ClearConnectionSelection);
            ClearCanvasCommand = new MyICommand(ClearCanvas);

            SelectSlotForConnectionCommand = new MyICommand<int>(SelectSlotForConnection);
            ClearSlotCommand = new MyICommand<CanvasSlot>(ClearSlot);

            PlaceEntityCommand = new MyICommand<EntityDropRequest>(
                PlaceEntityFromDrop,
                CanPlaceEntityFromDrop);
        }

        // =========================
        // External refresh
        // =========================

        public void RefreshState()
        {
            RefreshTreeViewGroups();
            RemoveInvalidConnections();
            RemoveInvalidConnectionSelection();

            OnPropertyChanged("CanvasSlots");
            OnPropertyChanged("TreeViewGroups");
            OnPropertyChanged("Connections");
            RaiseConnectionUiChanges();
        }

        private void RefreshTreeViewGroups()
        {
            foreach (DERGroup group in TreeViewGroups)
            {
                group.Entities.Clear();
            }

            List<int> placedIds = CanvasSlots
                .Where(slot => slot.Entity != null)
                .Select(slot => slot.Entity.Id)
                .ToList();

            foreach (DER entity in allEntities)
            {
                if (placedIds.Contains(entity.Id))
                {
                    continue;
                }

                DERGroup group = TreeViewGroups.FirstOrDefault(item => item.GroupName == entity.TypeName);

                if (group != null)
                {
                    group.Entities.Add(entity);
                }
            }
        }

        // =========================
        // Entity collection events
        // =========================

        private void AllEntitiesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (DER entity in e.NewItems)
                {
                    entity.PropertyChanged += EntityPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (DER entity in e.OldItems)
                {
                    entity.PropertyChanged -= EntityPropertyChanged;
                }
            }

            RemoveDeletedEntitiesFromCanvas();
            RefreshState();
        }

        private void EntityPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LastMeasurement" ||
                e.PropertyName == "IsMeasurementValid" ||
                e.PropertyName == "StatusText" ||
                e.PropertyName == "FormattedMeasurement" ||
                e.PropertyName == "Name" ||
                e.PropertyName == "EntityType" ||
                e.PropertyName == "TypeName")
            {
                RefreshTreeViewGroups();

                foreach (CanvasSlot slot in CanvasSlots)
                {
                    if (slot.Entity == sender)
                    {
                        slot.Entity = slot.Entity;
                    }
                }

                RaiseConnectionUiChanges();
            }
        }

        // =========================
        // Place / Move entity
        // =========================

        private void PlaceEntityFromDrop(EntityDropRequest request)
        {
            if (request == null)
            {
                return;
            }

            PlaceEntity(request.Entity, request.TargetSlotIndex);
        }

        private bool CanPlaceEntityFromDrop(EntityDropRequest request)
        {
            if (request == null)
            {
                return false;
            }

            return CanPlaceEntity(request.Entity, request.TargetSlotIndex);
        }

        public bool CanPlaceEntity(DER entity, int targetSlotIndex)
        {
            if (entity == null)
            {
                return false;
            }

            if (targetSlotIndex < 0 || targetSlotIndex >= CanvasSlots.Count)
            {
                return false;
            }

            CanvasSlot targetSlot = CanvasSlots[targetSlotIndex];

            return targetSlot.Entity == null || targetSlot.Entity == entity;
        }

        public bool PlaceEntity(DER entity, int targetSlotIndex)
        {
            if (!CanPlaceEntity(entity, targetSlotIndex))
            {
                mainWindowViewModel.ShowToast(
                    "Drop blocked",
                    "Target canvas slot is already occupied.",
                    "INFO");

                return false;
            }

            CanvasSlot targetSlot = CanvasSlots[targetSlotIndex];

            int oldSlotIndex = FindSlotIndexForEntity(entity);

            DER[] previousSlotEntities = SnapshotSlotEntities();
            List<Connection> previousConnections = SnapshotConnections();
            int? previousSelectedConnectionStartEntityId = selectedConnectionStartEntityId;

            mainWindowViewModel.PushUndo("Place/move entity on canvas", delegate
            {
                RestoreSlotEntities(previousSlotEntities);
                RestoreConnections(previousConnections);
                selectedConnectionStartEntityId = previousSelectedConnectionStartEntityId;
                RefreshState();
            });

            if (oldSlotIndex >= 0)
            {
                CanvasSlots[oldSlotIndex].Entity = null;
            }

            targetSlot.Entity = entity;

            RefreshState();

            mainWindowViewModel.AddTerminalLine(
                "display: placed #" + entity.Id + " on slot " + (targetSlotIndex + 1));

            return true;
        }

        // =========================
        // Clear one slot
        // =========================

        private void ClearSlot(CanvasSlot slot)
        {
            if (slot == null || slot.Entity == null)
            {
                return;
            }

            DER removedEntity = slot.Entity;

            DER[] previousSlotEntities = SnapshotSlotEntities();
            List<Connection> previousConnections = SnapshotConnections();
            int? previousSelectedConnectionStartEntityId = selectedConnectionStartEntityId;

            mainWindowViewModel.PushUndo("Clear entity from canvas", delegate
            {
                RestoreSlotEntities(previousSlotEntities);
                RestoreConnections(previousConnections);
                selectedConnectionStartEntityId = previousSelectedConnectionStartEntityId;
                RefreshState();
            });

            slot.Entity = null;

            RemoveConnectionsForEntity(removedEntity.Id);

            if (selectedConnectionStartEntityId == removedEntity.Id)
            {
                selectedConnectionStartEntityId = null;
            }

            RefreshState();

            mainWindowViewModel.ShowToast(
                "Canvas slot cleared",
                removedEntity.Name + " removed from slot " + (slot.Index + 1) + ".",
                "INFO");
        }

        public void ClearSlotFromView(CanvasSlot slot)
        {
            ClearSlot(slot);
        }

        // =========================
        // Clear whole canvas
        // =========================

        private void ClearCanvas()
        {
            bool hasEntitiesOnCanvas = CanvasSlots.Any(slot => slot.Entity != null);
            bool hasConnections = Connections.Count > 0;

            if (!hasEntitiesOnCanvas && !hasConnections)
            {
                selectedConnectionStartEntityId = null;
                RaiseConnectionUiChanges();

                mainWindowViewModel.ShowToast(
                    "Canvas already clear",
                    "There are no entities or connections on canvas.",
                    "INFO");

                mainWindowViewModel.AddTerminalLine("display: canvas already clear");

                return;
            }

            DER[] previousSlotEntities = SnapshotSlotEntities();
            List<Connection> previousConnections = SnapshotConnections();
            int? previousSelectedConnectionStartEntityId = selectedConnectionStartEntityId;

            mainWindowViewModel.PushUndo("Clear canvas", delegate
            {
                RestoreSlotEntities(previousSlotEntities);
                RestoreConnections(previousConnections);
                selectedConnectionStartEntityId = previousSelectedConnectionStartEntityId;
                RefreshState();
            });

            foreach (CanvasSlot slot in CanvasSlots)
            {
                slot.Entity = null;
            }

            Connections.Clear();
            selectedConnectionStartEntityId = null;

            RefreshState();

            mainWindowViewModel.ShowToast(
                "Canvas cleared",
                "All entities and connections were removed from canvas.",
                "INFO");

            mainWindowViewModel.AddTerminalLine("display: canvas cleared");
        }

        // =========================
        // Auto place
        // =========================

        private void AutoPlaceAll()
        {
            List<DER> availableEntities = GetAvailableEntities().ToList();
            List<CanvasSlot> freeSlots = CanvasSlots.Where(slot => slot.Entity == null).ToList();

            if (availableEntities.Count == 0)
            {
                mainWindowViewModel.ShowToast(
                    "Auto place unavailable",
                    "There are no available entities in TreeView.",
                    "INFO");

                return;
            }

            if (freeSlots.Count == 0)
            {
                mainWindowViewModel.ShowToast(
                    "Auto place unavailable",
                    "There are no free canvas slots.",
                    "INFO");

                return;
            }

            DER[] previousSlotEntities = SnapshotSlotEntities();
            List<Connection> previousConnections = SnapshotConnections();
            int? previousSelectedConnectionStartEntityId = selectedConnectionStartEntityId;

            mainWindowViewModel.PushUndo("Auto place all", delegate
            {
                RestoreSlotEntities(previousSlotEntities);
                RestoreConnections(previousConnections);
                selectedConnectionStartEntityId = previousSelectedConnectionStartEntityId;
                RefreshState();
            });

            int count = availableEntities.Count < freeSlots.Count
                ? availableEntities.Count
                : freeSlots.Count;

            for (int i = 0; i < count; i++)
            {
                freeSlots[i].Entity = availableEntities[i];
            }

            RefreshState();

            mainWindowViewModel.ShowToast(
                "Auto Place All",
                count + " entities placed on free canvas slots.",
                "SUCCESS");
        }

        private IEnumerable<DER> GetAvailableEntities()
        {
            List<int> placedIds = CanvasSlots
                .Where(slot => slot.Entity != null)
                .Select(slot => slot.Entity.Id)
                .ToList();

            return allEntities.Where(entity => !placedIds.Contains(entity.Id));
        }

        // =========================
        // Connection mode / selection
        // =========================

        private void ToggleConnectionMode()
        {
            IsConnectionMode = !IsConnectionMode;

            mainWindowViewModel.AddTerminalLine(IsConnectionMode
                ? "display: connection mode enabled"
                : "display: connection mode disabled");
        }

        private void ClearConnectionSelection()
        {
            selectedConnectionStartEntityId = null;

            RaiseConnectionUiChanges();

            mainWindowViewModel.AddTerminalLine("display: connection selection cleared");
        }

        private void SelectSlotForConnection(int slotIndex)
        {
            if (!IsConnectionMode)
            {
                return;
            }

            if (slotIndex < 0 || slotIndex >= CanvasSlots.Count)
            {
                return;
            }

            CanvasSlot selectedSlot = CanvasSlots[slotIndex];

            if (selectedSlot.Entity == null)
            {
                mainWindowViewModel.ShowToast(
                    "Connection unavailable",
                    "Select an occupied canvas slot.",
                    "INFO");

                return;
            }

            DER selectedEntity = selectedSlot.Entity;

            if (!selectedConnectionStartEntityId.HasValue)
            {
                selectedConnectionStartEntityId = selectedEntity.Id;
                RaiseConnectionUiChanges();
                return;
            }

            int firstEntityId = selectedConnectionStartEntityId.Value;
            int secondEntityId = selectedEntity.Id;

            selectedConnectionStartEntityId = null;

            if (firstEntityId == secondEntityId)
            {
                mainWindowViewModel.ShowToast(
                    "Connection blocked",
                    "Select two different occupied slots.",
                    "INFO");

                RaiseConnectionUiChanges();
                return;
            }

            DER firstEntity = FindEntityById(firstEntityId);
            DER secondEntity = FindEntityById(secondEntityId);

            if (firstEntity == null || secondEntity == null)
            {
                mainWindowViewModel.ShowToast(
                    "Connection unavailable",
                    "Both entities must exist.",
                    "INFO");

                RaiseConnectionUiChanges();
                return;
            }

            bool duplicate = Connections.Any(connection =>
                connection.MatchesEntities(firstEntity.Id, secondEntity.Id));

            if (duplicate)
            {
                mainWindowViewModel.ShowToast(
                    "Duplicate line blocked",
                    "These two entities are already connected.",
                    "INFO");

                RaiseConnectionUiChanges();
                return;
            }

            AddConnection(firstEntity, secondEntity);
        }

        private void AddConnection(DER firstEntity, DER secondEntity)
        {
            Connection newConnection = new Connection(firstEntity.Id, secondEntity.Id);

            mainWindowViewModel.PushUndo("Draw connection", delegate
            {
                Connections.Remove(newConnection);
                RaiseConnectionUiChanges();
            });

            Connections.Add(newConnection);

            mainWindowViewModel.ShowToast(
                "Connection drawn",
                "Connection #" + firstEntity.Id + " ↔ #" + secondEntity.Id + " created.",
                "SUCCESS");

            RaiseConnectionUiChanges();
        }

        // =========================
        // Connection cleanup
        // =========================

        private void RemoveConnectionsForEntity(int entityId)
        {
            List<Connection> toRemove = Connections
                .Where(connection => connection.ContainsEntity(entityId))
                .ToList();

            foreach (Connection connection in toRemove)
            {
                Connections.Remove(connection);
            }
        }

        private void RemoveInvalidConnections()
        {
            List<int> existingEntityIds = allEntities
                .Select(entity => entity.Id)
                .ToList();

            List<Connection> toRemove = Connections
                .Where(connection =>
                    !existingEntityIds.Contains(connection.FirstEntityId) ||
                    !existingEntityIds.Contains(connection.SecondEntityId))
                .ToList();

            foreach (Connection connection in toRemove)
            {
                Connections.Remove(connection);
            }
        }

        private void RemoveDeletedEntitiesFromCanvas()
        {
            List<int> existingIds = allEntities.Select(entity => entity.Id).ToList();

            foreach (CanvasSlot slot in CanvasSlots)
            {
                if (slot.Entity != null && !existingIds.Contains(slot.Entity.Id))
                {
                    int removedEntityId = slot.Entity.Id;

                    slot.Entity = null;
                    RemoveConnectionsForEntity(removedEntityId);

                    if (selectedConnectionStartEntityId == removedEntityId)
                    {
                        selectedConnectionStartEntityId = null;
                    }
                }
            }
        }

        private void RemoveInvalidConnectionSelection()
        {
            if (!selectedConnectionStartEntityId.HasValue)
            {
                return;
            }

            int selectedEntityId = selectedConnectionStartEntityId.Value;

            bool entityStillPlaced = CanvasSlots.Any(slot =>
                slot.Entity != null && slot.Entity.Id == selectedEntityId);

            if (!entityStillPlaced)
            {
                selectedConnectionStartEntityId = null;
            }
        }

        // =========================
        // Find helpers
        // =========================

        private int FindSlotIndexForEntity(DER entity)
        {
            if (entity == null)
            {
                return -1;
            }

            return FindSlotIndexForEntityId(entity.Id);
        }

        private int FindSlotIndexForEntityId(int entityId)
        {
            for (int i = 0; i < CanvasSlots.Count; i++)
            {
                if (CanvasSlots[i].Entity != null && CanvasSlots[i].Entity.Id == entityId)
                {
                    return i;
                }
            }

            return -1;
        }

        private DER FindEntityById(int entityId)
        {
            return allEntities.FirstOrDefault(entity => entity.Id == entityId);
        }

        // =========================
        // Undo snapshots
        // =========================

        private DER[] SnapshotSlotEntities()
        {
            DER[] snapshot = new DER[CanvasSlots.Count];

            for (int i = 0; i < CanvasSlots.Count; i++)
            {
                snapshot[i] = CanvasSlots[i].Entity;
            }

            return snapshot;
        }

        private List<Connection> SnapshotConnections()
        {
            return Connections
                .Select(connection => new Connection(connection.FirstEntityId, connection.SecondEntityId))
                .ToList();
        }

        private void RestoreSlotEntities(DER[] snapshot)
        {
            for (int i = 0; i < CanvasSlots.Count && i < snapshot.Length; i++)
            {
                CanvasSlots[i].Entity = snapshot[i];
            }
        }

        private void RestoreConnections(List<Connection> snapshot)
        {
            Connections.Clear();

            foreach (Connection connection in snapshot)
            {
                Connections.Add(connection);
            }
        }

        // =========================
        // UI notification helpers
        // =========================

        private void RaiseConnectionUiChanges()
        {
            OnPropertyChanged("ConnectionModeText");
            OnPropertyChanged("ConnectionStatusText");
            OnPropertyChanged("Connections");
        }
    }
}