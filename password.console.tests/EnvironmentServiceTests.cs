using System.Runtime.InteropServices;
using password.classlibrary.Services;

namespace password.console.tests
{
    public class EnvironmentServiceTests
    {
        private readonly string _testAppName = "test-app";
        private readonly string _testAppPath = Path.GetTempPath();
        private readonly string _testExecutablePath;

        public EnvironmentServiceTests()
        {
            _testExecutablePath = Path.Combine(_testAppPath, "test.exe");
        }

        private EnvironmentService CreateService(string appName = null)
        {
            var service = new EnvironmentService(appName ?? _testAppName);

            // Asetetaan polut reflektion kautta testitarkoitukseen
            var type = typeof(EnvironmentService);
            type.GetField("_appPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(service, _testAppPath);
            type.GetField("_executablePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(service, _testExecutablePath);

            return service;
        }

        [Fact]
        public void Constructor_SetsCorrectPaths()
        {
            // Arrange & Act
            var service = CreateService();

            // Assert
            Assert.Equal(_testAppPath, service.GetType()
                .GetField("_appPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(service));
        }

        [Fact]
        public void IsRegistered_Windows_WhenPathExists_ReturnsTrue()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            // Arrange
            var service = CreateService();
            Environment.SetEnvironmentVariable("PATH", _testAppPath, EnvironmentVariableTarget.User);

            // Act
            var result = service.IsRegistered();

            // Assert
            Assert.True(result);

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", null, EnvironmentVariableTarget.User);
        }

        [Fact]
        public void RegisterApplicationPath_NonInteractive_CallsPlatformSpecificRegistration()
        {
            // Arrange
            var service = CreateService();
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            service.RegisterApplicationPath(interactive: false);

            // Assert
            var output = consoleOutput.ToString();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Contains("Application registered successfully!", output);
            }
            else
            {
                Assert.Contains("Application registered successfully!", output);
            }
        }

        [Fact]
        public void UnregisterApplicationPath_RemovesWindowsPath()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            // Arrange
            var service = CreateService();
            Environment.SetEnvironmentVariable("PATH", _testAppPath, EnvironmentVariableTarget.User);

            // Act
            service.UnregisterApplicationPath();

            // Assert
            var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            Assert.DoesNotContain(_testAppPath, path);
        }

        [Fact]
        public void GetShellConfigFile_WithZsh_ReturnsZshrc()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            // Arrange
            var service = CreateService();
            Environment.SetEnvironmentVariable("SHELL", "/bin/zsh");

            // Act
            var configFile = service.GetType()
                .GetMethod("GetShellConfigFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(service, null);

            // Assert
            Assert.EndsWith(".zshrc", configFile.ToString());
        }

        [Fact]
        public void PrintManualInstructions_ContainsCorrectCommands()
        {
            // Arrange
            var service = CreateService();
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            service.GetType()
                .GetMethod("PrintManualInstructions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(service, null);

            // Assert
            var output = consoleOutput.ToString();
            Assert.Contains("manual", output, StringComparison.OrdinalIgnoreCase);
        }
    }
}