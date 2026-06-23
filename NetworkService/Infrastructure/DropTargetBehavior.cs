using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using NetworkService.Model;

namespace NetworkService.Infrastructure
{
    public class DropTargetBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                "Command",
                typeof(ICommand),
                typeof(DropTargetBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.AllowDrop = true;
            AssociatedObject.DragOver += AssociatedObjectDragOver;
            AssociatedObject.Drop += AssociatedObjectDrop;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.DragOver -= AssociatedObjectDragOver;
            AssociatedObject.Drop -= AssociatedObjectDrop;

            base.OnDetaching();
        }

        private void AssociatedObjectDragOver(object sender, DragEventArgs e)
        {
            EntityDropRequest request = CreateRequest(e);

            if (request != null && Command != null && Command.CanExecute(request))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void AssociatedObjectDrop(object sender, DragEventArgs e)
        {
            EntityDropRequest request = CreateRequest(e);

            if (request != null && Command != null && Command.CanExecute(request))
            {
                Command.Execute(request);
            }

            e.Handled = true;
        }

        private EntityDropRequest CreateRequest(DragEventArgs e)
        {
            DER entity = e.Data.GetData(typeof(DER)) as DER;

            if (entity == null)
            {
                return null;
            }

            CanvasSlot slot = AssociatedObject.DataContext as CanvasSlot;

            if (slot == null)
            {
                return null;
            }

            return new EntityDropRequest
            {
                Entity = entity,
                TargetSlotIndex = slot.Index
            };
        }
    }
}