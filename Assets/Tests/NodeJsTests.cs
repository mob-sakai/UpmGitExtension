using NUnit.Framework;

namespace Coffee.UpmGitExtension
{
    public class NodeJsTests
    {
        [Test]
        public void GetExeTest()
        {
            Assert.DoesNotThrow(() => NodeJs.GetExe());
        }
    }
}
