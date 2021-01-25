using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Partiality;
using Partiality.Modloader;

public class RegionSwitcher
{
    private MethodInfo _OverWorld_LoadWorld = typeof(OverWorld).GetMethod("LoadWorld", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    public void SwitchRegions(RainWorldGame game, string destWorld, string destRoom, IntVector2 destPos)
    {
        Debug.Log("WARP: Loading room " + destRoom + " from region " + destWorld + "!");
        AbstractCreature absPly = (game.Players.Count <= 0) ? null : (game.Players[0] as AbstractCreature);
        if (absPly != null)
        {
            Debug.Log("WARP: Initiating region warp.");
            AbstractRoom oldRoom = absPly.Room;
            if (WarpMod.customRegions)
            {
                if (Type.GetType("CustomRegions.OverWorldHook, CustomRegions") != null)
                {
                    Type.GetType("CustomRegions.OverWorldHook, CustomRegions").GetField("textLoadWorld", BindingFlags.Public | BindingFlags.Static).SetValue(null, destWorld);
                }
                else
                {
                    if (Type.GetType("CustomRegions.CWorld.OverWorldHook, CustomRegionsSupport") != null)
                    {
                        Type.GetType("CustomRegions.CWorld.OverWorldHook, CustomRegionsSupport").GetField("textLoadWorld", BindingFlags.Public | BindingFlags.Static).SetValue(null, destWorld);
                    }
                }
            }

            // Load the new world
            Debug.Log("WARP: Invoking original LoadWorld method for new region.");
            World oldWorld = game.overWorld.activeWorld;
            _OverWorld_LoadWorld.Invoke(game.overWorld, new object[] { destWorld, game.overWorld.PlayerCharacterNumber, false });

            // Move the player and held items to the new room
            Debug.Log("WARP: Moving player to new region.");
            WorldLoaded(game, oldRoom, oldWorld, destRoom, destPos);
        }
        else
        {
            Debug.Log("WARP: Cannot initiate region warp, player is null!");
        }
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
        // Realize the new room
        World newWorld = game.overWorld.activeWorld;
        AbstractRoom newRoom = newWorld.GetAbstractRoom(newRoomName);
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

        //Jolly Fix Attempt
        if (WarpMod.jollyCoop)
        {
            for (int i = 0; i < game.cameras[0].hud.fContainers.Length; i++)
            {
                game.cameras[0].hud.fContainers[i].RemoveAllChildren();
            }
            game.cameras[0].hud = null;
        }


        // Make sure the camera moves too
        game.cameras[0].MoveCamera(newRoom.realizedRoom, 0);

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
            ply.realizedCreature.PlaceInRoom(newRoom.realizedRoom);
            ply.ChangeRooms(newRoom.realizedRoom.LocalCoordinateOfNode(0));

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
            newRoom.world.game.roomRealizer.followCreature = ply;
            Debug.Log("Player " + j + " Moved to new Region");
        }

        // Move players' stomach objects
        for (int i = 0; i < game.Players.Count; i++)
        {
            if (game.Players[i].realizedCreature != null && (game.Players[i].realizedCreature as Player).objectInStomach != null)
            {
                (game.Players[i].realizedCreature as Player).objectInStomach.world = newWorld;
            }
            if (game.Players[i].realizedCreature != null && (game.Players[i].realizedCreature as Player).spearOnBack != null)
            {
                if((game.Players[i].realizedCreature as Player).spearOnBack.spear != null)
                {
                    (game.Players[i].realizedCreature as Player).spearOnBack.spear.abstractPhysicalObject.world = newWorld;
                    (game.Players[i].realizedCreature as Player).spearOnBack.spear.PlaceInRoom(newRoom.realizedRoom);
                }
            }
            if (game.Players[i].realizedCreature.grasps != null)
            {
                for (int g = 0; g < game.Players[i].realizedCreature.grasps.Length; g++)
                {
                    if (game.Players[i].realizedCreature.grasps[g] != null && game.Players[i].realizedCreature.grasps[g].grabbed != null && !game.Players[i].realizedCreature.grasps[g].discontinued && game.Players[i].realizedCreature.grasps[g].grabbed is Creature)
                    {
                        game.Players[i].realizedCreature.ReleaseGrasp(g);
                    }
                }
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

        //Move Slugcat to the first room exit
        for (int p = 0; p < game.Players.Count; p++)
        {
            AbstractCreature ply = game.Players[p];
            for (int i = 0; i < ply.realizedCreature.bodyChunks.Length; i++)
            {
                ply.realizedCreature.bodyChunks[i].pos = new Vector2((float)ply.realizedCreature.room.LocalCoordinateOfNode(0).x * 20f, (float)ply.realizedCreature.room.LocalCoordinateOfNode(0).y * 20f);
                ply.realizedCreature.bodyChunks[i].lastPos = new Vector2((float)ply.realizedCreature.room.LocalCoordinateOfNode(0).x * 20f, (float)ply.realizedCreature.room.LocalCoordinateOfNode(0).y * 20f);
                ply.realizedCreature.bodyChunks[i].lastLastPos = new Vector2((float)ply.realizedCreature.room.LocalCoordinateOfNode(0).x * 20f, (float)ply.realizedCreature.room.LocalCoordinateOfNode(0).y * 20f);
                ply.realizedCreature.bodyChunks[i].vel = new Vector2();
            }
        }
        if (WarpMod.jollyCoop)
        {
            game.cameras[0].FireUpSinglePlayerHUD(game.Players[0].realizedCreature as Player);
        }
        // Move the camera
        for (int i = 0; i < game.cameras.Length; i++)
        {
            game.cameras[i].hud.ResetMap(new HUD.Map.MapData(newWorld, game.rainWorld));
            if (game.cameras[i].hud.textPrompt.subregionTracker != null)
            {
                game.cameras[i].hud.textPrompt.subregionTracker.lastShownRegion = 0;
            }
        }

        game.cameras[0].virtualMicrophone.AllQuiet();

        // Adapt the region state to the new world
        oldWorld.regionState.AdaptRegionStateToWorld(-1, newRoom.index);
        oldWorld.regionState.world = null;
        newWorld.rainCycle.cycleLength = oldWorld.rainCycle.cycleLength;
        newWorld.rainCycle.timer = oldWorld.rainCycle.timer;
    }
}