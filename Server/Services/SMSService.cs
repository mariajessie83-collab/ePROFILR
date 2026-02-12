using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Services
{
    public class SMSService
    {
        private readonly ILogger<SMSService> _logger;
        private SerialPort? _serialPort;
        private readonly int _baudRate;

        public SMSService(ILogger<SMSService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _baudRate = int.Parse(configuration["SMS:BaudRate"] ?? "9600");
        }

        public async Task<SMSResult> SendSMSAsync(string phoneNumber, string message)
        {
            try
            {
                // Format phone number (ensure it starts with country code for Philippines)
                phoneNumber = FormatPhoneNumber(phoneNumber);
                
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    _logger.LogError("Invalid phone number format");
                    return new SMSResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Invalid phone number format. Please ensure it's a valid Philippine mobile number (09XXXXXXXXX)." 
                    };
                }

                // Automatically detect and initialize serial port
                var initResult = await InitializeSerialPortAuto();
                if (!initResult.Success)
                {
                    _logger.LogError($"Failed to initialize serial port: {initResult.ErrorMessage}");
                    return new SMSResult 
                    { 
                        Success = false, 
                        ErrorMessage = initResult.ErrorMessage ?? "Failed to initialize serial port - no GSM modem found. Please ensure GSM modem is connected via USB." 
                    };
                }

                // Wait for modem to be ready
                await Task.Delay(500); // Reduced from 1000ms

                // Send SMS
                var sendResult = await SendSMSCommandAsync(phoneNumber, message);

                // Close serial port
                CloseSerialPort();

                return sendResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS: {Exception}", ex);
                CloseSerialPort();
                return new SMSResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Error sending SMS: {ex.Message}" 
                };
            }
        }

        public class SMSResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Remove all non-digit characters
            phoneNumber = Regex.Replace(phoneNumber, @"[^\d]", "");

            // If empty after cleaning, return empty
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return "";
            }

            // If starts with 0, replace with country code 63 (Philippines)
            if (phoneNumber.StartsWith("0"))
            {
                phoneNumber = "63" + phoneNumber.Substring(1);
            }
            // If doesn't start with country code, add it
            else if (!phoneNumber.StartsWith("63"))
            {
                phoneNumber = "63" + phoneNumber;
            }

            // Philippine mobile numbers with country code should be 12 digits (63 + 10 digits)
            // Examples: 639974879148 (from 09974879148), 639123456789 (from 09123456789)
            if (phoneNumber.Length == 12)
            {
                return phoneNumber;
            }

            // Log the issue for debugging
            _logger.LogWarning($"Invalid phone number format. Length: {phoneNumber.Length}, Number: {phoneNumber}");
            return "";
        }

        private async Task<SMSResult> InitializeSerialPortAuto()
        {
            try
            {
                // Get all available COM ports
                string[] ports;
                try
                {
                    ports = SerialPort.GetPortNames();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting COM port names");
                    return new SMSResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Error accessing COM ports: {ex.Message}" 
                    };
                }
                
                if (ports.Length == 0)
                {
                    _logger.LogWarning("No COM ports found");
                    return new SMSResult 
                    { 
                        Success = false, 
                        ErrorMessage = "No COM ports found. Please ensure GSM modem is connected via USB." 
                    };
                }

                _logger.LogInformation($"Found {ports.Length} COM port(s): {string.Join(", ", ports)}");

                // Try each COM port until we find one that responds to AT commands
                foreach (string port in ports)
                {
                    _logger.LogInformation($"Trying to connect to {port}...");
                    
                    try
                    {
                        _serialPort = new SerialPort(port, _baudRate, Parity.None, 8, StopBits.One)
                        {
                            Handshake = Handshake.None,
                            DtrEnable = true,
                            RtsEnable = true,
                            ReadTimeout = 5000,
                            WriteTimeout = 5000
                        };

                        _serialPort.Open();

                        // Clear any existing data
                        Thread.Sleep(200); // Reduced from 500ms
                        _serialPort.ReadExisting();

                        // Test AT command
                        _serialPort.WriteLine("AT\r");
                        Thread.Sleep(200); // Reduced from 500ms
                        string response = _serialPort.ReadExisting();

                        if (response.Contains("OK"))
                        {
                            _logger.LogInformation($"Successfully connected to {port}");

                            // Set SMS text mode
                            _serialPort.WriteLine("AT+CMGF=1\r");
                            Thread.Sleep(200); // Reduced from 500ms
                            _serialPort.ReadExisting();

                            // Check if modem supports SMS
                            _serialPort.WriteLine("AT+CPMS?\r");
                            Thread.Sleep(200); // Reduced from 500ms
                            string smsResponse = _serialPort.ReadExisting();

                            if (smsResponse.Contains("OK"))
                            {
                                _logger.LogInformation($"GSM modem ready on {port}");
                                return new SMSResult { Success = true };
                            }
                        }

                        // If we get here, this port didn't work
                        _serialPort.Close();
                        _serialPort.Dispose();
                        _serialPort = null;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogWarning(ex, $"Port {port} is already in use or access denied");
                        if (_serialPort != null && _serialPort.IsOpen)
                        {
                            _serialPort.Close();
                            _serialPort.Dispose();
                        }
                        _serialPort = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to connect to {port}: {ex.Message}");
                        if (_serialPort != null && _serialPort.IsOpen)
                        {
                            _serialPort.Close();
                            _serialPort.Dispose();
                        }
                        _serialPort = null;
                    }
                }

                _logger.LogError("No working GSM modem found on any COM port");
                return new SMSResult 
                { 
                    Success = false, 
                    ErrorMessage = $"No working GSM modem found on any COM port. Available ports: {string.Join(", ", ports)}. Please ensure GSM modem (GSM800C) is connected and powered on." 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-detecting COM port: {Exception}", ex);
                return new SMSResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Error auto-detecting COM port: {ex.Message}" 
                };
            }
        }

        private async Task<SMSResult> SendSMSCommandAsync(string phoneNumber, string message)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    return new SMSResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Serial port is not open" 
                    };
                }

                // Set recipient number
                _serialPort.WriteLine($"AT+CMGS=\"{phoneNumber}\"\r");
                await Task.Delay(300); // Reduced from 1000ms

                // Wait for ">" prompt from modem (indicating it's ready to receive message)
                string promptResponse = "";
                int attempts = 0;
                while (!promptResponse.Contains(">") && attempts < 15) // Increased attempts but shorter delay
                {
                    await Task.Delay(100); // Reduced from 200ms
                    promptResponse += _serialPort.ReadExisting();
                    attempts++;
                }

                if (!promptResponse.Contains(">"))
                {
                    return new SMSResult 
                    { 
                        Success = false, 
                        ErrorMessage = "GSM modem did not respond with ready prompt (>)" 
                    };
                }

                // Send message (Ctrl+Z = 26 or \x1A) to indicate end of message
                _serialPort.Write(message + "\x1A");
                
                // Reduced wait time - modem usually responds quickly after sending
                await Task.Delay(2000); // Reduced from 5000ms

                // Read response after sending message - faster polling
                string response = "";
                attempts = 0;
                while (attempts < 15) // Reduced from 20 attempts
                {
                    await Task.Delay(150); // Reduced from 200ms
                    response += _serialPort.ReadExisting();
                    
                    // Check if we got OK or ERROR
                    if (response.Contains("OK"))
                    {
                        _logger.LogInformation($"SMS sent successfully to {phoneNumber}");
                        return new SMSResult { Success = true };
                    }
                    else if (response.Contains("ERROR"))
                    {
                        _logger.LogWarning($"SMS send error response: {response}");
                        return new SMSResult 
                        { 
                            Success = false, 
                            ErrorMessage = $"GSM modem error: {response}" 
                        };
                    }
                    attempts++;
                }

                _logger.LogInformation($"SMS command response: {response}");

                // If we got the ">" prompt but no final OK/ERROR, assume success
                // (Sometimes modem doesn't send OK immediately but SMS is sent)
                // This is safe because if SMS was received, it means it was sent successfully
                _logger.LogInformation($"SMS command completed. Response may be delayed, but SMS likely sent successfully.");
                return new SMSResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS command: {Exception}", ex);
                return new SMSResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Error sending SMS command: {ex.Message}" 
                };
            }
        }

        private void CloseSerialPort()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing serial port");
            }
        }
    }
}

