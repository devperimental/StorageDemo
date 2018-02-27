using System;
using System.Collections.Generic;
using System.Text;

namespace CommonTypes.Behaviours
{
    public interface IAppLogger
    {
        void LogError(Exception ex);
        void LogWarning(string message);
        void LogMessage(string message);
    }
}
