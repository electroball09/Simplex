using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
                ClientID = old.ClientID,
                GameName = old.GameName,
            };

            return copy;
        }

        public ISimplexLogger Logger { get; set; } = new ConsoleLogger();
        [MinLength(1)]
        public string ClientID { get; set; }
        [MinLength(1)]
        public string GameName { get; set; }

        public SimplexClientConfig Copy()
        {
            return Copy(this);
        }
    }
}
