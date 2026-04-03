using System.IO;

namespace PvPAdventure.Core.Net;

public interface IPacket<out TSelf>
{
    static abstract TSelf Deserialize(BinaryReader reader);
    void Serialize(BinaryWriter writer);
}