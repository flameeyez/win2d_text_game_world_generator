using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class ProtoCave
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public List<ProtoRoom> ProtoRooms = new List<ProtoRoom>();

        public ProtoCave(int id, ProtoRoom[,] MasterRoomList)
        {
            ID = id;
            Name = Statics.RandomCaveName();
            Color = Statics.RandomColor();

            AddRooms(MasterRoomList);
            ConnectRooms(MasterRoomList);
        }

        private void ConnectRooms(ProtoRoom[,] MasterRoomList)
        {
            //List<ProtoRoom> MainPath = new List<ProtoRoom>();
            //while (MainPath.Count < ProtoRooms.Count)
            //{
            foreach (ProtoRoom pr in ProtoRooms)
            {

                for (int i = 0; i < 10; i++)
                {
                    if (pr.DirectionalRoomConnections.Count == Statics.RoomMaxConnections) { break; }

                    switch (Statics.Random.Next(8))
                    {
                        case 0: AddRoomConnection(pr, "nw", MasterRoomList); break;
                        case 1: AddRoomConnection(pr, "n", MasterRoomList); break;
                        case 2: AddRoomConnection(pr, "ne", MasterRoomList); break;
                        case 3: AddRoomConnection(pr, "w", MasterRoomList); break;
                        case 4: AddRoomConnection(pr, "e", MasterRoomList); break;
                        case 5: AddRoomConnection(pr, "sw", MasterRoomList); break;
                        case 6: AddRoomConnection(pr, "s", MasterRoomList); break;
                        case 7: AddRoomConnection(pr, "se", MasterRoomList); break;
                        default: break;
                    }
                }
            }
            //}
        }

        private void AddRooms(ProtoRoom[,] MasterRoomList)
        {
            int RoomCountX = MasterRoomList.GetLength(0);
            int RoomCountY = MasterRoomList.GetLength(1);

            // grab random point as starting room
            int x = Statics.Random.Next(RoomCountX);
            int y = Statics.Random.Next(RoomCountY);
            while (!MasterRoomList[x, y].AvailableUnderground)
            {
                x = Statics.Random.Next(RoomCountX);
                y = Statics.Random.Next(RoomCountY);
            }

            // keep track of available adjacent roomss
            // list for random access; hashset for lookup by coordinates
            List<ProtoRoom> AvailableAdjacentRooms = new List<ProtoRoom>();
            HashSet<PointInt> AvailableAdjacentCoordinates = new HashSet<PointInt>();

            // create starting room
            ProtoRoom startingRoom = MasterRoomList[x, y];
            ProtoRooms.Add(startingRoom);
            startingRoom.AvailableUnderground = false;
            CheckAdjacentRooms(AvailableAdjacentRooms, AvailableAdjacentCoordinates, startingRoom, MasterRoomList);

            while (AvailableAdjacentRooms.Count > 0 && (ProtoRooms.Count < Statics.MinimumCaveSize || Statics.Random.Next(100) < 99))
            {
                // pick a random room from the available set
                ProtoRoom randomNeighbor = AvailableAdjacentRooms.RandomListItem();
                randomNeighbor.AvailableUnderground = false;

                AvailableAdjacentRooms.Remove(randomNeighbor);
                AvailableAdjacentCoordinates.Remove(randomNeighbor.Coordinates);
                ProtoRooms.Add(randomNeighbor);

                // add new room's available neighbors to the available list
                CheckAdjacentRooms(AvailableAdjacentRooms, AvailableAdjacentCoordinates, randomNeighbor, MasterRoomList);
            }
        }

        private void CheckAdjacentRooms(List<ProtoRoom> AvailableAdjacentRooms, HashSet<PointInt> AvailableAdjacentCoordinates, ProtoRoom protoRoom, ProtoRoom[,] MasterRoomList)
        {
            // left
            if (protoRoom.Coordinates.X > 0)
            {
                ProtoRoom neighborLeft = MasterRoomList[protoRoom.Coordinates.X - 1, protoRoom.Coordinates.Y];
                if (neighborLeft.AvailableUnderground && !AvailableAdjacentCoordinates.Contains(neighborLeft.Coordinates))
                {
                    AvailableAdjacentRooms.Add(neighborLeft);
                    AvailableAdjacentCoordinates.Add(neighborLeft.Coordinates);
                }
            }
            // right
            if (protoRoom.Coordinates.X < MasterRoomList.GetLength(0) - 1)
            {
                ProtoRoom neighborRight = MasterRoomList[protoRoom.Coordinates.X + 1, protoRoom.Coordinates.Y];
                if (neighborRight.AvailableUnderground && !AvailableAdjacentCoordinates.Contains(neighborRight.Coordinates))
                {
                    AvailableAdjacentRooms.Add(neighborRight);
                    AvailableAdjacentCoordinates.Add(neighborRight.Coordinates);
                }
            }
            // above
            if (protoRoom.Coordinates.Y > 0)
            {
                ProtoRoom neighborAbove = MasterRoomList[protoRoom.Coordinates.X, protoRoom.Coordinates.Y - 1];
                if (neighborAbove.AvailableUnderground && !AvailableAdjacentCoordinates.Contains(neighborAbove.Coordinates))
                {
                    AvailableAdjacentRooms.Add(neighborAbove);
                    AvailableAdjacentCoordinates.Add(neighborAbove.Coordinates);
                }
            }
            // below
            if (protoRoom.Coordinates.Y < MasterRoomList.GetLength(1) - 1)
            {
                ProtoRoom neighborBelow = MasterRoomList[protoRoom.Coordinates.X, protoRoom.Coordinates.Y + 1];
                if (neighborBelow.AvailableUnderground && !AvailableAdjacentCoordinates.Contains(neighborBelow.Coordinates))
                {
                    AvailableAdjacentRooms.Add(neighborBelow);
                    AvailableAdjacentCoordinates.Add(neighborBelow.Coordinates);
                }
            }
        }

        private ProtoRoom AddRoomConnection(ProtoRoom currentRoom, string strDirection, ProtoRoom[,] MasterRoomList, bool bForce = false)
        {
            if (currentRoom.DirectionalRoomConnections.Contains(strDirection)) { return currentRoom; }

            string strOppositeDirection = string.Empty;
            PointInt connectingRoomCoordinates = null;
            switch (strDirection)
            {
                case "nw":
                    strOppositeDirection = "se";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X - 1, currentRoom.Coordinates.Y - 1);
                    break;
                case "n":
                    strOppositeDirection = "s";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X, currentRoom.Coordinates.Y - 1);
                    break;
                case "ne":
                    strOppositeDirection = "sw";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X + 1, currentRoom.Coordinates.Y - 1);
                    break;
                case "w":
                    strOppositeDirection = "e";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X - 1, currentRoom.Coordinates.Y);
                    break;
                case "e":
                    strOppositeDirection = "w";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X + 1, currentRoom.Coordinates.Y);
                    break;
                case "sw":
                    strOppositeDirection = "ne";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X - 1, currentRoom.Coordinates.Y + 1);
                    break;
                case "s":
                    strOppositeDirection = "n";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X, currentRoom.Coordinates.Y + 1);
                    break;
                case "se":
                    strOppositeDirection = "nw";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X + 1, currentRoom.Coordinates.Y + 1);
                    break;
                default:
                    throw new Exception();
            }

            // abort if coordinates are outside of the array
            if (connectingRoomCoordinates.X < 0) { return currentRoom; }
            if (connectingRoomCoordinates.X > MasterRoomList.GetLength(0) - 1) { return currentRoom; }
            if (connectingRoomCoordinates.Y < 0) { return currentRoom; }
            if (connectingRoomCoordinates.Y > MasterRoomList.GetLength(1) - 1) { return currentRoom; }

            // abort if not allocated as underground room
            if (MasterRoomList[connectingRoomCoordinates.X, connectingRoomCoordinates.Y].AvailableUnderground) { return currentRoom; }

            ProtoRoom connectingRoom = MasterRoomList[connectingRoomCoordinates.X, connectingRoomCoordinates.Y];

            // abort if too many connections
            if (connectingRoom.DirectionalRoomConnections.Count >= Statics.RoomMaxConnections && !bForce) { return currentRoom; }

            // add connections to current room and connecting room
            currentRoom.DirectionalRoomConnections.Add(strDirection);
            connectingRoom.DirectionalRoomConnections.Add(strOppositeDirection);

            return connectingRoom;
        }

        internal void Draw(CanvasDrawingSession ds)
        {
            foreach (ProtoRoom pr in ProtoRooms)
            {
                pr.DrawCaves(ds);
            }
        }
    }
}
