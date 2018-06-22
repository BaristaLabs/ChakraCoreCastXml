using System;
using System.Collections.Generic;
using System.Text;

namespace BaristaLabs.ChakraCoreCastXml.Config
{
    public class TypeMapRule
    {
        public string From
        {
            get;
            set;
        }

        public ParameterDirection? FromDirection
        {
            get;
            set;
        }

        public string To
        {
            get;
            set;
        }

        public ParameterDirection? ToDirection
        {
            get;
            set;
        }
    }
}
