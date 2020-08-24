﻿using KaguyaProjectV2.KaguyaBot.Core.Global;
using System;

namespace KaguyaProjectV2.KaguyaBot.Core.Exceptions
{
    class KaguyaSupportException : Exception
    {
        /// <summary>
        /// Throws a new <see cref="KaguyaSupportException"/>, displaying a message.
        /// At the end of the message, the Kaguya Support Discord Server is automatically linked
        /// so that the user may find additional support.
        /// </summary>
        public KaguyaSupportException(string message) : base(KaguyaSupportExceptionMessage(message))
        { }
        private static string KaguyaSupportExceptionMessage(string msg)
        {
            return msg;
        }
    }

    class KaguyaPremiumException : Exception
    {
        public KaguyaPremiumException(string message = null) : base(KaguyaPremiumExceptionMessage(message))
        {
        }

        private static string KaguyaPremiumExceptionMessage(string msg = null)
        {
            return $"\nSorry, only servers with an active [Kaguya Premium]({ConfigProperties.KaguyaStore}) subscription are allowed to use this feature.\n\n{msg}";
        }
    }
}
