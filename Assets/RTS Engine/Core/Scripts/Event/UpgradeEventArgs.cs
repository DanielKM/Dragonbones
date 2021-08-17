using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Upgrades;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Event
{
    public class UpgradeEventArgs<T> : EventArgs
    {
        public UpgradeElement<T> UpgradeElement { private set; get; }
        public int FactionID { private set; get; }
        public T UpgradedInstance { private set; get; }

        public UpgradeEventArgs(UpgradeElement<T> upgradeElement, int factionID, T upgradedInstance)
        {
            this.UpgradeElement = upgradeElement;
            this.FactionID = factionID;
            this.UpgradedInstance = upgradedInstance;
        }
    }
}
