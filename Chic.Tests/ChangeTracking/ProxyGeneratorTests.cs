using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
        public void TestNullableNeqIsDirty()
        {
            var proxyType = proxyGenerator.GetProxy<NullableDtTestType>();
            proxyType.TestProperty = DateTime.MinValue;
            proxyType.TestProperty = DateTime.MaxValue;

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.AreEqual(DateTime.MinValue, ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
        }

        [TestMethod]
        public void TestNullableEqIsDirty()
        {
            var proxyType = proxyGenerator.GetProxy<NullableDecTestType>();
            proxyType.TestProperty = 0m;
            proxyType.TestProperty = 1m;

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.AreEqual(0m, ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
        }

        [TestMethod]
        public void TestNullablePrimitiveIsDirty()
        {
            var proxyType = proxyGenerator.GetProxy<NullablePrimTestType>();
            proxyType.TestProperty = 0;
            proxyType.TestProperty = 1;

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.AreEqual(0, ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
        }

        [TestMethod]
        public void TestNullableGenericIntIsDirty()
        {
            var proxyType = proxyGenerator.GetProxy<NullableGenericTestType<int>>();
            proxyType.TestProperty = 0;
            proxyType.TestProperty = 1;

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.AreEqual(0, ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
        }

        [TestMethod]
        public void TestNullableGenericByteIsDirty()
        {
            var proxyType = proxyGenerator.GetProxy<NullableGenericTestType<byte>>();
            proxyType.TestProperty = 0;
            proxyType.TestProperty = 1;

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.AreEqual((byte)0, ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
        }
        
        [TestMethod]
        public void TestNullableGenericDoubleIsDirty()
        {
            var proxyType = proxyGenerator.GetProxy<NullableGenericTestType<double>>();
            proxyType.TestProperty = 0d;
            proxyType.TestProperty = 1d;

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.AreEqual(0d, ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
        }
        
        [TestMethod]
        public void TestNullableGenericDecimalIsDirty()
        {
            var proxyType = proxyGenerator.GetProxy<NullableGenericTestType<decimal>>();
            proxyType.TestProperty = 0;
            proxyType.TestProperty = 1;

            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.AreEqual(0m, ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
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
        public void TestProxyGenerationWithFinalImplementation()
        {
            var proxyType = proxyGenerator.GetProxy<FinalTestType>();
            Assert.IsTrue(proxyType is IProxyChanges);
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

        [TestMethod]
        public void TestMapFromLiveObject()
        {
            var defaultObject = new TestType
            {
                TestProperty = "Test",
                BoxProperty = 5
            };
            var proxyType = proxyGenerator.ProxyLiveObject(defaultObject);
            Assert.IsTrue(proxyType is TestType);
            Assert.IsFalse(((IProxyChanges)proxyType).IsModified);
        }

        [TestMethod]
        public void TestMapFromLiveObjectWithPropertyValues()
        {
            var defaultObject = new TestType
            {
                TestProperty = "Test",
                BoxProperty = 5
            };
            var proxyType = proxyGenerator.ProxyLiveObject(defaultObject);
            Assert.AreEqual(defaultObject.TestProperty, proxyType.TestProperty);
            Assert.AreEqual(defaultObject.BoxProperty, proxyType.BoxProperty);
        }

        [TestMethod]
        public void TestMapFromLiveObjectChangeTracking()
        {
            var defaultObject = new TestType
            {
                TestProperty = "Test",
                BoxProperty = 5
            };
            var proxyType = proxyGenerator.ProxyLiveObject(defaultObject);

            proxyType.TestProperty = "New Value";
            Assert.IsTrue(((IProxyChanges)proxyType).IsModified);
            Assert.AreEqual(defaultObject.TestProperty, ((IProxyChanges)proxyType).OriginalValues[nameof(TestType.TestProperty)]);
            Assert.AreEqual("New Value", proxyType.TestProperty);
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

        public class FinalTestType : ITestType
        {
            public virtual string TestProperty { get; set; }
            public int BoxProperty { get; set; }
        }

        public class NullableDtTestType
        {
            public virtual DateTime? TestProperty { get; set; }
        }
        public class NullableDecTestType
        {
            public virtual decimal? TestProperty { get; set; }
        }
        public class NullablePrimTestType
        {
            public virtual int? TestProperty { get; set; }
        }
        public class NullableGenericTestType<T>
            where T : struct
        {
            public virtual T? TestProperty { get; set; }
        }
    }
}
