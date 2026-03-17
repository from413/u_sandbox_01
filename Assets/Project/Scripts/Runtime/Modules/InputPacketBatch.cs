using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Runtime.Modules
{
    [System.Serializable]
    public class InputPacketBatch
    {
        public List<InputPacket> Packets = new List<InputPacket>();
    }
}
