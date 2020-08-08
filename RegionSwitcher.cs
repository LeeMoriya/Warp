using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public class RegionSwitcher
{
    private MethodInfo _OverWorld_LoadWorld = typeof(OverWorld).GetMethod("LoadWorld", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    public void SwitchRegions(RainWorldGame game, string destWorld, string destRoom, IntVector2 destPos)
    {
        Debug.Log("Loading room " + destRoom + " from region " + destWorld + "!");
        AbstractCreature absPly = game.Players[0];
        AbstractRoom oldRoom = absPly.Room;
        Player ply = absPly.realizedCreature as Player;
        Type.GetType("CustomRegions.OverWorldHook, CustomRegions").GetField("textLoadWorld", BindingFlags.Public | BindingFlags.Static).SetValue(null, destWorld);

        // Load the new world
        World oldWorld = game.overWorld.activeWorld;
        _OverWorld_LoadWorld.Invoke(game.overWorld, new object[] { destWorld, game.overWorld.PlayerCharacterNumber, false });

        // Move the player and held items to the new room
        WorldLoaded(game, oldRoom, oldWorld, destRoom, destPos);
    }

    public AbstractRoom GetFirstRoom(AbstractRoom[] abstractRooms, string regionName)
    {
        for (int i = 0; i < abstractRooms.Length; i++)
        {
            if (abstractRooms[i].name.StartsWith(regionName))
            {
                return abstractRooms[i];
            }
        }
        return null;
    }

    // Taken from OverWorld.WorldLoaded
    private void WorldLoaded(RainWorldGame game, AbstractRoom oldRoom, World oldWorld, string newRoomName, IntVector2 newPos)
    {
        // Removed realized room transferrance code

        // Realize the new room
        World newWorld = game.overWorld.activeWorld;
        AbstractRoom newRoom = GetFirstRoom(newWorld.abstractRooms, newWorld.name);
        newRoom.RealizeRoom(newWorld, game);

        // Forcibly prepare all loaded rooms
        while (newWorld.loadingRooms.Count > 0)
        {
            for (int i = 0; i < 1; i++)
            {
                for (int j = newWorld.loadingRooms.Count - 1; j >= 0; j--)
                {
                    if (newWorld.loadingRooms[j].done)
                    {
                        newWorld.loadingRooms.RemoveAt(j);
                    }
                    else
                    {
                        newWorld.loadingRooms[j].Update();
                    }
                }
            }
        }

        if (game.roomRealizer != null)
        {
            game.roomRealizer = new RoomRealizer(game.roomRealizer.followCreature, newWorld);
        }
        game.overWorld.activeWorld = newWorld;

        // Find a suitable abstract node to place creatures at
        int abstractNode = 0;
        for (int i = 0; i < newRoom.nodes.Length; i++)
        {
            if (newRoom.nodes[i].type == AbstractRoomNode.Type.Exit && i < newRoom.connections.Length && newRoom.connections[i] > -1)
            {
                abstractNode = i;
                break;
            }
        }

        // Transfer entities between rooms
        for (int j = 0; j < game.Players.Count; j++)
        {
            AbstractCreature ply = game.Players[j];
            ply.world = newWorld;
            ply.pos.room = newRoom.index;
            ply.pos.abstractNode = abstractNode;
            ply.pos.x = newPos.x;
            ply.pos.y = newPos.y;
            newRoom.realizedRoom.aimap.NewWorld(newRoom.index);

            if (ply.realizedObject is Player realPly)
            {
                realPly.enteringShortCut = null;
            }
            ply.Move(ply.pos);
            ply.realizedObject.PlaceInRoom(newRoom.realizedRoom);

            if (ply is AbstractCreature && (ply as AbstractCreature).creatureTemplate.AI)
            {
                (ply as AbstractCreature).abstractAI.NewWorld(newWorld);
                (ply as AbstractCreature).InitiateAI();
                (ply as AbstractCreature).abstractAI.RealAI.NewRoom(newRoom.realizedRoom);
                if ((ply as AbstractCreature).creatureTemplate.type == CreatureTemplate.Type.Overseer && ((ply as AbstractCreature).abstractAI as OverseerAbstractAI).playerGuide)
                {
                    MethodInfo kpginw = typeof(OverWorld).GetMethod("KillPlayerGuideInNewWorld", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    kpginw.Invoke(game.overWorld, new object[] { newWorld, ply as AbstractCreature });
                }
            }
        }

        // Move players' stomach objects
        for (int i = 0; i < game.Players.Count; i++)
        {
            if (game.Players[i].realizedCreature != null && (game.Players[i].realizedCreature as Player).objectInStomach != null)
            {
                (game.Players[i].realizedCreature as Player).objectInStomach.world = newWorld;
            }
        }


        // Cut transport vessels from the old region
        for (int i = game.shortcuts.transportVessels.Count - 1; i >= 0; i--)
        {
            if (!game.overWorld.activeWorld.region.IsRoomInRegion(game.shortcuts.transportVessels[i].room.index))
            {
                game.shortcuts.transportVessels.RemoveAt(i);
            }
        }
        for (int i = game.shortcuts.betweenRoomsWaitingLobby.Count - 1; i >= 0; i--)
        {
            if (!game.overWorld.activeWorld.region.IsRoomInRegion(game.shortcuts.betweenRoomsWaitingLobby[i].room.index))
            {
                game.shortcuts.betweenRoomsWaitingLobby.RemoveAt(i);
            }
        }
        for (int i = game.shortcuts.borderTravelVessels.Count - 1; i >= 0; i--)
        {
            if (!game.overWorld.activeWorld.region.IsRoomInRegion(game.shortcuts.borderTravelVessels[i].room.index))
            {
                game.shortcuts.borderTravelVessels.RemoveAt(i);
            }
        }

        //game.overWorld.worldLoader = null;

        // Move the camera
        for (int i = 0; i < game.cameras.Length; i++)
        {
            game.cameras[i].hud.ResetMap(new HUD.Map.MapData(newWorld, game.rainWorld));
            if (game.cameras[i].hud.textPrompt.subregionTracker != null)
            {
                game.cameras[i].hud.textPrompt.subregionTracker.lastShownRegion = 0;
            }
        }

        // Adapt the region state to the new world
        oldWorld.regionState.AdaptRegionStateToWorld(-1, newRoom.index);
        oldWorld.regionState.world = null;
        newWorld.rainCycle.cycleLength = oldWorld.rainCycle.cycleLength;
        newWorld.rainCycle.timer = oldWorld.rainCycle.timer;

        // Make sure the camera moves too
        game.cameras[0].MoveCamera(newRoom.realizedRoom, 0);
    }
}