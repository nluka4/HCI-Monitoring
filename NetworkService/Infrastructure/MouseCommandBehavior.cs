using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using NetworkService.Model;

namespace NetworkService.Infrastructure
{
    public class MouseCommandBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                "Command",
                typeof(ICommand),
                typeof(MouseCommandBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                "CommandParameter",
                typeof(object),
                typeof(MouseCommandBehavior),
                new PropertyMetadata(null));

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly DependencyProperty UsePreviewEventProperty =
            DependencyProperty.Register(
                "UsePreviewEvent",
                typeof(bool),
                typeof(MouseCommandBehavior),
                new PropertyMetadata(false));

        public bool UsePreviewEvent
        {
            get { return (bool)GetValue(UsePreviewEventProperty); }
            set { SetValue(UsePreviewEventProperty, value); }
        }

        public static readonly DependencyProperty UseSlotIndexParameterProperty =
            DependencyProperty.Register(
                "UseSlotIndexParameter",
                typeof(bool),
                typeof(MouseCommandBehavior),
                new PropertyMetadata(false));

        public bool UseSlotIndexParameter
        {
            get { return (bool)GetValue(UseSlotIndexParameterProperty); }
            set { SetValue(UseSlotIndexParameterProperty, value); }
        }

        public static readonly DependencyProperty MarkHandledProperty =
            DependencyProperty.Register(
                "MarkHandled",
                typeof(bool),
                typeof(MouseCommandBehavior),
                new PropertyMetadata(false));

        public bool MarkHandled
        {
            get { return (bool)GetValue(MarkHandledProperty); }
            set { SetValue(MarkHandledProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.MouseLeftButtonDown += AssociatedObjectMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObjectPreviewMouseLeftButtonDown;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonDown -= AssociatedObjectMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObjectPreviewMouseLeftButtonDown;

            base.OnDetaching();
        }

        private void AssociatedObjectPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (UsePreviewEvent)
            {
                ExecuteCommand(e);
            }
        }

        private void AssociatedObjectMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!UsePreviewEvent)
            {
                ExecuteCommand(e);
            }
        }

        private void ExecuteCommand(MouseButtonEventArgs e)
        {
            if (Command == null)
            {
                return;
            }

            object parameter = ResolveCommandParameter();

            if (!Command.CanExecute(parameter))
            {
                return;
            }

            Command.Execute(parameter);

            if (MarkHandled)
            {
                e.Handled = true;
            }
        }

        private object ResolveCommandParameter()
        {
            object localValue = ReadLocalValue(CommandParameterProperty);

            if (localValue != DependencyProperty.UnsetValue)
            {
                return CommandParameter;
            }

            if (UseSlotIndexParameter)
            {
                CanvasSlot slot = AssociatedObject.DataContext as CanvasSlot;

                if (slot != null)
                {
                    return slot.Index;
                }
            }

            return AssociatedObject.DataContext;
        }
    }
}