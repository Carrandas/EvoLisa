using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GABase;
using GAgeneratedImagese.Tools;

namespace GABaseTests.SelectorTests
{
    [TestClass]
    public class SelectorTests
    {
        [TestInitialize]
        public void Setup()
        {
            Settings.ScreenWidth = 10;
            Settings.ScreenHeight = 10;
        }

        [TestMethod]
        public void Constructor_WithFastBitmap_InitializesCorrectly()
        {
            using (var bmp = new Bitmap(10, 10))
            {
                var fb = new FastBitmap(bmp);
                var selector = new Selector(fb);
                Assert.IsNotNull(selector);
            }
        }
    }
}