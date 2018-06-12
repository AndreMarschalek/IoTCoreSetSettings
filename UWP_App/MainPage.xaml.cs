using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace cyberh0me.net.IoTCoreSetSettings
{
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;

        private string _Folder { get { return ApplicationData.Current.LocalFolder.Path; } }
        private const string _BootFile = "boot.cfg";
        private const string _ConfigFile = "update.ps1";

        private string _BootFilePath { get { return Path.Combine(_Folder, _BootFile); } }
        private string _ConfigFilePath { get { return Path.Combine(_Folder, _ConfigFile); } }
        private ObservableCollection<Data.GroupInfoList> _Configurations = new ObservableCollection<Data.GroupInfoList>();
        private string _hostname;

        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Data.GroupInfoList info;
            Data.Configuration config;

            string content = await GetFileContent();
            if (content == string.Empty)
                return;

            string[] lines = content.Split('\n');

            _hostname = lines[0].Substring(0, lines[0].Length - 1);
            hostname.Text = _hostname;

            for (int i = 2; i < lines.Length; i++)
            {
                if (lines[i] != string.Empty)
                {
                    string line = lines[i];

                    if (line[0] == 'C')
                    {

                        config = GetConfiguration(lines, ref i);
                        if (config != null)
                        {
                            info = new Data.GroupInfoList();
                            info.Add(config);
                            info.Interface = config.Interface;
                            _Configurations.Add(info);
                        }
                    }
                }
            }
            ConfigurationCVS.Source = _Configurations;
        }

        private void ButtonSave_Tapped(object sender, TappedRoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Data.Configuration configuration in xListView.Items)
            {
                // IP
                if (!configuration.DHCP1)
                {
                    if (!CheckIPConfiguration(configuration))
                        return;

                    sb.Append($"netsh interface ipv4 set address \"{configuration.Interface}\" static {configuration.IPAddress} {configuration.Subnet}");

                    if (configuration.Gateway == string.Empty)
                        sb.Append("\r\n");
                    else
                        sb.Append($" {configuration.Gateway} 1\r\n");
                }
                else
                    sb.Append($"netsh interface ipv4 set address \"{configuration.Interface}\" dhcp\r\n");

                // DNS
                sb.Append($"netsh interface ipv4 delete dnsservers \"{configuration.Interface}\" all\r\n");
                if (!configuration.DHCP2)
                {
                    if (!CheckDHCPConfiguration(configuration))
                        return;

                    sb.Append($"netsh interface ipv4 set dnsservers \"{configuration.Interface}\" static {configuration.DNS1} primary\r\n");

                    if(configuration.DNS2 != string.Empty)
                        sb.Append($"netsh interface ipv4 add dnsservers \"{configuration.Interface}\" {configuration.DNS2} index=2\r\n");
                }
                else
                    sb.Append($"netsh interface ipv4 set dnsservers \"{configuration.Interface}\" dhcp\r\n");

                sb.Append("ipconfig /flushdns\r\n");
            }

            // hostname
            if (hostname.Text == string.Empty || hostname.Text.Length > 15)
            {
                NotifyUser($"hostname {hostname.Text} not valid", NotifyType.ErrorMessage);
                return;
            }

            if (!hostname.Text.Equals(_hostname))
            {
                sb.Append($"setcomputername {hostname.Text}\r\n");
                sb.Append("shutdown /r /t 0\r\n");
            }

            if (sb.Length > 0)
                WriteFile(sb.ToString());
        }

        /// <summary>
        /// Used to write the desired new Configuration to config file
        /// </summary>
        /// <param name="content">new configuration as string</param>
        private async void WriteFile(string content)
        {
            try
            {
                // ensure file does not exist
                IReadOnlyList<StorageFile> files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
                foreach (StorageFile item in files)
                {
                    if (item.Name.Equals(_ConfigFile))
                    {
                        await item.DeleteAsync();
                        break;
                    }
                }

                // create file
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(_Folder);
                await folder.CreateFileAsync(_ConfigFile);

                // write file
                StorageFile file = await StorageFile.GetFileFromPathAsync(_ConfigFilePath);
                await FileIO.WriteTextAsync(file, content);

                NotifyUser("Success", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                NotifyUser($"Write file error: {ex.Message}", NotifyType.ErrorMessage);
            }
        }

        /// <summary>
        /// Used to parse configuration part of bootconfig file
        /// </summary>
        /// <param name="lines">file content as lines as string</param>
        /// <param name="i">current line position</param>
        /// <returns>configuration as Data.Configuration</returns>
        private Data.Configuration GetConfiguration(string[] lines, ref int i)
        {
            try
            {
                Data.Configuration config;

                string line = lines[i];
                switch (line[29])
                {
                    case 'E':
                        config = new Data.Configuration("Ethernet");
                        break;

                    case 'W':
                        config = new Data.Configuration("Wi-Fi");
                        break;

                    default:
                        return null;
                }

                line = lines[i += 1];
                config.DHCP1 = line[42] == 'Y' ? true : false;

                line = lines[i += 1];
                config.IPAddress = line.Substring(42, line.Length - 43);

                line = lines[i += 1];
                config.Subnet = line.Substring(62, line.Length - 64);

                line = lines[i += 1];
                config.Gateway = line.Substring(42, line.Length - 43);

                line = lines[i += 3];
                config.DHCP2 = line[4] == 'D' ? true : false;
                config.DNS1 = line.Substring(42, line.Length - 43);

                line = lines[i += 1];
                if (line[4] == ' ')
                    config.DNS2 = line.Substring(42, line.Length - 43);
                else
                    config.DNS2 = string.Empty;

                return config;
            }
            catch (Exception ex)
            {
                NotifyUser($"Parsing error: {ex.Message}", NotifyType.ErrorMessage);
            }
            return null;
        }

        /// <summary>
        /// Used to read the content of the bootconfig file
        /// </summary>
        /// <returns>content as string</returns>
        private async Task<string> GetFileContent()
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(_BootFilePath);
                if (file == null)
                {
                    NotifyUser($"FileNotFound: {file.Path}", NotifyType.ErrorMessage);
                    return string.Empty;
                }
                return await FileIO.ReadTextAsync(file);
            }
            catch (Exception ex)
            {
                NotifyUser($"Get file error: {ex.Message}", NotifyType.ErrorMessage);
            }
            return string.Empty;
        }

        /// <summary>
        /// Used to perform base ipaddress validity checks
        /// </summary>
        /// <param name="config">interface configuration</param>
        /// <returns>true if ok otherwise false</returns>
        private bool CheckIPConfiguration(Data.Configuration config)
        {
            IPAddress address;

            if (config.IPAddress == string.Empty || !IPAddress.TryParse(config.IPAddress, out address))
            {
                NotifyUser($"IPAddress {config.IPAddress} from {config.Interface} not valid", NotifyType.ErrorMessage);
                return false;
            }

            if (config.IPAddress == string.Empty || !IPAddress.TryParse(config.Subnet, out address))
            {
                NotifyUser($"Subnet {config.Subnet} from {config.Interface} not valid", NotifyType.ErrorMessage);
                return false;
            }

            if (config.Gateway != string.Empty && !IPAddress.TryParse(config.Gateway, out address))
            {
                NotifyUser($"IPAddress {config.Gateway} from {config.Interface} not valid", NotifyType.ErrorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Used to perform base ipaddress validity checks
        /// </summary>
        /// <param name="config">interface configuration</param>
        /// <returns>true if ok otherwise false</returns>
        private bool CheckDHCPConfiguration(Data.Configuration config)
        {
            IPAddress address;

            if (config.DNS1 != string.Empty && !IPAddress.TryParse(config.DNS1, out address))
            {
                NotifyUser($"IPAddress {config.DNS1} from {config.Interface} not valid", NotifyType.ErrorMessage);
                return false;
            }

            if (config.DNS2 != string.Empty && !IPAddress.TryParse(config.DNS2, out address))
            {
                NotifyUser($"IPAddress {config.DNS2} from {config.Interface} not valid", NotifyType.ErrorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Used to display messages to the user
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        private void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        }
    }
}
