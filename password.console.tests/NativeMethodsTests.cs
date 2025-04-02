using password.classlibrary.Utils;

namespace password.console.tests
{
    public class NativeMethodsTests
    {
        [Fact]
        public void HWND_BROADCAST_ShouldBe0xFFFF()
        {
            // Assert
            Assert.Equal((nint)0xFFFF, NativeMethods.HWND_BROADCAST);
        }

        [Fact]
        public void WM_SETTINGCHANGE_ShouldBe0x001A()
        {
            // Assert
            Assert.Equal(0x001Au, NativeMethods.WM_SETTINGCHANGE);
        }

        [Fact]
        public void SMTO_ABORTIFHUNG_ShouldBe0x0002()
        {
            // Assert
            Assert.Equal(0x0002u, NativeMethods.SMTO_ABORTIFHUNG);
        }
    }
}