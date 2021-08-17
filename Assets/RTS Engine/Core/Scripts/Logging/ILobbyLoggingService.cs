using RTSEngine.Entities;
using RTSEngine.Game;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Logging
{
    public interface ILoggingService : IMonoBehaviour
    {
        void Log(string message, IMonoBehaviour source = null, LoggingType type = LoggingType.info);
        void LogError(string message, IMonoBehaviour source = null);
        void LogWarning(string message, IMonoBehaviour source = null);

        bool RequireValid(object target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error);
        bool RequireValid(Object target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error);
        bool RequireValid(IMonoBehaviour target, string message, IMonoBehaviour source, LoggingType type = LoggingType.error);
        bool RequireValid(IEnumerable<IMonoBehaviour> target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error);
        bool RequireValid(IEnumerable<Object> target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error);

        bool RequireTrue(bool condition, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error);
    }
}
