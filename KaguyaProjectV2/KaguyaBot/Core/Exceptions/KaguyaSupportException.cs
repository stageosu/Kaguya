﻿using KaguyaProjectV2.KaguyaBot.Core.Global;
using System;

namespace KaguyaProjectV2.KaguyaBot.Core.Exceptions
{
    class KaguyaSupportException : Exception
    {
        /// <summary>
        /// Throws a new <see cref="KaguyaSupportException"/>. This basically does nothing, please don't use this lol.
        /// Use the overloads instead :D
        /// </summary>
        public KaguyaSupportException()
        {
        }

        /// <summary>
        /// Throws a new <see cref="KaguyaSupportException"/>, displaying a message.
        /// At the end of the message, the Kaguya Support Discord Server is automatically linked
        /// so that the user may find additional support.
        /// </summary>
        public KaguyaSupportException(string message) : base(message)
        {
            message += $"\n\nPlease join this server for additional assistance: " +
                       $"[[Kaguya Support Discord Server]]({ConfigProperties.KaguyaSupportDiscordServer})";
        }

        /// <summary>
        /// Throws a new <see cref="KaguyaSupportException"/>, displaying a message with an inner exception.
        /// At the end of the message, the Kaguya Support Discord Server is automatically linked
        /// so that the user may find additional support.
        /// </summary>
        public KaguyaSupportException(string message, Exception inner) : base(message, inner)
        {
            message += $"\n\nPlease join this server for additional assistance: " +
                       $"[[Kaguya Support Discord Server]]({ConfigProperties.KaguyaSupportDiscordServer})";
        }
    }
}
