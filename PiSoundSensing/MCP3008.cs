using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;

namespace PiSoundSensing
{
    /// <summary>
    /// Helper class that assists in reading from MCP3008 channels
    /// using SPI0, Mode0 and Standard Configuration
    /// assuming 3v3 
    /// </summary>
    public class MCP3008
    {
        private SpiDevice _mcp3008 = null;

        /// <summary>
        /// Connects to the MCP3008 on SPI0
        /// </summary>
        /// <returns>True if successful, False otherwise</returns>
        public async Task<bool> Connect()
        {
            var spiSettings = new SpiConnectionSettings(0);//for spi bus index 0
            spiSettings.ClockFrequency = 3600000; //3.6 MHz
            spiSettings.Mode = SpiMode.Mode0;

            string spiQuery = SpiDevice.GetDeviceSelector("SPI0");
            //using Windows.Devices.Enumeration;
            var deviceInfo = await DeviceInformation.FindAllAsync(spiQuery);
            if (deviceInfo != null && deviceInfo.Count > 0)
            {
                _mcp3008 = await SpiDevice.FromIdAsync(deviceInfo[0].Id, spiSettings);
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Obtains a single sample reading on the specified channel
        /// </summary>
        /// <param name="channel">Channel to read</param>
        /// <returns>Analog Reading Value</returns>
        public int Read(byte channel)
        {
            //From data sheet -- 1 byte selector for channel 0 on the ADC
            //First Byte sends the Start bit for SPI
            //Second Byte is the Configuration Bit
            //1 - single ended 
            //0 - d2
            //0 - d1
            //0 - d0
            //             S321XXXX <-- single-ended channel selection configure bits
            // Channel 0 = 10000000 = 0x80 OR (8+channel) << 4
            int config = (8 + channel) << 4;
            var transmitBuffer = new byte[3] { 1, (byte)config, 0x00 };
            var receiveBuffer = new byte[3];

            _mcp3008.TransferFullDuplex(transmitBuffer, receiveBuffer);
            //first byte returned is 0 (00000000), 
            //second byte returned we are only interested in the last 2 bits 00000011 (mask of &3) 
            //then shift result 8 bits to make room for the data from the 3rd byte (makes 10 bits total)
            //third byte, need all bits, simply add it to the above result 
            var result = ((receiveBuffer[1] & 3) << 8) + receiveBuffer[2];
            return result;
        }

        /// <summary>
        /// Samples analog reads for a specified duration, on a specified channel and returns a value mapped based on the desired scale 
        /// </summary>
        /// <param name="milliseconds">Sample window duration in milliseconds</param>
        /// <param name="adcChannel">MCP3008 channel to perform the reads</param>
        /// <param name="scaleMax">Maximum value of the desired scale (minimum is by default 0)</param>
        /// <returns></returns>
        public async Task<int> Sample(int milliseconds, byte adcChannel, int scaleMax)
        {
            int retvalue = 0;
            await Task.Run(() => {
                var sw = Stopwatch.StartNew();
                var startMs = sw.ElapsedMilliseconds;
                int peakToPeak = 0;
                int readMin = 1024;
                int readMax = 0;

                while ((sw.ElapsedMilliseconds - startMs) < milliseconds)
                {
                    int sample = Read(adcChannel);
                    
                    if (sample > readMax)
                    {
                        readMax = sample;  // save just the max levels
                    }
                    else if (sample < readMin)
                    {
                        readMin = sample;  // save just the min levels
                    }
                }
                
                sw.Stop();
                peakToPeak = readMax - readMin;  // max - min = peak-peak amplitude
                //var volts = (peakToPeak * 3.3) / 1024;  // convert to volts
              
                //return result in the desired scale
                retvalue = Map(peakToPeak, 0, 1024, 0, scaleMax);
            });

            return retvalue;
        }

        /// <summary>
        /// Similar to the Arduino map function, this function adjusts the scale of the value returned
        /// </summary>
        /// <param name="value">value to map to a new scale</param>
        /// <param name="currentScaleLow">low value of the scale from which value was read</param>
        /// <param name="currentScaleHigh">high value of the scale form which value was read</param>
        /// <param name="targetScaleLow">low value of the target scale</param>
        /// <param name="targetScaleHigh">high value of the target scale</param>
        /// <returns>The equivalent of value on the target scale</returns>
        private int Map(int value, int currentScaleLow, int currentScaleHigh, int targetScaleLow, int targetScaleHigh)
        {
            decimal valueD = value;
            decimal currentScaleLowD = currentScaleLow;
            decimal currentScaleHighD = currentScaleHigh;
            decimal targetScaleLowD = targetScaleLow;
            decimal targetScaleHighD = targetScaleHigh;
            decimal result = (valueD - currentScaleLowD) / (currentScaleHighD - currentScaleLowD) * (targetScaleHighD - targetScaleLowD) + targetScaleLowD;

            int retvalue = Convert.ToInt32(Math.Ceiling(result));
            return retvalue;
        }
    }
}
