using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex.OAuthFormats
{
    public class OAuthExtAccountFormat
    {
        public virtual string UniqueID { get; set; } = "";
        public virtual string EmailAddress { get; set; } = "";
    }

    public class GoogleOAuthExtAccountFormat : OAuthExtAccountFormat
    {
        public class GoogleEmailAddress
        {
            public class GoogleEmailAddressMetadata
            {
                public class GoogleEmailAddressMetadataSource
                {
                    public string type { get; set; }
                    public string id { get; set; }
                }

                public GoogleEmailAddressMetadataSource source { get; set; }
            }

            public GoogleEmailAddressMetadata metadata { get; set; }
            public string value { get; set; }
        }

        public string resourceName { get; set; } = "";
        public string etag { get; set; } = "";
        public GoogleEmailAddress[] emailAddresses { get; set; } = new GoogleEmailAddress[0];

        private string uniqueID;
        public override string UniqueID 
        { 
            get
            {
                if (uniqueID == null)
                    uniqueID = resourceName.Substring(resourceName.IndexOf('/') + 1);
                return uniqueID;
            }
            set
            {
                uniqueID = value;
            }
        }

        private string email;
        public override string EmailAddress 
        { 
            get
            {
                if (email == null)
                    email = emailAddresses[0].value;
                return email;
            }
            set => email = value; 
        }
    }
}
