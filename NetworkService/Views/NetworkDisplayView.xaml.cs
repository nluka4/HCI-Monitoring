using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NetworkService.Model;
using NetworkService.ViewModel;

namespace NetworkService.Views
{
    public partial class NetworkDisplayView : UserControl
    {
        private Point dragStartPoint;
        private NetworkDisplayViewModel subscribedViewModel;

        public NetworkDisplayView()
        {
            InitializeComponent();
            DataContextChanged += NetworkDisplayView_DataContextChanged;
        }

        private NetworkDisplayViewModel ViewModel
        {
            get { return DataContext as NetworkDisplayViewModel; }
        }

        private void NetworkDisplayView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UnsubscribeFromViewModel();

            subscribedViewModel = e.NewValue as NetworkDisplayViewModel;

            if (subscribedViewModel != null)
            {
                subscribedViewModel.Connections.CollectionChanged += Connections_CollectionChanged;

                foreach (CanvasSlot slot in subscribedViewModel.CanvasSlots)
                {
                    slot.PropertyChanged += Slot_PropertyChanged;
                }
            }

            DrawConnectionLinesLater();
        }

        private void UnsubscribeFromViewModel()
        {
            if (subscribedViewModel == null)
            {
                return;
            }

            subscribedViewModel.Connections.CollectionChanged -= Connections_CollectionChanged;

            foreach (CanvasSlot slot in subscribedViewModel.CanvasSlots)
            {
                slot.PropertyChanged -= Slot_PropertyChanged;
            }
        }

        private void Connections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DrawConnectionLinesLater();
        }

        private void Slot_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Entity" ||
                e.PropertyName == "HasInvalidEntity" ||
                e.PropertyName == "IsEmpty")
            {
                DrawConnectionLinesLater();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DrawConnectionLinesLater();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawConnectionLinesLater();
        }

        private void EntitiesTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                dragStartPoint = e.GetPosition(null);
                return;
            }

            Point currentPosition = e.GetPosition(null);

            if (Math.Abs(currentPosition.X - dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

            if (treeViewItem == null)
            {
                return;
            }

            DER entity = treeViewItem.DataContext as DER;

            if (entity == null)
            {
                return;
            }

            DragDrop.DoDragDrop(treeViewItem, entity, DragDropEffects.Move);
        }

        private void EntityCard_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            FrameworkElement element = sender as FrameworkElement;

            if (element == null)
            {
                return;
            }

            CanvasSlot slot = element.DataContext as CanvasSlot;

            if (slot == null || slot.Entity == null)
            {
                return;
            }

            DragDrop.DoDragDrop(element, slot.Entity, DragDropEffects.Move);
        }

        private void SlotBorder_DragOver(object sender, DragEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            CanvasSlot slot = element == null ? null : element.DataContext as CanvasSlot;

            if (slot == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            DER draggedEntity = e.Data.GetData(typeof(DER)) as DER;

            if (draggedEntity == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (slot.Entity == null || slot.Entity == draggedEntity)
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void SlotBorder_Drop(object sender, DragEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            CanvasSlot slot = element == null ? null : element.DataContext as CanvasSlot;

            if (slot == null)
            {
                return;
            }

            DER draggedEntity = e.Data.GetData(typeof(DER)) as DER;

            if (draggedEntity == null)
            {
                return;
            }

            if (ViewModel != null)
            {
                ViewModel.PlaceEntity(draggedEntity, slot.Index);
            }

            DrawConnectionLinesLater();
            e.Handled = true;
        }

        private void SlotBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null || !ViewModel.IsConnectionMode)
            {
                return;
            }

            FrameworkElement element = sender as FrameworkElement;
            CanvasSlot slot = element == null ? null : element.DataContext as CanvasSlot;

            if (slot == null)
            {
                return;
            }

            ViewModel.SelectSlotForConnectionCommand.Execute(slot.Index);
            DrawConnectionLinesLater();

            e.Handled = true;
        }

        private void SlotBorder_Loaded(object sender, RoutedEventArgs e)
        {
            DrawConnectionLinesLater();
        }

        private void DrawConnectionLinesLater()
        {
            Dispatcher.BeginInvoke(new Action(DrawConnectionLines));
        }

        private void DrawConnectionLines()
        {
            if (ViewModel == null || LineCanvas == null)
            {
                return;
            }

            LineCanvas.Children.Clear();

            foreach (Connection connection in ViewModel.Connections)
            {
                Border firstBorder = FindSlotBorder(connection.FirstSlotIndex);
                Border secondBorder = FindSlotBorder(connection.SecondSlotIndex);

                if (firstBorder == null || secondBorder == null)
                {
                    continue;
                }

                Point firstCenter = firstBorder.TranslatePoint(
                    new Point(firstBorder.ActualWidth / 2, firstBorder.ActualHeight / 2),
                    LineCanvas);

                Point secondCenter = secondBorder.TranslatePoint(
                    new Point(secondBorder.ActualWidth / 2, secondBorder.ActualHeight / 2),
                    LineCanvas);

                Line line = new Line
                {
                    X1 = firstCenter.X,
                    Y1 = firstCenter.Y,
                    X2 = secondCenter.X,
                    Y2 = secondCenter.Y,
                    Stroke = (Brush)Application.Current.Resources["UIPrimaryBrush"],
                    StrokeThickness = 3,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                LineCanvas.Children.Add(line);
            }
        }

        private Border FindSlotBorder(int slotIndex)
        {
            if (ViewModel == null)
            {
                return null;
            }

            CanvasSlot slot = ViewModel.CanvasSlots.FirstOrDefault(item => item.Index == slotIndex);

            if (slot == null)
            {
                return null;
            }

            DependencyObject container = SlotsItemsControl.ItemContainerGenerator.ContainerFromItem(slot);

            if (container == null)
            {
                return null;
            }

            return FindVisualChildByTag<Border>(container, slotIndex);
        }

        private T FindVisualChildByTag<T>(DependencyObject parent, int tagValue) where T : FrameworkElement
        {
            if (parent == null)
            {
                return null;
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                T typedChild = child as T;

                if (typedChild != null &&
                    typedChild.Tag != null &&
                    typedChild.Tag.ToString() == tagValue.ToString())
                {
                    return typedChild;
                }

                T result = FindVisualChildByTag<T>(child, tagValue);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                T typedCurrent = current as T;

                if (typedCurrent != null)
                {
                    return typedCurrent;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void ClearSlotButton_Click(object sender, RoutedEventArgs e)
        {
            CanvasSlot slot = FindCanvasSlotFromVisual(sender as DependencyObject);
            NetworkDisplayViewModel viewModel = DataContext as NetworkDisplayViewModel;

            if (slot == null || viewModel == null)
            {
                return;
            }

            viewModel.ClearSlotFromView(slot);

            e.Handled = true;
        }

        private void ClearSlotButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;

            if (element == null)
            {
                return;
            }

            CanvasSlot slot = element.DataContext as CanvasSlot;
            NetworkDisplayViewModel viewModel = DataContext as NetworkDisplayViewModel;

            if (slot == null || viewModel == null)
            {
                return;
            }

            viewModel.ClearSlotFromView(slot);

            DrawConnectionLinesLater();

            e.Handled = true;
        }
        private CanvasSlot FindCanvasSlotFromVisual(DependencyObject source)
        {
            DependencyObject current = source;

            while (current != null)
            {
                FrameworkElement frameworkElement = current as FrameworkElement;

                if (frameworkElement != null)
                {
                    CanvasSlot slot = frameworkElement.DataContext as CanvasSlot;

                    if (slot != null)
                    {
                        return slot;
                    }
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}