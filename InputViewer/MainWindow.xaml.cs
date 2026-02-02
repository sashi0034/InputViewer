using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using SharpHook;
using SharpHook.Data;
using ModernWpf.Controls;
using System.Runtime.InteropServices;
using System.Text;

namespace InputViewer
{
    public partial class MainWindow : Window
    {
        private readonly TaskPoolGlobalHook _hook;
        private readonly ObservableCollection<string> _pressedKeys = new();
        private readonly HashSet<MouseButton> _pressedButtons = new();
        private double _originalWidth;
        private double _originalHeight;
        private ResizeMode _originalResizeMode;
        private readonly DispatcherTimer _windowTimer;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public MainWindow()
        {
            InitializeComponent();
            
            UpdatePressedKeysText();

            _hook = new TaskPoolGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;
            _hook.MouseMoved += OnMouseMoved;
            _hook.MousePressed += OnMousePressed;
            _hook.MouseReleased += OnMouseReleased;
            
            _hook.RunAsync();

            _originalWidth = Width;
            _originalHeight = Height;
            _originalResizeMode = ResizeMode;

            _windowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _windowTimer.Tick += (s, e) => UpdateActiveWindow();
            _windowTimer.Start();

            Closed += (s, e) => {
                _hook.Dispose();
                _windowTimer.Stop();
            };
        }

        private void UpdateActiveWindow()
        {
            IntPtr handle = GetForegroundWindow();
            StringBuilder buff = new StringBuilder(256);
            if (GetWindowText(handle, buff, 256) > 0)
            {
                ActiveWindowText.Text = buff.ToString();
            }
            else
            {
                ActiveWindowText.Text = "Unknown Window";
            }
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            var keyName = GetKeyName(e.Data.KeyCode);
            Dispatcher.Invoke(() =>
            {
                if (!_pressedKeys.Contains(keyName))
                {
                    _pressedKeys.Add(keyName);
                    UpdatePressedKeysText();
                }
            });
        }

        private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
        {
            var keyName = GetKeyName(e.Data.KeyCode);
            Dispatcher.Invoke(() =>
            {
                _pressedKeys.Remove(keyName);
                UpdatePressedKeysText();
            });
        }

        private void OnMouseMoved(object? sender, MouseHookEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MousePositionText.Text = $"({e.Data.X}, {e.Data.Y})";
            });
        }

        private void OnMousePressed(object? sender, MouseHookEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _pressedButtons.Add(e.Data.Button);
                UpdateMouseButtonText();
            });
        }

        private void OnMouseReleased(object? sender, MouseHookEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _pressedButtons.Remove(e.Data.Button);
                UpdateMouseButtonText();
            });
        }

        private void UpdateMouseButtonText()
        {
            if (_pressedButtons.Count == 0)
            {
                MouseButtonText.Text = "None";
                MouseButtonText.Opacity = 0.5;
                MouseButtonText.FontStyle = FontStyles.Italic;
            }
            else
            {
                MouseButtonText.Text = string.Join(", ", _pressedButtons);
                MouseButtonText.Opacity = 1.0;
                MouseButtonText.FontStyle = FontStyles.Normal;
            }
        }

        private void UpdatePressedKeysText()
        {
            if (_pressedKeys.Count == 0)
            {
                PressedKeysText.Text = "None";
                PressedKeysText.Opacity = 0.5;
                PressedKeysText.FontStyle = FontStyles.Italic;
            }
            else
            {
                PressedKeysText.Text = string.Join(", ", _pressedKeys);
                PressedKeysText.Opacity = 1.0;
                PressedKeysText.FontStyle = FontStyles.Normal;
            }
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

                    // Set to a small fixed size but tall enough for all content
                    MinWidth = 200;
                    MinHeight = 150;
                    Width = 280;
                    Height = 320;
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

