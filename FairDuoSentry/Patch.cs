using System.Timers;
using CG.Game;
using HarmonyLib;
using CG.Ship.Hull;
using CG.Objects;
using CG.Space;
using Gameplay.Utilities;

namespace FairDuoSentry
{
    [HarmonyPatch(typeof(CG.Ship.Hull.HomunculusAndBiomassSocket), nameof(CG.Ship.Hull.HomunculusAndBiomassSocket.DispenseHomunculusNow))]
    internal class HomunculusDispensePatch
    {
        // Fix the Postfix signature: it should take the instance of the patched class
        // and not be a 'ref bool __instance'.
        static void Postfix(HomunculusAndBiomassSocket __instance)
        {
            // Only log if we are the MasterClient (host) to avoid duplicate logs in multiplayer
            // if (!PhotonNetwork.IsMasterClient) { return; } // Uncomment this line if you only want host logs

            if (__instance.Payload == null)
            {
                BepinPlugin.Log.LogWarning("HomunculusDispenseNow: Payload is null after dispense. Cannot detect Homunculus info.");
                return;
            }

            // log the current ship's asset GUID and player count
            // ClientGame.Current.playerShip is lowercase 'p' in your code, assuming it's a property.
            // If it's `ClientGame.Current.PlayerShip` (capital P as seen in OrbitObject.cs), adjust accordingly.
            if (ClientGame.Current.PlayerShip?.ContainerGuid != null)
            {
                BepinPlugin.Log.LogInfo($"Current Ship Asset GUID: {ClientGame.Current.PlayerShip?.ContainerGuid}"); // Use null conditional operator for safety
            }
            
            BepinPlugin.Log.LogInfo($"Current Player Count: {RoomPlayersTracker.Instance.Players.Count}"); // Use RoomPlayersTracker

            // Get the dispensed CarryableObject, which is the Homunculus
            CarryableObject homunculusCarryable = __instance.Payload;
            BepinPlugin.Log.LogInfo($"--- Homunculus Dispensed Detected ---");
            BepinPlugin.Log.LogInfo($"Homunculus GameObject Name: {homunculusCarryable.gameObject.name}");
            // BepinPlugin.Log.LogInfo($"Homunculus Type (Asset GUID): {__instance.homunculusCriteria.AssetGuid}"); // Logs the GUID from the socket's criteria

            // Attempt to get the OrbitObject component on the Homunculus
            OrbitObject homunculusOrbitObject = homunculusCarryable.GetComponent<OrbitObject>();
            if (homunculusOrbitObject != null)
            {
                BepinPlugin.Log.LogInfo($"Homunculus is an OrbitObject. Display Name: {homunculusOrbitObject.DisplayName}");
                BepinPlugin.Log.LogInfo($"Homunculus Faction: {homunculusOrbitObject.Faction}");

                // Access and log its StatMods
                // StatTagCollection has a StatCollection named 'Stats' (lowercase s in IStatCollectionHolder)
                // and StatCollection itself has a dictionary of StatBase named 'Stats' (uppercase S)
                StatCollection homunculusStats = homunculusOrbitObject.Stats.Stats; 
                if (homunculusStats != null)
                {
                    BepinPlugin.Log.LogInfo($"--- Homunculus Active Modifiers ({homunculusStats.ActiveModifiers.Count}) ---");
                    if (homunculusStats.ActiveModifiers.Count == 0)
                    {
                        BepinPlugin.Log.LogInfo("No active StatMods found directly on Homunculus's StatCollection.");
                    }
                    else
                    {
                        foreach (StatMod mod in homunculusStats.ActiveModifiers)
                        {
                            string statName = StatType.GetNameById(mod.Type, false); // Get name, don't throw if not found
                            if (string.IsNullOrEmpty(statName)) statName = $"UnknownStat({mod.Type})";

                            // Accessing .Value from PrimitiveModifier (ensure PrimitiveModifier.cs is correctly defined or referenced)
                            string modValue = mod.Mod != null ? mod.Mod.Value.ToString() : "N/A";
                            string modType = mod.Mod != null ? mod.Mod.Type.ToString() : "N/A";
                            string dynamicStatus = mod.IsDynamic ? " (Dynamic)" : "";

                            BepinPlugin.Log.LogInfo($"- Stat: {statName}, Value: {modValue}, Type: {modType}{dynamicStatus}");
                        }
                    }

                    // Also log base stats if desired (from StatCollection.Stats dictionary)
                    BepinPlugin.Log.LogInfo($"--- Homunculus Base Stats (Registered) ---");
                    foreach(var kvp in homunculusStats.Stats) // This refers to the Dictionary<int, StatBase> Stats in StatCollection
                    {
                        string statName = StatType.GetNameById(kvp.Key, false);
                        if (string.IsNullOrEmpty(statName)) statName = $"UnknownStat({kvp.Key})";
                        
                        string baseValue = "N/A";
                        if (kvp.Value.Data is ModifiableFloat floatData)
                            baseValue = floatData.BaseValue.ToString();
                        else if (kvp.Value.Data is ModifiableInt intData)
                            baseValue = intData.BaseValue.ToString();

                        BepinPlugin.Log.LogInfo($"- Stat: {statName}, Base Value: {baseValue}");
                    }
                }
                else
                {
                    BepinPlugin.Log.LogInfo("Homunculus OrbitObject does not have a StatCollection.");
                }
            }
            else // This else block was for if (homunculusOrbitObject != null)
            {
                BepinPlugin.Log.LogWarning("Dispensed Homunculus is not an OrbitObject.");
            }
            BepinPlugin.Log.LogInfo($"-----------------------------------");
        }
    }
}