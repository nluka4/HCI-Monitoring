using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class EntitiesViewModel : BindableBase
    {
        private readonly MainWindowViewModel mainWindowViewModel;
        private readonly ObservableCollection<DER> allEntities;

        private DER selectedEntity;
        private DER formEntity;

        private string searchText;
        private bool searchByName;
        private bool searchByType;

        private DEREntityType filterType;
        private bool filterLess;
        private bool filterGreater;
        private bool filterEqual;
        private string filterIdText;

        private SavedSearch selectedSavedSearch;

        private bool isDeleteConfirmationVisible;
        private string deleteConfirmationText;

        public EntitiesViewModel(
            MainWindowViewModel mainWindowViewModel,
            ObservableCollection<DER> allEntities,
            ObservableCollection<DEREntityType> availableTypes)
        {
            this.mainWindowViewModel = mainWindowViewModel;
            this.allEntities = allEntities;

            AvailableTypes = availableTypes;
            DisplayedEntities = new ObservableCollection<DER>();
            SavedSearches = new ObservableCollection<SavedSearch>();

            SearchByName = true;
            SearchByType = false;

            AddCommand = new MyICommand(AddEntityFromForm);
            DeleteCommand = new MyICommand(RequestDeleteConfirmation, CanDeleteSelectedEntity);
            ConfirmDeleteCommand = new MyICommand(ConfirmDeleteSelectedEntity);
            CancelDeleteCommand = new MyICommand(CancelDelete);
            ClearFiltersCommand = new MyICommand(ClearFilters);
            SaveCurrentSearchCommand = new MyICommand(SaveCurrentSearch);

            FormEntity = new DER();

            this.allEntities.CollectionChanged += AllEntitiesCollectionChanged;

            foreach (DER entity in this.allEntities)
            {
                entity.PropertyChanged += EntityPropertyChanged;
            }

            RefreshDisplayedEntities();
        }

        public ObservableCollection<DER> DisplayedEntities { get; private set; }

        public ObservableCollection<DEREntityType> AvailableTypes { get; private set; }

        public ObservableCollection<SavedSearch> SavedSearches { get; private set; }

        public MyICommand AddCommand { get; private set; }

        public MyICommand DeleteCommand { get; private set; }

        public MyICommand ConfirmDeleteCommand { get; private set; }

        public MyICommand CancelDeleteCommand { get; private set; }

        public MyICommand ClearFiltersCommand { get; private set; }

        public MyICommand SaveCurrentSearchCommand { get; private set; }

        public DER SelectedEntity
        {
            get { return selectedEntity; }
            set
            {
                if (SetProperty(ref selectedEntity, value))
                {
                    DeleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DER FormEntity
        {
            get { return formEntity; }
            set { SetProperty(ref formEntity, value); }
        }

        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (SetProperty(ref searchText, value))
                {
                    RefreshDisplayedEntities();
                }
            }
        }

        public bool SearchByName
        {
            get { return searchByName; }
            set
            {
                if (SetProperty(ref searchByName, value))
                {
                    if (value)
                    {
                        SearchByType = false;
                    }

                    RefreshDisplayedEntities();
                }
            }
        }

        public bool SearchByType
        {
            get { return searchByType; }
            set
            {
                if (SetProperty(ref searchByType, value))
                {
                    if (value)
                    {
                        SearchByName = false;
                    }

                    RefreshDisplayedEntities();
                }
            }
        }

        public DEREntityType FilterType
        {
            get { return filterType; }
            set
            {
                if (SetProperty(ref filterType, value))
                {
                    RefreshDisplayedEntities();
                }
            }
        }

        public bool FilterLess
        {
            get { return filterLess; }
            set
            {
                if (SetProperty(ref filterLess, value))
                {
                    if (value)
                    {
                        FilterGreater = false;
                        FilterEqual = false;
                    }

                    RefreshDisplayedEntities();
                }
            }
        }

        public bool FilterGreater
        {
            get { return filterGreater; }
            set
            {
                if (SetProperty(ref filterGreater, value))
                {
                    if (value)
                    {
                        FilterLess = false;
                        FilterEqual = false;
                    }

                    RefreshDisplayedEntities();
                }
            }
        }

        public bool FilterEqual
        {
            get { return filterEqual; }
            set
            {
                if (SetProperty(ref filterEqual, value))
                {
                    if (value)
                    {
                        FilterLess = false;
                        FilterGreater = false;
                    }

                    RefreshDisplayedEntities();
                }
            }
        }

        public string FilterIdText
        {
            get { return filterIdText; }
            set
            {
                if (SetProperty(ref filterIdText, value))
                {
                    RefreshDisplayedEntities();
                }
            }
        }

        public SavedSearch SelectedSavedSearch
        {
            get { return selectedSavedSearch; }
            set
            {
                if (SetProperty(ref selectedSavedSearch, value))
                {
                    ApplySavedSearch(value);
                }
            }
        }

        public bool IsDeleteConfirmationVisible
        {
            get { return isDeleteConfirmationVisible; }
            set { SetProperty(ref isDeleteConfirmationVisible, value); }
        }

        public string DeleteConfirmationText
        {
            get { return deleteConfirmationText; }
            set { SetProperty(ref deleteConfirmationText, value); }
        }

        public int TotalCount
        {
            get { return allEntities.Count; }
        }

        public int ValidCount
        {
            get { return allEntities.Count(entity => entity.IsMeasurementValid); }
        }

        public int InvalidCount
        {
            get { return allEntities.Count(entity => !entity.IsMeasurementValid); }
        }

        public int DisplayedCount
        {
            get { return DisplayedEntities.Count; }
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

            RefreshDisplayedEntities();
            RaiseMetricChanges();
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
                RefreshDisplayedEntities();
                RaiseMetricChanges();
            }
        }

        public void RefreshDisplayedEntities()
        {
            DisplayedEntities.Clear();

            foreach (DER entity in allEntities)
            {
                if (PassesSearch(entity) && PassesFilter(entity))
                {
                    DisplayedEntities.Add(entity);
                }
            }

            OnPropertyChanged("DisplayedCount");
            RaiseMetricChanges();
        }

        private bool PassesSearch(DER entity)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            string normalizedSearch = SearchText.Trim().ToLower();

            if (SearchByName)
            {
                return entity.Name != null &&
                       entity.Name.ToLower().Contains(normalizedSearch);
            }

            if (SearchByType)
            {
                return entity.TypeName != null &&
                       entity.TypeName.ToLower().Contains(normalizedSearch);
            }

            return true;
        }

        private bool PassesFilter(DER entity)
        {
            if (FilterType != null && entity.EntityType != FilterType)
            {
                return false;
            }

            if (!FilterLess && !FilterGreater && !FilterEqual)
            {
                return true;
            }

            int idValue;

            if (!int.TryParse(FilterIdText, out idValue))
            {
                return true;
            }

            if (FilterLess)
            {
                return entity.Id < idValue;
            }

            if (FilterGreater)
            {
                return entity.Id > idValue;
            }

            if (FilterEqual)
            {
                return entity.Id == idValue;
            }

            return true;
        }

        private void AddEntityFromForm()
        {
            AddEntityInternal(FormEntity.Id, FormEntity.Name, FormEntity.EntityType, FormEntity.LastMeasurement, true);
        }

        public void AddEntityFromExternalCommand(int id, string name, DEREntityType type, double initialValue)
        {
            AddEntityInternal(id, name, type, initialValue, false);
        }

        private void AddEntityInternal(int id, string name, DEREntityType type, double initialValue, bool useFormValidation)
        {
            DER entityToValidate = new DER(id, name, type, initialValue);
            entityToValidate.SetExistingEntities(allEntities);
            entityToValidate.Validate();

            if (useFormValidation)
            {
                FormEntity.SetExistingEntities(allEntities);
                FormEntity.Validate();

                if (!FormEntity.IsValid)
                {
                    mainWindowViewModel.ShowToast(
                        "Validation failed",
                        "Fix the inline field errors before adding entity.",
                        "ERROR");

                    return;
                }
            }
            else if (!entityToValidate.IsValid)
            {
                mainWindowViewModel.ShowToast(
                    "Validation failed",
                    "Terminal command contains invalid entity data.",
                    "ERROR");

                return;
            }

            DER addedEntity = new DER(id, name, type, initialValue);

            mainWindowViewModel.PushUndo("Add entity #" + addedEntity.Id, delegate
            {
                allEntities.Remove(addedEntity);
            });

            allEntities.Add(addedEntity);

            ResetForm();

            mainWindowViewModel.ShowToast(
                "Entity Added",
                "'" + addedEntity.Name + "' (ID=" + addedEntity.Id + ") added successfully.",
                "SUCCESS");

            mainWindowViewModel.AddTerminalLine("entity added: #" + addedEntity.Id + " " + addedEntity.Name);

            mainWindowViewModel.RestartMeteringSimulator();
        }

        private bool CanDeleteSelectedEntity()
        {
            return SelectedEntity != null;
        }

        private void RequestDeleteConfirmation()
        {
            if (SelectedEntity == null)
            {
                mainWindowViewModel.ShowToast(
                    "Delete unavailable",
                    "Select one entity first.",
                    "INFO");

                return;
            }

            DeleteConfirmationText = "Delete '" + SelectedEntity.Name +
                                     "' (ID=" + SelectedEntity.Id + ")? This action can be undone.";

            IsDeleteConfirmationVisible = true;
        }

        private void ConfirmDeleteSelectedEntity()
        {
            if (SelectedEntity == null)
            {
                IsDeleteConfirmationVisible = false;
                return;
            }

            DeleteEntityInternal(SelectedEntity);
            IsDeleteConfirmationVisible = false;
        }

        public void DeleteEntityByIdFromExternalCommand(int id)
        {
            DER entity = allEntities.FirstOrDefault(item => item.Id == id);

            if (entity == null)
            {
                mainWindowViewModel.ShowToast(
                    "Delete unavailable",
                    "Entity with ID=" + id + " does not exist.",
                    "INFO");

                return;
            }

            DeleteEntityInternal(entity);
        }

        private void DeleteEntityInternal(DER entity)
        {
            int oldIndex = allEntities.IndexOf(entity);
            DER deletedCopy = entity;

            mainWindowViewModel.PushUndo("Delete entity #" + deletedCopy.Id, delegate
            {
                if (oldIndex >= 0 && oldIndex <= allEntities.Count)
                {
                    allEntities.Insert(oldIndex, deletedCopy);
                }
                else
                {
                    allEntities.Add(deletedCopy);
                }
            });

            allEntities.Remove(entity);

            if (SelectedEntity == entity)
            {
                SelectedEntity = null;
            }

            mainWindowViewModel.ShowToast(
                "Entity Removed",
                "'" + entity.Name + "' (ID=" + entity.Id + ") removed successfully.",
                "WARNING");

            mainWindowViewModel.AddTerminalLine("entity deleted: #" + entity.Id + " " + entity.Name);

            mainWindowViewModel.RestartMeteringSimulator();
        }

        private void CancelDelete()
        {
            IsDeleteConfirmationVisible = false;
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SearchByName = true;
            SearchByType = false;

            FilterType = null;
            FilterLess = false;
            FilterGreater = false;
            FilterEqual = false;
            FilterIdText = string.Empty;

            SelectedSavedSearch = null;

            RefreshDisplayedEntities();
            mainWindowViewModel.AddTerminalLine("filters cleared");
        }

        private void SaveCurrentSearch()
        {
            string label = BuildSavedSearchLabel();

            SavedSearch savedSearch = new SavedSearch
            {
                Label = label,
                SearchText = SearchText,
                SearchByName = SearchByName,
                SearchByType = SearchByType,
                FilterType = FilterType,
                FilterOperator = GetCurrentFilterOperator(),
                FilterIdValue = GetFilterIdValue()
            };

            SavedSearches.Add(savedSearch);

            mainWindowViewModel.ShowToast(
                "Search Saved",
                "Saved search: " + label,
                "INFO");

            mainWindowViewModel.AddTerminalLine("saved search: " + label);
        }

        private void ApplySavedSearch(SavedSearch savedSearch)
        {
            if (savedSearch == null)
            {
                return;
            }

            SearchText = savedSearch.SearchText;
            SearchByName = savedSearch.SearchByName;
            SearchByType = savedSearch.SearchByType;
            FilterType = savedSearch.FilterType;

            FilterLess = savedSearch.FilterOperator == "<";
            FilterGreater = savedSearch.FilterOperator == ">";
            FilterEqual = savedSearch.FilterOperator == "=";

            FilterIdText = savedSearch.FilterIdValue.HasValue
                ? savedSearch.FilterIdValue.Value.ToString()
                : string.Empty;

            RefreshDisplayedEntities();
        }

        private string BuildSavedSearchLabel()
        {
            string searchMode = SearchByName ? "name" : "type";
            string typeText = FilterType == null ? "any" : FilterType.TypeName;
            string operatorText = GetCurrentFilterOperator();
            int? idValue = GetFilterIdValue();

            string idText = operatorText == string.Empty || !idValue.HasValue
                ? "none"
                : "ID" + operatorText + idValue.Value;

            return searchMode + "='" + SearchText + "', type=" + typeText + ", " + idText;
        }

        private string GetCurrentFilterOperator()
        {
            if (FilterLess)
            {
                return "<";
            }

            if (FilterGreater)
            {
                return ">";
            }

            if (FilterEqual)
            {
                return "=";
            }

            return string.Empty;
        }

        private int? GetFilterIdValue()
        {
            int value;

            if (int.TryParse(FilterIdText, out value))
            {
                return value;
            }

            return null;
        }

        private void ResetForm()
        {
            FormEntity = new DER();
        }

        private void RaiseMetricChanges()
        {
            OnPropertyChanged("TotalCount");
            OnPropertyChanged("ValidCount");
            OnPropertyChanged("InvalidCount");
            OnPropertyChanged("DisplayedCount");
        }
    }
}