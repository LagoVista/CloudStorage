using NUnit.Framework;
using System;


namespace Relational.Tests.Core.Utils
{
    /// <summary>
    /// NUnit-friendly wrapper over ReflectionComparer.
    ///
    /// Usage example:
    ///
    /// var mapped = AutoMapper.Create(source, new TargetType(), org, user);
    /// MappingAssert.AllPropertiesMapped(source, mapped, opts => opts.Ignore("CreationDate"));
    /// </summary>
    public static class MappingAssert
    {
        public static void AllPropertiesMapped(object source, object target, Action<ReflectionCompareOptions> configure = null)
        {
            var options =  ReflectionCompareOptions.StrictDefaults();
            configure?.Invoke(options);

            var result = ReflectionComparer.Compare(source, target, options);
            if (!result.Success)
            {
                Assert.Fail("Reflection comparison failed:\n" + result);
            }
        }
    }
}
