using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chic.ChangeTracking
{
    [TestClass]
    public class ProxyGeneratorTest
    {
        ProxyGenerator proxyGenerator;
        public ProxyGeneratorTest()
        {
            proxyGenerator = new ProxyGenerator();
        }

        [TestMethod]
        public void TestProxyGenerationFromClass()
        {
            var proxyType = proxyGenerator.GetProxy<TestType>();

            Assert.IsTrue(proxyType is IProxyChanges);
        }
        [TestMethod]
        public void TestProxyMasksAsOriginalType()
        {
            var proxyType = proxyGenerator.GetProxy<TestType>();

            Assert.IsTrue(proxyType is TestType);
        }

        [TestMethod]
        public void TestIsDirtyAfterFirstSet()
        {
            var proxyType = proxyGenerator.GetProxy<TestType>();
            proxyType.TestProperty = "Hello";

            Assert.IsFalse(((IProxyChanges)proxyType).IsModified);
        }

        [TestMethod]
        public void TestIsDirtyAfterSecondSet()
        {
            var proxyType = proxyGenerator.GetProxy<TestType>();
            proxyType.TestProperty = "Hello";
            proxyType.TestProperty = "Test";

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
        }

        [TestMethod]
        public void TestOriginalValuesExists()
        {
            var proxyType = proxyGenerator.GetProxy<TestType>();
            proxyType.TestProperty = "Hello";

            Assert.IsTrue(((IProxyChanges)proxyType).OriginalValues.ContainsKey(nameof(TestType.TestProperty)));
        }

        [TestMethod]
        public void TestOriginalValuesDefaultStore()
        {
            var proxyType = proxyGenerator.GetProxy<TestType>();
            proxyType.TestProperty = "Hello";

            Assert.AreEqual("Hello", ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
        }

        [TestMethod]
        public void TestOnlyVirtualPropertiesAreTracked()
        {
            var proxyType = proxyGenerator.GetProxy<TestType>();
            proxyType.UntrackableProperty = "Hello";

            Assert.IsFalse(((IProxyChanges)proxyType).IsModified);
            Assert.IsFalse(((IProxyChanges)proxyType).OriginalValues?.ContainsKey(nameof(TestType.TestProperty)) ?? false);
        }

        [TestMethod]
        public void TestOriginalValuesAfterMultipleChanges()
        {
            var proxyType = proxyGenerator.GetProxy<TestType>();
            proxyType.TestProperty = "Hello";
            proxyType.TestProperty = "Another Change";
            proxyType.TestProperty = "And One More";

            Assert.AreEqual("Hello", ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
        }

        [TestMethod]
        public void TestPropertyTrackingWithBoxing()
        {
            var proxyType = proxyGenerator.GetProxy<TestType>();
            proxyType.BoxProperty = 1;
            proxyType.BoxProperty = 2;

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.IsTrue(((IProxyChanges)proxyType).OriginalValues.ContainsKey(nameof(TestType.BoxProperty)));
            Assert.AreEqual(1, ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.BoxProperty)]);
        }

        [TestMethod]
        public void TestProxyGenerationFromInterface()
        {
            var proxyType = proxyGenerator.GetProxy<ITestType>();
            Assert.IsTrue(proxyType is IProxyChanges);
            Assert.IsTrue(proxyType is ITestType);
        }

        [TestMethod]
        public void TestPropertyTrackingFromInterfaceProxy()
        {
            var proxyType = proxyGenerator.GetProxy<ITestType>();
            proxyType.TestProperty = "Test";
            proxyType.TestProperty = "Test2";

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.IsTrue(((IProxyChanges)proxyType).OriginalValues.ContainsKey(nameof(TestType.TestProperty)));
            Assert.AreEqual("Test", ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
        }

        public interface ITestType
        {
            string TestProperty { get; set; }
            int BoxProperty { get; set; }
        }

        public class TestType : ITestType
        {
            public virtual string TestProperty { get; set; }
            public string UntrackableProperty { get; set; }
            public virtual int BoxProperty { get; set; }
        }
    }
}
