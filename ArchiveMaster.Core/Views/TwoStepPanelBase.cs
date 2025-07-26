using Avalonia;
using Avalonia.Controls;
using System;

namespace ArchiveMaster.Views
{
    public class TwoStepPanelBase : PanelBase
    {
        protected override Type StyleKeyOverride { get; } = typeof(TwoStepPanelBase);

        public static readonly StyledProperty<object> ConfigsContentProperty =
            AvaloniaProperty.Register<TwoStepPanelBase, object>(nameof(ConfigsContent));

        public static readonly StyledProperty<object> ExecuteButtonContentProperty
            = AvaloniaProperty.Register<TwoStepPanelBase, object>(nameof(ExecuteButtonContent), "执行");

        public static readonly StyledProperty<object> InitializeButtonContentProperty
            = AvaloniaProperty.Register<TwoStepPanelBase, object>(nameof(InitializeButtonContent), "初始化");

        public static readonly StyledProperty<object> ResetButtonContentProperty
            = AvaloniaProperty.Register<TwoStepPanelBase, object>(nameof(ResetButtonContent), "重置");

        public static readonly StyledProperty<object> ResultsContentProperty =
            AvaloniaProperty.Register<TwoStepPanelBase, object>(nameof(ResultsContent));

        public static readonly StyledProperty<object> StopButtonContentProperty
            = AvaloniaProperty.Register<TwoStepPanelBase, object>(nameof(StopButtonContent), "取消");


        public object ConfigsContent
        {
            get => GetValue(ConfigsContentProperty);
            set => SetValue(ConfigsContentProperty, value);
        }

        public object ExecuteButtonContent
        {
            get => GetValue(ExecuteButtonContentProperty);
            set => SetValue(ExecuteButtonContentProperty, value);
        }

        public object InitializeButtonContent
        {
            get => GetValue(InitializeButtonContentProperty);
            set => SetValue(InitializeButtonContentProperty, value);
        }

        public object ResetButtonContent
        {
            get => GetValue(ResetButtonContentProperty);
            set => SetValue(ResetButtonContentProperty, value);
        }

        public object ResultsContent
        {
            get => GetValue(ResultsContentProperty);
            set => SetValue(ResultsContentProperty, value);
        }

        public object StopButtonContent
        {
            get => GetValue(StopButtonContentProperty);
            set => SetValue(StopButtonContentProperty, value);
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            if (Bounds.Width < 500)
            {
                Resources["ShowSingleLine"] = false;
                Resources["ShowTwoLines"] = true;
            }
            else
            {
                Resources["ShowSingleLine"] = true;
                Resources["ShowTwoLines"] = false;
            }

            if (Bounds.Height < 700)
            {
                Resources["ConfigMaxHeight"] = 200d;
            }
            else
            {
                Resources["ConfigMaxHeight"] = 300d;
            }
        }
    }
}