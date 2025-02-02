using CG.Objects;
using CG.Ship.Hull;
using Gameplay.Carryables;
using Photon.Pun;
using VoidManager.ModMessages;

namespace FasterSwap
{
    internal class MessageHandler : ModMessage
    {
        //Only changed when functionality between host and client would break
        private const int version = 1;

        public override void Handle(object[] arguments, Photon.Realtime.Player sender)
        {
            //Only run this code on host
            if (!PhotonNetwork.IsMasterClient) return;

            //Message must at least include the message version
            if (arguments.Length < 1) return;

            int versionReceived;
            int socketViewId;
            int playerCarryableViewId;

            if (arguments[0] is not int) return;
            versionReceived = (int)arguments[0];

            if (version != versionReceived)
            {
                BepinPlugin.Log.LogInfo($"Got version {versionReceived} from {sender.NickName}, expected version {version}");
                return;
            }

            if (arguments.Length != 3) return;
            if (arguments[1] is not int || arguments[2] is not int) return;
            socketViewId = (int)arguments[1];
            playerCarryableViewId = (int)arguments[2];

            //Get the socket the player is looking at, and the item dropped by the player
            CarryablesSocket socket = PhotonView.Find(socketViewId).gameObject.GetComponent<CarryablesSocket>();
            CarryableObject playerCarryable = PhotonView.Find(playerCarryableViewId).gameObject.GetComponent<CarryableObject>();

            if (playerCarryable.AmOwner)
            {
                checkSocketEmpty();
            }
            else
            {
                //wait to be owner of player item
                playerCarryable.OwnerChange += waitForOwnerChange;
                void waitForOwnerChange(Photon.Realtime.Player newOwner)
                {
                    playerCarryable.OwnerChange -= waitForOwnerChange;
                    checkSocketEmpty();
                }
            }

            void checkSocketEmpty()
            {
                if (socket.Payload == null)
                {
                    playerItemToSocket();
                }
                else
                {
                    //wait for socket to be empty
                    socket.OnRemoveCarryable += waitForEmptySocket;
                    void waitForEmptySocket(ICarrier _, CarryableObject __)
                    {
                        socket.OnRemoveCarryable -= waitForEmptySocket;
                        playerItemToSocket();
                    }
                }
            }

            void playerItemToSocket()
            {
                socket.TryInsertCarryable(playerCarryable);
            }
        }

        public static void SendSwapRequest(int slotItemViewId, int playerItemViewId)
        {
            Send(MyPluginInfo.PLUGIN_GUID, GetIdentifier(typeof(MessageHandler)), PhotonNetwork.MasterClient, new object[] { version, slotItemViewId, playerItemViewId });
        }
    }
}
