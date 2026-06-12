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
        private int? selectedConnectionStartSlotIndex;

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

            for (int i = 0; i < 12; i++)
            {
                CanvasSlots.Add(new CanvasSlot(i));
            }

            foreach (DEREntityType type in availableTypes)
            {
                TreeViewGroups.Add(new DERGroup(type.TypeName));
            }

            ClearSlotCommand = new MyICommand<CanvasSlot>(ClearSlot);
            AutoPlaceAllCommand = new MyICommand(AutoPlaceAll);
            ToggleConnectionModeCommand = new MyICommand(ToggleConnectionMode);
            SelectSlotForConnectionCommand = new MyICommand<int>(SelectSlotForConnection);
            ClearConnectionSelectionCommand = new MyICommand(ClearConnectionSelection);

            this.allEntities.CollectionChanged += AllEntitiesCollectionChanged;

            foreach (DER entity in this.allEntities)
            {
                entity.PropertyChanged += EntityPropertyChanged;
            }

            RefreshState();
        }

        public ObservableCollection<CanvasSlot> CanvasSlots { get; private set; }

        public ObservableCollection<DERGroup> TreeViewGroups { get; private set; }

        public ObservableCollection<Connection> Connections { get; private set; }

        public MyICommand AutoPlaceAllCommand { get; private set; }

        public MyICommand ToggleConnectionModeCommand { get; private set; }

        public MyICommand<int> SelectSlotForConnectionCommand { get; private set; }

        public MyICommand ClearConnectionSelectionCommand { get; private set; }

        public MyICommand<CanvasSlot> ClearSlotCommand { get; private set; }

        public bool IsConnectionMode
        {
            get { return isConnectionMode; }
            set
            {
                if (SetProperty(ref isConnectionMode, value))
                {
                    if (!value)
                    {
                        selectedConnectionStartSlotIndex = null;
                    }

                    OnPropertyChanged("ConnectionModeText");
                    OnPropertyChanged("ConnectionStatusText");
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

                if (!selectedConnectionStartSlotIndex.HasValue)
                {
                    return "Select first occupied slot.";
                }

                return "First slot selected: " + (selectedConnectionStartSlotIndex.Value + 1) + ". Select second slot.";
            }
        }

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
                e.PropertyName == "FormattedMeasurement")
            {
                foreach (CanvasSlot slot in CanvasSlots)
                {
                    if (slot.Entity == sender)
                    {
                        slot.Entity = slot.Entity;
                    }
                }
            }
        }

        public void RefreshState()
        {
            RefreshTreeViewGroups();
            RemoveInvalidConnections();

            OnPropertyChanged("CanvasSlots");
            OnPropertyChanged("TreeViewGroups");
            OnPropertyChanged("Connections");
            OnPropertyChanged("ConnectionStatusText");
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

        public bool PlaceEntity(DER entity, int targetSlotIndex)
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

            if (targetSlot.Entity != null && targetSlot.Entity != entity)
            {
                mainWindowViewModel.ShowToast(
                    "Drop blocked",
                    "Target canvas slot is already occupied.",
                    "INFO");

                return false;
            }

            int oldSlotIndex = FindSlotIndexForEntity(entity);
            DER[] previousSlotEntities = SnapshotSlotEntities();
            List<Connection> previousConnections = SnapshotConnections();

            mainWindowViewModel.PushUndo("Place/move entity on canvas", delegate
            {
                RestoreSlotEntities(previousSlotEntities);
                RestoreConnections(previousConnections);
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

        private void ClearSlot(CanvasSlot slot)
        {
            if (slot == null || slot.Entity == null)
            {
                return;
            }

            DER[] previousSlotEntities = SnapshotSlotEntities();
            List<Connection> previousConnections = SnapshotConnections();

            mainWindowViewModel.PushUndo("Clear entity from canvas", delegate
            {
                RestoreSlotEntities(previousSlotEntities);
                RestoreConnections(previousConnections);
                RefreshTreeViewGroups();
                RefreshState();
            });

            DER removedEntity = slot.Entity;

            slot.Entity = null;

            RemoveInvalidConnections();

            RefreshTreeViewGroups();
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

            mainWindowViewModel.PushUndo("Auto place all", delegate
            {
                RestoreSlotEntities(previousSlotEntities);
                RestoreConnections(previousConnections);
                RefreshState();
            });

            int count = availableEntities.Count < freeSlots.Count ? availableEntities.Count : freeSlots.Count;

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

        private void ToggleConnectionMode()
        {
            IsConnectionMode = !IsConnectionMode;

            mainWindowViewModel.AddTerminalLine(IsConnectionMode
                ? "display: connection mode enabled"
                : "display: connection mode disabled");
        }

        private void ClearConnectionSelection()
        {
            selectedConnectionStartSlotIndex = null;
            OnPropertyChanged("ConnectionStatusText");
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

            if (CanvasSlots[slotIndex].Entity == null)
            {
                mainWindowViewModel.ShowToast(
                    "Connection unavailable",
                    "Select an occupied canvas slot.",
                    "INFO");

                return;
            }

            if (!selectedConnectionStartSlotIndex.HasValue)
            {
                selectedConnectionStartSlotIndex = slotIndex;
                OnPropertyChanged("ConnectionStatusText");
                return;
            }

            int firstSlotIndex = selectedConnectionStartSlotIndex.Value;
            int secondSlotIndex = slotIndex;

            selectedConnectionStartSlotIndex = null;

            if (firstSlotIndex == secondSlotIndex)
            {
                mainWindowViewModel.ShowToast(
                    "Connection blocked",
                    "Select two different occupied slots.",
                    "INFO");

                OnPropertyChanged("ConnectionStatusText");
                return;
            }

            bool duplicate = Connections.Any(connection => connection.Matches(firstSlotIndex, secondSlotIndex));

            if (duplicate)
            {
                mainWindowViewModel.ShowToast(
                    "Duplicate line blocked",
                    "These two entities are already connected.",
                    "INFO");

                OnPropertyChanged("ConnectionStatusText");
                return;
            }

            Connection newConnection = new Connection(firstSlotIndex, secondSlotIndex);

            mainWindowViewModel.PushUndo("Draw connection", delegate
            {
                Connections.Remove(newConnection);
                OnPropertyChanged("Connections");
            });

            Connections.Add(newConnection);

            mainWindowViewModel.ShowToast(
                "Connection drawn",
                "Connection " + (firstSlotIndex + 1) + " ↔ " + (secondSlotIndex + 1) + " created.",
                "SUCCESS");

            OnPropertyChanged("ConnectionStatusText");
        }

        private int FindSlotIndexForEntity(DER entity)
        {
            for (int i = 0; i < CanvasSlots.Count; i++)
            {
                if (CanvasSlots[i].Entity == entity)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RemoveConnectionsForSlot(int slotIndex)
        {
            List<Connection> toRemove = Connections
                .Where(connection => connection.FirstSlotIndex == slotIndex ||
                                     connection.SecondSlotIndex == slotIndex)
                .ToList();

            foreach (Connection connection in toRemove)
            {
                Connections.Remove(connection);
            }
        }

        private void RemoveInvalidConnections()
        {
            List<Connection> toRemove = Connections
                .Where(connection =>
                    connection.FirstSlotIndex < 0 ||
                    connection.SecondSlotIndex < 0 ||
                    connection.FirstSlotIndex >= CanvasSlots.Count ||
                    connection.SecondSlotIndex >= CanvasSlots.Count ||
                    CanvasSlots[connection.FirstSlotIndex].Entity == null ||
                    CanvasSlots[connection.SecondSlotIndex].Entity == null)
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
                    slot.Entity = null;
                    RemoveConnectionsForSlot(slot.Index);
                }
            }
        }

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
                .Select(connection => new Connection(connection.FirstSlotIndex, connection.SecondSlotIndex))
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
    }
}