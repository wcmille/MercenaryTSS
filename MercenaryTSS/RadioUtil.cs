using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace MercenaryTSS
{
    public class RadioUtil
    {
        public HashSet<MyDataBroadcaster> radioBroadcasters = new HashSet<MyDataBroadcaster>();
        //private HashSet<long> tmpEntitiesOnHUD = new HashSet<long>();
        //private List<IMyTerminalBlock> dummyTerminalList = new List<IMyTerminalBlock>(0); // always empty

        public void GetAllRelayedBroadcasters(IMyPlayer lhp)
        {
            var pid = lhp.IdentityId;
            //MyDataReceiver r;
            var charRadio = lhp.Character.Components.Get<MyDataReceiver>();
            this.GetAllRelayedBroadcasters(charRadio, pid, false, null);
        }

        // HACK: copied from MyAntennaSystem.GetAllRelayedBroadcasters(MyDataReceiver receiver, ...)
        private void GetAllRelayedBroadcasters(MyDataReceiver receiver, long identityId, bool mutual, HashSet<MyDataBroadcaster> output = null)
        {
            if (output == null)
            {
                output = radioBroadcasters;
                output.Clear();
            }

            foreach (MyDataBroadcaster current in receiver.BroadcastersInRange)
            {
                if (!output.Contains(current) && !current.Closed && (!mutual || (current.Receiver != null && receiver.Broadcaster != null && current.Receiver.BroadcastersInRange.Contains(receiver.Broadcaster))))
                {
                    output.Add(current);

                    if (current.Receiver != null && current.CanBeUsedByPlayer(identityId))
                    {
                        GetAllRelayedBroadcasters(current.Receiver, identityId, mutual, output);
                    }
                }
            }
        }
    }
}
