using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Media;

namespace ArchiveMaster.Views
{
    [PseudoClasses(pcPressed)]
    public partial class ToolItemBox : UserControl
    {
        private const string pcPressed = ":pressed";

        public static readonly StyledProperty<string> DescriptionProperty =
            AvaloniaProperty.Register<ToolItemBox, string>(nameof(Description));

        public static readonly StyledProperty<string> IconProperty =
            AvaloniaProperty.Register<ToolItemBox, string>(nameof(Icon));

        public static readonly StyledProperty<bool> ShowDescriptionProperty =
            AvaloniaProperty.Register<ToolItemBox, bool>(nameof(ShowDescription), true);

        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<ToolItemBox, string>(nameof(Title));

        public ToolItemBox()
        {
            InitializeComponent();
        }

        public string Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public string Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool ShowDescription
        {
            get => GetValue(ShowDescriptionProperty);
            set => SetValue(ShowDescriptionProperty, value);
        }

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private void InputElement_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            PseudoClasses.Set(pcPressed, true);
        }

        private void InputElement_OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            PseudoClasses.Set(pcPressed, false);
        }

        private void InputElement_OnPointerExited(object sender, PointerEventArgs e)
        {
            PseudoClasses.Set(pcPressed, false);
        }
    }
}