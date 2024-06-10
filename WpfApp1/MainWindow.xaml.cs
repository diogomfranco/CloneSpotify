using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private SpotifyClient _spotify;
        private EmbedIOAuthServer _server;
        private string _selectedDeviceId;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSpotifyClient();
        }

        private async void InitializeSpotifyClient()
        {
            _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await _server.Start();

            _server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await _server.Stop();
                var token = await new OAuthClient().RequestToken(
                    new AuthorizationCodeTokenRequest("your_client_id", "your_client_secret", response.Code, new Uri("http://localhost:5000/callback"))
                );

                var config = SpotifyClientConfig.CreateDefault()
                    .WithToken(token.AccessToken);

                _spotify = new SpotifyClient(config);
            };

            var request = new LoginRequest(_server.BaseUri, "your_client_id", LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.Streaming, Scopes.UserReadPlaybackState, Scopes.UserModifyPlaybackState }
            };
            BrowserUtil.Open(request.ToUri());
        }

        private async void OnPlayClicked(object sender, RoutedEventArgs e)
        {
            if (ResultsList.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string trackUri)
            {
                if (!string.IsNullOrEmpty(_selectedDeviceId))
                {
                    var request = new PlayerResumePlaybackRequest
                    {
                        Uris = new List<string> { trackUri },
                        DeviceId = _selectedDeviceId
                    };
                    await _spotify.Player.ResumePlayback(request);
                }
                else
                {
                    MessageBox.Show("Please select a device first.");
                }
            }
        }

        private async void OnSearchClicked(object sender, RoutedEventArgs e)
        {
            var searchRequest = new SearchRequest(SearchRequest.Types.Track, SearchBox.Text);
            var searchResponse = await _spotify.Search.Item(searchRequest);

            ResultsList.Items.Clear();
            foreach (var item in searchResponse.Tracks.Items)
            {
                ResultsList.Items.Add(new ListBoxItem { Content = $"Track: {item.Name}, Artist: {item.Artists[0].Name}", Tag = item.Uri });
            }
        }

        private async void OnRefreshDevicesClicked(object sender, RoutedEventArgs e)
        {
            var devices = await _spotify.Player.GetAvailableDevices();
            DevicesList.Items.Clear();
            foreach (var device in devices.Devices)
            {
                DevicesList.Items.Add(new ListBoxItem { Content = device.Name, Tag = device.Id });
            }
        }

        private void OnSelectDeviceClicked(object sender, RoutedEventArgs e)
        {
            if (DevicesList.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string deviceId)
            {
                _selectedDeviceId = deviceId;
                MessageBox.Show($"Selected device: {selectedItem.Content}");
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Adicione qualquer lógica adicional para quando o texto mudar, se necessário
        }
    }
}
