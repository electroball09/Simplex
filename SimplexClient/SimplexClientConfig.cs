using System;
using System.Collections.Generic;
using System.Text;
using Flurl;
using Simplex.Transport;

namespace Simplex
{
    public class SimplexClientConfig
    {
        public static SimplexClientConfig Copy(SimplexClientConfig old)
        {
            SimplexClientConfig copy = new SimplexClientConfig()
            {
                Logger = old.Logger,
                Transport = old.Transport,
            };

            return copy;
        }

        public ISimplexLogger Logger { get; set; } = new ConsoleLogger();
        public ISimplexTransport Transport { get; private set; }

        public SimplexClientConfig Copy()
        {
            return Copy(this);
        }

        public T SetTransport<T>() where T : ISimplexTransport, new()
        {
            var t = new T();
            Transport = t;
            return t;
        }
    }
}
