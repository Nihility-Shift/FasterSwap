using CG.Client.Player.Interactions;
using CG.Client.Ship.Interactions;
using CG.Game.Player;
using CG.Ship.Hull;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using VoidManager.Utilities;

namespace FasterSwap
{
    [HarmonyPatch(typeof(CarryableInteract), "StartInteraction")]
    internal class CarryableInteractPatch
    {
        //Bypass the vanilla item swapping code and use CarryableInteractPatch.HandleSwap instead
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //if (socketInteractable.SocketActor.Socket.DoesAccept(this.player.Carrier.Payload))
            List<CodeInstruction> targetSequence = new()
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Ldfld),
                new CodeInstruction(OpCodes.Callvirt),
                new CodeInstruction(OpCodes.Callvirt)
            };

            //HandleSwap(DoesAccept(), interactable, this)
            List<CodeInstruction> patchSequence = new()
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CarryableInteractPatch), nameof(HandleSwap)))
            };

            return HarmonyHelpers.PatchBySequence(instructions, targetSequence, patchSequence, HarmonyHelpers.PatchMode.AFTER, HarmonyHelpers.CheckMode.NEVER);
        }

        public static bool HandleSwap(bool canSwap, AbstractInteractable interactable, CarryableInteract instance)
        {
            if (!VoidManagerPlugin.Enabled) return canSwap;
            if (!canSwap) return false;

            CarryablesSocket socket = (interactable as SocketInteractable).SocketActor.Socket;
            int playerItemViewId = LocalPlayer.Instance.Payload.photonView.ViewID;

            //Send swap message to host
            MessageHandler.SendSwapRequest(socket.photonView.ViewID, playerItemViewId);

            //Drop player item and transfer ownership to host
            var payload = LocalPlayer.Instance.Payload;
            LocalPlayer.Instance.Carrier.EjectCarryable(LocalPlayer.Instance.transform.rotation*Vector3.forward*3);
            payload.photonView.TransferOwnership(PhotonNetwork.MasterClient);

            //Pick up slot item
            Grabbable grabable = socket.Payload.GetComponent<Grabbable>();
            LocalPlayer.Instance.StartCoroutine(instance.DelayedPickupGrabable(grabable));

            return false;
        }
    }
}
