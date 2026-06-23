using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using NetworkService.Model;

namespace NetworkService.Infrastructure
{
    public class DragSourceBehavior : Behavior<FrameworkElement>
    {
        private Point dragStartPoint;

        public static readonly DependencyProperty DragDataProperty =
            DependencyProperty.Register(
                "DragData",
                typeof(object),
                typeof(DragSourceBehavior),
                new PropertyMetadata(null));

        public object DragData
        {
            get { return GetValue(DragDataProperty); }
            set { SetValue(DragDataProperty, value); }
        }

        public static readonly DependencyProperty UseSlotEntityAsDragDataProperty =
            DependencyProperty.Register(
                "UseSlotEntityAsDragData",
                typeof(bool),
                typeof(DragSourceBehavior),
                new PropertyMetadata(false));

        public bool UseSlotEntityAsDragData
        {
            get { return (bool)GetValue(UseSlotEntityAsDragDataProperty); }
            set { SetValue(UseSlotEntityAsDragDataProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObjectPreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove += AssociatedObjectPreviewMouseMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObjectPreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove -= AssociatedObjectPreviewMouseMove;

            base.OnDetaching();
        }

        private void AssociatedObjectPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(null);
        }

        private void AssociatedObjectPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPosition = e.GetPosition(null);

            if (Math.Abs(currentPosition.X - dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            object data = ResolveDragData();

            if (data == null)
            {
                return;
            }

            DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);
        }

        private object ResolveDragData()
        {
            if (UseSlotEntityAsDragData)
            {
                CanvasSlot slot = AssociatedObject.DataContext as CanvasSlot;

                return slot == null ? null : slot.Entity;
            }

            object localValue = ReadLocalValue(DragDataProperty);

            if (localValue != DependencyProperty.UnsetValue)
            {
                return DragData;
            }

            return AssociatedObject.DataContext;
        }
    }
}