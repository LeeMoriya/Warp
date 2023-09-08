using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Menu;

public class RegionSwitcher
{
    public ErrorKey error;
    public enum ErrorKey
    {
        LoadWorld,
        AbstractRoom,
        RealiseRoom,
        RoomRealiser,
        FindNode,
        MovePlayer,
        MoveObjects,
        MoveCamera
    }

    private MethodInfo _OverWorld_LoadWorld = typeof(OverWorld).GetMethod("LoadWorld", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    public void SwitchRegions(RainWorldGame game, string destWorld, string destRoom, IntVector2 destPos)
    {
        error = ErrorKey.LoadWorld;
        Debug.Log("WARP: Loading room " + destRoom + " from region " + destWorld + "!");

        for (int i = 0; i < game.AlivePlayers.Count; i++)
        {
            AbstractCreature absPly = game.AlivePlayers[i] as AbstractCreature;
            if (absPly != null)
            {
                Debug.Log("WARP: Initiating region warp.");
                AbstractRoom oldRoom = absPly.Room;

                try
                {
                    // Load the new world
                    Debug.Log("WARP: Invoking original LoadWorld method for new region.");
                    World oldWorld = game.overWorld.activeWorld;
                    _OverWorld_LoadWorld.Invoke(game.overWorld, new object[] { destWorld, game.overWorld.PlayerCharacterNumber, false });

                    // Move the player and held items to the new room
                    Debug.Log("WARP: Moving player to new region.");
                    WorldLoaded(game, oldRoom, oldWorld, destRoom, destPos);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.Log("WARP ERROR: " + this.GetErrorText(error));
                    WarpModMenu.warpError = this.GetErrorText(error);
                    game.pauseMenu = new PauseMenu(game.manager, game);
                    break;
                };
            }
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
        this.error = ErrorKey.AbstractRoom;
        World newWorld = game.overWorld.activeWorld;
        AbstractRoom newRoom = newWorld.GetAbstractRoom(newRoomName);
        this.error = ErrorKey.RealiseRoom;
        newRoom.RealizeRoom(newWorld, game);

        this.error = ErrorKey.RoomRealiser;
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

        this.error = ErrorKey.FindNode;
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

        game.cameras[0].virtualMicrophone.AllQuiet();
        for (int i = 0; i < game.cameras[0].hud.fContainers.Length; i++)
        {
            game.cameras[0].hud.fContainers[i].RemoveAllChildren();
        }
        game.cameras[0].hud = null;

        // Transfer entities between rooms
        List<Player> slugpups = new List<Player>();
        for (int j = 0; j < game.AlivePlayers.Count; j++)
        {
            this.error = ErrorKey.MovePlayer;
            AbstractCreature ply = game.AlivePlayers[j];
            if (ply.realizedCreature.grasps != null)
            {
                for (int g = 0; g < ply.realizedCreature.grasps.Length; g++)
                {
                    //If it's a creature, let it go
                    if (ply.realizedCreature.grasps[g] != null && ply.realizedCreature.grasps[g].grabbed != null && !ply.realizedCreature.grasps[g].discontinued && (ply.realizedCreature.grasps[g].grabbed is Creature))
                    {
                        if (!(ply.realizedCreature.grasps[g].grabbed is Player && (ply.realizedCreature.grasps[g].grabbed as Player).isSlugpup))
                        {
                            ply.realizedCreature.ReleaseGrasp(g);
                        }
                    }
                }
            }
            ply.world = newWorld;
            ply.pos.room = newRoom.index;
            ply.pos.abstractNode = abstractNode;
            ply.pos.x = newPos.x;
            ply.pos.y = newPos.y;

            if(WarpModMenu.coords != new IntVector2(-1, -1))
            {
                ply.pos.x = WarpModMenu.coords.x;
                ply.pos.y = WarpModMenu.coords.y;
            }

            if (j == 0)
            {
                newRoom.realizedRoom.aimap.NewWorld(newRoom.index);
            }

            if (ply.realizedObject is Player)
            {
                (ply.realizedObject as Player).enteringShortCut = null;
            }

            //Transfer connected objects to new world/room
            List<AbstractPhysicalObject> objs = ply.GetAllConnectedObjects();
            for (int i = 0; i < objs.Count; i++)
            {
                objs[i].world = newWorld;
                objs[i].pos = ply.pos;
                objs[i].Room.RemoveEntity(objs[i]);
                newRoom.AddEntity(objs[i]);
                objs[i].realizedObject.sticksRespawned = true;
            }

            Spear hasSpear = null;
            AbstractPhysicalObject stomachObject = null;
            // Move players' stomach objects
            if (ply.realizedCreature != null && (ply.realizedCreature as Player).objectInStomach != null)
            {
                (ply.realizedCreature as Player).objectInStomach.world = newWorld;
                stomachObject = (ply.realizedCreature as Player).objectInStomach;
            }
            //Check for backspears
            if (ply.realizedCreature != null && (ply.realizedCreature as Player).spearOnBack != null)
            {
                if ((ply.realizedCreature as Player).spearOnBack.spear != null)
                {
                    hasSpear = (ply.realizedCreature as Player).spearOnBack.spear;
                }
            }

            //Move player to new room
            ply.timeSpentHere = 0;
            ply.distanceToMyNode = 0;
            oldRoom.realizedRoom.RemoveObject(ply.realizedCreature);
            ply.Move(WarpModMenu.coords != new IntVector2(-1,-1) ? new WorldCoordinate(newRoom.index, WarpModMenu.coords.x, WarpModMenu.coords.y, -1) : newRoom.realizedRoom.LocalCoordinateOfNode(0));

            if (ply.creatureTemplate.AI && ply.abstractAI.RealAI != null && ply.abstractAI.RealAI.pathFinder != null)
            {
                ply.abstractAI.SetDestination(QuickConnectivity.DefineNodeOfLocalCoordinate(ply.abstractAI.destination, ply.world, ply.creatureTemplate));
                ply.abstractAI.timeBuffer = 0;
                if (ply.abstractAI.destination.room == ply.pos.room && ply.abstractAI.destination.abstractNode == ply.pos.abstractNode)
                {
                    ply.abstractAI.path.Clear();
                }
                else
                {
                    List<WorldCoordinate> list = ply.abstractAI.RealAI.pathFinder.CreatePathForAbstractreature(ply.abstractAI.destination);
                    if (list != null)
                    {
                        ply.abstractAI.path = list;
                    }
                    else
                    {
                        ply.abstractAI.FindPath(ply.abstractAI.destination);
                    }
                }
                ply.abstractAI.RealAI = null;
            }
            ply.RealizeInRoom();

            //Remove duplicate objects in updateList
            if (j == 0)
            {
                for (int i = 0; i < objs.Count; i++)
                {
                    int num = 0;
                    for (int s = 0; s < newRoom.realizedRoom.updateList.Count; s++)
                    {
                        if (objs[i].realizedObject == newRoom.realizedRoom.updateList[s])
                        {
                            num++;
                        }
                        if (num > 1)
                        {
                            newRoom.realizedRoom.updateList.RemoveAt(s);
                        }
                    }
                }
            }

            this.error = ErrorKey.MoveObjects;
            //Re-add any backspears
            if (hasSpear != null && (ply.realizedCreature as Player).spearOnBack != null && (ply.realizedCreature as Player).spearOnBack.spear != hasSpear)
            {
                (ply.realizedCreature as Player).spearOnBack.SpearToBack(hasSpear);
                (ply.realizedCreature as Player).abstractPhysicalObject.stuckObjects.Add((ply.realizedCreature as Player).spearOnBack.abstractStick);
            }
            //Re-add any stomach objects
            if (stomachObject != null && (ply.realizedCreature as Player).objectInStomach == null)
            {
                (ply.realizedCreature as Player).objectInStomach = stomachObject;
            }

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
            if (j == 0)
                newRoom.world.game.roomRealizer.followCreature = ply;
            Debug.Log("Player " + j + " Moved to new Region");
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
        this.error = ErrorKey.MoveCamera;
        // Make sure the camera moves too
        game.cameras[0].MoveCamera(newRoom.realizedRoom, 0);
        //game.cameras[0].ApplyPositionChange();
        game.cameras[0].FireUpSinglePlayerHUD(game.AlivePlayers[0].realizedCreature as Player);

        // Move the camera
        for (int i = 0; i < game.cameras.Length; i++)
        {
            game.cameras[0].hud.ResetMap(new HUD.Map.MapData(newWorld, game.rainWorld));
            if (game.cameras[i].hud.textPrompt.subregionTracker != null)
            {
                game.cameras[i].hud.textPrompt.subregionTracker.lastShownRegion = 0;
            }
        }

        game.cameras[0].virtualMicrophone.NewRoom(game.cameras[0].room);

        // Adapt the region state to the new world
        oldWorld.regionState.AdaptRegionStateToWorld(-1, newRoom.index);
        oldWorld.regionState.world = null;
        newWorld.rainCycle.cycleLength = oldWorld.rainCycle.cycleLength;
        newWorld.rainCycle.timer = oldWorld.rainCycle.timer;

        WarpModMenu.coords = new IntVector2(-1, -1);
    }

    public string GetErrorText(ErrorKey key)
    {
        switch (key)
        {
            case ErrorKey.LoadWorld:
                {
                    return "An error occurred while loading the new region, check your room connections";
                }
            case ErrorKey.AbstractRoom:
                {
                    return "An error occurred while loading the destination AbstractRoom";
                }
            case ErrorKey.RealiseRoom:
                {
                    return "An error occurred while realising the destination room";
                }
            case ErrorKey.RoomRealiser:
                {
                    return "An error occurred while loading rooms in the new region";
                }
            case ErrorKey.FindNode:
                {
                    return "An error occurred while finding a node to place the player";
                }
            case ErrorKey.MovePlayer:
                {
                    return "An error occurred while moving the player to the new region";
                }
            case ErrorKey.MoveObjects:
                {
                    return "An error occurred while moving the player's items";
                }
            case ErrorKey.MoveCamera:
                {
                    return "An error occurred while moving the RoomCamera to the new room";
                }
        }
        return "I have no idea how you got this error";
    }
}