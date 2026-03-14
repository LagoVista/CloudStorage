using System;

namespace Relational.Tests.Core.Utils
{
    using LagoVista.Core.Models;
    using System.Text;

    public static class TestSeeds
    {
        public static string CreateGuidString(byte value)
        {
            var bytes = new byte[16];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = value;

            return new Guid(bytes).ToString();
        }

    
        public static string CreateNormalizedId32String(byte value)
        {
            var bldr = new StringBuilder();

            for (var i = 0; i < 16; i++)
                bldr.Append(value.ToString("X2"));

            return bldr.ToString();
        }
    }
}