using System;

using RTSEngine.Attack;

namespace RTSEngine.Event
{
    public class AttackLaunchEventArgs : EventArgs
    {
        public AttackObjectLaunchLog LaunchLog { private set; get; }

        public AttackLaunchEventArgs(AttackObjectLaunchLog launchLog)
        {
            LaunchLog = launchLog;
        }
    }
}
