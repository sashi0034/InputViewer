using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using SharpHook;
using SharpHook.Data;
using ModernWpf.Controls;

namespace InputViewer
{
    public partial class MainWindow : Window
    {
        private readonly TaskPoolGlobalHook _hook;
        private readonly ObservableCollection<string> _pressedKeys = new();
        private double _originalWidth;
        private double _originalHeight;
        private ResizeMode _originalResizeMode;

        public MainWindow()
        {
            InitializeComponent();
            
            PressedKeysList.ItemsSource = _pressedKeys;
            
            _hook = new TaskPoolGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;
            _hook.MouseMoved += OnMouseMoved;
            
            _hook.RunAsync();

            _originalWidth = Width;
            _originalHeight = Height;
            _originalResizeMode = ResizeMode;

            Closed += (s, e) => {
                _hook.Dispose();
            };
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            var keyName = GetKeyName(e.Data.KeyCode);
            Dispatcher.Invoke(() =>
            {
                if (!_pressedKeys.Contains(keyName))
                {
                    _pressedKeys.Add(keyName);
                }
            });
        }

        private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
        {
            var keyName = GetKeyName(e.Data.KeyCode);
            Dispatcher.Invoke(() =>
            {
                _pressedKeys.Remove(keyName);
            });
        }

        private void OnMouseMoved(object? sender, MouseHookEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MousePositionText.Text = $"({e.Data.X}, {e.Data.Y})";
            });
        }

        private string GetKeyName(KeyCode keyCode)
        {
            var name = keyCode.ToString();
            // Remove 'Vc' prefix if present from SharpHook names
            if (name.StartsWith("Vc"))
            {
                name = name.Substring(2);
            }
            return name;
        }

        private void AlwaysOnTop_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts)
            {
                Topmost = ts.IsOn;
            }
        }

        private void LockSize_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts)
            {
                if (ts.IsOn)
                {
                    _originalWidth = Width;
                    _originalHeight = Height;
                    _originalResizeMode = ResizeMode;

                    // Set to a small fixed size
                    MinWidth = 200;
                    MinHeight = 150;
                    Width = 250;
                    Height = 200;
                    ResizeMode = ResizeMode.NoResize;
                }
                else
                {
                    ResizeMode = _originalResizeMode;
                    Width = _originalWidth;
                    Height = _originalHeight;
                    MinWidth = 100; // Reset to defaults
                    MinHeight = 100;
                }
            }
        }
    }
}
