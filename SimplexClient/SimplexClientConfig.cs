using System;
using System.Collections.Generic;
using System.Text;
using Flurl;

namespace Simplex
{
    public class SimplexClientConfig
    {
        public static SimplexClientConfig Default { get; private set; } = new SimplexClientConfig();

        public static SimplexClientConfig Copy(SimplexClientConfig old)
        {
            SimplexClientConfig copy = new SimplexClientConfig()
            {
                Logger = old.Logger,
                APIUrl = old.APIUrl,
                APIStage = old.APIStage,
                APIResource = old.APIResource
            };

            return copy;
        }

        public ISimplexLogger Logger { get; set; } = new ConsoleLogger();

        public string APIUrl { get; set; } = "";
        public string APIStage { get; set; } = "";
        public string APIResource { get; set; } = "";

        public string AssembleURL()
        {
            if (string.IsNullOrEmpty(APIUrl))
            {
                Logger.Error("API Url is empty");
                return null;
            }

            return APIUrl
                .AppendPathSegment(APIStage)
                .AppendPathSegment(APIResource);
        }

        public SimplexClientConfig Copy()
        {
            return Copy(this);  
        }
    }
}
