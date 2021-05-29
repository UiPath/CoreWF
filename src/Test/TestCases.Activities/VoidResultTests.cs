using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestCases.Activities
{
    public class VoidResultTests
    {
        [Fact]
        public async Task ShouldBeEqualToEachOther()
        {
            var vr1 = VoidResult.Value;
            var vr2 = await VoidResult.Task;

            Assert.Equal(vr1, vr2);
        }

        [Fact]
        public void ShouldBeEquitable()
        {
            var dictionary = new Dictionary<VoidResult, string>()
            {
                [new VoidResult()] = "value"
            };

            Assert.Equal("value", dictionary[default]);
        }

        [Fact]
        public void ShouldEqualToString()
        {
            var vr = VoidResult.Value;
            Assert.Equal("()", vr.ToString());
        }

        [Fact]
        public void ShouldCompareToAsZero()
        {
            var vr1 = new VoidResult();
            var vr2 = new VoidResult();

            Assert.Equal(0, vr1.CompareTo(vr2));
        }

        public static object[][] ValueData()
        {
            return new[]
            {
                new object[] {new object(), false},
                new object[] {"", false},
                new object[] {"()", false},
                new object[] {null, false},
                new object[] {new Uri("https://www.google.com"), false},
                new object[] {new VoidResult(), true},
                new object[] {VoidResult.Value, true},
                new object[] {VoidResult.Task.Result, true},
                new object[] {default(VoidResult), true},
            };
        }

        public static object[][] CompareToValueData()
            => ValueData().Select(objects => new[] {objects[0]}).ToArray();

        [Theory]
        [MemberData(nameof(ValueData))]
        public void ShouldBeEqual(object value, bool isEqual)
        {
            var vr1 = VoidResult.Value;

            if (isEqual)
                Assert.True(vr1.Equals(value));
            else
                Assert.False(vr1.Equals(value));
        }

        [Theory]
        [MemberData(nameof(CompareToValueData))]
        public void ShouldCompareToValueAsZero(object value)
        {
            var vr1 = new VoidResult();

            Assert.Equal(0, ((IComparable)vr1).CompareTo(value));
        }
    }
}
