using password.classlibrary.Utils;

namespace password.console.tests
{
    public class HelperTests : IDisposable
    {
        private readonly StringWriter _consoleOutput;
        private readonly StringReader _consoleInput;

        public HelperTests()
        {
            _consoleOutput = new StringWriter();
            _consoleInput = new StringReader("");
            Console.SetOut(_consoleOutput);
            Console.SetIn(_consoleInput);
        }

        public void Dispose()
        {
            _consoleOutput.Dispose();
            _consoleInput.Dispose();
        }

        private static void SetConsoleInput(string input)
        {
            Console.SetIn(new StringReader(input));
        }

        [Fact]
        public async Task ExecuteOperationAsync_ShouldCompleteSuccessfulOperation()
        {
            bool operationCompleted = false;

            await Helpers.ExecuteOperationAsync(async () =>
            {
                await Task.Delay(100);
                operationCompleted = true;
            }, "Testi");

            Assert.True(operationCompleted);
        }

        [Fact]
        public async Task ExecuteOperationAsync_ShouldThrowOnTimeout()
        {
            await Assert.ThrowsAsync<TimeoutException>(() =>
                Helpers.ExecuteOperationAsync(
                    () => Task.Delay(TimeSpan.FromSeconds(31)),
                    "Testi")
            );
        }

        [Fact]
        public async Task ShowSpinnerAsync_ShouldDisplaySpinnerCharacters()
        {
            using var cts = new CancellationTokenSource();

            // Run for exactly 5 iterations
            await Helpers.ShowSpinnerAsync(cts.Token, maxIterations: 5);

            var output = _consoleOutput.ToString();
            Assert.Contains("⣾", output);
        }

        [Theory]
        [InlineData("Test!123", "test123")]
        [InlineData("Hello World", "helloworld")]
        [InlineData("ÄäÖöÅå", "ääööåå")]
        public void CleanName_ShouldSanitizeInput(string input, string expected)
        {
            var result = Helpers.CleanName(input);
            Assert.Equal(expected, result);
        } 

        [Fact]
        public void HandleError_ShouldShowCorrectErrorMessage()
        {
            var ex = new UnauthorizedAccessException("Test error");
            Helpers.HandleError(ex);

            var output = _consoleOutput.ToString();
            Assert.Contains("TEST ERROR: Test error", output);
            Assert.Contains("\u001b[31m", output); // Värikoodi
        }

        [Fact]
        public void PrintSuccess_ShouldOutputGreenText()
        {
            Helpers.PrintSuccess("Onnistui!");
            Assert.Contains("Onnistui", _consoleOutput.ToString());
        }

        [Fact]
        public void DecodeEncodeName_ShouldRoundtripCorrectly()
        {
            var original = "Testi Nimi!@#";
            var encoded = Helpers.EncodeName(original);
            var decoded = Helpers.DecodeName(encoded);

            Assert.True(original == decoded);
        }

        [Fact]
        public void ResetConsole_ShouldDisplayHeader()
        {
            Helpers.ResetConsole();
            Assert.Contains("KEYVAULT", _consoleOutput.ToString());
        }

        [Fact]
        public void ReadInput_ShouldReturnTrimmedInput()
        {
            SetConsoleInput("  testi  \n");
            var result = Helpers.ReadInput("Anna nimi");

            Assert.Equal("testi", result);
            Assert.Contains("Anna nimi:", _consoleOutput.ToString());
        }
    }
}