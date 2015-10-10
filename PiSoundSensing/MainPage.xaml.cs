using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PiSoundSensing
{
    /// <summary>
    /// Sound Sensing Main User Interface
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //an instance of MCP3008 used to assist in communication with the chip
        private MCP3008 _adc = new MCP3008();
        //a timer that will be used to sample audio in the current environment
        private DispatcherTimer _timer = new DispatcherTimer();
     
        public MainPage()
        {
            this.InitializeComponent();
            
            Setup();
        }

        /// <summary>
        /// Initializes the connection to the MCP3008 chip
        /// </summary>
        private async void Setup()
        {
            //connect to the ADC
            bool isConnected = await _adc.Connect();
            if (!isConnected)
            {
                txtStatus.Text = "There was a problem connecting to the MCP3008, please check your wiring and try again.";
            }
            else
            {
                btnStartSampling.IsEnabled = true;
                btnEndSampling.IsEnabled = true;
                txtStatus.Text = "";

                //retrieve sample from microphone every 100ms
                _timer.Interval = TimeSpan.FromMilliseconds(100);
                _timer.Tick += timer_Tick;

                //set scale of the progress bar, on the scale of 0 to 100: a percentage
                pbVolume.Minimum = 0;
                pbVolume.Maximum = 100;
            }
        }

        /// <summary>
        /// When actively sampling audio, this method will be called every 100ms and sample audio through
        /// the MCP3008 chip from the Electret Microphone Amplifier, retrieve it on a scale of 0-100 
        /// and display the value visually in the Progress Bar
        /// </summary>
        /// <param name="sender">ignore</param>
        /// <param name="e">ignore</param>
        private async void timer_Tick(object sender, object e)
        {
            //retrieve volume in the scale of 0 to 100 
            int volume = await _adc.Sample(50, 0, 100);
            pbVolume.Value = volume;
        }

        /// <summary>
        /// Begins the timer to sample audio every 100ms
        /// </summary>
        /// <param name="sender">Start Button</param>
        /// <param name="e">ignore</param>
        private void btnStartSampling_Click(object sender, RoutedEventArgs e)
        {
            _timer.Start();
        }

        /// <summary>
        /// Stops the timer to suspend sampling audio
        /// </summary>
        /// <param name="sender">Stop Button</param>
        /// <param name="e">ignore</param>
        private void btnEndSampling_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }
    }
}
