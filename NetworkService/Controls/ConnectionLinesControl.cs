using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetworkService.Model;

namespace NetworkService.Controls
{
    public class ConnectionLinesControl : Canvas
    {
        private INotifyCollectionChanged subscribedConnections;
        private INotifyCollectionChanged subscribedSlots;
        private readonly List<CanvasSlot> subscribedSlotItems;

        public ConnectionLinesControl()
        {
            subscribedSlotItems = new List<CanvasSlot>();
        }

        public static readonly DependencyProperty ConnectionsProperty =
            DependencyProperty.Register(
                "Connections",
                typeof(IEnumerable),
                typeof(ConnectionLinesControl),
                new PropertyMetadata(null, OnConnectionsChanged));

        public IEnumerable Connections
        {
            get { return (IEnumerable)GetValue(ConnectionsProperty); }
            set { SetValue(ConnectionsProperty, value); }
        }

        public static readonly DependencyProperty CanvasSlotsProperty =
            DependencyProperty.Register(
                "CanvasSlots",
                typeof(IEnumerable),
                typeof(ConnectionLinesControl),
                new PropertyMetadata(null, OnCanvasSlotsChanged));

        public IEnumerable CanvasSlots
        {
            get { return (IEnumerable)GetValue(CanvasSlotsProperty); }
            set { SetValue(CanvasSlotsProperty, value); }
        }

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(
                "Rows",
                typeof(int),
                typeof(ConnectionLinesControl),
                new PropertyMetadata(3, OnLayoutPropertyChanged));

        public int Rows
        {
            get { return (int)GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                "Columns",
                typeof(int),
                typeof(ConnectionLinesControl),
                new PropertyMetadata(4, OnLayoutPropertyChanged));

        public int Columns
        {
            get { return (int)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        private static void OnConnectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConnectionLinesControl control = d as ConnectionLinesControl;

            if (control == null)
            {
                return;
            }

            control.UnsubscribeConnections();
            control.SubscribeConnections(e.NewValue as INotifyCollectionChanged);
            control.InvalidateVisual();
        }

        private static void OnCanvasSlotsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConnectionLinesControl control = d as ConnectionLinesControl;

            if (control == null)
            {
                return;
            }

            control.UnsubscribeSlots();
            control.SubscribeSlots(e.NewValue as INotifyCollectionChanged);
            control.SubscribeSlotItems();
            control.InvalidateVisual();
        }

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConnectionLinesControl control = d as ConnectionLinesControl;

            if (control != null)
            {
                control.InvalidateVisual();
            }
        }

        private void SubscribeConnections(INotifyCollectionChanged collection)
        {
            subscribedConnections = collection;

            if (subscribedConnections != null)
            {
                subscribedConnections.CollectionChanged += CollectionChanged;
            }
        }

        private void UnsubscribeConnections()
        {
            if (subscribedConnections != null)
            {
                subscribedConnections.CollectionChanged -= CollectionChanged;
                subscribedConnections = null;
            }
        }

        private void SubscribeSlots(INotifyCollectionChanged collection)
        {
            subscribedSlots = collection;

            if (subscribedSlots != null)
            {
                subscribedSlots.CollectionChanged += SlotsCollectionChanged;
            }
        }

        private void UnsubscribeSlots()
        {
            if (subscribedSlots != null)
            {
                subscribedSlots.CollectionChanged -= SlotsCollectionChanged;
                subscribedSlots = null;
            }

            foreach (CanvasSlot slot in subscribedSlotItems)
            {
                slot.PropertyChanged -= SlotPropertyChanged;
            }

            subscribedSlotItems.Clear();
        }

        private void SubscribeSlotItems()
        {
            foreach (CanvasSlot slot in subscribedSlotItems)
            {
                slot.PropertyChanged -= SlotPropertyChanged;
            }

            subscribedSlotItems.Clear();

            if (CanvasSlots == null)
            {
                return;
            }

            foreach (CanvasSlot slot in CanvasSlots.OfType<CanvasSlot>())
            {
                slot.PropertyChanged += SlotPropertyChanged;
                subscribedSlotItems.Add(slot);
            }
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        private void SlotsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SubscribeSlotItems();
            InvalidateVisual();
        }

        private void SlotPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Entity" ||
                e.PropertyName == "IsEmpty" ||
                e.PropertyName == "HasInvalidEntity")
            {
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (Rows <= 0 || Columns <= 0)
            {
                return;
            }

            List<CanvasSlot> slots = CanvasSlots == null
                ? new List<CanvasSlot>()
                : CanvasSlots.OfType<CanvasSlot>().OrderBy(slot => slot.Index).ToList();

            List<Connection> connections = Connections == null
                ? new List<Connection>()
                : Connections.OfType<Connection>().ToList();

            if (slots.Count == 0 || connections.Count == 0)
            {
                return;
            }

            double cellWidth = ActualWidth / Columns;
            double cellHeight = ActualHeight / Rows;

            Pen pen = new Pen(ResourceBrush("UIPrimaryBrush", Brushes.Black), 3);
            pen.StartLineCap = PenLineCap.Round;
            pen.EndLineCap = PenLineCap.Round;

            foreach (Connection connection in connections)
            {
                int firstSlotIndex = FindSlotIndexByEntityId(slots, connection.FirstEntityId);
                int secondSlotIndex = FindSlotIndexByEntityId(slots, connection.SecondEntityId);

                if (firstSlotIndex < 0 || secondSlotIndex < 0)
                {
                    continue;
                }

                Point first = GetSlotCenter(firstSlotIndex, cellWidth, cellHeight);
                Point second = GetSlotCenter(secondSlotIndex, cellWidth, cellHeight);

                dc.DrawLine(pen, first, second);
            }
        }

        private int FindSlotIndexByEntityId(List<CanvasSlot> slots, int entityId)
        {
            foreach (CanvasSlot slot in slots)
            {
                if (slot.Entity != null && slot.Entity.Id == entityId)
                {
                    return slot.Index;
                }
            }

            return -1;
        }

        private Point GetSlotCenter(int slotIndex, double cellWidth, double cellHeight)
        {
            int row = slotIndex / Columns;
            int column = slotIndex % Columns;

            return new Point(
                column * cellWidth + cellWidth / 2,
                row * cellHeight + cellHeight / 2);
        }

        private Brush ResourceBrush(string key, Brush fallback)
        {
            object value = Application.Current.Resources[key];

            Brush brush = value as Brush;

            return brush ?? fallback;
        }
    }
}