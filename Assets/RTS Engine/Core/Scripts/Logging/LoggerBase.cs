using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace RTSEngine.Logging
{
    public class LoggerBase : MonoBehaviour
    {
        public void LogError(string message, IMonoBehaviour source = null) => Log(message, source, LoggingType.error);

        public void LogWarning(string message, IMonoBehaviour source = null) => Log(message, source, LoggingType.warning);

        public void Log(string message, IMonoBehaviour source = null, LoggingType type = LoggingType.info)
        {
            message = source.IsValid()
                ? $"*RTS ENGINE - SOURCE: {source.GetType().Name}* {message}"
                : $"*RTS ENGINE* {message}";

            switch (type)
            {
                case LoggingType.info:
                    Debug.Log(message, source as Object);
                    break;

                case LoggingType.warning:
                    Debug.LogWarning(message, source as Object);
                    break;

                case LoggingType.error:
                    Debug.LogError(message, source as Object);
                    break;
            }
        }

        public bool RequireValid(object target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (!target.IsValid())
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid(Object target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (!target.IsValid())
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid(IMonoBehaviour target, string message, IMonoBehaviour source, LoggingType type = LoggingType.error)
        {
            if (!target.IsValid())
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid(IEnumerable<IMonoBehaviour> target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (!target.All(instance => instance.IsValid()))
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid(IEnumerable<Object> target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (!target.All(instance => instance.IsValid()))
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireTrue(bool condition, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (condition)
                return true;

            Log(message, source, type);
            return false;
        }
    }
}
