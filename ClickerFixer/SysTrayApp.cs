using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClickerFixer.Services;
using ClickerFixer.Utils;
using Mono.Options;
using static ClickerFixer.Services.ClickerInputListener;

namespace ClickerFixer
{
    public class SysTrayApp : Form
    {
        [STAThread]
        public static void Main()
        {
            var p = new OptionSet() {
                { "p|port=", "ProPresenter network port number", (int _port) => AppConfig.port = _port },
                { "r|password=", "ProPresenter network remote control password", (string _password) => AppConfig.password = _password }
            };

            try
            {
                p.Parse(Environment.GetCommandLineArgs());
            }
            catch (OptionException e)
            {
                var stringBuilder = new StringBuilder();

                using (TextWriter writer = new StringWriter(stringBuilder))
                {
                    p.WriteOptionDescriptions(writer);
                }

                MessageBox.Show("Command line arguments failed to parse. Error:\n" + e.Message);

                MessageBox.Show("ClickerFixer command line arguments usage:\n" + stringBuilder.ToString());

                return;
            }

            Application.ApplicationExit += new EventHandler((s, e) => {
                if (_clickerListener != null)
                {
                    _clickerListener.Abort();
                }
            });

            Application.Run(new SysTrayApp());
        }

        private static ClickerInputListener _clickerListener;
        private NotifyIcon _trayIcon;
        private ContextMenu _trayMenu;
        
        public SysTrayApp()
        {
            _trayMenu = new ContextMenu();
            _trayMenu.MenuItems.Add("Exit", OnExit);

            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "ClickerFixer";
            _trayIcon.Icon = Icon.FromHandle((new Bitmap(ClickerFixer.Properties.Resources.tray_idle)).GetHicon());
            _trayIcon.ContextMenu = _trayMenu;
            _trayIcon.Visible = true;

            _clickerListener = new ClickerInputListener();

            Action restoreIdleIcon = () =>
            {
                try
                {
                    _trayIcon.Icon = Icon.FromHandle((new Bitmap(ClickerFixer.Properties.Resources.tray_idle)).GetHicon());
                }
                catch (Exception) { }
            };

            Action debouncedRestoreIdleIcon = restoreIdleIcon.Debounce();

            _clickerListener.KeyPressed += (object sender, KeyPressedHandledEventArgs e) => {
                try {
                    
                    Bitmap bitmap;

                    switch (e.HandledByTargetName) {
                        case "PowerPoint":
                            bitmap = ClickerFixer.Properties.Resources.tray_powerpoint;
                            break;
                        case "ProPresenter":
                            bitmap = ClickerFixer.Properties.Resources.tray_propresenter;
                            break;
                        default:
                            bitmap = ClickerFixer.Properties.Resources.tray_default;
                            break;
                    }

                    _trayIcon.Icon = Icon.FromHandle(bitmap.GetHicon());

                    Task.Delay(100).ContinueWith((task) => {
                        debouncedRestoreIdleIcon();
                    });
                }
                catch (Exception) { }
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}
