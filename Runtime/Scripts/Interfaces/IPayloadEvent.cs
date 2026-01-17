using Unity.Netcode;

namespace GreedyVox.NetCode.Interfaces
{
    /// <summary>
    /// Defines a bidirectional payload contract for networked objects.
    /// Implementations are responsible for both producing authoritative
    /// payload data and consuming received payload data.
    /// </summary>
    public interface IPayloadEvent
    {
        /// <summary>
        /// Consumes and applies payload data received from the network.
        /// Implementations should validate the payload type and
        /// initialize local state accordingly.
        /// </summary>
        /// <param name="data">
        /// The network-serializable payload data to apply.
        /// </param>
        /// <returns>
        /// <c>true</c> if the payload was successfully applied;
        /// otherwise, <c>false</c>.
        /// </returns>
        bool Payload(INetworkSerializable data);

        /// <summary>
        /// Produces the authoritative payload data for this object.
        /// Called by the networking layer to serialize and transmit
        /// initialization or state data to remote instances.
        /// </summary>
        /// <param name="data">
        /// When this method returns, contains the generated
        /// network-serializable payload data.
        /// </param>
        /// <returns>
        /// <c>true</c> if the payload was successfully generated;
        /// otherwise, <c>false</c>.
        /// </returns>
        bool Payload(out INetworkSerializable data);
    }
}
