using Serilog;
using System.Runtime.CompilerServices;

namespace Backend
{
    public class Lobby
    {
        private static Lobby _instance = new Lobby();
        private static readonly object _lock = new object();
        private static List<Client> clients = new List<Client>();
        private static List<Room> rooms = new List<Room>();

        private Lobby() { }

        public static Lobby Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance;
                }
            }
        }

        public static void AddClient(Client client)
        {
            Log.Debug($"Client {client.ID} joined the lobby!");
            clients.Add(client);
        }

        public static void RemoveClient(Client client)
        {
            try
            {
                Log.Debug($"Client {client.ID} left the lobby!");
                clients.Remove(client);
            }
            catch
            {

            }
        }

        public static void MatchClient(Client client)
        {
            Log.Debug($"Start matching for {client.ID}");
            foreach (var opp in clients)
            {
                if ((opp != client) && !client.wasOpp(opp) && opp.Ready)
                {
                    opp.Ready = false;
                    client.Ready = false;

                    var newRoom = new Room();
                    newRoom.AddClientToRoom(client);
                    newRoom.AddClientToRoom(opp);

                    client.curRoom = newRoom;
                    opp.curRoom = newRoom;

                    client.previousOpps.Add(opp);
                    opp.previousOpps.Add(client);


                    rooms.Add(newRoom);
                    break;
                }
            }
            Log.Debug($"Currently no match for {client.ID}");
            Log.Debug($"cur room count: {rooms.Count}");
        }

        public static void DeleteRoom(Room room)
        {

            Log.Debug("Deleted room after people got removed");
            rooms.Remove(room);
        }

        public static Room GetRoomFromClient(Client client)
        {
            Log.Debug($"getroom cur room count: {Lobby.rooms.Count}");
            foreach (var room in Lobby.rooms)
            {
                if (room.clients.Contains(client))
                {
                    return room;

                }
            }
            
            return null;
        }
    }
}
